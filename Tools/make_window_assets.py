"""Trích xuất sprite cửa sổ từ left_window.jpg, closed_window.jpg, open_window.jpg
(tạo 6 frame animation 2 cánh cửa sổ đung đưa đóng-mở trước gió) và tạo bộ background
trời đêm + mây + mặt trăng chi tiết (có hố mặt trăng & quầng sáng) + rừng cây pixel art
rậm rạp nhiều tầng lớp (thông nhọn, sồi tròn, cành cây và lá bắt sáng ánh trăng) đúng chuẩn pixel art.
"""
import math
import os
import random
from PIL import Image

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
SRC = os.path.join(ROOT, "References", "Plan_for_new_update")
RES = os.path.join(ROOT, "Assets", "SchoolHorror", "Resources")

WOOD_DARK = (42, 26, 24, 255)
WOOD_MID = (76, 48, 40, 255)
WOOD_LIGHT = (104, 68, 56, 255)
GLASS_CLOSED = (24, 36, 58, 95)
GLASS_WING = (30, 46, 72, 135)


def is_wood(r, g, b):
    if r > 150 and g > 140 and b > 115:
        return False
    return r > b + 6 and r >= g and r > 30


def extract_left_window():
    """Trích xuất left_window (khung gỗ nghiêng + mặt kính bán trong suốt) kích thước 16x28."""
    im = Image.open(os.path.join(SRC, "left_window.jpg")).convert("RGB")
    crop = im.crop((70, 0, 154, 316)).resize((14, 28), Image.NEAREST)
    out = Image.new("RGBA", (16, 28), (0, 0, 0, 0))
    px = crop.load()
    dst = out.load()
    for y in range(28):
        for x in range(14):
            r, g, b = px[x, y]
            if is_wood(r, g, b):
                dst[x + 1, y] = (int(r * 0.88), int(g * 0.84), int(b * 0.88), 255)
            elif b > 36 and 3 <= x <= 11 and 2 <= y <= 25:
                alpha = 155 if b > 62 else 110
                dst[x + 1, y] = (r, g, b, alpha)
    return out


def build_arched_base(include_inner_cross):
    """Dựng khung cửa sổ vòm 32x34 đặt giữa ô 48x34 từ closed_window.jpg."""
    closed_im = Image.open(os.path.join(SRC, "closed_window.jpg")).convert("RGB")
    W, H = 32, 34
    crop = closed_im.crop((37, 53, 435, 590)).resize((W, H), Image.NEAREST)
    cpx = crop.load()

    inside = [[False] * W for _ in range(H)]
    for y in range(H):
        wood_xs = [x for x in range(W) if is_wood(*cpx[x, y])]
        if len(wood_xs) >= 2:
            x0, x1 = min(wood_xs), max(wood_xs)
            for x in range(x0, x1 + 1):
                inside[y][x] = True

    out = Image.new("RGBA", (48, H), (0, 0, 0, 0))
    dst = out.load()
    ox = 8
    for y in range(H):
        for x in range(W):
            r, g, b = cpx[x, y]
            is_border_or_arch = (x <= 2 or x >= W - 3 or y >= H - 3 or y <= 10)
            if is_wood(r, g, b) and (include_inner_cross or is_border_or_arch):
                dst[ox + x, y] = (int(r * 0.88), int(g * 0.84), int(b * 0.88), 255)
            elif inside[y][x]:
                if include_inner_cross or y <= 9:
                    dst[ox + x, y] = GLASS_CLOSED

    if include_inner_cross:
        for y in range(2, H - 2):
            dst[ox + 15, y] = WOOD_MID
            dst[ox + 16, y] = WOOD_DARK
        for x in range(2, W - 2):
            dst[ox + x, 9] = WOOD_MID
            dst[ox + x, 10] = WOOD_DARK
            dst[ox + x, 20] = WOOD_MID
            dst[ox + x, 21] = WOOD_DARK
    else:
        for x in range(2, W - 2):
            dst[ox + x, 9] = WOOD_MID
            dst[ox + x, 10] = WOOD_DARK

    return out


