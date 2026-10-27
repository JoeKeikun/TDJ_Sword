#!/usr/bin/env python3
"""从 APK 的 data.unity3d 导出全部动画帧到 Assets/Resources/Texture。

背景：Assets/Objects 与 Assets/Resources/Texture 都是导出产物，不入版本库。
新环境克隆仓库后必须先跑本脚本，再执行 Unity 构建。

导出逻辑严格按 sprite 自身的 rect / textureRect 裁剪，不做任何后处理
（尤其是不要自己把"黑底"抠成透明，那会破坏 Stage 过场动画的合成结果）。

用法：
    # 默认从 APK 的 data.unity3d 导出
    python3 tools/export_assets.py

    # Stage 等资源在 APK 里是 ETC2，部分纹理解码失败，需要用 PC 版数据
    unzip -q SwordWin64.zip -d /tmp/swordwin
    TDJ_SRC=/tmp/swordwin/Sword_Data python3 tools/export_assets.py

    ANIM="Stage/Stage00/Eanm_01" python3 tools/export_assets.py   # 只重导某个动画

可用环境变量：
    TDJ_SRC        数据源，可以是 data.unity3d 文件或 PC 版 Sword_Data 目录，
                   默认 <repo>/Man/apk_data/assets/bin/Data/data.unity3d
    TDJ_DUMMY_DLL  il2cpp DummyDll 目录（生成 GameAnimation 的 typetree 用）
    TDJ_SAF_DIR    SAF 目录，默认 <repo>/Man/Client/Assets/Objects/SAF
    OUT_DIR        输出目录，默认 <repo>/Man/Client/Assets/Resources/Texture
    ANIM           只导出名字以该前缀开头的动画
    LIMIT          只处理前 N 个纹理（调试用）
"""

import collections
import glob
import json
import os
import re
import struct
import sys
import uuid

import texture2ddecoder as t
import UnityPy
from PIL import Image, ImageOps
from TypeTreeGeneratorAPI import TypeTreeGenerator

REPO = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))


def _first(*candidates):
    """返回第一个存在的路径，用于多位置探测。"""
    for c in candidates:
        hits = glob.glob(c)
        if hits:
            return sorted(hits)[0]
    return candidates[0]


SRC = os.environ.get("TDJ_SRC", _first(
    os.path.join(REPO, "Man/apk_data/assets/bin/Data/data.unity3d"),
    os.path.expanduser("~/.trae-cn/work/*/apk_data/assets/bin/Data/data.unity3d"),
))
DUMMY_DLL = os.environ.get("TDJ_DUMMY_DLL", _first(
    os.path.join(REPO, "Man/il2cpp_out/DummyDll"),
    os.path.expanduser("~/.trae-cn/work/*/il2cpp_out/DummyDll"),
))
SAF_DIR = os.environ.get("TDJ_SAF_DIR", os.path.join(REPO, "Man/Client/Assets/Objects/SAF"))
OUT_DIR = os.environ.get("OUT_DIR", os.path.join(REPO, "Man/Client/Assets/Resources/Texture"))
ANIM = os.environ.get("ANIM", "")
LIMIT = int(os.environ.get("LIMIT", "0"))

TEX_FMT = {
    47: ("etc2a8", False), 45: ("etc2", False), 63: ("etc2a8", False),
    64: ("etc2", True), 65: ("etc2a8", True),
    10: ("bc1", False), 12: ("bc3", False),
    28: ("bc1", True), 29: ("bc3", True),
}


def parse_tex(raw):
    nlen = struct.unpack_from("<i", raw, 0)[0]
    name = raw[4:4 + nlen].decode("utf-8", "ignore")
    p = (4 + nlen + 3) & ~3
    width = struct.unpack_from("<i", raw, p + 8)[0]
    height = struct.unpack_from("<i", raw, p + 12)[0]
    fmt = struct.unpack_from("<i", raw, p + 24)[0]
    return name, width, height, fmt


def build_typetree():
    """有 DummyDll 时返回 typetree；缺失时返回 None，由字节解析兜底。"""
    try:
        gen = TypeTreeGenerator("2021.3.16f1", "AssetsTools")
        for fp in sorted(glob.glob(os.path.join(DUMMY_DLL, "*.dll"))):
            try:
                gen.load_dll(open(fp, "rb").read())
            except Exception:
                pass
        return json.loads(gen.get_nodes_as_json("Assembly-CSharp.dll", "GameAnimation"))
    except Exception:
        return None


STR_RUN = re.compile(rb"[\x20-\x7e]{5,140}")


