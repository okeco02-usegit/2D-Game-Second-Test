using UnityEngine;

namespace SchoolHorror
{
    /// <summary>Vẽ từng loại ô 16x16 lên bản đồ. (tx, ty) là tọa độ ô; lx/ly là tọa độ điểm ảnh trong ô.</summary>
    public static class TilePainter
    {
        const int T = GameConfig.PixelsPerTile;
        static Color32[] atlas;
        static int atlasW, atlasH;

        static void EnsureAtlas()
        {
            if (atlas != null) return;
            var tex = Resources.Load<Texture2D>("FurnitureAbandoned");
            if (tex == null) return;
            atlas = tex.GetPixels32();
            atlasW = tex.width;
            atlasH = tex.height;
        }

        static void Blit(PixelBuffer b, int srcX, int srcYTop, int w, int h, int dstX, int dstYBottom)
        {
            EnsureAtlas();
            b.BlitAtlas(atlas, atlasW, atlasH, srcX, srcYTop, w, h, dstX, dstYBottom);
        }

        public static Color32 Vary(Color32 c, int amount, System.Random rng)
        {
            int d = rng.Next(-amount, amount + 1);
            return new Color32(Clamp(c.r + d), Clamp(c.g + d), Clamp(c.b + d), 255);
        }

        static byte Clamp(int v) => (byte)Mathf.Clamp(v, 0, 255);

        /// <summary>Gạch đá hoa cương cũ: ô đậm nhạt xen kẽ, mạch vữa, vân đá và vết nứt.</summary>
        public static void CorridorFloor(PixelBuffer b, int tx, int ty, System.Random rng)
        {
            int ox = tx * T, oy = ty * T;
            Color32 baseColor = ((tx + ty) & 1) == 0 ? Palette.MarbleA : Palette.MarbleB;
            for (int ly = 0; ly < T; ly++)
                for (int lx = 0; lx < T; lx++)
                    b.Set(ox + lx, oy + ly, lx == 0 || ly == 0 ? Palette.Grout : Vary(baseColor, 3, rng));

            for (int i = 0; i < 3; i++)
            {
                int vx = rng.Next(2, T - 3), vy = rng.Next(2, T - 2);
                b.Set(ox + vx, oy + vy, Palette.MarbleVein);
                b.Set(ox + vx + 1, oy + vy + (rng.Next(2) == 0 ? 0 : 1), Palette.MarbleVein);
            }

            if (rng.NextDouble() < 0.2)
            {
                int cx = rng.Next(2, T - 2), cy = rng.Next(1, 5);
                int length = rng.Next(6, 12);
                for (int i = 0; i < length && cy < T; i++)
                {
                    b.Set(ox + cx, oy + cy, Palette.Crack);
                    cx = Mathf.Clamp(cx + rng.Next(-1, 2), 1, T - 1);
                    if (rng.Next(3) != 0) cy++;
                }
            }
        }

        /// <summary>Ván gỗ ngang (mỗi tấm cao 4px), mối nối so le và đinh tán ở hai đầu ván.</summary>
        public static void RoomFloor(PixelBuffer b, int tx, int ty, System.Random rng)
        {
            int ox = tx * T, oy = ty * T;
            for (int ly = 0; ly < T; ly++)
            {
                int gy = oy + ly;
                int plank = gy / 4;
                int rowInPlank = gy % 4;
                Color32 baseColor = (plank * 7 % 3) == 0 ? Palette.WoodA : (plank * 7 % 3) == 1 ? Palette.WoodB : Palette.WoodC;
                for (int lx = 0; lx < T; lx++)
                {
                    int along = (ox + lx + plank * 11) % 24; // vị trí dọc theo tấm ván dài 24px
                    Color32 c;
                    if (rowInPlank == 0 || along == 0) c = Palette.WoodGap;
                    else if (rowInPlank == 2 && (along == 2 || along == 22)) c = Palette.Nail;
                    else c = Vary(baseColor, 3, rng);
                    b.Set(ox + lx, gy, c);
                }
            }
        }

