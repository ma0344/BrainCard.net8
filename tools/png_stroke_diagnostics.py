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
        ln = struct.unpack(">I", data[pos:pos+4])[0]
        pos += 4
        typ = data[pos:pos+4].decode("ascii")
        pos += 4
        payload = data[pos:pos+ln]
        pos += ln
        pos += 4
        chunks.append((typ, payload))
        if typ == "IEND":
            break
    return chunks


def load_rgba(path: Path):
    chunks = parse_png(path)
    w = h = bit = ct = None
    idat = b""
    for typ, payload in chunks:
        if typ == "IHDR":
            w, h, bit, ct, *_ = struct.unpack(">IIBBBBB", payload)
        if typ == "IDAT":
            idat += payload
    if w is None or h is None:
        raise ValueError("missing IHDR")
    if bit != 8 or ct != 6:
        raise ValueError(f"unsupported bit={bit} ct={ct}")
    raw = zlib.decompress(idat)
    stride = 1 + w * 4
    rgba = bytearray(w * h * 4)
    for y in range(h):
        row = raw[y*stride:(y+1)*stride]
        rgba[y*w*4:(y+1)*w*4] = row[1:]
    return w, h, rgba


def main() -> int:
    ap = argparse.ArgumentParser()
    ap.add_argument("png", help="path to PNG")
    ap.add_argument("--alpha-threshold", type=int, default=1)
    args = ap.parse_args()

    w, h, rgba = load_rgba(Path(args.png))
    thr = args.alpha_threshold

    nonzero = 0
    a255 = 0
    for i in range(3, len(rgba), 4):
        a = rgba[i]
        if a >= thr:
            nonzero += 1
        if a == 255:
            a255 += 1

    # quick proxy for "dashed" look: count transitions along rows
    transitions = 0
    for y in range(h):
        row = rgba[y*w*4:(y+1)*w*4]
        prev = 1 if row[3] >= thr else 0
        for x in range(1, w):
            cur = 1 if row[x*4+3] >= thr else 0
            if cur != prev:
                transitions += 1
            prev = cur

    print({
        "width": w,
        "height": h,
        "alpha_threshold": thr,
        "pixels_alpha_ge_thr": nonzero,
        "pixels_alpha_255": a255,
        "row_alpha_transitions": transitions,
    })
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
