#!/usr/bin/env python3
"""从 Sword.apk 全量导出 Unity 素材（贴图 / 精灵 / 音频 / 视频）。

自己反解素材不对，基本都是这几条没处理：

    1. 像素流方向：Unity 的 Texture2D 数据是自下而上存储的，解码后必须整体垂直翻转；
    2. 图集裁剪：Sprite 只是图集上的一块，必须按 rect / textureRect 裁剪，不能整张丢出来；
    3. 压缩格式：贴图是 ETC2 / ETC2A8 / BC1 / BC3，部分还套了 Crunch，要先解 Crunch 再解码；
    4. 名字重名：APK 里 Sprite 名字并不唯一（例如 Eanm_01_t12 这个名字同时对应上百个
       完全不同的图），按名字导出必然串图。正确做法是读 GameAnimation 组件里的
       sprites[] 引用顺序，按动画归位。

用法：
    python3 tools/dump_apk_assets.py                     # 全量导出到 ./AssetDump
    python3 tools/dump_apk_assets.py --only sprite       # 只导出按动画归位的精灵
    python3 tools/dump_apk_assets.py --only texture      # 只导出图集原图
    python3 tools/dump_apk_assets.py --only audio,video  # 只导出音频 / 视频
    python3 tools/dump_apk_assets.py --anim Stage/Stage00 # 只导出该前缀下的动画

输出结构：
    AssetDump/Texture2D/<贴图名>.png          图集原图（已翻转，完整尺寸）
    AssetDump/Sprite/<动画名>/<帧名>.png      按 GameAnimation 归位的单帧（透明背景）
    AssetDump/Sprite/_other/<精灵名>.png      没有被任何动画引用的零散精灵（UI / 地图等）
    AssetDump/AudioClip/<片段名>.wav
    AssetDump/Video/<片段名>.<扩展名>
    AssetDump/index.html                      可直接双击打开的素材浏览页
    AssetDump/report.json                     导出清单与失败项

依赖：UnityPy、texture2ddecoder、Pillow、TypeTreeGeneratorAPI
     其中「按动画归位」需要 il2cpp 的 DummyDll（Assembly-CSharp.dll），
     缺失时会自动退化为按名字导出，并在报告里标注。
"""

import argparse
import collections
import glob
import json
import os
import re
import struct
import sys
import zipfile

import texture2ddecoder as t
import UnityPy
from PIL import Image, ImageOps

REPO = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))

# TextureFormat -> (texture2ddecoder 函数名, 是否为 Crunch 压缩)
TEX_FMT = {
    47: ("etc2a8", False), 45: ("etc2", False), 63: ("etc2a8", False),
    64: ("etc2", True), 65: ("etc2a8", True),
    10: ("bc1", False), 12: ("bc3", False),
    28: ("bc1", True), 29: ("bc3", True),
}

APK_MEMBERS = [
    "assets/bin/Data/data.unity3d",
    "assets/bin/Data/resources.resource",
]

GAME_ANIMATION_SCRIPT_ID = 951  # GameAnimation 在 MonoScript 里的 fileID


def log(*a):
    print(*a, flush=True)


def parse_tex(raw):
    """解析 Texture2D 头部：名称、宽高、流大小、格式。"""
    nlen = struct.unpack_from("<i", raw, 0)[0]
    name = raw[4:4 + nlen].decode("utf-8", "ignore")
    p = (4 + nlen + 3) & ~3
    width = struct.unpack_from("<i", raw, p + 8)[0]
    height = struct.unpack_from("<i", raw, p + 12)[0]
    csize = struct.unpack_from("<i", raw, p + 16)[0]
    fmt = struct.unpack_from("<i", raw, p + 24)[0]
    return name, width, height, csize, fmt


def safe(name):
    n = (name or "unnamed").replace("\\", "_").replace(":", "_")
    n = re.sub(r"[\x00-\x1f]", "", n).strip()
    return n or "unnamed"


