using UnityEngine;

namespace SchoolHorror
{
    /// <summary>Tạo sẵn toàn bộ sprite pixel-art của nhân vật, vật phẩm và cửa (sinh bằng mã, không cần file ảnh).</summary>
    public sealed class SpriteLibrary
    {
        public readonly Material UnlitMaterial;
        public readonly Sprite Player;
        public readonly Sprite Angel;
        public readonly Sprite Key;
        public readonly Sprite Battery;
        public readonly Sprite Candle;
        public readonly Sprite DoorOpen;
        public readonly Sprite DoorClosed;
        public readonly Sprite DoorLocked;
        public readonly Sprite JumpscareFace;

        static readonly Vector2 Feet = new Vector2(0.5f, 0.1f);
        static readonly Vector2 Center = new Vector2(0.5f, 0.5f);

        public SpriteLibrary()
        {
            Shader shader = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default");
            if (shader == null) shader = Shader.Find("Sprites/Default");
            UnlitMaterial = new Material(shader);

            Player = PixelBuffer.FromArt(PlayerArt, PlayerColor).ToSprite(Feet);
            Angel = PixelBuffer.FromArt(AngelArt, AngelColor).ToSprite(Feet);
            Key = PixelBuffer.FromArt(KeyArt, ItemColor).ToSprite(Center);
            Battery = PixelBuffer.FromArt(BatteryArt, ItemColor).ToSprite(Center);
            Candle = PixelBuffer.FromArt(CandleArt, ItemColor).ToSprite(new Vector2(0.5f, 0.2f));
            DoorOpen = BuildDoor(open: true, locked: false);
            DoorClosed = BuildDoor(open: false, locked: false);
            DoorLocked = BuildDoor(open: false, locked: true);
            JumpscareFace = BuildJumpscareFace();
        }

        static Color32 C(int r, int g, int b) => new Color32((byte)r, (byte)g, (byte)b, 255);

        static readonly string[] PlayerArt =
        {
            "....hhhh....",
            "...hhhhhh...",
            "..hhhhhhhh..",
            "..hhsssshh..",
            "..hsesseSh..",
            "..hssssssh..",
            "...ssssss...",
            "..cccccccc..",
            ".cccccccccc.",
            ".cscccccCsc.",
            ".cscccccCsc.",
            ".csccccccsc.",
            "..cccccccc..",
            "..pppppppp..",
            "..ppp..ppp..",
            "..bbb..bbb..",
        };

        static Color32 PlayerColor(char ch)
        {
            switch (ch)
            {
                case 'h': return C(40, 28, 30);
                case 's': return C(214, 170, 140);
                case 'S': return C(180, 138, 112);
                case 'e': return C(24, 18, 24);
                case 'c': return C(70, 84, 120);
                case 'C': return C(52, 62, 92);
                case 'p': return C(40, 40, 60);
                default: return C(24, 20, 24);
            }
        }

        static readonly string[] AngelArt =
        {
            "......SSSS......",
            ".....SLLLLS.....",
            ".....LLLLLL.....",
            ".....LDLLDL.....",
            ".....LLLLLL.....",
            "......LDDL......",
            ".W....SLLS....W.",
            "WWW..LLLLLL..WWW",
            "WLWW.LLLLLL.WWLW",
            "WLLWWLLLLLLWWLLW",
            "WLLLWLLLLLLWLLLW",
            "WWLLWLLLLLLWLLWW",
            ".WLLWSLLLLSWLLW.",
            ".WWLWSLLLLSWLWW.",
            "..WLWSLLLLSWLW..",
            "..WW.SLLLLS.WW..",
            "...W.SLLLLS.W...",
            ".....SLLLLS.....",
            "....SLLLLLLS....",
            "....SLLLLLLS....",
            "...SLLLLLLLLS...",
            "...SLLLLLLLLS...",
            "..SSSSSSSSSSSS..",
            "..DDDDDDDDDDDD..",
        };

        static Color32 AngelColor(char ch)
        {
            switch (ch)
            {
                case 'L': return Palette.Stone;
                case 'S': return Palette.StoneDark;
                case 'W': return Palette.StoneWing;
                default: return Palette.StoneShadow;
            }
        }

        // Chìa khóa và pin vẽ to, có viền tối để nổi bật trên nền sàn.
        static readonly string[] KeyArt =
        {
            "...oooo....",
            "..oyyyyo...",
            ".oyyooyyo..",
            ".oyo..oyo..",
            ".oyyooyyo..",
            "..oyyyyo...",
            "...oyyo....",
            "...oyyo....",
            "...oyyoo...",
            "...oyyyyo..",
            "...oyyoo...",
            "...oyyyyo..",
            "...oyyoo...",
            "....oo.....",
        };