def draw_casement_wing(dst, hinge_x, dir_sign, dx_reach, dy_perspective):
    """Vẽ 1 cánh cửa sổ hình thang có khung gỗ, đố ngang và kính."""
    y_top0, y_bot0 = 10, 31
    mid_y0 = 20
    steps = max(2, abs(dx_reach))
    for i in range(steps + 1):
        t = i / steps
        x = hinge_x + int(round(i * dir_sign))
        if not (0 <= x < 48):
            continue
        yt = int(round(y_top0 + dy_perspective * t))
        yb = int(round(y_bot0 - dy_perspective * t * 0.6))
        ym = int(round(mid_y0 + dy_perspective * t * 0.2))
        for y in range(yt, yb + 1):
            if not (0 <= y < 34):
                continue
            is_frame = (
                i == 0
                or i == steps
                or y <= yt + 1
                or y >= yb - 1
                or abs(y - ym) <= 0
            )
            if is_frame:
                dst[x, y] = WOOD_LIGHT if (y == yt or i == steps) else WOOD_MID
            else:
                dst[x, y] = GLASS_WING


def extract_closed_and_open_frames():
    closed_out = build_arched_base(include_inner_cross=True)
    open_base = build_arched_base(include_inner_cross=False)

    configs = [
        (-1, 7, -2.5),  # Frame 0: mở rộng ra ngoài
        (-1, 5, -2.0),  # Frame 1: hơi khép lại
        (-1, 3, -1.2),  # Frame 2: vuông góc bản lề
        (+1, 5, +1.5),  # Frame 3: gió đẩy khép vào trong
        (+1, 9, +1.0),  # Frame 4: khép gần sát nhau
        (+1, 4, +1.4),  # Frame 5: mở ngược trở ra
    ]
    frames = []
    ox, W = 8, 32
    for dir_sign, reach, persp in configs:
        fr = open_base.copy()
        fdst = fr.load()
        draw_casement_wing(fdst, ox + 2, dir_sign, reach, persp)
        draw_casement_wing(fdst, ox + W - 3, -dir_sign, reach, persp)
        frames.append(fr)

    return closed_out, frames


def value_noise(x, y, scale, seed):
    def h(ix, iy):
        n = (ix * 374761393 + iy * 668265263 + seed * 1442695041) & 0xFFFFFFFF
        n = ((n ^ (n >> 13)) * 1274126177) & 0xFFFFFFFF
        return ((n ^ (n >> 16)) & 0xFFFF) / 65535.0

    fx, fy = x / scale, y / scale
    x0, y0 = int(math.floor(fx)), int(math.floor(fy))
    tx, ty = fx - x0, fy - y0
    tx = tx * tx * (3 - 2 * tx)
    ty = ty * ty * (3 - 2 * ty)
    a = h(x0, y0) * (1 - tx) + h(x0 + 1, y0) * tx
    b = h(x0, y0 + 1) * (1 - tx) + h(x0 + 1, y0 + 1) * tx
    return a * (1 - ty) + b * ty


