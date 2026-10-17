"""Report what a binary FBX actually contains: each object's transform and each mesh's size and facing.

Usage: python fbx_report.py MartialDisplay.fbx

Written while building the Martial Display, to see why tagged faces painted wrong. Valheim Heraldry Mod
reads a tagged face in its mesh's own coordinates (mesh +Y = up), so object rotation/scale and how the mesh
itself lies all matter.
"""
import struct
import sys
import zlib


def read(path):
    d = open(path, "rb").read()
    if not d.startswith(b"Kaydara FBX Binary"):
        sys.exit("Not a binary FBX (export as binary from Blender).")
    ver = struct.unpack_from("<I", d, 23)[0]
    big = ver >= 7500
    sentinel = 25 if big else 13

    def node(o):
        if big:
            end, nprops, _ = struct.unpack_from("<QQQ", d, o); o += 24
        else:
            end, nprops, _ = struct.unpack_from("<III", d, o); o += 12
        if end == 0:
            return None, o
        nl = d[o]; o += 1
        name = d[o:o + nl].decode(); o += nl
        props = []
        for _ in range(nprops):
            t = chr(d[o]); o += 1
            if t in "YCIFDL":
                fmt = {"Y": "<h", "C": "<?", "I": "<i", "F": "<f", "D": "<d", "L": "<q"}[t]
                props.append(struct.unpack_from(fmt, d, o)[0]); o += struct.calcsize(fmt)
            elif t in "SR":
                ln = struct.unpack_from("<I", d, o)[0]; o += 4
                v = d[o:o + ln]; o += ln
                props.append(v.decode("utf8", "replace") if t == "S" else v)
            else:
                ln, enc, cl = struct.unpack_from("<III", d, o); o += 12
                raw = d[o:o + cl]; o += cl
                if enc:
                    raw = zlib.decompress(raw)
                f = {"d": "d", "f": "f", "i": "i", "l": "q", "b": "?"}[t]
                props.append(list(struct.unpack("<%d%s" % (ln, f), raw)))
        kids = []
        while o < end - sentinel:
            k, o = node(o)
            if k is None:
                break
            kids.append(k)
        return (name, props, kids), end

    top, o = [], 27
    while o < len(d):
        n, o2 = node(o)
        if n is None:
            break
        top.append(n); o = o2
    return top


def kids(n, name):
    return [k for k in n[2] if k[0] == name]


def main(path):
    top = read(path)
    objs = [t for t in top if t[0] == "Objects"]
    if not objs:
        sys.exit("No objects in this FBX (exported with 'Selected Objects' and nothing selected?).")
    objs = objs[0]
    models = kids(objs, "Model")
    geoms = kids(objs, "Geometry")
    print(f"{len(models)} objects, {len(geoms)} meshes\n")
    for m in models:
        name = m[1][1].split("\x00")[0]
        p70 = kids(m, "Properties70")
        tr = {p[1][0]: [round(x, 3) for x in p[1][4:]] for p in (kids(p70[0], "P") if p70 else []) if p[1][0].startswith("Lcl")}
        warn = []
        if "Lcl Scaling" in tr and any(abs(s - 1) > 1e-3 for s in tr["Lcl Scaling"]):
            warn.append("SCALE NOT APPLIED")
        print(f"object {name:32s} position {tr.get('Lcl Translation', [0, 0, 0])}  rotation {tr.get('Lcl Rotation', [0, 0, 0])}  "
              f"scale {tr.get('Lcl Scaling', [1, 1, 1])}  {' '.join(warn)}")
    print()
    for g in geoms:
        name = g[1][1].split("\x00")[0]
        v = kids(g, "Vertices")[0][1][0]
        pts = [v[i:i + 3] for i in range(0, len(v), 3)]
        lo = [round(min(p[k] for p in pts), 3) for k in range(3)]
        hi = [round(max(p[k] for p in pts), 3) for k in range(3)]
        size = [round(h - l, 3) for l, h in zip(lo, hi)]
        normal = None
        ln = kids(g, "LayerElementNormal")
        if ln:
            nv = kids(ln[0], "Normals")[0][1][0]
            s = [sum(nv[i + k] for i in range(0, len(nv), 3)) for k in range(3)]
            mag = sum(x * x for x in s) ** 0.5
            count = len(nv) // 3
            agree = mag / count if count else 0
            normal = [round(x / mag, 3) for x in s] if mag else [0, 0, 0]
        print(f"mesh   {name:32s} size (own X,Y,Z) {size}  min {lo} max {hi}  average facing {normal}")


if __name__ == "__main__":
    main(sys.argv[1] if len(sys.argv) > 1 else "MartialDisplay.fbx")
