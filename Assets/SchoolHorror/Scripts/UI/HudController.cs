using UnityEngine;
using UnityEngine.UI;

namespace SchoolHorror
{
    /// <summary>Giao diện trong game: thanh pin / mỏi mắt / thể lực, số chìa khóa, gợi ý phím, mí mắt và màn hình kết thúc.</summary>
    public sealed class HudController : MonoBehaviour
    {
        const float IntroSeconds = 7f;
        static readonly Color Safe = new Color(0.35f, 0.8f, 0.5f);
        static readonly Color Warning = new Color(0.92f, 0.8f, 0.3f);
        static readonly Color Critical = new Color(0.88f, 0.26f, 0.22f);
        static readonly Color BatteryColor = new Color(0.95f, 0.88f, 0.55f);
        static readonly Color StaminaColor = new Color(0.45f, 0.65f, 0.95f);

        PlayerController player;
        BlinkSystem blink;
        PlayerInteraction interaction;

        UiBar batteryBar, eyeBar, staminaBar;
        Text keysText, promptText, introText, warningText;
        EyelidOverlay eyelids;
        EndScreen endScreen;
        float playStartTime;

        public void Init(PlayerController playerController, BlinkSystem blinkSystem, PlayerInteraction playerInteraction, Sprite jumpscareFace)
        {
            player = playerController;
            blink = blinkSystem;
            interaction = playerInteraction;

            Transform canvas = UiFactory.CreateCanvas("HUD Canvas", transform).transform;

            // Mí mắt nằm dưới HUD để các thanh trạng thái vẫn đọc được khi đang nhắm mắt
            eyelids = new EyelidOverlay(canvas);

            float quickMark = 1f - GameConfig.FatigueQuickLimit / GameConfig.FatigueMax;
            float mediumMark = 1f - GameConfig.FatigueMediumLimit / GameConfig.FatigueMax;
            batteryBar = new UiBar(canvas, "PIN", 20f);
            eyeBar = new UiBar(canvas, "MỎI MẮT", 46f, quickMark, mediumMark);
            staminaBar = new UiBar(canvas, "THỂ LỰC", 72f);

            keysText = UiFactory.CreateText(canvas, "Keys", 20, TextAnchor.MiddleRight, new Color(0.95f, 0.82f, 0.4f));
            UiFactory.Place(keysText.rectTransform, new Vector2(1f, 1f), new Vector2(-20f, -18f), new Vector2(300f, 26f));

            Text controls = UiFactory.CreateText(canvas, "Controls", 14, TextAnchor.LowerLeft, new Color(0.7f, 0.68f, 0.78f, 0.9f));
            controls.text = "WASD: Di chuyển   Shift: Chạy   Chuột trái: Bật/Tắt đèn   Chuột phải: Chớp mắt   E: Cửa   F: Khóa cửa";
            UiFactory.Place(controls.rectTransform, new Vector2(0f, 0f), new Vector2(20f, 14f), new Vector2(960f, 20f));

            promptText = UiFactory.CreateText(canvas, "Prompt", 20, TextAnchor.MiddleCenter, Color.white);
            UiFactory.Place(promptText.rectTransform, new Vector2(0.5f, 0f), new Vector2(0f, 70f), new Vector2(900f, 28f));

            introText = UiFactory.CreateText(canvas, "Intro", 22, TextAnchor.MiddleCenter, new Color(0.9f, 0.88f, 0.95f));
            introText.text = "Tìm đủ " + GameConfig.KeyCount + " chìa khóa và thoát qua cổng chính.\nĐừng rời mắt khỏi bức tượng.";
            UiFactory.Place(introText.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -110f), new Vector2(900f, 60f));

            warningText = UiFactory.CreateText(canvas, "Angel Warning", 24, TextAnchor.MiddleCenter, Critical);
            warningText.fontStyle = FontStyle.Bold;
            UiFactory.Place(warningText.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -110f), new Vector2(900f, 60f));

            endScreen = new GameObject("End Screen").AddComponent<EndScreen>();
            endScreen.transform.SetParent(canvas, false);
            endScreen.Init(jumpscareFace);
        }

        public void OnGameStart() => playStartTime = Time.time;
        public void ShowJumpscare() => endScreen.ShowJumpscare();
        public void ShowWin() => endScreen.ShowWin();

        /// <summary>Cập nhật HUD theo trạng thái hiện tại. Gọi mỗi khung hình.</summary>
        public void Refresh()
        {
            float battery = player.Flashlight.Battery;
            bool batteryLow = battery < GameConfig.BatteryLowThreshold;
            bool blinkOn = Mathf.Sin(Time.time * 10f) > 0f;
            batteryBar.Set(battery, batteryLow ? (blinkOn ? Critical : Warning) : BatteryColor);

            float fatigue = blink.Fatigue;
            Color eyeColor = fatigue < GameConfig.FatigueQuickLimit ? Safe
                : fatigue < GameConfig.FatigueMediumLimit ? Warning
                : Critical;
            eyeBar.Set(1f - fatigue / GameConfig.FatigueMax, eyeColor);

            staminaBar.Set(player.Stamina, player.Stamina < GameConfig.StaminaMinToRun ? Critical : StaminaColor);

            keysText.text = "CHÌA KHÓA  " + player.Keys + " / " + GameConfig.KeyCount;
            promptText.text = interaction.Prompt;
            bool warn = player.KeyWarningTimer > 0f;
            warningText.enabled = warn;
            if (warn)
            {
                float pulse = 0.65f + 0.35f * Mathf.Sin(Time.time * 14f);
                warningText.color = new Color(1f, 0.22f, 0.2f, pulse);
                warningText.text = "ĐÃ NHẶT CHÌA KHÓA (" + player.Keys + "/" + GameConfig.KeyCount + ") — TƯỢNG ANGEL ĐANG NHANH HƠN!";
            }
            introText.enabled = !warn && (Time.time - playStartTime < IntroSeconds);
            eyelids.SetClosedAmount(blink.LidAmount);
        }
    }
}