def draw_detailed_moon(spx, W, H, mx, my, R):
    """Vẽ mặt trăng pixel-art cực kỳ chi tiết với các hố trăng (maria), viền sáng và quầng hào quang mềm."""
    # 1. Quầng hào quang tỏa sáng (atmospheric halo)
    glow_r = int(R * 2.8)
    for dy in range(-glow_r, glow_r + 1):
        for dx in range(-glow_r, glow_r + 1):
            px_x, px_y = mx + dx, my + dy
            if not (0 <= px_x < W and 0 <= px_y < H):
                continue
            d = math.sqrt(dx * dx + dy * dy)
            if d > R and d <= glow_r:
                t = (d - R) / float(glow_r - R)
                # Dither nhẹ quầng sáng theo pixel
                dither = ((px_x * 3 + px_y * 7) % 5) * 0.04
                alpha_factor = max(0.0, 1.0 - t - dither) * 0.45
                old_r, old_g, old_b, _ = spx[px_x, px_y]
                nr = min(255, int(old_r + (115 - old_r) * alpha_factor))
                ng = min(255, int(old_g + (145 - old_g) * alpha_factor))
                nb = min(255, int(old_b + (195 - old_b) * alpha_factor))
                spx[px_x, px_y] = (nr, ng, nb, 255)

    # 2. Đĩa mặt trăng và các hố biển trăng (Lunar Maria)
    for dy in range(-R, R + 1):
        for dx in range(-R, R + 1):
            px_x, px_y = mx + dx, my + dy
            if not (0 <= px_x < W and 0 <= px_y < H):
                continue
            d = math.sqrt(dx * dx + dy * dy)
            if d <= R:
                # Địa hình biển mặt trăng
                # - Mare Imbrium (hố tròn lớn phía trên trái)
                imbrium = (dx + 6) ** 2 + (dy + 4) ** 2 < 38
                imbrium_rim = (dx + 6) ** 2 + (dy + 4) ** 2 >= 38 and (dx + 6) ** 2 + (dy + 4) ** 2 <= 50 and dx < -6
                # - Oceanus Procellarum (vùng rộng mép tây)
                procellarum = (dx + 12) ** 2 + (dy - 2) ** 2 < 26 or (dx + 10) ** 2 + (dy + 9) ** 2 < 20
                # - Mare Serenitatis & Tranquillitatis (phía trung-đông)
                serenitatis = (dx - 4) ** 2 + (dy + 3) ** 2 < 22
                tranquillitatis = (dx - 7) ** 2 + (dy - 5) ** 2 < 28
                # - Mare Crisium (hố bầu dục biệt lập mép đông)
                crisium = (dx - 14) ** 2 + (dy + 1) ** 2 < 12
                # - Hố Tycho ở cực nam và tia sáng tỏa ra (rays)
                tycho = (dx + 1) ** 2 + (dy - 13) ** 2 < 6
                tycho_shadow = (dx + 1) ** 2 + (dy - 13) ** 2 < 3
                tycho_rays = abs(dx - dy * 0.18) <= 0.8 and dy < -8

                # Viền trăng đón sáng (trăng tròn đón sáng góc đông-bắc)
                is_limb_bright = d > R - 1.4

                if tycho_shadow:
                    col = (135, 126, 114, 255)
                elif tycho or imbrium_rim or is_limb_bright or tycho_rays:
                    col = (248, 244, 230, 255)  # viền đón sáng chói
                elif imbrium or serenitatis or tranquillitatis or procellarum or crisium:
                    # Vân đá bên trong các biển trăng
                    sub_noise = ((px_x * 11 + px_y * 17) % 7)
                    if sub_noise == 0:
                        col = (148, 138, 124, 255)
                    elif sub_noise <= 2:
                        col = (168, 158, 142, 255)
                    else:
                        col = (184, 175, 158, 255)
                else:
                    # Vùng cao nguyên sáng (Highlands)
                    high_noise = ((px_x * 7 + px_y * 13) % 5)
                    if high_noise == 0:
                        col = (222, 214, 194, 255)
                    else:
                        col = (238, 232, 216, 255)

                spx[px_x, px_y] = col


