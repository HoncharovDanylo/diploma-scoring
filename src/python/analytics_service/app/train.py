from __future__ import annotations
import logging
from pathlib import Path
from app.ml.train import train_and_save

def main() -> None:
    logging.basicConfig(level=logging.INFO)
    root = Path(__file__).resolve().parent.parent
    train_and_save(package_root=root)
if __name__ == '__main__':
    main()
