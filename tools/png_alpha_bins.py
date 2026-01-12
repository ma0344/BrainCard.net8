from __future__ import annotations

from pathlib import Path
import struct
import zlib


def parse_rgba(path: Path):
    data = path.read_bytes()
    pos = 8
    w = h = bit = ct = None
    idat = b""

    while pos < len(data):
        ln = struct.unpack(">I", data[pos : pos + 4])[0]
        pos += 4
        typ = data[pos : pos + 4].decode("ascii")
        pos += 4
        payload = data[pos : pos + ln]
        pos += ln
        pos += 4

        if typ == "IHDR":
            w, h, bit, ct, comp, flt, inter = struct.unpack(">IIBBBBB", payload)
        if typ == "IDAT":
            idat += payload
        if typ == "IEND":
            break

    if w is None or h is None:
        raise ValueError("missing IHDR")
    if bit != 8 or ct != 6:
        raise ValueError(f"unsupported bit={bit} ct={ct}")

    raw = zlib.decompress(idat)
    stride = 1 + w * 4
    return w, h, raw, stride


def alpha_hist(path: Path):
    w, h, raw, stride = parse_rgba(path)
    hist = [0] * 256
    for y in range(h):
        row = raw[y * stride : (y + 1) * stride]
        pix = row[1:]
        for x in range(w):
            a = pix[x * 4 + 3]
            hist[a] += 1
    return hist


def main():
    a = Path("Untitled.bcf.Assets")
    b = Path("Untitled.bcf.Assets_")

    common = sorted([p.name for p in a.glob("*.png") if (b / p.name).exists()])
    if not common:
        print("no common pngs")
        return

    name = common[0]
    ha = alpha_hist(a / name)
    hb = alpha_hist(b / name)

    def bins(h):
        return [(i, c) for i, c in enumerate(h) if c and i not in (0, 255)][:20]

    print("file", name)
    print("new bins", bins(ha))
    print("old bins", bins(hb))


if __name__ == "__main__":
    main()
