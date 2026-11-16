using UnityEngine;
using UnityEngine.InputSystem;

namespace SchoolHorror
{
    /// <summary>Di chuyển WASD, chạy bằng Shift (tốn thể lực), va chạm với lưới bản đồ và giữ chìa khóa.</summary>
    public sealed class PlayerController : MonoBehaviour
    {
        public float Stamina { get; private set; } = 1f;
        public int Keys { get; private set; }
        public Vector2 Position => transform.position;
        public Flashlight Flashlight { get; private set; }

        SchoolMap map;
        GameManager game;
        AudioManager sound;
        SpriteRenderer spriteRenderer;
        bool exhausted;
        float stepTimer;

        public void Init(SchoolMap schoolMap, GameManager gameManager, AudioManager audioManager, Flashlight flashlight, SpriteRenderer renderer)
        {
            map = schoolMap;
            game = gameManager;
            sound = audioManager;
            Flashlight = flashlight;
            spriteRenderer = renderer;
        }

        void Update()
        {
            if (!game.IsPlaying) return;
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null) return;

            Vector2 input = new Vector2(
                (keyboard.dKey.isPressed ? 1f : 0f) - (keyboard.aKey.isPressed ? 1f : 0f),
                (keyboard.wKey.isPressed ? 1f : 0f) - (keyboard.sKey.isPressed ? 1f : 0f));
            bool moving = input.sqrMagnitude > 0f;
            bool running = moving && keyboard.leftShiftKey.isPressed && !exhausted;

            UpdateStamina(running);
            if (moving)
            {
                float speed = running ? GameConfig.RunSpeed : GameConfig.WalkSpeed;
                Move(input.normalized * (speed * Time.deltaTime));
                stepTimer -= Time.deltaTime;
                if (stepTimer <= 0f)
                {
                    sound.PlayFootstep();
                    stepTimer = running ? 0.27f : 0.42f;
                }
            }
            else stepTimer = 0f;

            spriteRenderer.flipX = Flashlight.AimDirection.x < 0f;
        }

        void UpdateStamina(bool running)
        {
            if (running)
            {
                Stamina = Mathf.Max(0f, Stamina - GameConfig.StaminaDrainPerSec * Time.deltaTime);
                if (Stamina <= 0f) exhausted = true;
            }
            else
            {
                Stamina = Mathf.Min(1f, Stamina + GameConfig.StaminaRegenPerSec * Time.deltaTime);
                if (Stamina >= GameConfig.StaminaMinToRun) exhausted = false;
            }
        }

        /// <summary>Dịch chuyển từng trục một để trượt dọc theo tường thay vì kẹt lại.</summary>
        void Move(Vector2 delta)
        {
            Vector2 pos = Position;
            if (!Collides(new Vector2(pos.x + delta.x, pos.y))) pos.x += delta.x;
            if (!Collides(new Vector2(pos.x, pos.y + delta.y))) pos.y += delta.y;
            transform.position = pos;
        }

        bool Collides(Vector2 p)
        {
            float r = GameConfig.PlayerRadius;
            int minX = Mathf.FloorToInt(p.x - r), maxX = Mathf.FloorToInt(p.x + r);
            int minY = Mathf.FloorToInt(p.y - r), maxY = Mathf.FloorToInt(p.y + r);
            for (int y = minY; y <= maxY; y++)
                for (int x = minX; x <= maxX; x++)
                    if (map.IsSolid(x, y)) return true;
            return false;
        }

        /// <summary>Thân người chơi có đang chạm vào ô này không (để không đóng cửa đè lên người).</summary>
        public bool OverlapsTile(Vector2Int tile)
        {
            float r = GameConfig.PlayerRadius;
            Vector2 p = Position;
            return p.x + r > tile.x && p.x - r < tile.x + 1 && p.y + r > tile.y && p.y - r < tile.y + 1;
        }

        public void AddKey() => Keys++;
    }
}
