from __future__ import annotations

from pathlib import Path
import argparse
import hashlib


def sha256(path: Path) -> str:
    h = hashlib.sha256()
    with path.open('rb') as f:
        for chunk in iter(lambda: f.read(1024 * 1024), b''):
            h.update(chunk)
    return h.hexdigest()


def main() -> int:
    ap = argparse.ArgumentParser()
    ap.add_argument('--new', required=True)
    ap.add_argument('--old', required=True)
    ap.add_argument('files', nargs='+')
    args = ap.parse_args()

    new_dir = Path(args.new)
    old_dir = Path(args.old)

    for name in args.files:
        pa = new_dir / name
        pb = old_dir / name
        if not pa.exists() or not pb.exists():
            print(name, 'missing', str(pa.exists()), str(pb.exists()))
            continue
        ha = sha256(pa)
        hb = sha256(pb)
        print(name, 'same_bytes' if ha == hb else 'diff_bytes', pa.stat().st_size, pb.stat().st_size)

    return 0


if __name__ == '__main__':
    raise SystemExit(main())