def _longest_pptr_run(raw, spr_by_pid):
    """在原始字节里找最长的 PPtr 连续段，即 sprites[] 数组。"""
    best = []
    i = 28
    n = len(raw)
    while i < n - 12:
        arr = []
        p = i
        while p + 12 <= n:
            fid, pid = struct.unpack_from("<iq", raw, p)
            if fid in (0, 1) and pid in spr_by_pid:
                arr.append(pid)
                p += 12
            else:
                break
        if len(arr) >= 3 and len(arr) > len(best):
            best = arr
            i = p
        else:
            i += 4
    return best


def parse_anim_sprites(env, anim_frames):
    """没有 typetree 时解析 GameAnimation：
    animationName 与 sprites[] 都在原始字节里，用 SAF 动画名做锚点，
    命中后取该对象内最长的 PPtr 连续段作为帧引用。"""
    spr_by_pid = {}
    for o in env.objects:
        if o.type.name == "Sprite":
            spr_by_pid[o.path_id] = o

    names = set(anim_frames)
    out = {}
    for o in env.objects:
        if o.type.name != "MonoBehaviour":
            continue
        raw = o.get_raw_data()
        if len(raw) < 100:
            continue
        hits = []
        for m in STR_RUN.finditer(raw):
            if b"/" not in m.group():
                continue
            t = m.group().decode()
            if t in names:
                hits.append(t)
        if not hits:
            continue
        hits.sort(key=len, reverse=True)
        nm = hits[0]
        if nm in out:
            continue
        pids = _longest_pptr_run(raw, spr_by_pid)
        if pids:
            out[nm] = [spr_by_pid[p] for p in pids]
    return out