        static readonly string[] BatteryArt =
        {
            "...ooo...",
            "..ommmo..",
            ".ooooooo.",
            ".ogggggo.",
            ".ogLgggo.",
            ".ogLgggo.",
            ".ogggggo.",
            ".oyyyyyo.",
            ".ogggggo.",
            ".ogLgggo.",
            ".ogggggo.",
            ".ogggggo.",
            ".ooooooo.",
        };

        static readonly string[] CandleArt =
        {
            "...f....",
            "..fFf...",
            "..fFf...",
            "...k....",
            "..www...",
            "..www...",
            "..wWw...",
            "..www...",
            ".bbbbb..",
        };

        static Color32 ItemColor(char ch)
        {
            switch (ch)
            {
                case 'y': return C(250, 210, 80);
                case 'o': return C(34, 24, 14);
                case 'm': return C(200, 200, 210);
                case 'g': return C(80, 210, 120);
                case 'L': return C(200, 250, 200);
                case 'f': return C(250, 160, 50);
                case 'F': return C(255, 236, 150);
                case 'k': return C(30, 24, 24);
                case 'w': return C(226, 216, 190);
                case 'W': return C(180, 170, 150);
                default: return C(90, 70, 40);
            }
        }

        static Sprite BuildDoor(bool open, bool locked)
        {
            const int T = GameConfig.PixelsPerTile;
            var b = new PixelBuffer(T, T);
            if (open)
            {
                // Cánh cửa mở áp sát khung bên trái
                b.Fill(0, 0, 3, T, Palette.DoorWood);
                b.Fill(2, 0, 1, T, Palette.DoorWoodDark);
                b.Set(1, 7, Palette.Brass);
            }
            else
            {
                b.Fill(0, 0, T, T, Palette.DoorWood);
                b.Fill(0, 0, 1, T, Palette.DoorWoodDark);
                b.Fill(T - 1, 0, 1, T, Palette.DoorWoodDark);
                b.Fill(0, T - 1, T, 1, Palette.DoorWoodDark);
                for (int panel = 0; panel < 2; panel++) // hai ô pa-nô
                {
                    int py = panel == 0 ? 2 : 9;
                    b.Fill(3, py, 10, 1, Palette.DoorWoodDark);
                    b.Fill(3, py + 4, 10, 1, Palette.DoorWoodDark);
                    b.Fill(3, py, 1, 5, Palette.DoorWoodDark);
                    b.Fill(12, py, 1, 5, Palette.DoorWoodDark);
                }
                b.Set(13, 7, Palette.Brass);
                if (locked)
                {
                    b.Fill(6, 5, 4, 4, Palette.Brass);
                    b.Fill(7, 9, 2, 2, Palette.MetalDark);
                    b.Set(7, 7, Palette.MetalDark);
                    b.Set(8, 6, Palette.MetalDark);
                }
            }
            return b.ToSprite(Vector2.zero);
        }

        /// <summary>Khuôn mặt tượng đá gào thét dùng cho jumpscare.</summary>
        static Sprite BuildJumpscareFace()
        {
            const int S = 48;
            var b = new PixelBuffer(S, S);
            var rng = new System.Random(7);
            for (int y = 0; y < S; y++)
            {
                for (int x = 0; x < S; x++)
                {
                    float u = (x + 0.5f) / S * 2f - 1f;
                    float v = (y + 0.5f) / S * 2f - 1f;
                    float au = Mathf.Abs(u);
                    if (Sq(u / 0.78f) + Sq(v / 0.95f) > 1f) continue;

                    int shade = (int)(165 - 70 * (u * u + v * v)) + rng.Next(-6, 7);
                    Color32 c = C(shade, shade, shade + 8);

                    bool eye = Sq((au - 0.36f) / 0.2f) + Sq((v - 0.2f) / 0.17f) <= 1f;
                    bool pupil = Sq((au - 0.36f) / 0.05f) + Sq((v - 0.2f) / 0.05f) <= 1f;
                    bool brow = au > 0.12f && au < 0.62f && v > 0.5f - au * 0.35f && v < 0.57f - au * 0.35f;
                    float mouthEdge = 1f - Sq(u / 0.42f);
                    bool mouth = mouthEdge > 0f && Mathf.Abs(v + 0.45f) <= 0.3f * Mathf.Sqrt(mouthEdge);
                    bool tooth = mouth && ((int)((u + 1f) * 11f) % 2 == 0)
                        && (0.3f * Mathf.Sqrt(mouthEdge) - Mathf.Abs(v + 0.45f) < 0.12f);

                    if (pupil) c = C(230, 30, 30);
                    else if (eye) c = C(8, 4, 8);
                    else if (brow) c = C(50, 50, 60);
                    else if (tooth) c = C(224, 218, 200);
                    else if (mouth) c = C(24, 0, 4);
                    b.Set(x, y, c);
                }
            }
            return b.ToSprite(Center);
        }

        static float Sq(float v) => v * v;
    }
}
