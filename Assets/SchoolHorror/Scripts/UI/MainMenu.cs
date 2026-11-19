using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace SchoolHorror
{
    /// <summary>Menu chính trước khi vào game: nền trường bỏ hoang, Play, Tutorial (7 trang có minh họa), Settings, Exit.</summary>
    public sealed class MainMenu : MonoBehaviour
    {
        struct MenuButton
        {
            public RectTransform Rect;
            public Image Image;
            public Sprite Normal;
            public Sprite Hover;
            public Action OnClick;
        }

        struct TutorialSlide
        {
            public string Title;
            public string Body;
            public Sprite Illustration;
        }

        GameManager game;
        AudioManager sound;
        GameObject mainGroup;
        GameObject settingsGroup;
        GameObject tutorialGroup;

        Text musicLabel;
        Text sfxLabel;

        TutorialSlide[] slides;
        int slideIndex;
        Text tutHeader;
        Text tutBody;
        Image tutImage;
        Text nextBtnLabel;
        GameObject prevBtnObj;

        readonly List<MenuButton> buttons = new List<MenuButton>();

        public void Init(GameManager gameManager, AudioManager audioManager, SpriteLibrary gameSprites)
        {
            game = gameManager;
            sound = audioManager;

            var tex = Resources.Load<Texture2D>("FreeHorrorUi");
            if (tex != null) tex.filterMode = FilterMode.Point;

            Sprite panelTall = Slice(tex, 128, 131, 64, 76);
            Sprite panelWide = Slice(tex, 136, 3, 96, 60);
            Sprite skull = Slice(tex, 130, 66, 28, 29);
            Sprite bear = Slice(tex, 166, 70, 20, 21);

            Sprite playNormal = Slice(tex, 6, 138, 52, 15);
            Sprite playHover = Slice(tex, 70, 138, 52, 15);
            Sprite setNormal = Slice(tex, 6, 170, 52, 15);
            Sprite setHover = Slice(tex, 70, 170, 52, 15);
            Sprite exitNormal = Slice(tex, 6, 202, 52, 15);
            Sprite exitHover = Slice(tex, 70, 202, 52, 15);
            Sprite blankNormal = Slice(tex, 6, 234, 52, 15);
            Sprite blankHover = Slice(tex, 70, 234, 52, 15);
            Sprite tutNormal = MakeTutorialButton(tex, 6, 234, new Color32(200, 200, 200, 255));
            Sprite tutHover = MakeTutorialButton(tex, 70, 234, new Color32(212, 194, 194, 255));

            slides = BuildSlides(gameSprites, tex);

            Canvas canvas = UiFactory.CreateCanvas("Menu Canvas", transform);
            canvas.sortingOrder = 20;
            Transform root = canvas.transform;

            // Ảnh nền School_Abandon_background.png phủ toàn màn hình
            var bgTex = Resources.Load<Texture2D>("School_Abandon_background");
            Image backdrop = UiFactory.CreateImage(root, "Background", bgTex != null ? new Color(0.78f, 0.78f, 0.85f, 1f) : new Color(0.03f, 0.02f, 0.04f, 0.88f));
            if (bgTex != null)
                backdrop.sprite = Sprite.Create(bgTex, new Rect(0, 0, bgTex.width, bgTex.height), new Vector2(0.5f, 0.5f), 100f);
            UiFactory.Stretch(backdrop.rectTransform);

            // Lớp phủ tối nhẹ để nổi bật bảng UI
            Image shade = UiFactory.CreateImage(root, "Shade", new Color(0.02f, 0.01f, 0.03f, 0.42f));
            UiFactory.Stretch(shade.rectTransform);

            // Tiêu đề trên cùng
            Text title = UiFactory.CreateText(root, "Game Title", 50, TextAnchor.MiddleCenter, new Color(0.88f, 0.18f, 0.18f));
            title.fontStyle = FontStyle.Bold;
            title.text = "KEEP BLINKING";
            UiFactory.Place(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -48f), new Vector2(900f, 60f));

            // --- 1. Nhóm trang chính (PLAY / TUTORIAL / SETTINGS / EXIT) ---
            mainGroup = new GameObject("Main Group", typeof(RectTransform));
            mainGroup.transform.SetParent(root, false);
            UiFactory.Stretch(mainGroup.GetComponent<RectTransform>());

            Image mainPanel = CreateSpriteImage(mainGroup.transform, "Main Panel", panelTall, new Vector2(0f, -32f), new Vector2(400f, 490f));
            CreateSpriteImage(mainPanel.transform, "Skull", skull, new Vector2(0f, 152f), new Vector2(78f, 80f));

            AddButton(mainPanel.transform, "Play Button", playNormal, playHover, new Vector2(0f, 62f), new Vector2(228f, 62f), null, 0, OnPlay);
            AddButton(mainPanel.transform, "Tutorial Button", tutNormal, tutHover, new Vector2(0f, -10f), new Vector2(228f, 62f), null, 0, () => OpenTutorial(0));
            AddButton(mainPanel.transform, "Settings Button", setNormal, setHover, new Vector2(0f, -82f), new Vector2(228f, 62f), null, 0, ShowSettings);
            AddButton(mainPanel.transform, "Exit Button", exitNormal, exitHover, new Vector2(0f, -154f), new Vector2(228f, 62f), null, 0, OnExit);

            // --- 2. Nhóm trang cài đặt (NHẠC NỀN / SFX / QUAY LẠI) ---
            settingsGroup = new GameObject("Settings Group", typeof(RectTransform));
            settingsGroup.transform.SetParent(root, false);
            UiFactory.Stretch(settingsGroup.GetComponent<RectTransform>());

            Image setPanel = CreateSpriteImage(settingsGroup.transform, "Settings Panel", panelWide, new Vector2(0f, -30f), new Vector2(560f, 380f));
            CreateSpriteImage(setPanel.transform, "Bear", bear, new Vector2(-190f, 128f), new Vector2(60f, 63f));

            Text setHeader = UiFactory.CreateText(setPanel.transform, "Settings Header", 26, TextAnchor.MiddleCenter, new Color(0.92f, 0.85f, 0.82f));
            setHeader.fontStyle = FontStyle.Bold;
            setHeader.text = "AUDIO SETTINGS";
            UiFactory.Place(setHeader.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(10f, 128f), new Vector2(400f, 40f));

            musicLabel = AddButton(setPanel.transform, "Music Toggle", blankNormal, blankHover, new Vector2(0f, 42f), new Vector2(310f, 64f), "", 20, OnToggleMusic);
            sfxLabel = AddButton(setPanel.transform, "SFX Toggle", blankNormal, blankHover, new Vector2(0f, -38f), new Vector2(310f, 64f), "", 20, OnToggleSfx);

            AddButton(setPanel.transform, "Back Button", blankNormal, blankHover, new Vector2(0f, -124f), new Vector2(220f, 58f), "BACK", 20, ShowMain);

            // --- 3. Nhóm trang hướng dẫn (TUTORIAL 7 BẢNG) ---
            tutorialGroup = new GameObject("Tutorial Group", typeof(RectTransform));
            tutorialGroup.transform.SetParent(root, false);
            UiFactory.Stretch(tutorialGroup.GetComponent<RectTransform>());

            Image tutPanel = CreateSpriteImage(tutorialGroup.transform, "Tutorial Panel", panelTall, new Vector2(0f, -32f), new Vector2(620f, 480f));

            tutHeader = UiFactory.CreateText(tutPanel.transform, "Tutorial Header", 24, TextAnchor.MiddleCenter, new Color(0.95f, 0.82f, 0.45f));
            tutHeader.fontStyle = FontStyle.Bold;
            UiFactory.Place(tutHeader.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, 162f), new Vector2(520f, 36f));

            // Khung viền chứa ảnh minh họa
            Image picBorder = UiFactory.CreateImage(tutPanel.transform, "Illustration Border", new Color(0.28f, 0.12f, 0.14f, 1f));
            UiFactory.Place(picBorder.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, 52f), new Vector2(332f, 164f));

            Image picBg = UiFactory.CreateImage(picBorder.transform, "Illustration Bg", new Color(0.06f, 0.05f, 0.08f, 1f));
            UiFactory.Place(picBg.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(324f, 156f));

            tutImage = UiFactory.CreateImage(picBg.transform, "Illustration", Color.white);
            tutImage.preserveAspect = true;
            UiFactory.Place(tutImage.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(304f, 144f));

            tutBody = UiFactory.CreateText(tutPanel.transform, "Tutorial Body", 20, TextAnchor.MiddleCenter, new Color(0.92f, 0.9f, 0.94f));
            tutBody.horizontalOverflow = HorizontalWrapMode.Wrap;
            tutBody.verticalOverflow = VerticalWrapMode.Overflow;
            tutBody.lineSpacing = 1.2f;
            UiFactory.Place(tutBody.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, -72f), new Vector2(510f, 96f));

            Text prevTxt = AddButton(tutPanel.transform, "Prev Button", blankNormal, blankHover, new Vector2(-175f, -162f), new Vector2(156f, 52f), "PREV", 18, OnPrevSlide);
            prevBtnObj = prevTxt.transform.parent.gameObject;
            AddButton(tutPanel.transform, "Close Tutorial", blankNormal, blankHover, new Vector2(0f, -162f), new Vector2(156f, 52f), "CLOSE", 18, ShowMain);
            nextBtnLabel = AddButton(tutPanel.transform, "Next Button", blankNormal, blankHover, new Vector2(175f, -162f), new Vector2(156f, 52f), "NEXT >", 18, OnNextSlide);

            RefreshAudioLabels();
            ShowMain();
        }

        void Update()
        {
            Mouse mouse = Mouse.current;
            if (mouse == null) return;
            Vector2 mousePos = mouse.position.ReadValue();
            bool clicked = mouse.leftButton.wasPressedThisFrame;

            for (int i = 0; i < buttons.Count; i++)
            {
                MenuButton btn = buttons[i];
                if (!btn.Rect.gameObject.activeInHierarchy) continue;
                bool hovered = RectTransformUtility.RectangleContainsScreenPoint(btn.Rect, mousePos, null);
                if (btn.Normal != null && btn.Hover != null)
                    btn.Image.sprite = hovered ? btn.Hover : btn.Normal;
                if (hovered && clicked)
                {
                    sound.PlayClick();
                    btn.OnClick?.Invoke();
                    break;
                }
            }
        }

        void OnPlay()
        {
            gameObject.SetActive(false);
            game.StartGame();
        }

        void ShowMain()
        {
            mainGroup.SetActive(true);
            settingsGroup.SetActive(false);
            tutorialGroup.SetActive(false);
        }

        void ShowSettings()
        {
            mainGroup.SetActive(false);
            settingsGroup.SetActive(true);
            tutorialGroup.SetActive(false);
            RefreshAudioLabels();
        }

        void OpenTutorial(int index)
        {
            mainGroup.SetActive(false);
            settingsGroup.SetActive(false);
            tutorialGroup.SetActive(true);
            SetSlide(index);
        }

        void OnPrevSlide()
        {
            if (slideIndex > 0) SetSlide(slideIndex - 1);
        }

        void OnNextSlide()
        {
            if (slideIndex < slides.Length - 1) SetSlide(slideIndex + 1);
            else ShowMain();
        }

        void SetSlide(int index)
        {
            slideIndex = Mathf.Clamp(index, 0, slides.Length - 1);
            TutorialSlide s = slides[slideIndex];
            tutHeader.text = "TUTORIAL (" + (slideIndex + 1) + "/" + slides.Length + ") — " + s.Title;
            tutBody.text = s.Body;
            tutImage.sprite = s.Illustration;
            prevBtnObj.SetActive(slideIndex > 0);
            nextBtnLabel.text = slideIndex == slides.Length - 1 ? "DONE" : "NEXT >";
        }

        void OnToggleMusic()
        {
            sound.ToggleMusic();
            RefreshAudioLabels();
        }

        void OnToggleSfx()
        {
            sound.ToggleSfx();
            RefreshAudioLabels();
        }

        void RefreshAudioLabels()
        {
            bool m = sound.MusicEnabled;
            bool s = sound.SfxEnabled;
            musicLabel.text = m ? "MUSIC: ON" : "MUSIC: OFF";
            musicLabel.color = m ? new Color(0.95f, 0.9f, 0.85f) : new Color(0.55f, 0.5f, 0.52f);

            sfxLabel.text = s ? "SFX: ON" : "SFX: OFF";
            sfxLabel.color = s ? new Color(0.95f, 0.9f, 0.85f) : new Color(0.55f, 0.5f, 0.52f);
        }

        void OnExit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        static TutorialSlide[] BuildSlides(SpriteLibrary sp, Texture2D uiTex)
        {
            return new[]
            {
                new TutorialSlide
                {
                    Title = "FIND 5 KEYS",
                    Body = "To win, find all 5 keys scattered across classrooms and corridors, then unlock the main exit gate.",
                    Illustration = MakeSlideKeys(sp)
                },
                new TutorialSlide
                {
                    Title = "ANGEL PURSUIT",
                    Body = "The Angel statue relentlessly hunts you in the darkness. Shine your flashlight directly at it — it will freeze into stone!",
                    Illustration = MakeSlideAngel(sp)
                },
                new TutorialSlide
                {
                    Title = "EYE STRAIN & BLINKING",
                    Body = "Staring too long strains your eyes. Press [Right Click] to blink quickly. The longer you strain, the longer you will be forced to close your eyes!",
                    Illustration = MakeSlideBlink()
                },
                new TutorialSlide
                {
                    Title = "DOORS [E] & LOCKS [F]",
                    Body = "Press [E] to open/close doors. Closed doors can be broken by the Angel — stand close and hold [F] to lock them, forcing it to find another path.",
                    Illustration = MakeSlideDoor(sp)
                },
                new TutorialSlide
                {
                    Title = "SPRINTING [SHIFT]",
                    Body = "Hold [Shift] while moving to sprint in emergencies. Watch your Stamina bar carefully to avoid exhaustion.",
                    Illustration = MakeSlideRun(sp)
                },
                new TutorialSlide
                {
                    Title = "FLASHLIGHT & BATTERIES",
                    Body = "Press [Left Click] to toggle your flashlight. Be careful: low battery narrows your vision — collect spare batteries scattered across the map.",
                    Illustration = MakeSlideBattery(sp)
                },
                new TutorialSlide
                {
                    Title = "GOOD LUCK!",
                    Body = "Listen closely to footsteps, rattling doors, and your own heartbeat.\nGood luck escaping the abandoned school alive!",
                    Illustration = MakeSlideGoodLuck(sp, uiTex)
                }
            };
        }

        static PixelBuffer NewCardBg()
        {
            var b = new PixelBuffer(80, 38);
            b.Fill(0, 0, 80, 38, new Color32(18, 16, 24, 255));
            b.Fill(0, 0, 80, 8, new Color32(32, 26, 36, 255));
            return b;
        }

        static void CopySprite(PixelBuffer dst, Sprite s, int ox, int oy)
        {
            if (s == null || s.texture == null) return;
            Color32[] px = s.texture.GetPixels32();
            int tw = s.texture.width, th = s.texture.height;
            for (int y = 0; y < th; y++)
                for (int x = 0; x < tw; x++)
                {
                    Color32 c = px[y * tw + x];
                    if (c.a > 0) dst.Set(ox + x, oy + y, c);
                }
        }

        static Sprite MakeSlideKeys(SpriteLibrary sp)
        {
            PixelBuffer b = NewCardBg();
            for (int i = 0; i < 5; i++)
                CopySprite(b, sp.Key, 6 + i * 9, 12);
            // Mũi tên hướng tới cổng EXIT
            b.Fill(52, 18, 6, 2, new Color32(220, 200, 120, 255));
            b.Set(56, 20, new Color32(220, 200, 120, 255));
            b.Set(56, 17, new Color32(220, 200, 120, 255));
            // Cổng sắt + bảng EXIT xanh
            b.Fill(61, 8, 14, 18, Palette.MetalDark);
            for (int x = 63; x <= 73; x += 2) b.Fill(x, 8, 1, 18, Palette.Metal);
            b.Fill(66, 15, 4, 4, Palette.Brass);
            b.Fill(64, 28, 8, 3, Palette.ExitGreen);
            return b.ToSprite(new Vector2(0.5f, 0.5f));
        }

        static Sprite MakeSlideAngel(SpriteLibrary sp)
        {
            PixelBuffer b = NewCardBg();
            // Chùm sáng đèn pin chiếu từ người chơi sang tượng Angel
            for (int x = 18; x <= 56; x++)
            {
                int halfH = 2 + (x - 18) * 10 / 38;
                for (int y = 16 - halfH; y <= 16 + halfH; y++)
                    b.Set(x, y, new Color32(95, 86, 48, 255));
            }
            CopySprite(b, sp.Player, 8, 8);
            CopySprite(b, sp.Angel, 52, 7);
            return b.ToSprite(new Vector2(0.5f, 0.5f));
        }

        static Sprite MakeSlideBlink()
        {
            PixelBuffer b = NewCardBg();
            // Con mắt pixel bên trái
            b.Fill(10, 17, 18, 8, new Color32(225, 220, 230, 255));
            b.Fill(12, 15, 14, 12, new Color32(225, 220, 230, 255));
            b.Fill(16, 17, 6, 8, new Color32(180, 40, 45, 255));
            b.Fill(18, 19, 2, 4, new Color32(15, 10, 18, 255));
            b.Fill(10, 26, 18, 2, new Color32(40, 28, 36, 255));
            b.Fill(10, 14, 18, 2, new Color32(40, 28, 36, 255));
            // Thanh mỏi mắt 3 mức (Xanh -> Vàng -> Đỏ)
            b.Fill(36, 18, 36, 7, new Color32(10, 10, 14, 255));
            b.Fill(37, 19, 11, 5, new Color32(90, 204, 128, 255));
            b.Fill(49, 19, 11, 5, new Color32(235, 204, 76, 255));
            b.Fill(61, 19, 10, 5, new Color32(224, 66, 56, 255));
            return b.ToSprite(new Vector2(0.5f, 0.5f));
        }

        static Sprite MakeSlideDoor(SpriteLibrary sp)
        {
            PixelBuffer b = NewCardBg();
            CopySprite(b, sp.DoorClosed, 16, 11);
            b.Fill(37, 18, 6, 2, new Color32(220, 200, 120, 255));
            b.Set(41, 20, new Color32(220, 200, 120, 255));
            b.Set(41, 17, new Color32(220, 200, 120, 255));
            CopySprite(b, sp.DoorLocked, 48, 11);
            return b.ToSprite(new Vector2(0.5f, 0.5f));
        }

        static Sprite MakeSlideRun(SpriteLibrary sp)
        {
            PixelBuffer b = NewCardBg();
            // Vệt gió tốc độ
            b.Fill(8, 20, 8, 1, new Color32(115, 165, 240, 255));
            b.Fill(6, 16, 10, 1, new Color32(115, 165, 240, 255));
            b.Fill(9, 12, 7, 1, new Color32(115, 165, 240, 255));
            CopySprite(b, sp.PlayerWalk != null && sp.PlayerWalk.Length > 0 ? sp.PlayerWalk[0] : sp.Player, 18, 9);
            // Thanh thể lực xanh dương
            b.Fill(38, 16, 34, 7, new Color32(10, 10, 14, 255));
            b.Fill(39, 17, 26, 5, new Color32(115, 165, 242, 255));
            return b.ToSprite(new Vector2(0.5f, 0.5f));
        }

        static Sprite MakeSlideBattery(SpriteLibrary sp)
        {
            PixelBuffer b = NewCardBg();
            CopySprite(b, sp.Battery, 18, 12);
            CopySprite(b, sp.Battery, 32, 12);
            // Tia chớp / năng lượng nạp pin
            b.Fill(50, 18, 14, 6, new Color32(80, 210, 120, 255));
            b.Fill(64, 19, 2, 4, new Color32(200, 200, 210, 255));
            return b.ToSprite(new Vector2(0.5f, 0.5f));
        }

        static Sprite MakeSlideGoodLuck(SpriteLibrary sp, Texture2D uiTex)
        {
            PixelBuffer b = NewCardBg();
            CopySprite(b, sp.Candle, 16, 10);
            CopySprite(b, sp.Candle, 56, 10);
            if (uiTex != null)
                b.BlitAtlas(uiTex.GetPixels32(), uiTex.width, uiTex.height, 130, 66, 28, 29, 26, 5);
            return b.ToSprite(new Vector2(0.5f, 0.5f));
        }

        static readonly string[] TutorialGlyphRows =
        {
            "####.#..#.####.####.####.##.####.##..",
            "####.#..#.####.####.####.##.####.##..",
            ".##..#..#..##..#..#.#..#.##.#..#.##..",
            ".##..#..#..##..#..#.####.##.####.##..",
            ".##..#..#..##..#..#.###..##.####.##..",
            ".##..####..##..####.#..#.##.#..#.####",
            ".##..####..##..####.#..#.##.#..#.####",
        };

        /// <summary>Tạo nút TUTORIAL với đúng kiểu chữ pixel-art 7 hàng giống hệt PLAY và SETTINGS trong FreeHorrorUi.png.</summary>
        static Sprite MakeTutorialButton(Texture2D uiTex, int srcX, int srcYTop, Color32 textColor)
        {
            var b = new PixelBuffer(52, 15);
            if (uiTex != null)
                b.BlitAtlas(uiTex.GetPixels32(), uiTex.width, uiTex.height, srcX, srcYTop, 52, 15, 0, 0);
            int ox = (52 - TutorialGlyphRows[0].Length) / 2;
            for (int r = 0; r < TutorialGlyphRows.Length; r++)
            {
                int py = 15 - 1 - (3 + r);
                string row = TutorialGlyphRows[r];
                for (int c = 0; c < row.Length; c++)
                    if (row[c] == '#') b.Set(ox + c, py, textColor);
            }
            return b.ToSprite(new Vector2(0.5f, 0.5f));
        }

        static Sprite Slice(Texture2D tex, int x, int yTop, int w, int h)
        {
            if (tex == null) return null;
            return Sprite.Create(tex, new Rect(x, tex.height - yTop - h, w, h), new Vector2(0.5f, 0.5f), 16f);
        }

        static Image CreateSpriteImage(Transform parent, string name, Sprite sprite, Vector2 pos, Vector2 size)
        {
            Image img = UiFactory.CreateImage(parent, name, Color.white);
            img.sprite = sprite;
            UiFactory.Place(img.rectTransform, new Vector2(0.5f, 0.5f), pos, size);
            return img;
        }

        Text AddButton(Transform parent, string name, Sprite normal, Sprite hover, Vector2 pos, Vector2 size, string label, int fontSize, Action onClick)
        {
            Image img = CreateSpriteImage(parent, name, normal, pos, size);
            Text txt = null;
            if (!string.IsNullOrEmpty(label) || fontSize > 0)
            {
                txt = UiFactory.CreateText(img.transform, "Label", fontSize, TextAnchor.MiddleCenter, new Color(0.95f, 0.9f, 0.85f));
                txt.fontStyle = FontStyle.Bold;
                txt.text = label;
                UiFactory.Stretch(txt.rectTransform);
            }
            buttons.Add(new MenuButton { Rect = img.rectTransform, Image = img, Normal = normal, Hover = hover, OnClick = onClick });
            return txt;
        }
    }
}