        public static void WallTop(PixelBuffer b, int tx, int ty, System.Random rng)
        {
            int ox = tx * T, oy = ty * T;
            for (int ly = 0; ly < T; ly++)
                for (int lx = 0; lx < T; lx++)
                    b.Set(ox + lx, oy + ly, Vary(Palette.WallTop, 2, rng));
        }

        /// <summary>Mặt tường nhìn từ phía nam: phào chỉ trần, nẹp giữa tường và chân tường gỗ.</summary>
        public static void WallFace(PixelBuffer b, int tx, int ty, System.Random rng)
        {
            int ox = tx * T, oy = ty * T;
            for (int ly = 0; ly < T; ly++)
            {
                for (int lx = 0; lx < T; lx++)
                {
                    Color32 c;
                    if (ly <= 2) c = Vary(Palette.Baseboard, 2, rng);
                    else if (ly == 3) c = Palette.BaseboardLight;
                    else if (ly == 15 || ly == 12) c = Palette.MoldingDark;
                    else if (ly == 14 || ly == 13) c = ly == 14 ? Palette.Molding : Vary(Palette.Molding, 4, rng);
                    else if (ly == 8) c = Palette.Molding;
                    else if (ly == 7) c = Palette.PlasterDark;
                    else if (ly < 7 && lx % 8 == 0) c = Palette.PlasterDark; // ô ván ốp chân tường
                    else c = Vary(Palette.Plaster, 3, rng);
                    b.Set(ox + lx, oy + ly, c);
                }
            }

            if (rng.NextDouble() < 0.35) // vệt ố chảy xuống
            {
                int sx = rng.Next(1, T - 1);
                int length = rng.Next(2, 5);
                for (int i = 0; i < length; i++) b.Darken(ox + sx, oy + 11 - i, 0.75f);
            }
        }

        /// <summary>Bục giảng chạy dọc theo tường có bảng; mép phía lớp học (đông) và hai đầu có gờ tối.</summary>
        public static void Podium(PixelBuffer b, int tx, int ty, System.Random rng, bool southEnd, bool northEnd)
        {
            int ox = tx * T, oy = ty * T;
            for (int ly = 0; ly < T; ly++)
                for (int lx = 0; lx < T; lx++)
                {
                    bool edge = lx >= T - 2 || (southEnd && ly == 0) || (northEnd && ly == T - 1);
                    b.Set(ox + lx, oy + ly, edge ? Palette.DeskDark : (lx % 5 == 2 ? Palette.WoodGap : Vary(Palette.PodiumWood, 3, rng)));
                }
        }

        public static void Threshold(PixelBuffer b, int tx, int ty, System.Random rng)
        {
            int ox = tx * T, oy = ty * T;
            for (int ly = 0; ly < T; ly++)
                for (int lx = 0; lx < T; lx++)
                    b.Set(ox + lx, oy + ly, lx < 1 || lx > T - 2 ? Palette.MoldingDark : Vary(Palette.Threshold, 3, rng));
        }

        /// <summary>Bảng đen treo trên tường phía tây của lớp, nhìn từ trên xuống là một dải dọc ở mép đông ô tường.</summary>
        public static void Blackboard(PixelBuffer b, int tx, int ty, System.Random rng, bool southEnd, bool northEnd)
        {
            WallTop(b, tx, ty, rng);
            int ox = tx * T, oy = ty * T;
            for (int ly = 0; ly < T; ly++)
            {
                if ((southEnd && ly < 2) || (northEnd && ly > T - 3)) continue;
                bool frameEnd = (southEnd && ly == 2) || (northEnd && ly == T - 3);
                for (int lx = T - 6; lx < T; lx++)
                {
                    bool frame = frameEnd || lx == T - 6 || lx == T - 1;
                    b.Set(ox + lx, oy + ly, frame ? Palette.BoardFrame : Vary(Palette.Board, 2, rng));
                }
            }

            for (int i = 0; i < 2; i++) // nét phấn còn sót lại
            {
                int cy = rng.Next(4, T - 5), length = rng.Next(2, 5);
                for (int k = 0; k < length; k++) b.Set(ox + T - 3, oy + cy + k, Palette.Chalk);
            }
        }

