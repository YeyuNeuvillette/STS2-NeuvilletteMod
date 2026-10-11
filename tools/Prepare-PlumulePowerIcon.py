from __future__ import annotations

import argparse
from pathlib import Path

import numpy as np
from PIL import Image


def extract_from_white(source: Path) -> Image.Image:
    rgb = np.asarray(Image.open(source).convert("RGB"), dtype=np.float32)
    height, width, _ = rgb.shape

    # The supplied image contains a faint disconnected generator mark in the lower-right.
    # Restrict extraction to the feather-staff region without changing any subject pixels.
    region = np.zeros((height, width), dtype=bool)
    region[int(height * 0.05) : int(height * 0.95), int(width * 0.20) : int(width * 0.78)] = True

    distance_from_white = 255.0 - rgb.min(axis=2)
    alpha = np.clip((distance_from_white - 8.0) / 24.0, 0.0, 1.0)
    alpha *= region

    # Remove the white matte from antialiased edge pixels to prevent a pale fringe in game.
    safe_alpha = np.maximum(alpha[..., None], 1.0 / 255.0)
    foreground = 255.0 + (rgb - 255.0) / safe_alpha
    foreground = np.clip(foreground, 0.0, 255.0)
    foreground[alpha == 0.0] = 0.0

    rgba = np.dstack((foreground, alpha[..., None] * 255.0)).astype(np.uint8)
    image = Image.fromarray(rgba, "RGBA")
    bounds = image.getchannel("A").getbbox()
    if bounds is None:
        raise RuntimeError("No foreground was extracted from the source image.")
    return image.crop(bounds)


def fit_square_touching_edge(subject: Image.Image, size: int) -> Image.Image:
    scale = min(size / subject.width, size / subject.height)
    resized_size = (
        max(1, round(subject.width * scale)),
        max(1, round(subject.height * scale)),
    )
    resized = subject.resize(resized_size, Image.Resampling.LANCZOS)
    alpha_bounds = resized.getchannel("A").getbbox()
    if alpha_bounds is None:
        raise RuntimeError("The resized icon unexpectedly has no visible pixels.")
    resized = resized.crop(alpha_bounds)

    canvas = Image.new("RGBA", (size, size), (0, 0, 0, 0))
    x = (size - resized.width) // 2
    y = (size - resized.height) // 2
    canvas.alpha_composite(resized, (x, y))
    return canvas


def main() -> None:
    parser = argparse.ArgumentParser()
    parser.add_argument("source", type=Path)
    parser.add_argument("output_roots", nargs="+", type=Path)
    args = parser.parse_args()

    subject = extract_from_white(args.source)
    for root in args.output_roots:
        small = root / "neuvillette_power_plumule_power.png"
        large = root / "big" / "neuvillette_power_plumule_power.png"
        small.parent.mkdir(parents=True, exist_ok=True)
        large.parent.mkdir(parents=True, exist_ok=True)
        fit_square_touching_edge(subject, 64).save(small, optimize=True)
        fit_square_touching_edge(subject, 256).save(large, optimize=True)


if __name__ == "__main__":
    main()
