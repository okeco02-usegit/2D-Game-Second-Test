using UnityEngine;
using UnityEngine.InputSystem;

namespace SchoolHorror
{
    /// <summary>
    /// Cơ chế mỏi mắt: thanh mỏi mắt chạy 0 → 12 giây. Chuột phải để chớp mắt;
    /// chớp càng muộn thì phải nhắm càng lâu (0.5s / 1s / 2s). Quá 12 giây sẽ tự chớp 2 giây.
    /// </summary>
    public sealed class BlinkSystem : MonoBehaviour
    {
        const float CloseTime = 0.1f;

        /// <summary>Số giây đã trôi qua kể từ lần chớp mắt gần nhất.</summary>
        public float Fatigue { get; private set; }
        /// <summary>True trong suốt thời gian chớp mắt: tượng coi như không bị nhìn.</summary>
        public bool EyesClosed { get; private set; }
        /// <summary>Độ khép của mí mắt, 0 = mở hẳn, 1 = nhắm kín.</summary>
        public float LidAmount { get; private set; }

        GameManager game;
        AudioManager sound;
        float blinkElapsed;
        float blinkDuration;
        float openTime;

        public void Init(GameManager gameManager, AudioManager audioManager)
        {
            game = gameManager;
            sound = audioManager;
        }

        public static float BlinkDurationFor(float fatigue)
        {
            if (fatigue < GameConfig.FatigueQuickLimit) return GameConfig.BlinkQuick;
            if (fatigue < GameConfig.FatigueMediumLimit) return GameConfig.BlinkMedium;
            return GameConfig.BlinkLong;
        }

        void Update()
        {
            if (!game.IsPlaying) return;

            if (EyesClosed)
            {
                blinkElapsed += Time.deltaTime;
                if (blinkElapsed >= blinkDuration)
                {
                    EyesClosed = false;
                    LidAmount = 0f;
                    Fatigue = 0f;
                }
                else if (blinkElapsed < CloseTime) LidAmount = blinkElapsed / CloseTime;
                else if (blinkElapsed > blinkDuration - openTime) LidAmount = (blinkDuration - blinkElapsed) / openTime;
                else LidAmount = 1f;
                return;
            }

            Fatigue += Time.deltaTime;
            Mouse mouse = Mouse.current;
            if (Fatigue >= GameConfig.FatigueMax) StartBlink(GameConfig.BlinkLong);
            else if (mouse != null && mouse.rightButton.wasPressedThisFrame) StartBlink(BlinkDurationFor(Fatigue));
        }

        void StartBlink(float duration)
        {
            EyesClosed = true;
            blinkElapsed = 0f;
            blinkDuration = duration;
            // Chớp nhanh thì mở mắt ngay; bị phạt 1s/2s thì tối đen rồi mới mở từ từ.
            openTime = duration <= GameConfig.BlinkQuick ? 0.15f : 0.45f;
            if (sound != null) sound.PlayBlink();
        }
    }
}