        /// <summary>Cửa sổ kính trên mặt tường: khung gỗ, kính tối có vệt phản chiếu.</summary>
        public static void Window(PixelBuffer b, int tx, int ty, System.Random rng, bool leftEnd, bool rightEnd)
        {
            WallFace(b, tx, ty, rng);
            int ox = tx * T, oy = ty * T;
            for (int ly = 4; ly <= 12; ly++)
                for (int lx = 0; lx < T; lx++)
                {
                    if ((leftEnd && lx < 1) || (rightEnd && lx > T - 2)) continue;
                    bool frame = ly == 4 || ly == 12 || ly == 8 || (leftEnd && lx == 1) || (rightEnd && lx == T - 2) || lx == 8;
                    Color32 glass = (lx + ly) % 9 == 0 ? Palette.GlassShine : Vary(Palette.Glass, 2, rng);
                    b.Set(ox + lx, oy + ly, frame ? Palette.BoardFrame : glass);
                }
        }

        /// <summary>Ô tường ngoài có gắn cửa sổ: vẽ tường bình thường, cảnh Parallax được chiếu qua SpriteMask của cửa sổ.</summary>
        public static void OuterWindowWall(PixelBuffer b, int tx, int ty, System.Random rng, bool isTopWall, bool leftHalf)
        {
            if (isTopWall)
                WallFace(b, tx, ty, rng);
            else
                WallTop(b, tx, ty, rng);
        }

        /// <summary>Mảng tường sập: nền gạch vụn, hai bên còn sót mép vữa lởm chởm.</summary>
        public static void BrokenWall(PixelBuffer b, int tx, int ty, System.Random rng)
        {
            int ox = tx * T, oy = ty * T;
            for (int ly = 0; ly < T; ly++)
            {
                int leftJag = rng.Next(1, 4), rightJag = rng.Next(1, 4);
                for (int lx = 0; lx < T; lx++)
                {
                    bool remnant = lx < leftJag || lx >= T - rightJag;
                    b.Set(ox + lx, oy + ly, remnant ? Vary(Palette.PlasterDark, 4, rng) : Vary(Palette.Threshold, 4, rng));
                }
            }
            for (int i = 0; i < 9; i++) // đá vụn
            {
                int rx = ox + rng.Next(3, T - 4), ry = oy + rng.Next(1, T - 2);
                b.Set(rx, ry, Palette.Rubble);
                if (rng.Next(2) == 0) b.Set(rx + 1, ry, Palette.RubbleDark);
            }
        }

        /// <summary>Cổng sắt hai cánh, có xích khóa và biển EXIT xanh.</summary>
        public static void Gate(PixelBuffer b, int tx, int ty, System.Random rng, bool leftHalf)
        {
            WallFace(b, tx, ty, rng);
            int ox = tx * T, oy = ty * T;
            for (int ly = 0; ly <= 11; ly++)
                for (int lx = 0; lx < T; lx++)
                {
                    bool seam = leftHalf ? lx == T - 1 : lx == 0;
                    Color32 c = seam || lx % 4 == 1 || ly == 11 ? Palette.MetalDark : Vary(Palette.Metal, 3, rng);
                    if (rng.NextDouble() < 0.06) c = Palette.Rust;
                    b.Set(ox + lx, oy + ly, c);
                }

            int lockX = leftHalf ? T - 3 : 0; // xích + ổ khóa vắt qua khe giữa
            b.Fill(ox + lockX, oy + 5, 3, 3, Palette.Brass);
            b.Set(ox + lockX + 1, oy + 6, Palette.MetalDark);

            int signX = leftHalf ? T - 6 : 0;
            b.Fill(ox + signX, oy + 13, 6, 2, Palette.ExitGreen);
        }

