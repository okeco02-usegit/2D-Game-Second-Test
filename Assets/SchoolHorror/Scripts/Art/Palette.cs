using UnityEngine;

namespace SchoolHorror
{
    /// <summary>Bảng màu: tím than, đen sẫm, nâu gỗ mục.</summary>
    public static class Palette
    {
        static Color32 C(int r, int g, int b) => new Color32((byte)r, (byte)g, (byte)b, 255);

        // Tường
        public static readonly Color32 WallTop = C(20, 16, 30);
        public static readonly Color32 Plaster = C(62, 52, 84);
        public static readonly Color32 PlasterDark = C(46, 38, 66);
        public static readonly Color32 Molding = C(100, 88, 124);
        public static readonly Color32 MoldingDark = C(38, 30, 54);
        public static readonly Color32 Baseboard = C(46, 30, 26);
        public static readonly Color32 BaseboardLight = C(74, 50, 40);

        // Sàn đá hoa cương
        public static readonly Color32 MarbleA = C(66, 60, 80);
        public static readonly Color32 MarbleB = C(56, 51, 70);
        public static readonly Color32 MarbleVein = C(84, 78, 98);
        public static readonly Color32 Grout = C(34, 30, 44);
        public static readonly Color32 Crack = C(24, 20, 32);

        // Sàn gỗ
        public static readonly Color32 WoodA = C(78, 52, 38);
        public static readonly Color32 WoodB = C(68, 45, 33);
        public static readonly Color32 WoodC = C(88, 60, 42);
        public static readonly Color32 WoodGap = C(38, 25, 20);
        public static readonly Color32 Nail = C(28, 20, 20);

        // Giấy vụn
        public static readonly Color32 Paper = C(196, 190, 172);
        public static readonly Color32 PaperShade = C(146, 140, 128);

        // Nội thất
        public static readonly Color32 Desk = C(108, 74, 48);
        public static readonly Color32 DeskLight = C(134, 96, 62);
        public static readonly Color32 DeskDark = C(60, 38, 28);
        public static readonly Color32 Chair = C(72, 48, 38);
        public static readonly Color32 PodiumWood = C(94, 64, 44);
        public static readonly Color32 Board = C(28, 54, 46);
        public static readonly Color32 BoardFrame = C(92, 62, 40);
        public static readonly Color32 Chalk = C(184, 196, 186);
        public static readonly Color32 Glass = C(34, 44, 74);
        public static readonly Color32 GlassShine = C(96, 112, 150);
        public static readonly Color32 Rubble = C(104, 98, 112);
        public static readonly Color32 RubbleDark = C(58, 52, 66);

        // Cửa & cổng
        public static readonly Color32 DoorWood = C(98, 64, 42);
        public static readonly Color32 DoorWoodDark = C(58, 36, 26);
        public static readonly Color32 Brass = C(204, 172, 84);
        public static readonly Color32 Metal = C(76, 80, 92);
        public static readonly Color32 MetalDark = C(42, 44, 54);
        public static readonly Color32 Rust = C(112, 60, 40);
        public static readonly Color32 ExitGreen = C(70, 190, 110);
        public static readonly Color32 Threshold = C(40, 30, 30);

        // Tượng đá
        public static readonly Color32 Stone = C(152, 152, 162);
        public static readonly Color32 StoneDark = C(96, 96, 110);
        public static readonly Color32 StoneWing = C(122, 122, 136);
        public static readonly Color32 StoneShadow = C(40, 40, 52);
    }
}
