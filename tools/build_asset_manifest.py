#!/usr/bin/env python3
"""从工程内已迁移的资源生成机器可读对照表（不读 APK）。

数据来源（全部在 Man/Client/Assets 下）：
    Objects/SAF/**/*.saf               动画帧与图层（权威数据）
    Resources/Prefab/**/*.prefab       动画 prefab（GameAnimation）
    Resources/Texture/**/*.png         精灵贴图（扁平命名 <动画目录>/<基名>_t<ID>.png）
    Resources/Sound/*.wav              音效
    Resources/Music/*.wav              音乐
    Resources/Movie/*                  视频
    Objects/DAT/Misc/man_base.dat      单位表（→ 动画 prefab）
    Objects/DAT/Misc/Mag_dat.dat       技能表（→ 特效 prefab）

输出（默认 AssetManifest/）：
    manifest.json            结构化：动画 → 帧 → 图层
    manifest_animations.csv  每个动画一行（含完整性统计）
    manifest_frames.csv      每帧一行
    manifest_layers.csv      每层一行（主表）
    manifest_assets.csv      贴图 / 音效 / 音乐 / 视频清单
    manifest_units.csv       单位 → 动画 prefab
    manifest_skills.csv      技能 → 特效 prefab
    manifest_check.csv       逐动画的对照结论
    summary.json             全局统计与问题汇总

用法：
    python3 tools/build_asset_manifest.py
    python3 tools/build_asset_manifest.py --out /tmp/mf --anim Stage/Stage00
"""

import argparse
import collections
import csv
import datetime
import json
import os
import re
import struct
import sys

REPO = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
ASSETS = os.path.join(REPO, "Man/Client/Assets")

SAF_DIR = os.path.join(ASSETS, "Objects/SAF")
PREFAB_DIR = os.path.join(ASSETS, "Resources/Prefab")
TEX_DIR = os.path.join(ASSETS, "Resources/Texture")
SND_DIR = os.path.join(ASSETS, "Resources/Sound")
MUS_DIR = os.path.join(ASSETS, "Resources/Music")
MOV_DIR = os.path.join(ASSETS, "Resources/Movie")
DAT_DIR = os.path.join(ASSETS, "Objects/DAT/Misc")

COUNT3_NAMES = {0: "Stand", 1: "Move", 2: "MoveStart", 3: "MoveEnd", 4: "Win", 5: "Skill"}
DIRECTION_NAMES = {0: "South", 1: "West", 2: "North", 3: "East"}
LAYER_COLUMNS = ["textureId", "x", "y", "type", "unknow8", "unknow10", "unknow12",
                 "external", "otherFrame", "sprite", "pngPath", "gamePath", "material"]
EXTERNAL_BIT = 0x8000   # textureID >= 0x8000 表示取 otherGameAnimation 的图


def log(*a):
    print(*a, flush=True)


def parse_saf(path):
    """按 GameAnimation.load() 的字段顺序解析 SAF。"""
    b = open(path, "rb").read()
    i = 0
    count1, count2 = struct.unpack_from("<hh", b, i)
    i += 4
    count3 = list(struct.unpack_from("<32h", b, i))
    i += 64
    frames = []
    for _ in range(count1):
        sound, time = struct.unpack_from("<hh", b, i)
        i += 4
        hit, color, shake, u4 = b[i], b[i + 1], b[i + 2], b[i + 3]
        i += 4
        cnt = struct.unpack_from("<h", b, i)[0]
        i += 2
        layers = []
        for _ in range(cnt):
            tid, x, y, typ, u8, u10, u12 = struct.unpack_from("<HhhBhhh", b, i)
            i += 13
            layers.append([tid, x, y, typ, u8, u10, u12])
        frames.append({"sound": sound, "time": time, "hit": hit, "color": color,
                       "shake": shake, "unknow4": u4, "layers": layers})
    for _ in range(count2):
        struct.unpack_from("<hh", b, i)
        i += 4
    if i != len(b):
        raise ValueError("SAF 长度不符：解析 %d / 实际 %d" % (i, len(b)))
    return count1, count2, count3, frames


