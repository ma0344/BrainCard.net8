from __future__ import annotations

from pathlib import Path
import argparse
import struct
import zlib


def parse_png(path: Path):
    data = path.read_bytes()
    if data[:8] != b"\x89PNG\r\n\x1a\n":
        raise ValueError(f"not png: {path}")
    pos = 8
    chunks: list[tuple[str, bytes]] = []
    while pos < len(data):
        ln = struct.unpack(">I", data[pos : pos + 4])[0]
        pos += 4
        typ = data[pos : pos + 4].decode("ascii")
        pos += 4
        payload = data[pos : pos + ln]
        pos += ln
        pos += 4
        chunks.append((typ, payload))
        if typ == "IEND":
            break
    return chunks


def alpha_stats(path: Path):
    chunks = parse_png(path)
    w = h = bit = ct = None
    idat = b""
    for typ, payload in chunks:
        if typ == "IHDR":
            w, h, bit, ct, comp, flt, inter = struct.unpack(">IIBBBBB", payload)
        if typ == "IDAT":
            idat += payload

    if w is None or h is None:
        raise ValueError(f"missing IHDR: {path}")
    if bit != 8 or ct != 6:
        raise ValueError(f"unsupported format bit={bit} ct={ct}: {path}")

    raw = zlib.decompress(idat)
    stride = 1 + w * 4

    amin = 255
    amax = 0
    a0 = 0
    a255 = 0
    total = w * h

    for y in range(h):
        row = raw[y * stride : (y + 1) * stride]
        pix = row[1:]
        for x in range(w):
            a = pix[x * 4 + 3]
            if a < amin:
                amin = a
            if a > amax:
                amax = a
            if a == 0:
                a0 += 1
            if a == 255:
                a255 += 1

    return {
        "alpha_min": amin,
        "alpha_max": amax,
        "alpha0": a0,
        "alpha255": a255,
        "total": total,
    }


def main() -> int:
    ap = argparse.ArgumentParser()
    ap.add_argument('--new', required=True)
    ap.add_argument('--old', required=True)
    ap.add_argument('files', nargs='+')
    args = ap.parse_args()

    new_dir = Path(args.new)
    old_dir = Path(args.old)

    for name in args.files:
        print("\n" + name)
        for label, root in (("new", new_dir), ("old", old_dir)):
            p = root / name
            if not p.exists():
                print(f" {label} missing")
                continue
            s = alpha_stats(p)
            print(f" {label} {s}")

    return 0


if __name__ == '__main__':
    raise SystemExit(main())