        /// <summary>Ghế học sinh (chân sắt trắng, mặt & lưng gỗ) đặt tại một chỗ ngồi của bàn.</summary>
        static void PaintStudentChair(PixelBuffer b, int ox, int oy, System.Random rng)
        {
            int roll = rng.Next(100);
            if (roll < 12) // ghế dính máu nhìn từ trước
                Blit(b, 273, 119, 14, 25, ox + 1, oy - 3);
            else if (roll < 20) // ghế dính máu nhìn ngang
                Blit(b, 290, 113, 13, 22, ox + 2, oy - 2);
            else if (roll < 42) // ghế nhìn ngang (quay về phía bảng)
                Blit(b, 258, 113, 13, 22, ox + 2, oy - 2);
            else // ghế nhìn chính diện (lưng tựa nhô lên phía sau mặt bàn)
                Blit(b, 225, 119, 14, 25, ox + 1, oy - 3);
        }

        static void PaintDeskBook(PixelBuffer b, int ox, int oy, System.Random rng)
        {
            int roll = rng.Next(100);
            if (roll < 22) Blit(b, 144, 15, 13, 7, ox + 2, oy + 7);      // vở mở
            else if (roll < 38) Blit(b, 130, 14, 7, 8, ox + 4, oy + 7);  // sổ xanh
            else if (roll < 52) Blit(b, 116, 14, 6, 7, ox + 5, oy + 7);  // bài kiểm tra + bút
        }

        /// <summary>Bàn học sinh đôi (32x19) kèm 2 ghế học sinh từ bộ horror_school_furnitures.</summary>
        public static void DoubleDesk(PixelBuffer b, int tx, int ty, System.Random rng)
        {
            int ox = tx * T, oy = ty * T;
            PaintStudentChair(b, ox, oy, rng);
            PaintStudentChair(b, ox + T, oy, rng);

            int kind = rng.Next(100);
            if (kind < 14) // bàn gãy đôi thành 2 nửa
            {
                Blit(b, 224, 93, 20, 19, ox, oy - 2);
                Blit(b, 271, 93, 17, 19, ox + 15, oy - 2);
            }
            else if (kind < 38) // bàn sứt mẻ góc phải
                Blit(b, 224, 61, 32, 19, ox, oy - 2);
            else if (kind < 65) // bàn bị khắc chữ / vết bẩn
                Blit(b, 256, 45, 32, 19, ox, oy - 2);
            else if (kind < 82)
                Blit(b, 256, 13, 32, 19, ox, oy - 2);
            else
                Blit(b, 224, 13, 32, 19, ox, oy - 2);

            if (kind >= 14)
            {
                PaintDeskBook(b, ox, oy, rng);
                PaintDeskBook(b, ox + T, oy, rng);
            }
            // Một số chỗ có thêm ghế quay lưng phía nam bàn
            if (rng.Next(100) < 25)
                Blit(b, 241, 119, 14, 25, ox + (rng.Next(2) * T) + 1, oy - 6);
        }

        /// <summary>Bàn học sinh đơn (16x19) kèm ghế học sinh.</summary>
        public static void SingleDesk(PixelBuffer b, int tx, int ty, System.Random rng)
        {
            int ox = tx * T, oy = ty * T;
            PaintStudentChair(b, ox, oy, rng);
            if (rng.Next(2) == 0) Blit(b, 320, 13, 16, 19, ox, oy - 2);
            else Blit(b, 320, 61, 16, 19, ox, oy - 2);
            PaintDeskBook(b, ox, oy, rng);
        }

        /// <summary>Bàn giáo viên kèm ghế xoay văn phòng màu đen và sổ sách trên mặt bàn.</summary>
        public static void TeacherDesk(PixelBuffer b, int tx, int ty)
        {
            int ox = tx * T, oy = ty * T;
            // Ghế xoay giáo viên nhìn nghiêng đặt bên trái (phía bục giảng)
            Blit(b, 179, 67, 13, 24, ox - 11, oy - 3);
            // Bàn gỗ giáo viên đặt dọc (12x32)
            Blit(b, 180, 0, 12, 32, ox + 2, oy - 8);
            // Quyển sổ mở trên bàn giáo viên
            Blit(b, 144, 15, 13, 7, ox + 1, oy + 4);
        }