def build_parallax_layers():
    """Tạo 2 lớp Parallax (Bầu trời đêm + mây + trăng chi tiết, và Rừng cây rậm) đúng style pixel art."""
    W, H = 640, 360
    sky = Image.new("RGBA", (W, H), (14, 18, 30, 255))
    spx = sky.load()
    rng = random.Random(42)

    # 1. Nền bầu trời đêm xanh thẫm chuyển màu mượt mà
    for y in range(H):
        t = y / (H - 1)
        r = int(13 + 12 * t)
        g = int(18 + 16 * t)
        b = int(32 + 24 * t)
        for x in range(W):
            spx[x, y] = (r, g, b, 255)

    # 2. Các dải mây đêm mềm lững lờ trôi ngang bầu trời
    for y in range(H):
        for x in range(W):
            n = (
                value_noise(x * 0.7 + y * 0.3, y * 2.2, 32.0, 11) * 0.65
                + value_noise(x * 1.2, y * 1.6, 14.0, 29) * 0.35
            )
            if n > 0.68:
                spx[x, y] = (56, 76, 110, 255)
            elif n > 0.61:
                spx[x, y] = (42, 58, 88, 255)
            elif n > 0.54:
                spx[x, y] = (28, 42, 66, 255)

    # 3. Các ngôi sao pixel lấp lánh (sao điểm, sao sáng và sao chữ thập)
    for _ in range(160):
        sx = rng.randint(0, W - 1)
        sy = rng.randint(6, H - 25)
        roll = rng.random()
        if roll < 0.12:
            # Ngôi sao 4 cánh chữ thập lấp lánh
            spx[sx, sy] = (245, 250, 255, 255)
            if sx > 0: spx[sx - 1, sy] = (160, 180, 220, 220)
            if sx < W - 1: spx[sx + 1, sy] = (160, 180, 220, 220)
            if sy > 0: spx[sx, sy - 1] = (160, 180, 220, 220)
            if sy < H - 1: spx[sx, sy + 1] = (160, 180, 220, 220)
        elif roll < 0.45:
            spx[sx, sy] = (210, 225, 248, 255)
        else:
            spx[sx, sy] = (130, 148, 180, 255)

    # Chòm sao Bắc Đẩu / sao sáng góc trên bên phải (nhìn qua cửa sổ góc phải)
    constellation = [(475, 45), (488, 52), (502, 60), (518, 68), (512, 85), (535, 87), (540, 70)]
    for cx, cy in constellation:
        spx[cx, cy] = (250, 252, 255, 255)
        if cx > 0: spx[cx - 1, cy] = (180, 200, 235, 255)
        if cx < W - 1: spx[cx + 1, cy] = (180, 200, 235, 255)
        if cy > 0: spx[cx, cy - 1] = (180, 200, 235, 255)
        if cy < H - 1: spx[cx, cy + 1] = (180, 200, 235, 255)

    # 4. Mặt trăng tròn chi tiết ở góc trên bên trái (nhìn qua cửa sổ x=2..3)
    draw_detailed_moon(spx, W, H, 138, 52, 21)

    # ==============================================================
    # LỚP 2: RỪNG CÂY ĐÊM NHIỀU TẦNG LỚP (Cây thông búp nhọn & cây sồi vòm lá)
    # ==============================================================
    trees = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    tpx = trees.load()

    # Màu tán lá pixel-art (nhìn dưới ánh trăng)
    LEAF_MOON_HIGH = (52, 82, 92, 255)   # đỉnh lá bắt sáng ánh trăng
    LEAF_LIGHT = (38, 62, 70, 255)       # mặt trên tán lá
    LEAF_MID = (24, 40, 48, 255)         # thân tán lá
    LEAF_DARK = (14, 24, 30, 255)        # hốc tối giữa các nhánh lá
    LEAF_DEEP = (8, 14, 18, 255)         # bóng tối dưới cùng
    TRUNK_COL = (16, 12, 14, 255)        # thân gỗ gồ ghề

    def draw_pine_tree(root_x, root_y, height, width):
        """Vẽ cây thông hình tháp nhọn từng tầng búp tán đón ánh trăng từ trên-trái."""
        tiers = 7
        trunk_w = max(2, int(width * 0.14))
        # Thân cây gỗ
        for y in range(max(0, root_y - height // 2), min(H, root_y)):
            for x in range(root_x - trunk_w // 2, root_x + trunk_w // 2 + 1):
                if 0 <= x < W:
                    tpx[x, y] = TRUNK_COL

        top_y = root_y - height
        for t in range(tiers):
            t_frac = (t + 1) / float(tiers)
            t_h = int(height * (0.24 + t_frac * 0.14))
            t_w = int(width * (0.28 + t_frac * 0.72))
            tier_base_y = int(top_y + height * t_frac)
            tier_top_y = tier_base_y - t_h

            for y in range(tier_top_y, tier_base_y):
                if not (0 <= y < H): continue
                prog = (y - tier_top_y) / float(max(1, tier_base_y - tier_top_y))
                span = int(t_w * prog * 0.5)
                # Đuôi cành hơi cong rủ xuống ở mép ngoài
                drop = int(prog * prog * 3)
                for x in range(root_x - span, root_x + span + 1):
                    if not (0 <= x < W): continue
                    py = y + (drop if abs(x - root_x) > span - 3 else 0)
                    if not (0 <= py < H): continue

                    # Mép răng cưa kim thông
                    if abs(x - root_x) == span and (x + py) % 3 == 0:
                        continue

                    # Ánh sáng đón từ góc trên bên trái (hướng trăng)
                    is_rim_top = (y == tier_top_y or abs(x - root_x) >= span - 2)
                    is_bottom_shadow = y >= tier_base_y - 2
                    is_left_face = x <= root_x

                    if is_bottom_shadow:
                        c = LEAF_DEEP
                    elif is_rim_top and is_left_face:
                        c = LEAF_MOON_HIGH if y < tier_top_y + 3 else LEAF_LIGHT
                    elif is_rim_top:
                        c = LEAF_LIGHT if (x + y) % 2 == 0 else LEAF_MID
                    elif (x * 7 + py * 13) % 4 == 0:
                        c = LEAF_MID
                    else:
                        c = LEAF_DARK

                    tpx[x, py] = c

    def draw_oak_crown(center_x, center_y, radius_x, radius_y):
        """Vẽ vòm tán cây sồi tròn xum xuê gồm nhiều cụm lá cầu (foliage puffs)."""
        puffs = [
            (0, 0, 1.0),
            (-int(radius_x * 0.45), -int(radius_y * 0.25), 0.75),
            (int(radius_x * 0.45), -int(radius_y * 0.2), 0.72),
            (-int(radius_x * 0.3), int(radius_y * 0.28), 0.68),
            (int(radius_x * 0.35), int(radius_y * 0.3), 0.7),
            (0, -int(radius_y * 0.45), 0.65),
        ]
        for ox, oy, scale in puffs:
            cx = center_x + ox
            cy = center_y + oy
            rx = int(radius_x * scale)
            ry = int(radius_y * scale)
            for dy in range(-ry, ry + 1):
                for dx in range(-rx, rx + 1):
                    px = cx + dx
                    py = cy + dy
                    if not (0 <= px < W and 0 <= py < H):
                        continue
                    d = (dx * dx) / float(max(1, rx * rx)) + (dy * dy) / float(max(1, ry * ry))
                    if d <= 1.0:
                        # Răng cưa viền lá
                        if d > 0.85 and (px * 3 + py * 5) % 4 == 0:
                            continue

                        # Đón ánh trăng từ trên-trái
                        is_high = dy < -ry * 0.3 and dx < rx * 0.1
                        is_shadow = dy > ry * 0.35

                        if is_shadow:
                            c = LEAF_DEEP
                        elif is_high and d < 0.65:
                            c = LEAF_MOON_HIGH
                        elif is_high or d < 0.5:
                            c = LEAF_LIGHT
                        elif d < 0.82:
                            c = LEAF_MID
                        else:
                            c = LEAF_DARK

                        tpx[px, py] = c

    # 1. Tầng rừng xa phía sau: rặng thông silhouette sẫm màu chạy suốt chân trời
    for x in range(0, W, 14):
        h = int(120 + rng.randint(0, 35) + math.sin(x * 0.02) * 20)
        draw_pine_tree(x, 340, h, 28)

    # 2. Tầng rừng giữa: các cây thông và sồi lớn tạo hình thể hùng vĩ
    trees_layout = [
        # Khung quanh cửa sổ góc trên bên trái (x=2..3, vị trí texture ~135)
        (75, 340, 190, 68, "pine"),
        (115, 350, 165, 58, "pine"),
        (165, 345, 175, 62, "pine"),
        (195, 350, 150, 54, "pine"),
        (235, 360, 55, 42, "oak"),
        # Khung quanh cửa sổ giữa (x=25..26, vị trí texture ~320)
        (275, 340, 185, 64, "pine"),
        (315, 350, 68, 52, "oak"),
        (355, 345, 205, 72, "pine"),
        (395, 355, 60, 48, "oak"),
        # Khung quanh cửa sổ góc trên bên phải (x=47..48, vị trí texture ~480)
        (435, 340, 195, 66, "pine"),
        (475, 350, 170, 60, "pine"),
        (515, 345, 210, 74, "pine"),
        (555, 350, 64, 50, "oak"),
        (595, 340, 185, 64, "pine"),
        (630, 345, 170, 58, "pine"),
        (25, 340, 180, 62, "pine"),
    ]

    for root_x, root_y, p1, p2, kind in trees_layout:
        if kind == "pine":
            draw_pine_tree(root_x, root_y, p1, p2)
        else:
            draw_oak_crown(root_x, root_y - p1, p1, p2)

    # 3. Lớp tán lá tiền cảnh rủ xuống ở góc trên ngoài (framing chi tiết)
    # Đặc biệt cành thông vươn ngang dưới mặt trăng (x ~ 100..165, y ~ 85..130)
    for y in range(H):
        for x in range(W):
            # Tạo thảm cây rậm rạp che kín phần chân rừng (y >= 260)
            if y >= 250:
                dense_noise = value_noise(x, y, 9.0, 311)
                if dense_noise > 0.42:
                    current = tpx[x, y]
                    if current[3] == 0 or current[0] < 18:
                        tpx[x, y] = LEAF_DARK if dense_noise > 0.58 else LEAF_DEEP

    return sky, trees


def build_window_masks():
    """Tạo sprite mặt nạ (SpriteMask) chuẩn xác từng pixel cho cửa sổ:
    - arched_mask (48x34): chỉ những pixel bên trong vòm kính mới là màu trắng đục (255,255,255,255),
      toàn bộ khung gỗ và khoảng trống ngoài vòm là hoàn toàn trong suốt (0,0,0,0).
    - left_mask (16x28): chỉ phần kính trong suốt (x=4..12, y=2..25) là màu trắng đục.
    """
    closed_im = Image.open(os.path.join(SRC, "closed_window.jpg")).convert("RGB")
    W, H = 32, 34
    crop = closed_im.crop((37, 53, 435, 590)).resize((W, H), Image.NEAREST)
    cpx = crop.load()

    inside = [[False] * W for _ in range(H)]
    for y in range(H):
        wood_xs = [x for x in range(W) if is_wood(*cpx[x, y])]
        if len(wood_xs) >= 2:
            x0, x1 = min(wood_xs), max(wood_xs)
            for x in range(x0, x1 + 1):
                inside[y][x] = True

    arched_mask = Image.new("RGBA", (48, 34), (0, 0, 0, 0))
    adst = arched_mask.load()
    ox = 8
    for y in range(H):
        for x in range(W):
            is_outer_border = (x <= 2 or x >= W - 3 or y >= H - 2 or y <= 1)
            if inside[y][x] and not is_outer_border:
                adst[ox + x, y] = (255, 255, 255, 255)

    left_mask = Image.new("RGBA", (16, 28), (0, 0, 0, 0))
    ldst = left_mask.load()
    for y in range(2, 26):
        for x in range(3, 12):
            ldst[x + 1, y] = (255, 255, 255, 255)

    return left_mask, arched_mask


def main():
    os.makedirs(RES, exist_ok=True)
    left_win = extract_left_window()
    closed_win, open_frames = extract_closed_and_open_frames()
    left_mask, arched_mask = build_window_masks()

    atlas = Image.new("RGBA", (512, 72), (0, 0, 0, 0))
    atlas.paste(left_win, (0, 0))
    atlas.paste(closed_win, (16, 0))
    for i, fr in enumerate(open_frames):
        atlas.paste(fr, (64 + i * 48, 0))

    atlas.paste(left_mask, (0, 36))
    atlas.paste(arched_mask, (16, 36))

    atlas.save(os.path.join(RES, "WindowAtlas.png"))

    sky, trees = build_parallax_layers()
    sky.save(os.path.join(RES, "NightSkyParallax.png"))
    trees.save(os.path.join(RES, "NightTreesParallax.png"))
    print("Đã cập nhật thành công WindowAtlas.png (kèm WindowMasks), NightSkyParallax.png, NightTreesParallax.png")


if __name__ == "__main__":
    main()
