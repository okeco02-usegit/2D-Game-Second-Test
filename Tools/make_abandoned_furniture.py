"""Tạo bản "bỏ hoang" của bộ nội thất gốc trong References/horror_school_furnitures.

Giữ nguyên tọa độ sprite của hai sheet gốc (Classrooms ở trên, corridor school ở dưới, từ y=256)
rồi làm cũ: ngả màu tối lạnh, ố bẩn, gỉ sét kim loại, mốc xanh, vết xước, bụi và sứt mẻ.

Chạy:  python Tools/make_abandoned_furniture.py   (cần Pillow)
"""
import colorsys
import os
import random
from collections import deque

from PIL import Image

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
SRC = os.path.join(ROOT, "References", "horror_school_furnitures")
OUT = os.path.join(ROOT, "Assets", "SchoolHorror", "Resources", "FurnitureAbandoned.png")

RUST = (104, 52, 34)
RUST_DARK = (62, 30, 24)
MOLD = (44, 60, 40)
MOLD_DARK = (28, 38, 30)
DUST = (128, 122, 130)
DRIED_BLOOD = (84, 14, 18)


def mix(a, b, t):
    return tuple(int(round(a[i] + (b[i] - a[i]) * t)) for i in range(3))


def value_noise(x, y, cell, seed):
    """Nhiễu mượt 0..1 (nội suy song tuyến tính trên lưới ô `cell` px)."""
    def corner(cx, cy):
        return random.Random(cx * 73856093 ^ cy * 19349663 ^ seed).random()
    fx, fy = x / cell, y / cell
    x0, y0 = int(fx), int(fy)
    tx, ty = fx - x0, fy - y0
    top = corner(x0, y0) * (1 - tx) + corner(x0 + 1, y0) * tx
    bottom = corner(x0, y0 + 1) * (1 - tx) + corner(x0 + 1, y0 + 1) * tx
    return top * (1 - ty) + bottom * ty


def components(px, w, h):
    """Các cụm điểm ảnh liền nhau = từng món đồ."""
    seen = set()
    for y in range(h):
        for x in range(w):
            if px[x, y][3] == 0 or (x, y) in seen:
                continue
            queue, cells = deque([(x, y)]), []
            seen.add((x, y))
            while queue:
                a, b = queue.popleft()
                cells.append((a, b))
                for da in (-1, 0, 1):
                    for db in (-1, 0, 1):
                        c, d = a + da, b + db
                        if 0 <= c < w and 0 <= d < h and (c, d) not in seen and px[c, d][3] > 0:
                            seen.add((c, d))
                            queue.append((c, d))
            yield cells


def kind_of(rgb, x, y):
    r, g, b = rgb
    h, s, v = colorsys.rgb_to_hsv(r / 255, g / 255, b / 255)
    if r > 110 and g < 70 and b < 70:
        return "blood"
    # Tường sáng (góc trên-trái) và bảng trắng không phải kim loại
    if (x < 96 and y < 48) or (115 <= x <= 155 and 200 <= y <= 242):
        return "plaster"
    if s < 0.16 and 0.45 < v < 0.88:
        return "metal"
    if 0.02 < h < 0.13 and s > 0.2:
        return "wood"
    return "other"


def grade(rgb, kind):
    """Ngả màu: giảm bão hòa, tối đi và hơi ám tím lạnh cho hợp bảng màu của game."""
    r, g, b = rgb
    lum = 0.3 * r + 0.59 * g + 0.11 * b
    if kind == "blood":
        return mix(DRIED_BLOOD, (r, g, b), 0.25)
    keep = {"wood": 0.62, "metal": 0.5, "plaster": 0.35}.get(kind, 0.42)
    r, g, b = (lum + (c - lum) * keep for c in (r, g, b))
    dark = {"wood": 0.92, "metal": 0.62, "plaster": 0.58}.get(kind, 0.66)
    return (int(r * dark * 0.98), int(g * dark * 0.94), int(b * dark * 1.04))