def png_size(path):
    try:
        with open(path, "rb") as f:
            head = f.read(24)
        if head[:8] != b"\x89PNG\r\n\x1a\n":
            return 0, 0
        w, h = struct.unpack(">II", head[16:24])
        return w, h
    except Exception:
        return 0, 0


def build_index(root):
    """目录/文件索引（小写键，兼容 macOS 大小写不敏感）。"""
    idx = {}
    for r, _, fs in os.walk(root):
        for f in fs:
            if f.endswith(".meta"):
                continue
            idx[os.path.join(r, f).lower()] = os.path.join(r, f)
    return idx


def read_units(path):
    """man_base.dat：116 条 × 58 字节，Sprite 在偏移 36。"""
    out = []
    if not os.path.exists(path):
        return out
    b = open(path, "rb").read()
    for i in range(len(b) // 58):
        out.append(struct.unpack_from("<h", b, i * 58 + 36)[0])
    return out


def read_skills(path):
    """Mag_dat.dat：144 字节/条，AnimationID 在 62，BattleType 66，BattleSprite 70。"""
    out = []
    if not os.path.exists(path):
        return out
    b = open(path, "rb").read()
    for i in range(len(b) // 144):
        off = i * 144
        aid, _, btype, _, bsprite = struct.unpack_from("<hhhhh", b, off + 62)
        out.append({"id": i, "animationId": aid, "battleType": btype, "battleSprite": bsprite})
    return out


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--out", default=os.path.join(REPO, "AssetManifest"))
    ap.add_argument("--anim", default="", help="只处理该前缀的动画")
    a = ap.parse_args()
    os.makedirs(a.out, exist_ok=True)

    # ---------- 索引 ----------
    tex_idx = build_index(TEX_DIR)
    prefab_idx = build_index(PREFAB_DIR)
    sound_idx = build_index(SND_DIR)
    music_idx = build_index(MUS_DIR)
    movie_idx = build_index(MOV_DIR)

    # prefab 里的 animationName（用于与 SAF 名对照）
    prefab_names = {}
    for r, _, fs in os.walk(PREFAB_DIR):
        for f in fs:
            if not f.endswith(".prefab"):
                continue
            p = os.path.join(r, f)
            with open(p, encoding="utf-8", errors="ignore") as fh:
                m = re.search(r"^  animationName: (.+)$", fh.read(), re.M)
            if m:
                prefab_names[m.group(1).strip().lower()] = os.path.relpath(p, PREFAB_DIR)[:-7]

    saf_files = []
    for r, _, fs in os.walk(SAF_DIR):
        for f in fs:
            if f.endswith(".saf"):
                saf_files.append(os.path.join(r, f))
    saf_files.sort()

    sounds = set(os.path.splitext(os.path.basename(p))[0] for p in sound_idx.values())
    sound_missing = collections.Counter()

    animations = []
    parse_errors = []
    for p in saf_files:
        rel = os.path.relpath(p, SAF_DIR)[:-4]
        if a.anim and not rel.startswith(a.anim):
            continue
        try:
            c1, c2, c3, frames = parse_saf(p)
        except Exception as e:
            parse_errors.append({"animation": rel, "error": str(e)})
            continue

        base = rel.split("/")[-1]
        tex_dir = os.path.join(TEX_DIR, os.path.dirname(rel))
        missing_tex = []
        out_of_range = []
        external_layers = 0
        snd_used = set()
        for fi, fr in enumerate(frames):
            layers = []
            for ly in fr["layers"]:
                tid = ly[0]
                external = 1 if tid >= EXTERNAL_BIT else 0
                other_frame = (tid - EXTERNAL_BIT + 1) if external else ""
                if external:
                    # 取 otherGameAnimation 的图，本动画没有对应贴图
                    external_layers += 1
                    layers.append([tid, ly[1], ly[2], ly[3], ly[4], ly[5], ly[6],
                                   external, other_frame, "", "", "",
                                   "Material/Type%d" % ly[3]])
                    continue
                name = "%s_t%d" % (base, tid)
                png = os.path.join(tex_dir, name + ".png")
                exists = png.lower() in tex_idx
                if not exists:
                    if tid < c2:
                        missing_tex.append(tid)
                    else:
                        out_of_range.append(tid)
                layers.append([
                    tid, ly[1], ly[2], ly[3], ly[4], ly[5], ly[6],
                    external, other_frame,
                    name,
                    os.path.relpath(png, REPO) if exists else "",
                    "Texture/%s_t%d" % (rel, tid),
                    "Material/Type%d" % ly[3],
                ])
            if fr["sound"] >= 0:
                snd_used.add(fr["sound"])
                if str(fr["sound"]) not in sounds:
                    sound_missing[fr["sound"]] += 1
            fr["layers"] = layers

        # 未被帧引用、但 count2 里声明存在的贴图，也要检查
        used_ids = set(ly[0] for fr in frames for ly in fr["layers"])
        for tid in range(c2):
            if tid not in used_ids:
                name = "%s_t%d" % (base, tid)
                if os.path.join(tex_dir, name + ".png").lower() not in tex_idx:
                    missing_tex.append(tid)

        animations.append({
            "name": rel,
            "prefab": prefab_names.get(rel.lower(), ""),
            "frameCount": c1,
            "spriteCount": c2,
            "count3": c3,
            "sounds": sorted(snd_used),
            "missingTextures": sorted(set(missing_tex)),
            "outOfRangeIds": sorted(set(out_of_range)),
            "externalLayers": external_layers,
            "safBytes": os.path.getsize(p),
            "frames": frames,
        })

    animations.sort(key=lambda x: x["name"])

    # ---------- manifest.json ----------
    total_layers = sum(len(fr["layers"]) for x in animations for fr in x["frames"])
    meta = {
        "generator": "tools/build_asset_manifest.py",
        "source": "Man/Client/Assets",
        "generatedAt": datetime.datetime.now().isoformat(timespec="seconds"),
        "animationCount": len(animations),
        "frameCount": sum(x["frameCount"] for x in animations),
        "layerCount": total_layers,
        "layerColumns": LAYER_COLUMNS,
        "count3Index": {str(k): v for k, v in COUNT3_NAMES.items()},
        "directionIndex": {str(k): v for k, v in DIRECTION_NAMES.items()},
        "notes": [
            "动画名 = SAF 相对路径；贴图路径规则 Texture/<动画名>_t<textureId>.png",
            "layers 顺序即绘制顺序；世界分支 z=-i，textureType==7 时 z=+i",
            "坐标单位 SAF（1 单位 = 2 像素），位置 = (x + offsetX, -y + offsetY)",
            "textureId >= 0x8000 表示 external：取 otherGameAnimation 的第 (id-0x8000+1) 帧第 0 层贴图",
            "count3 全零的动画由 playAnimation(s,e)/showFrame 取帧，不按动作分区",
            "UI 动画（UI=1）在场景里，不在这份 SAF 清单中",
        ],
    }
    with open(os.path.join(a.out, "manifest.json"), "w", encoding="utf-8") as f:
        json.dump({"meta": meta, "animations": animations}, f, ensure_ascii=False,
                  separators=(",", ":"))

    # ---------- CSV ----------
    def w(name, header):
        f = open(os.path.join(a.out, name), "w", encoding="utf-8-sig", newline="")
        wr = csv.writer(f)
        wr.writerow(header)
        return f, wr

    f, wr = w("manifest_animations.csv",
              ["animation", "prefab", "prefabExists", "frameCount", "spriteCount", "layerCount",
               "externalLayers", "sounds", "textureMissing", "missingIds", "outOfRange", "safBytes"])
    for x in animations:
        wr.writerow([x["name"], x["prefab"], int(bool(x["prefab"])), x["frameCount"],
                     x["spriteCount"], sum(len(fr["layers"]) for fr in x["frames"]),
                     x["externalLayers"],
                     " ".join(str(s) for s in x["sounds"]), len(x["missingTextures"]),
                     " ".join(str(t) for t in x["missingTextures"][:50]),
                     " ".join(str(t) for t in x["outOfRangeIds"][:50]), x["safBytes"]])
    f.close()

    f, wr = w("manifest_frames.csv",
              ["animation", "frame", "sound", "soundFile", "soundExists", "time", "hit",
               "color", "shake", "unknow4", "layerCount"])
    for x in animations:
        for fi, fr in enumerate(x["frames"]):
            sid = fr["sound"]
            sfile = "Sound/%d.wav" % sid if sid >= 0 else ""
            wr.writerow([x["name"], fi, sid, sfile,
                         (int(str(sid) in sounds) if sid >= 0 else ""),
                         fr["time"], fr["hit"], fr["color"], fr["shake"], fr["unknow4"],
                         len(fr["layers"])])
    f.close()

    f, wr = w("manifest_layers.csv",
              ["animation", "frame", "layer", "textureId", "sprite", "x", "y", "type",
               "material", "unknow8", "unknow10", "unknow12", "external", "otherFrame",
               "pngPath", "gamePath", "pngExists"])
    for x in animations:
        for fi, fr in enumerate(x["frames"]):
            for li, ly in enumerate(fr["layers"]):
                wr.writerow([x["name"], fi, li, ly[0], ly[9], ly[1], ly[2], ly[3], ly[12],
                             ly[4], ly[5], ly[6], ly[7], ly[8], ly[10], ly[11],
                             int(bool(ly[10]))])
    f.close()

    # ---------- 资源清单 ----------
    f, wr = w("manifest_assets.csv", ["category", "name", "relativePath", "width", "height", "extra"])
    seen = set()
    for p in sorted(tex_idx.values()):
        rel = os.path.relpath(p, ASSETS)
        if "/Texture/" not in rel.replace(os.sep, "/"):
            continue
        if rel.lower() in seen:
            continue
        seen.add(rel.lower())
        wpx, hpx = png_size(p)
        anim = os.path.dirname(rel).replace("Texture/", "", 1).replace(os.sep, "/")
        wr.writerow(["Texture", os.path.basename(p)[:-4], rel, wpx, hpx, anim])
    for p in sorted(sound_idx.values()):
        wr.writerow(["Sound", os.path.splitext(os.path.basename(p))[0],
                     os.path.relpath(p, ASSETS), "", "", ""])
    for p in sorted(music_idx.values()):
        wr.writerow(["Music", os.path.splitext(os.path.basename(p))[0],
                     os.path.relpath(p, ASSETS), "", "", ""])
    for p in sorted(movie_idx.values()):
        wr.writerow(["Movie", os.path.basename(p), os.path.relpath(p, ASSETS), "", "", ""])
    f.close()

    # ---------- 数据表对照 ----------
    units = read_units(os.path.join(DAT_DIR, "man_base.dat"))
    f, wr = w("manifest_units.csv",
              ["unitId", "spriteId", "prefabPath", "prefabExists", "actualFile", "alternateFile"])
    unit_missing = 0
    unit_naming = []
    for uid, sprite in enumerate(units):
        rel_path = "Sprite/man%03d/%03dman" % (sprite, sprite)
        actual = prefab_idx.get(os.path.join(PREFAB_DIR, rel_path + ".prefab").lower(), "")
        # 规则之外的同名回退（例如 SAF 里叫 110 而规则要 110man）
        alt_rel = "Sprite/man%03d/%03d" % (sprite, sprite)
        alt = prefab_idx.get(os.path.join(PREFAB_DIR, alt_rel + ".prefab").lower(), "")
        if not actual:
            unit_missing += 1
            if alt:
                unit_naming.append({"unitId": uid, "spriteId": sprite,
                                    "expected": rel_path, "actual": alt_rel})
        wr.writerow([uid, sprite, "Prefab/" + rel_path, int(bool(actual)),
                     os.path.relpath(actual, PREFAB_DIR)[:-7] if actual else "",
                     os.path.relpath(alt, PREFAB_DIR)[:-7] if alt else ""])
    f.close()

    skills = read_skills(os.path.join(DAT_DIR, "Mag_dat.dat"))
    f, wr = w("manifest_skills.csv",
              ["skillId", "animationId", "battleType", "battleSprite", "usesMagic",
               "magicPrefab", "prefabExists", "actualFile"])
    magic_missing = []
    magic_used = 0
    for s in skills:
        bt = s["battleType"]
        aid = s["animationId"]
        # Normal=-1 必用特效；Map=1 / UIMap=2 仅当 id>0 且 battleSprite==0；UI=0 不用
        uses = (bt == -1 and aid > 0) or (bt in (1, 2) and aid > 0 and s["battleSprite"] == 0)
        rel_path = "Magic/magic%03d/M%d" % (aid, aid)
        actual = prefab_idx.get(os.path.join(PREFAB_DIR, rel_path + ".prefab").lower(), "")
        if uses:
            magic_used += 1
            if not actual:
                magic_missing.append({"skillId": s["id"], "animationId": aid, "battleType": bt})
        wr.writerow([s["id"], aid, bt, s["battleSprite"], int(uses), "Prefab/" + rel_path,
                     int(bool(actual)),
                     os.path.relpath(actual, PREFAB_DIR)[:-7] if actual else ""])
    f.close()

    # ---------- 逐动画对照 ----------
    f, wr = w("manifest_check.csv",
              ["animation", "prefabExists", "safOk", "frameCount", "spriteCount", "layerCount",
               "externalLayers", "textureMissing", "textureOutOfRange", "soundMissing", "status"])
    for x in animations:
        miss_snd = sum(1 for s in x["sounds"] if str(s) not in sounds)
        ok = bool(x["prefab"]) and not x["missingTextures"] and not miss_snd
        wr.writerow([x["name"], int(bool(x["prefab"])), 1, x["frameCount"], x["spriteCount"],
                     sum(len(fr["layers"]) for fr in x["frames"]), x["externalLayers"],
                     len(x["missingTextures"]), len(x["outOfRangeIds"]), miss_snd,
                     "ok" if ok else "gap"])
    f.close()

    # ---------- 汇总 ----------
    prefab_name_set = set(prefab_names.keys())
    saf_name_set = set(x["name"].lower() for x in animations)
    summary = {
        "generatedAt": meta["generatedAt"],
        "animations": len(animations),
        "frames": meta["frameCount"],
        "layers": meta["layerCount"],
        "externalLayers": sum(x["externalLayers"] for x in animations),
        "textureOutOfRangeIds": sum(len(x["outOfRangeIds"]) for x in animations),
        "textureFiles": len([1 for p in tex_idx if p.endswith(".png")]),
        "safParseErrors": parse_errors,
        "animationsWithMissingTextures": [
            {"animation": x["name"], "missing": len(x["missingTextures"])}
            for x in animations if x["missingTextures"]
        ],
        "soundMissing": dict(sound_missing),
        "prefabMissing": [x["name"] for x in animations if not x["prefab"]],
        "prefabOnlyNames": sorted(prefab_name_set - saf_name_set)[:20],
        "safOnlyNames": sorted(saf_name_set - prefab_name_set)[:20],
        "units": {"count": len(units), "prefabMissing": unit_missing,
                  "namingMismatch": unit_naming},
        "skills": {"count": len(skills), "usesMagic": magic_used,
                   "magicMissing": magic_missing},
        "materialTypes": sorted(os.path.splitext(f)[0] for f in os.listdir(os.path.join(ASSETS, "Resources/Material"))
                                if f.endswith(".mat")),
    }
    with open(os.path.join(a.out, "summary.json"), "w", encoding="utf-8") as f:
        json.dump(summary, f, ensure_ascii=False, indent=1)

    log("动画 %d，帧 %d，图层 %d" % (len(animations), meta["frameCount"], meta["layerCount"]))
    log("缺贴图动画 %d，缺音效 %d，缺 prefab %d"
        % (len(summary["animationsWithMissingTextures"]), len(sound_missing), len(summary["prefabMissing"])))
    log("单位 %d（规则路径缺失 %d，其中命名不一致 %d）；技能 %d（用特效 %d，缺特效 %d）"
        % (len(units), unit_missing, len(unit_naming), len(skills), magic_used, len(magic_missing)))
    log("完成 → %s" % a.out)


if __name__ == "__main__":
    main()
