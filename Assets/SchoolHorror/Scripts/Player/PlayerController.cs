using UnityEngine;
using UnityEngine.InputSystem;

namespace SchoolHorror
{
    /// <summary>Di chuyển WASD, chạy bằng Shift (tốn thể lực), va chạm với lưới bản đồ và giữ chìa khóa.</summary>
    public sealed class PlayerController : MonoBehaviour
    {
        public float Stamina { get; private set; } = 1f;
        public int Keys { get; private set; }
        public float KeyWarningTimer { get; private set; }
        public Vector2 Position => transform.position;
        public Flashlight Flashlight { get; private set; }

        SchoolMap map;
        GameManager game;
        AudioManager sound;
        SpriteRenderer spriteRenderer;
        SpriteRenderer keyArrowRenderer;
        Sprite idleSprite;
        Sprite[] walkFrames;
        KeyPickup targetKey;
        bool exhausted;
        float animTimer;
        float timeSinceLastKey;

        static readonly string[] ArrowArt =
        {
            ".oo......",
            ".oyoo....",
            ".oyyyoo..",
            ".oyyyyyoo",
            ".oyyyyyyo",
            ".oyyyyyoo",
            ".oyyyoo..",
            ".oyoo....",
            ".oo......",
        };

        public void Init(SchoolMap schoolMap, GameManager gameManager, AudioManager audioManager, Flashlight flashlight, SpriteRenderer renderer, SpriteLibrary sprites)
        {
            map = schoolMap;
            game = gameManager;
            sound = audioManager;
            Flashlight = flashlight;
            spriteRenderer = renderer;
            idleSprite = sprites.Player;
            walkFrames = sprites.PlayerWalk;

            var arrowObj = new GameObject("Key Arrow");
            arrowObj.transform.SetParent(transform, false);
            keyArrowRenderer = arrowObj.AddComponent<SpriteRenderer>();
            keyArrowRenderer.sprite = PixelBuffer.FromArt(ArrowArt, ch => ch == 'y' ? new Color32(255, 215, 70, 255) : new Color32(30, 20, 12, 255)).ToSprite(new Vector2(0.5f, 0.5f));
            keyArrowRenderer.sharedMaterial = sprites.UnlitMaterial;
            keyArrowRenderer.sortingOrder = 1005;
            keyArrowRenderer.enabled = false;
        }

        void Update()
        {
            if (!game.IsPlaying)
            {
                sound.UpdateFootsteps(false, false, TileType.CorridorFloor);
                return;
            }
            if (KeyWarningTimer > 0f) KeyWarningTimer -= Time.deltaTime;
            timeSinceLastKey += Time.deltaTime;
            UpdateKeyArrow();
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
                animTimer += Time.deltaTime * (running ? 11f : 7f);
                if (walkFrames != null && walkFrames.Length > 0)
                    spriteRenderer.sprite = walkFrames[(int)animTimer % walkFrames.Length];
            }
            else
            {
                animTimer = 0f;
                if (idleSprite != null) spriteRenderer.sprite = idleSprite;
            }

            Vector2Int tile = SchoolMap.TileAt(Position);
            sound.UpdateFootsteps(moving, running, map.Get(tile.x, tile.y));

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

        public void AddKey()
        {
            Keys++;
            timeSinceLastKey = 0f;
            targetKey = null;
            if (keyArrowRenderer != null) keyArrowRenderer.enabled = false;
            KeyWarningTimer = 3.6f;
        }

        void UpdateKeyArrow()
        {
            if (keyArrowRenderer == null) return;
            if (timeSinceLastKey < 30f || Keys >= GameConfig.KeyCount || KeyPickup.Active.Count == 0)
            {
                keyArrowRenderer.enabled = false;
                targetKey = null;
                return;
            }

            if (targetKey == null)
                targetKey = KeyPickup.Active[Random.Range(0, KeyPickup.Active.Count)];

            Vector2 dir = ((Vector2)targetKey.transform.position - Position).normalized;
            float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
            keyArrowRenderer.transform.position = Position + new Vector2(0f, 0.2f) + dir * 0.62f;
            keyArrowRenderer.transform.rotation = Quaternion.Euler(0f, 0f, angle);
            keyArrowRenderer.enabled = true;
        }
    }
}
