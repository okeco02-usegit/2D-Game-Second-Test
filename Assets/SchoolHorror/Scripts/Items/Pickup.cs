using UnityEngine;

namespace SchoolHorror
{
    /// <summary>Lớp cha của mọi vật phẩm nhặt được: lơ lửng nhấp nhô lên xuống, đi tới gần là tự nhặt.</summary>
    public abstract class Pickup : MonoBehaviour
    {
        static Sprite shadowSprite;

        protected PlayerController Player { get; private set; }
        protected AudioManager Sound { get; private set; }

        Vector2 basePos;
        float phase;
        GameObject shadowObj;

        public void Init(PlayerController player, AudioManager sound)
        {
            Player = player;
            Sound = sound;
            basePos = transform.position;
            phase = (basePos.x * 1.7f + basePos.y * 2.3f) % 6.28f;

            var sr = GetComponent<SpriteRenderer>();
            if (sr != null) sr.sortingOrder = 7;

            if (shadowSprite == null) shadowSprite = BuildShadow();
            shadowObj = new GameObject(name + " Shadow");
            shadowObj.transform.SetParent(transform.parent, false);
            shadowObj.transform.position = basePos + new Vector2(0f, -0.24f);
            var shadowRenderer = shadowObj.AddComponent<SpriteRenderer>();
            shadowRenderer.sprite = shadowSprite;
            if (sr != null) shadowRenderer.sharedMaterial = sr.sharedMaterial;
            shadowRenderer.sortingOrder = 4;
        }

        void Update()
        {
            float bob = 0.07f + Mathf.Sin(Time.time * 4.2f + phase) * 0.08f;
            transform.position = new Vector3(basePos.x, basePos.y + bob, 0f);

            float range = GameConfig.PickupRange;
            if ((basePos - Player.Position).sqrMagnitude > range * range) return;
            OnCollected();
            if (shadowObj != null) Destroy(shadowObj);
            Destroy(gameObject);
        }

        static Sprite BuildShadow()
        {
            var b = new PixelBuffer(10, 4);
            var dark = new Color32(0, 0, 0, 130);
            b.Fill(2, 0, 6, 4, dark);
            b.Fill(1, 1, 8, 2, dark);
            return b.ToSprite(new Vector2(0.5f, 0.5f));
        }

        protected abstract void OnCollected();
    }
}
