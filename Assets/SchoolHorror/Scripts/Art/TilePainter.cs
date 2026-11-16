using UnityEngine;

namespace SchoolHorror
{
    /// <summary>Vẽ từng loại ô 16x16 lên bản đồ. (tx, ty) là tọa độ ô; lx/ly là tọa độ điểm ảnh trong ô.</summary>
    public static class TilePainter
    {
        const int T = GameConfig.PixelsPerTile;

        static Color32 Vary(Color32 c, int amount, System.Random rng)
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

        /// <summary>Bàn học sinh kèm ghế, có bóng đổ dưới mép bàn.</summary>
        public static void Desk(PixelBuffer b, int tx, int ty)
        {
            int ox = tx * T, oy = ty * T;
            for (int lx = 2; lx <= 13; lx++) b.Darken(ox + lx, oy + 5, 0.55f);
            b.Fill(ox + 2, oy + 6, 12, 8, Palette.Desk);
            b.Fill(ox + 2, oy + 13, 12, 1, Palette.DeskLight);
            b.Fill(ox + 2, oy + 6, 12, 1, Palette.DeskDark);
            b.Fill(ox + 2, oy + 6, 1, 8, Palette.DeskDark);
            b.Fill(ox + 13, oy + 6, 1, 8, Palette.DeskDark);
            b.Fill(ox + 5, oy + 1, 6, 4, Palette.Chair);
            b.Fill(ox + 5, oy + 1, 6, 1, Palette.DeskDark);
        }

        public static void TeacherDesk(PixelBuffer b, int tx, int ty)
        {
            int ox = tx * T, oy = ty * T;
            for (int lx = 1; lx <= 14; lx++) b.Darken(ox + lx, oy + 2, 0.55f);
            b.Fill(ox + 1, oy + 3, 14, 10, Palette.Desk);
            b.Fill(ox + 1, oy + 12, 14, 1, Palette.DeskLight);
            b.Fill(ox + 1, oy + 3, 14, 2, Palette.DeskDark);
            b.Fill(ox + 4, oy + 7, 4, 3, Palette.Paper);
            b.Set(ox + 10, oy + 9, Palette.Brass);
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