def find_dummy_dll():
    cands = [os.environ.get("TDJ_DUMMY_DLL", "")]
    cands += [os.path.join(REPO, "Man/il2cpp_out/DummyDll")]
    cands += sorted(glob.glob(os.path.expanduser("~/.trae-cn/work/*/il2cpp_out/DummyDll")))
    for c in cands:
        if c and glob.glob(os.path.join(c, "Assembly-CSharp.dll")):
            return c
    return ""


def find_fsbtool():
    """Unity 自带的 FSBTool 可以把 FSB5(Vorbis) 转成 wav。"""
    cands = [os.environ.get("TDJ_FSBTOOL", "")]
    for root in ("/Applications/Unity/Hub/Editor", "/Applications/Tuanjie/Hub/Editor"):
        cands += sorted(glob.glob(os.path.join(root, "*", "*.app/Contents/Tools/FSBTool/FSBTool")),
                        reverse=True)
    for c in cands:
        if c and os.path.exists(c):
            return c
    return ""


def run_fsbtool(fsbtool, fsb, wav, cache):
    import subprocess
    lib = os.path.dirname(fsbtool)
    try:
        r = subprocess.run([fsbtool, "-i", fsb, "-o", wav, "-l", lib, "-h", cache,
                            "-c", "pcm", "-C", "wav"],
                           stdout=subprocess.DEVNULL, stderr=subprocess.DEVNULL, timeout=60)
        return r.returncode == 0 and os.path.exists(wav) and os.path.getsize(wav) > 44
    except Exception:
        return False


def ensure_data(apk, work):
    """从 APK 解出 Unity 数据（已存在则复用）。"""
    dst = os.path.join(work, "assets", "bin", "Data")
    if all(os.path.exists(os.path.join(dst, os.path.basename(m))) for m in APK_MEMBERS):
        return dst
    if not os.path.exists(apk):
        sys.exit("找不到 APK：%s" % apk)
    os.makedirs(dst, exist_ok=True)
    log("解包 APK → %s" % dst)
    with zipfile.ZipFile(apk) as z:
        for m in APK_MEMBERS:
            log("  %s" % m)
            z.extract(m, work)
    return dst


