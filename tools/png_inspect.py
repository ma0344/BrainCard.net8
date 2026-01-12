from __future__ import annotations

from pathlib import Path
import os
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
        pos += 4  # crc
        chunks.append((typ, ln, payload))
        if typ == "IEND":
            break

    return chunks


def ihdr_info(chunks):
    for typ, ln, payload in chunks:
        if typ == "IHDR":
            w, h, bit, ct, comp, flt, inter = struct.unpack(">IIBBBBB", payload)
            return {
                "width": w,
                "height": h,
                "bit_depth": bit,
                "color_type": ct,
                "compression": comp,
                "filter": flt,
                "interlace": inter,
            }
    return None


def idat_total(chunks) -> int:
    return sum(ln for typ, ln, _ in chunks if typ == "IDAT")


def chunk_types(chunks):
    return [typ for typ, ln, _ in chunks if typ not in ("IDAT", "IEND")]


def main():
    a = Path(os.environ.get("PNG_DIR_NEW", "Untitled.bcf.Assets"))
    b = Path(os.environ.get("PNG_DIR_OLD", "Untitled.bcf.Assets_"))

    common = sorted([p.name for p in a.glob("*.png") if (b / p.name).exists()])
    print("common_count", len(common))

    for name in common[:5]:
        print("\n" + name)
        for label, root in (("new", a), ("old", b)):
            p = root / name
            chunks = parse_png(p)
            print(
                f" {label} ihdr={ihdr_info(chunks)} idat_bytes={idat_total(chunks)} chunks={chunk_types(chunks)}"
            )


if __name__ == "__main__":
    main()