def main():
    if not os.path.exists(SRC):
        sys.exit("找不到数据源：%s（可用 TDJ_SRC 指定 data.unity3d 或 Sword_Data 目录）" % SRC)

    nodes = build_typetree()
    env = UnityPy.load(SRC)
    res_cache = {}

    # data.unity3d 是 bundle，assets 与 .resS 在内部文件里；
    # PC 版 Sword_Data 则是平铺的独立文件。这里统一成 name -> reader。
    inner = {}
    for k, v in env.files.items():
        sub = getattr(v, "files", None)
        if isinstance(sub, dict):
            inner.update(sub)
        else:
            inner[k] = v

    def get_res(name):
        if name not in res_cache:
            v = inner.get(name)
            if v is None:
                for k, vv in inner.items():
                    if str(k).endswith(name):
                        v = vv
                        break
            res_cache[name] = v.bytes if hasattr(v, "bytes") else None
        return res_cache[name]

    sprite_by_name = collections.defaultdict(list)
    for o in env.objects:
        if o.type.name == "Sprite":
            try:
                sprite_by_name[o.read().m_Name].append(o)
            except Exception:
                pass

    def sinfo(o):
        x = o.read()
        rd = x.m_RD
        reader = rd.texture.deref()
        key = (reader.assets_file.name, reader.path_id)
        return dict(
            name=x.m_Name, key=key, reader=reader,
            rect=(x.m_Rect.x, x.m_Rect.y, x.m_Rect.width, x.m_Rect.height),
            tr=(rd.textureRect.x, rd.textureRect.y, rd.textureRect.width, rd.textureRect.height),
            pivot=(x.m_Pivot.x, x.m_Pivot.y), ppu=x.m_PixelsToUnits,
        )

    # SAF 头部给出每个动画的帧数
    anim_frames = {}
    for root, _, files in os.walk(SAF_DIR):
        for f in files:
            if not f.endswith(".saf"):
                continue
            rel = os.path.relpath(os.path.join(root, f), SAF_DIR)[:-4]
            raw = open(os.path.join(root, f), "rb").read()
            anim_frames[rel] = struct.unpack_from("<hh", raw, 0)[1]

    anim_sprites = {}

    if nodes:
        for o in env.objects:
            if o.type.name != "MonoBehaviour":
                continue
            d = o.get_raw_data()
            if len(d) < 28 or struct.unpack_from("<q", d, 20)[0] != 951:
                continue
            tt = o.read_typetree(nodes=nodes)
            nm = tt.get("animationName")
            if nm in anim_sprites or nm not in anim_frames:
                continue
            infos = []
            for p in (tt.get("sprites") or []):
                try:
                    infos.append(sinfo(p.read()))
                except Exception:
                    infos.append(None)
            anim_sprites[nm] = infos
    else:
        print("没有 typetree，改用字节解析 sprites[]", flush=True)
        for nm, objs in parse_anim_sprites(env, anim_frames).items():
            infos = []
            for p in objs:
                try:
                    infos.append(sinfo(p))
                except Exception:
                    infos.append(None)
            anim_sprites[nm] = infos

    print("解析到动画引用 %d / %d" % (len(anim_sprites), len(anim_frames)), flush=True)

    tasks = collections.defaultdict(list)
    missing = []
    for nm, count in anim_frames.items():
        base = nm.split("/")[-1]
        infos = anim_sprites.get(nm) or []
        for i in range(count):
            info = None
            if i < len(infos) and infos[i] is not None:
                info = infos[i]
            else:
                cands = sprite_by_name.get(base + "_t" + str(i))
                if cands:
                    try:
                        info = sinfo(cands[0])
                    except Exception:
                        info = None
            if info is None:
                missing.append((nm, i))
                continue
            tasks[info["key"]].append((nm, base, i, info))

    print("纹理 %d 张，待导出帧 %d，缺失 %d"
          % (len(tasks), sum(len(v) for v in tasks.values()), len(missing)), flush=True)

    def decode(reader):
        raw = reader.get_raw_data()
        nlen = struct.unpack_from("<i", raw, 0)[0]
        p = (4 + nlen + 3) & ~3
        w = struct.unpack_from("<i", raw, p + 8)[0]
        h = struct.unpack_from("<i", raw, p + 12)[0]
        fmt = struct.unpack_from("<i", raw, p + 24)[0]
        sf = reader.assets_file
        res_name = (sf.name if sf else "") + ".resS"
        csize = struct.unpack_from("<i", raw, p + 16)[0]
        i = raw.find(res_name.encode())
        if i <= 0:
            return None
        off = struct.unpack_from("<q", raw, i - 16)[0]
        size = csize if csize > 0 else struct.unpack_from("<i", raw, i - 8)[0]
        blob_all = get_res(res_name)
        if blob_all is None or off < 0 or size <= 0 or w <= 0 or h <= 0:
            return None
        blob = bytes(blob_all[off:off + size])
        if fmt == 4:
            if len(blob) < w * h * 4:
                return None
            return ImageOps.flip(Image.frombytes("RGBA", (w, h), blob[:w * h * 4]))
        if fmt == 3:
            if len(blob) < w * h * 3:
                return None
            return ImageOps.flip(Image.frombytes("RGB", (w, h), blob[:w * h * 3]))
        spec = TEX_FMT.get(fmt)
        if not spec:
            return None
        func, crunchy = spec
        if crunchy:
            blob = t.unpack_unity_crunch(blob)
        rgba = getattr(t, "decode_" + func)(blob, w, h)
        return ImageOps.flip(Image.frombytes("RGBA", (w, h), rgba, "raw", "BGRA"))

    with open(os.path.join(os.path.dirname(os.path.abspath(__file__)), "texture_meta.tpl")) as f:
        TEX_META = f.read()

    written = 0
    fail = 0
    for n, (key, items) in enumerate(tasks.items()):
        if ANIM:
            items = [it for it in items if it[0].startswith(ANIM)]
            if not items:
                continue
        if LIMIT and n >= LIMIT:
            break
        img = None
        try:
            img = decode(items[0][3]["reader"])
        except Exception:
            img = None
        if img is None:
            fail += 1
            continue
        TW, TH = img.size
        for nm, base, idx, info in items:
            m = re.match(r"^" + re.escape(base) + r"_t(\d+)$", info.get("name") or "")
            if m:
                idx = int(m.group(1))
            rx, ry, rw, rh = info["rect"]
            tx, ty, tw, th = info["tr"]
            cw, ch = max(1, int(round(rw))), max(1, int(round(rh)))
            sx, sy = int(round(tx)), int(round(ty))
            sw, sh = max(1, int(round(tw))), max(1, int(round(th)))
            dx, dy = int(round(tx - rx)), int(round(ty - ry))
            top = TH - (sy + sh)
            canvas = Image.new("RGBA", (cw, ch), (0, 0, 0, 0))
            canvas.paste(img.crop((sx, top, sx + sw, top + sh)), (dx, ch - (dy + sh)))
            d = os.path.join(OUT_DIR, os.path.dirname(nm))
            os.makedirs(d, exist_ok=True)
            fp = os.path.join(d, base + "_t" + str(idx) + ".png")
            canvas.save(fp, optimize=False, compress_level=1)
            maxsize = 2048 if max(cw, ch) <= 2048 else 4096
            with open(fp + ".meta", "w") as f:
                f.write(TEX_META.format(guid=uuid.uuid4().hex, px=info["pivot"][0], py=info["pivot"][1],
                                        ppu=info["ppu"], maxsize=maxsize, spriteid=uuid.uuid4().hex))
            written += 1
        if n % 200 == 0:
            print("  纹理 %d / 已写出 %d" % (n, written), flush=True)

    print("完成：写出 %d 帧，解码失败 %d 张纹理，缺失 %d 帧"
          % (written, fail, len(missing)), flush=True)


if __name__ == "__main__":
    main()