class Dumper:
    def __init__(self, data_dir, out_dir):
        self.data_dir = data_dir
        self.out = out_dir
        self.env = UnityPy.load(os.path.join(data_dir, "data.unity3d"))
        self.streams = self.env.file.files
        self.entries = []
        self.failed = []
        self.used = set()
        self.used_lower = set()     # macOS 文件系统大小写不敏感，需按小写判重
        self.dir_names = {}
        self.dir_used = {}
        self.resource_path = os.path.join(data_dir, "resources.resource")
        self.anim_mapping = False

    # ---------- 基础 ----------
    def stream_bytes(self, name):
        v = self.streams.get(name)
        if v is None:
            for k, vv in self.streams.items():
                if str(k).endswith(name):
                    v = vv
                    break
        return v.bytes if v is not None else None

    def unique_path(self, rel, ext=".png"):
        p = os.path.join(self.out, rel + ext)
        base, n = rel, 1
        while p in self.used or p.lower() in self.used_lower:
            p = os.path.join(self.out, "%s~%d%s" % (base, n, ext))
            n += 1
        self.used.add(p)
        self.used_lower.add(p.lower())
        os.makedirs(os.path.dirname(p), exist_ok=True)
        return p

    def anim_dir(self, name):
        """动画目录名去重：仅大小写不同的目录在 macOS 上会被合并，这里加后缀区分。"""
        if name in self.dir_names:
            return self.dir_names[name]
        d, n = name, 2
        while d.lower() in self.dir_used:
            d = "%s~%d" % (name, n)
            n += 1
        self.dir_used[d.lower()] = name
        self.dir_names[name] = d
        return d

    def save(self, rel, img, category, name, group=""):
        p = self.unique_path(rel)
        img.save(p, compress_level=1)
        e = {"c": category, "n": name, "p": os.path.relpath(p, self.out),
             "w": img.size[0], "h": img.size[1]}
        if group:
            e["g"] = group
        self.entries.append(e)

    # ---------- 贴图解码 ----------
    def decode_texture(self, o):
        raw = o.get_raw_data()
        name, w, h, csize, fmt = parse_tex(raw)
        sf = o.assets_file
        sf_name = sf.name if sf is not None else ""
        for res_name in (sf_name + ".resS", "resources.resource"):
            i = raw.find(res_name.encode())
            if i <= 0:
                continue
            blob_all = self.stream_bytes(res_name)
            if blob_all is None:
                continue
            off = struct.unpack_from("<q", raw, i - 16)[0]
            size = csize if csize > 0 else struct.unpack_from("<i", raw, i - 8)[0]
            if off < 0 or size <= 0 or w <= 0 or h <= 0:
                return None
            blob = bytes(blob_all[off:off + size])
            if fmt == 4:      # RGBA32
                if len(blob) < w * h * 4:
                    return None
                return ImageOps.flip(Image.frombytes("RGBA", (w, h), blob[:w * h * 4]))
            if fmt == 3:      # RGB24
                if len(blob) < w * h * 3:
                    return None
                return ImageOps.flip(Image.frombytes("RGB", (w, h), blob[:w * h * 3]))
            if fmt == 7:      # RGB565
                if len(blob) < w * h * 2:
                    return None
                return ImageOps.flip(Image.frombytes("RGB", (w, h), blob[:w * h * 2],
                                                     "raw", "BGR;16")).convert("RGBA")
            spec = TEX_FMT.get(fmt)
            if not spec:
                return None
            func, crunchy = spec
            if crunchy:
                try:
                    blob = t.unpack_unity_crunch(blob)
                except Exception:
                    pass
            rgba = getattr(t, "decode_" + func)(blob, w, h)
            return ImageOps.flip(Image.frombytes("RGBA", (w, h), rgba, "raw", "BGRA"))
        return None

    def crop_sprite(self, img, info):
        """把图集切成精灵：rect 是未裁剪框，textureRect 是实际像素区域。"""
        rx, ry, rw, rh = info["rect"]
        tx, ty, tw, th = info["tr"]
        cw, ch = max(1, int(round(rw))), max(1, int(round(rh)))
        sx, sy = int(round(tx)), int(round(ty))
        sw, sh = max(1, int(round(tw))), max(1, int(round(th)))
        dx, dy = int(round(tx - rx)), int(round(ty - ry))
        top = img.size[1] - (sy + sh)          # 纹理坐标(y 向上) -> 像素(y 向下)
        canvas = Image.new("RGBA", (cw, ch), (0, 0, 0, 0))
        canvas.paste(img.crop((sx, top, sx + sw, top + sh)), (dx, ch - (dy + sh)))
        return canvas

    # ---------- 动画归位 ----------
    def build_anim_map(self, dummy):
        """读 GameAnimation 组件，得到「动画名 → 有序精灵列表」。"""
        if not dummy:
            log("未找到 DummyDll，退化为按名字导出（可能串图）")
            return None
        try:
            from TypeTreeGeneratorAPI import TypeTreeGenerator
        except Exception:
            log("缺少 TypeTreeGeneratorAPI，退化为按名字导出")
            return None
        gen = TypeTreeGenerator("2021.3.16f1", "AssetsTools")
        for fp in sorted(glob.glob(os.path.join(dummy, "*.dll"))):
            try:
                gen.load_dll(open(fp, "rb").read())
            except Exception:
                pass
        try:
            nodes = json.loads(gen.get_nodes_as_json("Assembly-CSharp.dll", "GameAnimation"))
        except Exception as e:
            log("typetree 生成失败：%s" % e)
            return None

        by_key = {}
        for o in self.env.objects:
            if o.type.name == "Sprite":
                by_key[(o.assets_file.name, o.path_id)] = o

        def resolve(o, pptr):
            fid = pptr["m_FileID"]
            pid = pptr["m_PathID"]
            if fid == 0:
                return (o.assets_file.name, pid)
            ext = o.assets_file.externals[fid - 1].name
            return (ext, pid)

        anims = {}
        used = collections.defaultdict(list)   # 精灵 key -> [(动画名, 序号)]
        for o in self.env.objects:
            if o.type.name != "MonoBehaviour":
                continue
            d = o.get_raw_data()
            if len(d) < 28 or struct.unpack_from("<q", d, 20)[0] != GAME_ANIMATION_SCRIPT_ID:
                continue
            try:
                tt = o.read_typetree(nodes=nodes)
            except Exception:
                continue
            nm = tt.get("animationName")
            if not nm or nm in anims:
                continue
            keys = []
            for p in (tt.get("sprites") or []):
                try:
                    keys.append(resolve(o, p))
                except Exception:
                    keys.append(None)
            anims[nm] = keys
            for i, k in enumerate(keys):
                if k:
                    used[k].append((nm, i))
        self.anim_mapping = True
        log("动画 %d 个，引用精灵 %d 个" % (len(anims), len(used)))
        return anims, used, by_key

    # ---------- 导出 ----------
    def dump_textures_and_sprites(self, do_tex, do_sprite, anim="", dummy=""):
        mapping = self.build_anim_map(dummy) if do_sprite else None
        if mapping:
            used, by_key = mapping[1], mapping[2]
        else:
            # 没有 typetree 时退化为「所有精灵都算零散精灵」，至少内容是对的
            used = {}
            by_key = {(o.assets_file.name, o.path_id): o
                      for o in self.env.objects if o.type.name == "Sprite"}

        tex_objs = {}
        for o in self.env.objects:
            if o.type.name != "Texture2D":
                continue
            try:
                name = parse_tex(o.get_raw_data())[0]
            except Exception:
                continue
            tex_objs[(o.assets_file.name, o.path_id)] = (o, name)

        # 精灵信息 + 归属贴图
        sprite_of = {}      # 精灵 key -> info
        by_tex = collections.defaultdict(list)
        for key, o in by_key.items():
            try:
                s = o.read()
                reader = s.m_RD.texture.deref()
                info = dict(
                    name=s.m_Name, key=key,
                    tex=(reader.assets_file.name, reader.path_id),
                    rect=(s.m_Rect.x, s.m_Rect.y, s.m_Rect.width, s.m_Rect.height),
                    tr=(s.m_RD.textureRect.x, s.m_RD.textureRect.y,
                        s.m_RD.textureRect.width, s.m_RD.textureRect.height),
                )
            except Exception as e:
                self.failed.append({"type": "Sprite", "name": str(key), "reason": str(e)[:120]})
                continue
            sprite_of[key] = info
            by_tex[info["tex"]].append(key)

        # 没有被动画引用的精灵：只按名字导出（内容正确，但归属未知）
        others = [k for k in sprite_of if k not in used]
        log("图集 %d 张，动画精灵 %d 个，零散精灵 %d 个"
            % (len(tex_objs), len(sprite_of) - len(others), len(others)))

        for n, (tkey, (o, tname)) in enumerate(tex_objs.items()):
            keys = by_tex.get(tkey, []) if do_sprite else []
            if anim and do_sprite:
                keys = [k for k in keys if any(a.startswith(anim) for a, _ in used.get(k, []))]
            if not do_tex and not keys:
                continue
            if n % 500 == 0:
                log("  图集 %d/%d  已导出 %d" % (n, len(tex_objs), len(self.entries)))
            try:
                img = self.decode_texture(o)
            except Exception as e:
                img = None
                self.failed.append({"type": "Texture2D", "name": tname, "reason": str(e)[:120]})
            if img is None:
                self.failed.append({"type": "Texture2D", "name": tname, "reason": "decode/stream"})
                continue

            if do_tex:
                self.save("Texture2D/" + safe(tname), img, "Texture2D", tname)

            if not do_sprite:
                continue
            for k in keys:
                info = sprite_of[k]
                try:
                    spr = self.crop_sprite(img, info)
                except Exception as e:
                    self.failed.append({"type": "Sprite", "name": info["name"], "reason": str(e)[:120]})
                    continue
                refs = [r for r in used.get(k, []) if not anim or r[0].startswith(anim)]
                if refs:
                    # 同一个精灵被多个动画引用时，各动画目录各放一份，方便按动画浏览
                    for nm, idx in refs:
                        self.save("Sprite/%s/%s" % (self.anim_dir(nm), safe(info["name"])),
                                  spr, "Sprite", info["name"], nm)
                else:
                    self.save("Sprite/_other/" + safe(info["name"]), spr, "Sprite", info["name"])

    def dump_audio(self):
        """AudioClip 的音频数据在 resources.resource 里，是 FSB5（Vorbis）容器。
        先落盘 .fsb，再用 Unity 自带的 FSBTool 转成 wav；没有 FSBTool 时保留 .fsb。"""
        fsbtool = find_fsbtool()
        if not os.path.exists(self.resource_path):
            log("没有 resources.resource，跳过音频")
            return
        cache = os.path.join(self.data_dir, ".fsbcache")
        os.makedirs(cache, exist_ok=True)
        n_wav = n_fsb = 0
        with open(self.resource_path, "rb") as rf:
            for o in self.env.objects:
                if o.type.name != "AudioClip":
                    continue
                try:
                    clip = o.read()
                except Exception as e:
                    self.failed.append({"type": "AudioClip", "name": "?", "reason": str(e)[:120]})
                    continue
                blob = None
                res = getattr(clip, "m_Resource", None)
                if res is not None and (getattr(res, "m_Source", "") or "").endswith("resources.resource"):
                    rf.seek(res.m_Offset)
                    blob = rf.read(res.m_Size)
                elif getattr(clip, "m_AudioData", None):
                    blob = clip.m_AudioData
                if not blob:
                    self.failed.append({"type": "AudioClip", "name": clip.m_Name, "reason": "no data"})
                    continue

                base = "AudioClip/" + safe(clip.m_Name)
                fsb = self.unique_path(base, ".fsb")
                with open(fsb, "wb") as f:
                    f.write(blob)

                if fsbtool:
                    wav = os.path.splitext(fsb)[0] + ".wav"
                    ok = run_fsbtool(fsbtool, fsb, wav, cache)
                    if ok:
                        os.remove(fsb)
                        p = wav
                        n_wav += 1
                    else:
                        p = fsb
                        n_fsb += 1
                        self.failed.append({"type": "AudioClip", "name": clip.m_Name,
                                            "reason": "fsbtool failed"})
                else:
                    p = fsb
                    n_fsb += 1
                self.entries.append({"c": "AudioClip", "n": clip.m_Name,
                                     "p": os.path.relpath(p, self.out), "w": 0, "h": 0})
        log("音频导出 %d 个（wav %d / fsb %d）" % (n_wav + n_fsb, n_wav, n_fsb))

    def dump_video(self):
        if not os.path.exists(self.resource_path):
            log("没有 resources.resource，跳过视频")
            return
        n = 0
        with open(self.resource_path, "rb") as rf:
            for o in self.env.objects:
                if o.type.name != "VideoClip":
                    continue
                v = o.read()
                ext = os.path.splitext(v.m_OriginalPath or "")[1] or ".mp4"
                res = v.m_ExternalResources
                src = getattr(res, "m_Source", "") or ""
                if not src.endswith("resources.resource"):
                    self.failed.append({"type": "VideoClip", "name": v.m_Name, "reason": "external:" + src})
                    continue
                rf.seek(res.m_Offset)
                data = rf.read(res.m_Size)
                p = self.unique_path("Video/" + safe(v.m_Name), ext)
                with open(p, "wb") as f:
                    f.write(data)
                self.entries.append({"c": "Video", "n": v.m_Name, "p": os.path.relpath(p, self.out),
                                     "w": v.Width, "h": v.Height})
                n += 1
        log("视频导出 %d 个" % n)

    # ---------- 浏览页 ----------
    def write_index(self):
        # 支持分次导出（如先 texture/sprite，再 audio）：合并已有清单
        merged = {}
        old = os.path.join(self.out, "entries.json")
        if os.path.exists(old):
            try:
                with open(old, encoding="utf-8") as f:
                    for e in json.load(f):
                        merged[e["p"]] = e
            except Exception:
                pass
        for e in self.entries:
            merged[e["p"]] = e
        entries = sorted(merged.values(), key=lambda e: (e["c"], e.get("g", ""), e["p"]))
        with open(os.path.join(self.out, "entries.json"), "w", encoding="utf-8") as f:
            json.dump(entries, f, ensure_ascii=False, separators=(",", ":"))
        html = INDEX_TPL.replace("__DATA__", json.dumps(entries, ensure_ascii=False,
                                                        separators=(",", ":")))
        with open(os.path.join(self.out, "index.html"), "w", encoding="utf-8") as f:
            f.write(html)
        with open(os.path.join(self.out, "report.json"), "w", encoding="utf-8") as f:
            json.dump({"total": len(entries), "anim_mapping": self.anim_mapping,
                       "failed": self.failed}, f, ensure_ascii=False, indent=1)