        /// <summary>Trang trí nội thất lớp học (tủ sách, kệ gỗ, bản đồ treo tường) ở các ô tường/góc trống.</summary>
        public static void ClassroomDecor(PixelBuffer b, RectInt room, bool frontIsNorth, int roomIndex, System.Random rng)
        {
            // Bản đồ thế giới (nguyên hoặc rách) treo trên mặt tường phía bắc của lớp (nếu tường bắc là tường kín)
            if (!frontIsNorth)
            {
                int mapX = (room.xMin + 4) * T;
                int mapY = (room.yMax) * T;
                if (rng.Next(2) == 0) Blit(b, 96, 81, 25, 16, mapX, mapY);
                else Blit(b, 96, 65, 32, 16, mapX, mapY);
            }
            // Kệ gỗ thấp / sách vở sát góc đông-bắc hoặc đông-nam không vướng cửa
            int cornerX = (room.xMax - 2) * T;
            int cornerY = (frontIsNorth ? room.yMin : room.yMax - 1) * T;
            Blit(b, 36, 152, 11, 6, cornerX + 2, cornerY + 2); // sách rơi trên sàn góc lớp
            if (roomIndex % 2 == 0)
                Blit(b, 65, 150, 12, 9, cornerX + 14, cornerY + 4);
        }

        /// <summary>Đổ bóng pixel 3D dưới chân và cạnh bên phải của tủ locker / máy bán nước.</summary>
        static void PaintPropShadow(PixelBuffer b, int ox, int oy, int w, int h)
        {
            // Bóng đổ chéo sang phải và xuống dưới mặt sàn tạo chiều sâu 3D
            for (int dy = -4; dy < 6; dy++)
            {
                float factor = dy < 0 ? 0.42f + (dy + 4) * 0.06f : 0.48f;
                int shiftRight = dy < 0 ? (-dy) / 2 : 3;
                for (int dx = -1; dx < w + shiftRight; dx++)
                    b.Darken(ox + dx, oy + dy, factor);
            }
            // Bóng cạnh bên phải dọc thân tủ
            for (int dy = 0; dy < h - 8; dy++)
            {
                b.Darken(ox + w, oy + dy, 0.46f);
                b.Darken(ox + w + 1, oy + dy, 0.65f);
                if (dy < h - 14) b.Darken(ox + w + 2, oy + dy, 0.80f);
            }
        }

        static void PlaceCorridorCabinet(PixelBuffer b, int srcX, int srcYTop, int w, int h, int dstX, int dstY)
        {
            PaintPropShadow(b, dstX, dstY, w, h);
            Blit(b, srcX, srcYTop, w, h, dstX, dstY);
        }

