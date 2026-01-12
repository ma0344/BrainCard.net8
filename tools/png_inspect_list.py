from __future__ import annotations

from pathlib import Path
import argparse
import struct


def parse_png(path: Path):
    data = path.read_bytes()
    if data[:8] != b"\x89PNG\r\n\x1a\n":
        raise ValueError(f"not png: {path}")

    pos = 8
    chunks: list[tuple[str, int, bytes]] = []
    while pos < len(data):
        ln = struct.unpack(">I", data[pos : pos + 4])[0]
        pos += 4
        typ = data[pos : pos + 4].decode("ascii")
        pos += 4
        payload = data[pos : pos + ln]
        pos += ln
        pos += 4
        chunks.append((typ, ln, payload))
        if typ == "IEND":
            break
    return chunks


def ihdr_info(chunks):
    for typ, ln, payload in chunks:
        if typ == "IHDR":
            w, h, bit, ct, comp, flt, inter = struct.unpack(">IIBBBBB", payload)
            return (w, h, bit, ct, comp, flt, inter)
    return None


def idat_total(chunks) -> int:
    return sum(ln for typ, ln, _ in chunks if typ == "IDAT")


def chunk_types(chunks):
    return [typ for typ, ln, _ in chunks if typ not in ("IDAT", "IEND")]


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
            chunks = parse_png(p)
            print(
                f" {label} ihdr={ihdr_info(chunks)} idat_bytes={idat_total(chunks)} chunks={chunk_types(chunks)}"
            )

    return 0


if __name__ == '__main__':
    raise SystemExit(main())
