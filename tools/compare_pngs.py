from __future__ import annotations

from pathlib import Path


def alpha_info(path: Path):
    try:
        from PIL import Image
    except Exception as e:  # pragma: no cover
        raise SystemExit(f"Pillow (PIL) is required. Install: pip install pillow\n{e}")

    im = Image.open(path).convert("RGBA")
    a = im.getchannel("A")
    mn, mx = a.getextrema()
    hist = a.histogram()
    total = im.size[0] * im.size[1]
    return {
        "size": im.size,
        "alpha_min": mn,
        "alpha_max": mx,
        "transparent": hist[0],
        "opaque": hist[255],
        "total": total,
    }


def diff_bbox(path1: Path, path2: Path):
    from PIL import Image, ImageChops

    im1 = Image.open(path1).convert("RGBA")
    im2 = Image.open(path2).convert("RGBA")
    if im1.size != im2.size:
        return ("size_diff", im1.size, im2.size)
    diff = ImageChops.difference(im1, im2)
    return diff.getbbox()


def main():
    a = Path("Untitled.bcf.Assets")
    b = Path("Untitled.bcf.Assets_")

    if not a.exists() or not b.exists():
        raise SystemExit(f"Missing dirs: {a} or {b}")

    common = sorted([p.name for p in a.glob("*.png") if (b / p.name).exists()])
    print("common_count", len(common))

    for name in common[:10]:
        pa = a / name
        pb = b / name
        ia = alpha_info(pa)
        ib = alpha_info(pb)
        bbox = diff_bbox(pa, pb)
        print("\n" + name)
        print(" new", ia)
        print(" old", ib)
        print(" diff_bbox", bbox)


if __name__ == "__main__":
    main()