INDEX_TPL = """<!DOCTYPE html>
<html lang="zh-CN">
<head>
<meta charset="utf-8">
<title>Sword.apk 素材总览</title>
<style>
  :root{--bg:#14110d;--card:#221c15;--line:#3a3126;--fg:#e8dcc6;--dim:#9a8b74;--acc:#d9a94a}
  *{box-sizing:border-box}
  body{margin:0;background:var(--bg);color:var(--fg);font:13px/1.5 -apple-system,"PingFang SC",Helvetica,Arial,sans-serif}
  header{position:sticky;top:0;z-index:9;background:rgba(20,17,13,.96);border-bottom:1px solid var(--line);padding:10px 14px;display:flex;gap:10px;align-items:center;flex-wrap:wrap}
  h1{font-size:15px;margin:0;color:var(--acc);font-weight:600}
  select,input{background:var(--card);color:var(--fg);border:1px solid var(--line);border-radius:6px;padding:5px 8px;font-size:13px}
  input{width:220px}
  .grow{flex:1}
  .btn{cursor:pointer;user-select:none;background:var(--card);border:1px solid var(--line);border-radius:6px;padding:5px 10px}
  .btn:hover{border-color:var(--acc)}
  #grid{display:grid;grid-template-columns:repeat(auto-fill,minmax(110px,1fr));gap:8px;padding:12px}
  .card{background:var(--card);border:1px solid var(--line);border-radius:6px;overflow:hidden;text-decoration:none;color:var(--fg);display:block}
  .card:hover{border-color:var(--acc)}
  .thumb{height:96px;display:flex;align-items:center;justify-content:center;background:
    repeating-conic-gradient(#2a241c 0 25%,#211c16 0 50%) 0 0/16px 16px}
  .thumb img{max-width:100%;max-height:96px;image-rendering:pixelated}
  .nm{padding:4px 6px;font-size:11px;color:var(--fg);white-space:nowrap;overflow:hidden;text-overflow:ellipsis}
  .meta{padding:0 6px 5px;font-size:10px;color:#6f6353;white-space:nowrap;overflow:hidden;text-overflow:ellipsis}
  footer{position:sticky;bottom:0;background:rgba(20,17,13,.96);border-top:1px solid var(--line);padding:8px 14px;display:flex;gap:10px;align-items:center}
  .dim{color:var(--dim)}
</style>
</head>
<body>
<header>
  <h1>Sword.apk 素材总览</h1>
  <select id="cat"></select>
  <input id="q" placeholder="搜索名字 / 动画名…">
  <span class="dim" id="stat"></span>
  <span class="grow"></span>
  <span class="btn" id="prev">← 上一页</span>
  <span class="dim" id="page"></span>
  <span class="btn" id="next">下一页 →</span>
</header>
<div id="grid"></div>
<footer><span class="dim">缩略图按需加载；点击卡片在新标签页打开原文件</span></footer>
<script>
const DATA = __DATA__;
const PAGE = 240;
let view = DATA, page = 0;
const $ = id => document.getElementById(id);
const cats = [...new Set(DATA.map(d => d.c))];
$('cat').innerHTML = '<option value="">全部类别</option>' + cats.map(c =>
  `<option value="${c}">${c} (${DATA.filter(d => d.c === c).length})</option>`).join('');
function render(){
  const q = $('q').value.trim().toLowerCase();
  const c = $('cat').value;
  view = DATA.filter(d => (!c || d.c === c) &&
    (!q || d.n.toLowerCase().includes(q) || (d.g || '').toLowerCase().includes(q)));
  page = Math.min(page, Math.max(0, Math.ceil(view.length / PAGE) - 1));
  const s = view.slice(page * PAGE, page * PAGE + PAGE);
  $('grid').innerHTML = s.map(d => {
    const media = (d.c === 'AudioClip' || d.c === 'Video')
      ? `<div style="font-size:26px;color:#6f6353">${d.c === 'Video' ? '▶' : '♪'}</div>`
      : `<img loading="lazy" src="${encodeURI(d.p)}" alt="">`;
    return `<a class="card" href="${encodeURI(d.p)}" target="_blank" title="${d.p}">
      <div class="thumb">${media}</div>
      <div class="nm">${d.n}</div>
      <div class="meta">${d.g || (d.w ? d.w + '×' + d.h : d.c)}</div></a>`;
  }).join('');
  $('stat').textContent = view.length + ' 项';
  $('page').textContent = (page + 1) + ' / ' + Math.max(1, Math.ceil(view.length / PAGE));
  window.scrollTo(0, 0);
}
$('cat').onchange = () => { page = 0; render(); };
$('q').oninput = () => { page = 0; render(); };
$('prev').onclick = () => { if (page > 0) { page--; render(); } };
$('next').onclick = () => { if ((page + 1) * PAGE < view.length) { page++; render(); } };
render();
</script>
</body>
</html>
"""


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--apk", default=os.path.join(REPO, "Sword.apk"))
    ap.add_argument("--work", default="/tmp/sword_apk_data", help="APK 解包缓存目录")
    ap.add_argument("--out", default=os.path.join(REPO, "AssetDump"))
    ap.add_argument("--only", default="texture,sprite,audio,video")
    ap.add_argument("--anim", default="", help="只导出该前缀下的动画精灵，如 Stage/Stage00")
    ap.add_argument("--dummy-dll", default=find_dummy_dll(), help="il2cpp DummyDll 目录")
    a = ap.parse_args()

    cats = {c.strip() for c in a.only.split(",") if c.strip()}
    os.makedirs(a.out, exist_ok=True)
    d = Dumper(ensure_data(a.apk, a.work), a.out)

    if "texture" in cats or "sprite" in cats:
        d.dump_textures_and_sprites("texture" in cats, "sprite" in cats, a.anim, a.dummy_dll)
    if "audio" in cats:
        d.dump_audio()
    if "video" in cats:
        d.dump_video()

    d.write_index()
    log("完成：%d 个文件 → %s" % (len(d.entries), a.out))
    if d.failed:
        log("失败 %d 项，详见 report.json" % len(d.failed))


if __name__ == "__main__":
    main()
