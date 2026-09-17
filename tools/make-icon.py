#!/usr/bin/env python3
"""Draws the package and ribbon icons: the Markdown mark, a rounded outline around an M and a caret.

Kept as a script rather than a checked-in binary alone so the icons can be regenerated at another size or
colour without a drawing program. Run it from the repository root:

    python3 tools/make-icon.py
"""
import struct
import zlib

SIZE = 128
INK = (36, 41, 47, 255)      # the same near-black the light stylesheet uses for text
CLEAR = (0, 0, 0, 0)


def blank():
    return [[CLEAR for _ in range(SIZE)] for _ in range(SIZE)]


def fill_rect(px, x0, y0, x1, y1, colour=INK):
    for y in range(max(0, y0), min(SIZE, y1)):
        for x in range(max(0, x0), min(SIZE, x1)):
            px[y][x] = colour


def rounded_outline(px, x0, y0, x1, y1, radius, thickness):
    """A rounded rectangle border, drawn by keeping the ring between two rounded shapes."""
    def inside(x, y, ox0, oy0, ox1, oy1, r):
        if not (ox0 <= x < ox1 and oy0 <= y < oy1):
            return False
        for cx, cy in ((ox0 + r, oy0 + r), (ox1 - 1 - r, oy0 + r),
                       (ox0 + r, oy1 - 1 - r), (ox1 - 1 - r, oy1 - 1 - r)):
            # Only the corner squares are curved; the straight edges always count as inside.
            if ((x < ox0 + r or x > ox1 - 1 - r) and (y < oy0 + r or y > oy1 - 1 - r)
                    and (x < cx) == (cx == ox0 + r) and (y < cy) == (cy == oy0 + r)):
                if (x - cx) ** 2 + (y - cy) ** 2 > r * r:
                    return False
        return True

    t = thickness
    for y in range(SIZE):
        for x in range(SIZE):
            outer = inside(x, y, x0, y0, x1, y1, radius)
            inner = inside(x, y, x0 + t, y0 + t, x1 - t, y1 - t, max(0, radius - t))
            if outer and not inner:
                px[y][x] = INK


def draw_m(px, left, top, height, stroke):
    """The M: two uprights, two diagonals meeting in the middle."""
    width = int(height * 0.95)
    right = left + width
    bottom = top + height

    fill_rect(px, left, top, left + stroke, bottom)
    fill_rect(px, right - stroke, top, right, bottom)

    # The diagonals, stepped one row at a time from each upright down to the centre.
    for step in range(height // 2):
        y = top + step
        dx = int(step * (width / 2) / (height / 2))
        fill_rect(px, left + dx, y, left + dx + stroke, y + 1)
        fill_rect(px, right - dx - stroke, y, right - dx, y + 1)


def draw_caret(px, cx, top, height, stroke):
    """The downward arrow: a stem with a solid triangular head."""
    half = int(height * 0.30)
    fill_rect(px, cx - stroke // 2, top, cx - stroke // 2 + stroke, top + height - half)

    for row in range(half):
        span = int(half * 0.95 * (1 - row / half))
        y = top + height - half + row
        fill_rect(px, cx - span, y, cx + span + 1, y + 1)


def write_png(path, px):
    raw = b"".join(
        b"\x00" + b"".join(struct.pack("BBBB", *px[y][x]) for x in range(SIZE))
        for y in range(SIZE)
    )

    def chunk(tag, data):
        body = tag + data
        return struct.pack(">I", len(data)) + body + struct.pack(">I", zlib.crc32(body) & 0xFFFFFFFF)

    png = (b"\x89PNG\r\n\x1a\n"
           + chunk(b"IHDR", struct.pack(">IIBBBBB", SIZE, SIZE, 8, 6, 0, 0, 0))
           + chunk(b"IDAT", zlib.compress(raw, 9))
           + chunk(b"IEND", b""))

    with open(path, "wb") as handle:
        handle.write(png)
    print("wrote", path)


def main():
    px = blank()
    rounded_outline(px, 6, 22, SIZE - 6, SIZE - 22, radius=14, thickness=8)
    draw_m(px, left=24, top=44, height=40, stroke=8)
    draw_caret(px, cx=88, top=44, height=40, stroke=8)

    for path in ("src/Shaker.Markdown.Activities/package-icon.png",
                 "src/Shaker.Markdown.Activities.Wizard/Resources/markdown.png"):
        write_png(path, px)


if __name__ == "__main__":
    main()