        /// <summary>Trang trí hành lang từ corridor school.png: tủ locker, máy bán nước, bình cứu hỏa, biển EXIT, biển lớp.</summary>
        public static void CorridorDecor(PixelBuffer b, SchoolMap map, System.Random rng)
        {
            // Biển EXIT xanh lá cạnh cổng thoát hiểm (29..30, 38)
            Blit(b, 32, 276, 16, 7, 28 * T, 38 * T + 5);
            Blit(b, 32, 276, 16, 7, 31 * T, 38 * T + 5);

            // Dọc bức tường phía bắc của hành lang B (y = 37..38): đặt dãy tủ locker và máy bán hàng tự động
            int[] lockerColsB = { 5, 8, 12, 18, 22, 35, 39, 45, 50, 54 };
            for (int i = 0; i < lockerColsB.Length; i++)
            {
                int tx = lockerColsB[i];
                int ox = tx * T, oy = 37 * T + 4;
                if (i == 0) PlaceCorridorCabinet(b, 0, 264, 26, 40, ox + 3, oy - 2);         // máy bán nước đỏ
                else if (i == 6) PlaceCorridorCabinet(b, 0, 312, 26, 40, ox + 3, oy - 2);    // máy bán nước xanh
                else if (i == 3 || i == 8) PlaceCorridorCabinet(b, 32, 313, 32, 39, ox, oy); // tủ locker 4 ngăn xám
                else if (i == 4) PlaceCorridorCabinet(b, 160, 313, 16, 39, ox, oy);          // tủ locker mở trống có vết máu (không có xác)
                else if (i == 2) PlaceCorridorCabinet(b, 128, 313, 16, 39, ox, oy);          // tủ đỏ mở hé
                else if (i == 7) PlaceCorridorCabinet(b, 144, 313, 16, 39, ox, oy);          // tủ vàng mở hé
                else
                {
                    PaintPropShadow(b, ox, oy, 32, 39);
                    Blit(b, 64, 265, 16, 39, ox, oy);                                        // tủ đỏ đóng
                    Blit(b, 80, 265, 16, 39, ox + T, oy);                                    // tủ vàng đóng
                }
            }

            // Bình chữa cháy + biển báo và biển tên lớp (A1..A3, B1..B3) trên mặt tường hành lang
            int[] roomCols = { 5, 17, 33, 45 };
            for (int i = 0; i < roomCols.Length; i++)
            {
                int x0 = roomCols[i];
                // Tường mặt nam của dãy phòng giữa (y = 15, nhìn ra hành lang A)
                int signCol = (i % 3);
                Blit(b, 65 + signCol * 16, 195, 14, 10, (x0 + 2) * T + 1, 15 * T + 4); // biển A1, A2, A3
                Blit(b, 50, 273, 11, 14, (x0 + 8) * T + 2, 15 * T + 2);                // bình chữa cháy
                PlaceCorridorCabinet(b, 160, 265, 16, 39, x0 * T, 14 * T + 4);         // tủ locker xám đầu phòng

                // Tường mặt nam của dãy phòng trên (y = 26, nhìn ra hành lang giữa)
                Blit(b, 65 + signCol * 16, 211, 14, 10, (x0 + 2) * T + 1, 26 * T + 4); // biển B1, B2, B3
                if (i % 2 == 1)
                    PlaceCorridorCabinet(b, 192, 313, 16, 39, (x0 + 10) * T, 25 * T + 4); // tủ locker mở trống
            }
        }

        /// <summary>Đổ bóng chân tường (ambient occlusion) lên ô sàn nằm sát tường.</summary>
        public static void AmbientOcclusion(PixelBuffer b, int tx, int ty, bool north, bool south, bool west, bool east)
        {
            int ox = tx * T, oy = ty * T;
            float[] northShade = { 0.42f, 0.56f, 0.7f, 0.82f, 0.92f };
            float[] sideShade = { 0.68f, 0.82f, 0.93f };
            for (int i = 0; i < T; i++)
            {
                if (north) for (int k = 0; k < northShade.Length; k++) b.Darken(ox + i, oy + T - 1 - k, northShade[k]);
                if (west) for (int k = 0; k < sideShade.Length; k++) b.Darken(ox + k, oy + i, sideShade[k]);
                if (east) for (int k = 0; k < sideShade.Length; k++) b.Darken(ox + T - 1 - k, oy + i, sideShade[k]);
                if (south) { b.Darken(ox + i, oy, 0.75f); b.Darken(ox + i, oy + 1, 0.9f); }
            }
        }

        /// <summary>Một mẩu giấy vụn trên sàn.</summary>
        public static void Paper(PixelBuffer b, int tx, int ty, System.Random rng)
        {
            int px = tx * T + rng.Next(2, T - 5), py = ty * T + rng.Next(2, T - 4);
            b.Fill(px, py, 3, 2, Palette.Paper);
            b.Set(px + 2, py, Palette.PaperShade);
            if (rng.Next(2) == 0) b.Set(px + 3, py + 1, Palette.Paper);
        }
    }
}
