from __future__ import annotations

from pathlib import Path
import hashlib
import os


def sha256(path: Path) -> str:
    h = hashlib.sha256()
    with path.open("rb") as f:
        for chunk in iter(lambda: f.read(1024 * 1024), b""):
            h.update(chunk)
    return h.hexdigest()


def main() -> int:
    a = Path(os.environ.get("PNG_DIR_NEW", "Untitled.bcf.Assets"))
    b = Path(os.environ.get("PNG_DIR_OLD", "Untitled.bcf.Assets_"))

    if not a.exists() or not b.exists():
        print(f"Missing dirs: {a} or {b}")
        return 2

    common = sorted([p.name for p in a.glob("*.png") if (b / p.name).exists()])
    print("common_count", len(common))

    for name in common[:20]:
        pa = a / name
        pb = b / name
        ha = sha256(pa)
        hb = sha256(pb)
        print(name, "same_bytes" if ha == hb else "diff_bytes", ha[:16], hb[:16])

    return 0


if __name__ == "__main__":
    raise SystemExit(main())
