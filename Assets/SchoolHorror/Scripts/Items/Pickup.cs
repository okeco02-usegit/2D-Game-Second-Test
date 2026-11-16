using UnityEngine;

namespace SchoolHorror
{
    /// <summary>Lớp cha của mọi vật phẩm nhặt được: người chơi đi tới gần là tự nhặt.</summary>
    public abstract class Pickup : MonoBehaviour
    {
        protected PlayerController Player { get; private set; }
        protected AudioManager Sound { get; private set; }

        public void Init(PlayerController player, AudioManager sound)
        {
            Player = player;
            Sound = sound;
        }

        void Update()
        {
            float range = GameConfig.PickupRange;
            if (((Vector2)transform.position - Player.Position).sqrMagnitude > range * range) return;
            OnCollected();
            Destroy(gameObject);
        }

        protected abstract void OnCollected();
    }
}
