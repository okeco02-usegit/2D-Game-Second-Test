using UnityEngine;

namespace SchoolHorror
{
    /// <summary>
    /// Dựng nền trời đêm + mây + mặt trăng + rừng cây có hiệu ứng Parallax Scrolling,
    /// cùng cửa sổ left_window (trái/phải flip), closed_window và open_window (đung đưa đóng-mở cánh).
    /// </summary>
    public static class OutsideScenery
    {
        public static readonly Vector2 MoonlightOrigin = new Vector2(3f, 38.3f);
        public static readonly Vector2[] TopWindowPositions = {
            new Vector2(3.0f, 38.3f),
            new Vector2(26.0f, 38.3f),
            new Vector2(48.0f, 38.3f)
        };

        // Vị trí cửa sổ bên hông: (x, y) trung tâm cửa sổ
        public static readonly Vector2[] LeftWindowPositions = { new Vector2(0f, 8f), new Vector2(0f, 20f), new Vector2(0f, 32f) };
        public static readonly Vector2[] RightWindowPositions = { new Vector2(60f, 8f), new Vector2(60f, 20f), new Vector2(60f, 32f) };

        public static void Build(SchoolMap map, Material material, Transform parent, System.Random rng)
        {
            SpawnParallaxBackground(map, material, parent);
            SpawnWindows(map, material, parent);
            SpawnCeilingOverhang(map, material, parent, rng);
            SpawnMoonbeam(material, parent);
            SpawnSideWindowBeams(map, material, parent);
        }

        static void SpawnParallaxBackground(SchoolMap map, Material material, Transform parent)
        {
            Vector2 center = new Vector2(map.Width * 0.5f, map.Height * 0.5f);
            CreateParallaxLayer("NightSkyParallax", center, 0.72f, 2.2f, 2, material, parent);
            CreateParallaxLayer("NightTreesParallax", center, 0.44f, 2.0f, 3, material, parent);
        }

        static void CreateParallaxLayer(string resourceName, Vector2 center, float factor, float scale, int sortingOrder, Material material, Transform parent)
        {
            var tex = Resources.Load<Texture2D>(resourceName);
            if (tex == null) return;
            tex.filterMode = FilterMode.Point;

            var go = new GameObject(resourceName);
            go.transform.SetParent(parent, false);
            go.transform.position = center;
            go.transform.localScale = new Vector3(scale, scale, 1f);

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), GameConfig.PixelsPerTile);
            sr.sharedMaterial = material;
            sr.sortingOrder = sortingOrder;
            sr.maskInteraction = SpriteMaskInteraction.VisibleInsideMask;