def age(sheet, seed, y_offset=0):
    img = sheet.copy()
    px = img.load()
    w, h = img.size
    src = sheet.load()
    rng = random.Random(seed)

    for cells in components(src, w, h):
        y0 = min(c[1] for c in cells)
        y1 = max(c[1] for c in cells)
        height = y1 - y0 + 1
        big = len(cells) >= 60
        solid = set(cells)
        kinds = {c: kind_of(src[c][:3], c[0], c[1] + y_offset) for c in cells}

        for (x, y) in cells:
            kind = kinds[(x, y)]
            color = grade(src[x, y][:3], kind)

            # Ố bẩn loang lổ + bụi bẩn dồn xuống chân đồ vật
            grime = 0.72 + 0.36 * value_noise(x, y, 5, seed)
            if big:
                grime *= 1.0 - 0.28 * max(0.0, (y - y0) / height - 0.55) / 0.45
            color = tuple(int(c * grime) for c in color)

            if kind == "metal":  # gỉ sét ăn từng mảng
                rust = value_noise(x, y, 3, seed + 7)
                if rust > 0.5:
                    color = mix(color, RUST if rust < 0.72 else RUST_DARK, min(1.0, (rust - 0.5) * 3.2))

            # Bụi phủ trên các mép nằm ngang phía trên
            if big and (x, y - 1) not in solid and rng.random() < 0.45:
                color = mix(color, DUST, 0.35)

            px[x, y] = color + (src[x, y][3],)

        if not big:
            continue
        wood = [c for c in cells if kinds[c] == "wood"]
        paint = [c for c in cells if kinds[c] in ("wood", "other", "metal", "plaster")]

        # Mốc xanh: các đốm loang, ưu tiên nửa dưới
        for _ in range(len(cells) // 220 + (1 if rng.random() < 0.6 else 0)):
            x, y = rng.choice(paint)
            if y < y0 + height * 0.35 and rng.random() < 0.7:
                continue
            for _ in range(rng.randint(4, 11)):
                if (x, y) in solid:
                    px[x, y] = mix(px[x, y][:3], MOLD if rng.random() < 0.6 else MOLD_DARK, 0.75) + (px[x, y][3],)
                x += rng.choice((-1, 0, 1))
                y += rng.choice((-1, 0, 1))

        # Vết xước sáng/tối trên gỗ
        for _ in range(len(wood) // 90):
            x, y = rng.choice(wood)
            dx, dy = rng.choice(((1, 0), (1, 1), (1, -1), (0, 1)))
            light = rng.random() < 0.5
            for _ in range(rng.randint(2, 5)):
                if (x, y) in solid and kinds[(x, y)] == "wood":
                    c = px[x, y]
                    f = 1.3 if light else 0.6
                    px[x, y] = tuple(min(255, int(v * f)) for v in c[:3]) + (c[3],)
                x += dx
                y += dy

        # Vệt gỉ/nước bẩn chảy dọc trên đồ kim loại lớn (tủ locker, máy bán hàng)
        metal_share = sum(1 for c in cells if kinds[c] == "metal") / len(cells)
        if metal_share > 0.25 or (y_offset >= 256 and height >= 36):
            for _ in range(max(2, len(cells) // 260)):
                x, y = rng.choice(cells)
                for _ in range(rng.randint(4, 14)):
                    if (x, y) not in solid:
                        break
                    px[x, y] = mix(px[x, y][:3], RUST_DARK, 0.5) + (px[x, y][3],)
                    y += 1

        # Vệt máu khô nhỏ giọt li ti trên một số bàn/ghế/tủ gỗ
        if wood and rng.random() < 0.45:
            bx, by = rng.choice(wood)
            for _ in range(rng.randint(2, 6)):
                if (bx, by) in solid:
                    px[bx, by] = mix(px[bx, by][:3], DRIED_BLOOD, 0.72) + (px[bx, by][3],)
                bx += rng.choice((-1, 0, 1))
                by += rng.choice((0, 1))

        # Sứt mẻ ở mép
        for (x, y) in cells:
            edge = any((x + dx, y + dy) not in solid for dx, dy in ((1, 0), (-1, 0), (0, -1)))
            if edge and rng.random() < 0.035:
                px[x, y] = (0, 0, 0, 0)

    return img


def main():
    classrooms = Image.open(os.path.join(SRC, "Classrooms.png")).convert("RGBA")
    corridor = Image.open(os.path.join(SRC, "corridor school.png")).convert("RGBA")
    atlas = Image.new("RGBA", (512, 512), (0, 0, 0, 0))
    atlas.paste(age(classrooms, 11, 0), (0, 0))
    atlas.paste(age(corridor, 23, 256), (0, 256))
    os.makedirs(os.path.dirname(OUT), exist_ok=True)
    atlas.save(OUT)
    print("Đã ghi", OUT)


if __name__ == "__main__":
    main()