            go.AddComponent<ParallaxLayer>().Init(center, factor);
        }

        static void SpawnWindows(SchoolMap map, Material material, Transform parent)
        {
            var tex = Resources.Load<Texture2D>("WindowAtlas");
            if (tex == null) return;
            tex.filterMode = FilterMode.Point;

            Sprite leftWin = SliceTop(tex, 0, 0, 16, 28, new Vector2(0.5f, 0.5f), 20f);
            Sprite leftMask = SliceTop(tex, 0, 36, 16, 28, new Vector2(0.5f, 0.5f), 20f);
            // Đặt pivot ở cạnh dưới (0.5, 0) để đáy cửa sổ nằm trên chân tường (y = 38.28), không chạm sàn (y = 38.0)
            Sprite closedWin = SliceTop(tex, 16, 0, 48, 34, new Vector2(0.5f, 0f), 22f);
            Sprite archedMask = SliceTop(tex, 16, 36, 48, 34, new Vector2(0.5f, 0f), 22f);
            var openFrames = new Sprite[6];
            for (int i = 0; i < 6; i++)
                openFrames[i] = SliceTop(tex, 64 + i * 48, 0, 48, 34, new Vector2(0.5f, 0f), 22f);

            // Tường trên cùng (y = 38): 3 cửa sổ (x=2..3: open, x=25..26: closed, x=47..48: open)
            for (int x = 0; x < map.Width - 1; x++)
            {
                if (map.Get(x, 38) != TileType.Window || map.Get(x + 1, 38) != TileType.Window) continue;
                bool isOpen = (x == 2 || x == 47);
                Vector2 pos = new Vector2(x + 1f, 38.28f);
                var sr = CreateWindowObject(isOpen ? openFrames[0] : closedWin, archedMask, pos, false, material, parent);
                if (isOpen)
                    sr.gameObject.AddComponent<OpenWindowSwing>().Init(openFrames, x * 0.9f);
                x++;
            }

            // Tường trái (x = 0) và tường phải (x = Width - 1, flip sang phải)
            for (int y = 0; y < map.Height; y++)
            {
                if (map.Get(0, y) == TileType.Window)
                    CreateWindowObject(leftWin, leftMask, new Vector2(0.45f, y + 0.5f), false, material, parent);
                if (map.Get(map.Width - 1, y) == TileType.Window)
                    CreateWindowObject(leftWin, leftMask, new Vector2(map.Width - 0.45f, y + 0.5f), true, material, parent);
            }
        }

        static SpriteRenderer CreateWindowObject(Sprite sprite, Sprite maskSprite, Vector2 pos, bool flipX, Material material, Transform parent)
        {
            var go = new GameObject("OuterWindow");
            go.transform.SetParent(parent, false);
            go.transform.position = pos;
            if (flipX) go.transform.localScale = new Vector3(-1f, 1f, 1f);

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sharedMaterial = material;
            sr.sortingOrder = 4;

            if (maskSprite != null)
            {
                var mask = go.AddComponent<SpriteMask>();
                mask.sprite = maskSprite;
                mask.backSortingOrder = 1;
                mask.frontSortingOrder = 3;
            }

            return sr;
        }

        static void SpawnMoonbeam(Material material, Transform parent)
        {
            var b = new PixelBuffer(48, 48);
            for (int y = 0; y < 48; y++)
            {
                float t = 1f - y / 47f;
                int halfW = (int)Mathf.Lerp(7f, 19f, t);
                int cx = (int)Mathf.Lerp(18f, 28f, t);
                for (int x = cx - halfW; x <= cx + halfW; x++)
                {
                    float edge = 1f - Mathf.Abs(x - cx) / (float)Mathf.Max(1, halfW);
                    byte alpha = (byte)(Mathf.Clamp01((1f - t * 0.65f) * edge) * 58f);
                    b.Set(x, y, new Color32(150, 185, 235, alpha));
                }
            }

            Sprite beamSprite = b.ToSprite(new Vector2(0.5f, 0.5f));

            // Dựng vệt ánh trăng cho cả 3 cửa sổ ở hành lang trên cùng (trái, giữa, phải)
            for (int i = 0; i < TopWindowPositions.Length; i++)
            {
                Vector2 origin = TopWindowPositions[i];
                var go = new GameObject("Moonbeam_Top_" + i);
                go.transform.SetParent(parent, false);
                go.transform.position = new Vector3(origin.x + 0.15f, 36.6f, 0f);
                var sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = beamSprite;
                sr.sharedMaterial = material;
                sr.sortingOrder = 2;
                go.AddComponent<MoonbeamGlow>().Init(i * 1.7f);
            }
        }

        static void SpawnCeilingOverhang(SchoolMap map, Material material, Transform parent, System.Random rng)
        {
            int T = GameConfig.PixelsPerTile;
            int width = map.Width * T;
            int height = T; // 1 ô cao (y = 39.0 .. 40.0)
            var b = new PixelBuffer(width, height);

            for (int tx = 0; tx < map.Width; tx++)
            {
                int ox = tx * T;
                for (int lx = 0; lx < T; lx++)
                {
                    for (int ly = 0; ly < height; ly++)
                    {
                        if (ly >= 12)
                        {
                            // Dải phào dầm trần lồi ra (Ceiling beam overhang) chạy suốt chiều ngang phía trên cùng
                            Color32 beamCol;
                            if (ly == 12) beamCol = new Color32(16, 12, 22, 220); // bóng đổ xuống tường
                            else if (ly == 13) beamCol = new Color32(32, 24, 34, 255); // mặt dưới dầm
                            else if (ly == 14) beamCol = new Color32(54, 42, 54, 255); // mặt trước dầm gỗ/bê tông
                            else beamCol = new Color32(78, 62, 76, 255); // gờ nóc trần đón sáng
                            b.Set(ox + lx, ly, beamCol);
                        }
                        else
                        {
                            // Toàn bộ là tường bình thường tiếp nối mặt tường phía dưới (y = 39.0 .. 39.75)
                            Color32 wallCol;
                            if (ly == 0) wallCol = Palette.MoldingDark;
                            else if (ly == 9) wallCol = Palette.Molding;
                            else if (ly == 10) wallCol = Palette.MoldingDark;
                            else if (ly == 11) wallCol = Palette.PlasterDark;
                            else wallCol = TilePainter.Vary(Palette.Plaster, 3, rng);
                            b.Set(ox + lx, ly, wallCol);
                        }
                    }
                }
            }

            var go = new GameObject("CeilingOverhang");
            go.transform.SetParent(parent, false);
            go.transform.position = new Vector3(0f, 39.0f, 0f);

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = b.ToSprite(Vector2.zero);
            sr.sharedMaterial = material;
            sr.sortingOrder = 1; // phía sau parallax masked (2..3) và cửa sổ (4), phía trước map nền (0)
        }

        static Sprite SliceTop(Texture2D tex, int x, int yTop, int w, int h, Vector2 pivot, float ppu)
        {
            return Sprite.Create(tex, new Rect(x, tex.height - yTop - h, w, h), pivot, ppu);
        }

        /// <summary>Tạo vệt ánh trăng chiếu xiên xuống dưới (~45°) qua các cửa sổ bên trái và bên phải.</summary>
        static void SpawnSideWindowBeams(SchoolMap map, Material material, Transform parent)
        {
            foreach (var pos in LeftWindowPositions)
                CreateSideBeam(pos, false, map.Width, material, parent);
            foreach (var pos in RightWindowPositions)
                CreateSideBeam(pos, true, map.Width, material, parent);
        }

        static void CreateSideBeam(Vector2 windowPos, bool fromRight, float mapWidth, Material material, Transform parent)
        {
            // Vệt sáng xiên: 36 x 60 pixel, chiếu chéo xuống dưới ~45° từ cửa sổ tới giữa hành lang
            int bw = 36, bh = 60;
            var b = new PixelBuffer(bw, bh);

            for (int y = 0; y < bh; y++)
            {
                for (int x = 0; x < bw; x++)
                {
                    float lx = (x + 0.5f) / 16f;
                    float ly = 1.25f - ((bh - 1 - y) + 0.5f) / 16f;
                    float S = lx + ly;

                    if (lx >= 0f && lx <= 2.15f && ly <= 1.25f && S >= -0.20f && S <= 2.05f)
                    {
                        // Giữ nguyên cường độ bên trong cửa sổ (lx <= 0.8), phai dần ra giữa hành lang (0.8 -> 2.15)
                        float distFade = lx <= 0.80f ? 1.0f : Mathf.Clamp01(1f - (lx - 0.80f) / 1.35f);
                        distFade = distFade * distFade;

                        // Phai mềm ở 2 bên mép tia (đường chéo đỏ)
                        float S_center = 0.925f;
                        float S_half = 1.125f;
                        float normDist = Mathf.Abs(S - S_center) / S_half;
                        float edgeFade = Mathf.Clamp01(1f - normDist * normDist);

                        float intensity = distFade * edgeFade;
                        byte alpha = (byte)(Mathf.Clamp01(intensity) * 50f);
                        if (alpha > 0)
                            b.Set(x, y, new Color32(130, 165, 220, alpha));
                    }
                }
            }

            Vector2 pivot = new Vector2(0f, 1f); // Góc trên-trái
            float worldX = fromRight ? (mapWidth - 0.05f) : 0.05f;
            float worldY = windowPos.y + 1.25f;

            var go = new GameObject("SideBeam");
            go.transform.SetParent(parent, false);
            go.transform.position = new Vector3(worldX, worldY, 0f);
            if (fromRight) go.transform.localScale = new Vector3(-1f, 1f, 1f);

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = b.ToSprite(pivot);
            sr.sharedMaterial = material;
            sr.sortingOrder = 5; // Phủ lên cả cửa sổ (4) và sàn nền (0..2)

            go.AddComponent<MoonbeamGlow>().Init(windowPos.y * 1.3f);
        }
    }

    /// <summary>Di chuyển lớp nền chậm hơn camera (Parallax Scrolling) tạo chiều sâu không gian bên ngoài.</summary>
    public sealed class ParallaxLayer : MonoBehaviour
    {
        Vector2 baseCenter;
        float factor;

        public void Init(Vector2 center, float parallaxFactor)
        {
            baseCenter = center;
            factor = parallaxFactor;
        }

        void LateUpdate()
        {
            Camera cam = Camera.main;
            if (cam == null) return;
            Vector2 camPos = cam.transform.position;
            Vector2 offset = (camPos - baseCenter) * factor;
            transform.position = new Vector3(baseCenter.x + offset.x, baseCenter.y + offset.y, 0f);
        }
    }

    /// <summary>Diễn hoạt 2 cánh cửa sổ của open_window đung đưa đóng-mở liên tục theo nhịp gió.</summary>
    public sealed class OpenWindowSwing : MonoBehaviour
    {
        Sprite[] frames;
        SpriteRenderer sr;
        float phase;

        public void Init(Sprite[] animFrames, float startPhase)
        {
            frames = animFrames;
            phase = startPhase;
            sr = GetComponent<SpriteRenderer>();
        }

        void Update()
        {
            if (frames == null || frames.Length == 0 || sr == null) return;
            float wave = 0.5f + 0.5f * (Mathf.Sin(Time.time * 1.55f + phase) * 0.75f + Mathf.Sin(Time.time * 2.9f + phase) * 0.25f);
            int idx = Mathf.Clamp(Mathf.RoundToInt(wave * (frames.Length - 1)), 0, frames.Length - 1);
            sr.sprite = frames[idx];
        }
    }

    /// <summary>Hiệu ứng ánh trăng mờ ảo pulsing qua cửa sổ bên hông – nhiều lớp sóng sin chồng nhau.</summary>
    public sealed class MoonbeamGlow : MonoBehaviour
    {
        SpriteRenderer sr;
        float phase;

        public void Init(float startPhase)
        {
            sr = GetComponent<SpriteRenderer>();
            phase = startPhase;
        }

        void Update()
        {
            if (sr == null) return;
            // 3 lớp sin chồng nhau tạo nhịp thở tự nhiên, không đều
            float t = Time.time;
            float wave = Mathf.Sin(t * 0.4f + phase) * 0.35f
                       + Mathf.Sin(t * 0.9f + phase * 1.7f) * 0.15f
                       + Mathf.Sin(t * 1.6f + phase * 0.6f) * 0.08f;
            float alpha = Mathf.Clamp01(0.65f + wave); // dao động ~0.22 đến 1.0
            Color c = sr.color;
            c.a = alpha;
            sr.color = c;
        }
    }
}
