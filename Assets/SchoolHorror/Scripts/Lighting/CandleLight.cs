using UnityEngine;

namespace SchoolHorror
{
    /// <summary>Vùng sáng cố định của một ngọn nến, tính bằng raycasting nên không xuyên tường/cửa đóng.</summary>
    public sealed class CandleLight
    {
        const int RayCount = 256;
        const float Brightness = 0.85f;

        public readonly Vector2 Position;
        public readonly float Radius;
        public float Flicker = 1f;

        readonly float[] rays = new float[RayCount];
        readonly float flickerSeed;

        public CandleLight(Vector2 position, float radius, float flickerSeed)
        {
            Position = position;
            Radius = radius;
            this.flickerSeed = flickerSeed;
        }

        /// <summary>Bắn lại các tia (cửa có thể vừa đóng/mở) và cập nhật độ lung linh.</summary>
        public void Refresh(SchoolMap map, float time)
        {
            for (int i = 0; i < RayCount; i++)
            {
                float angle = i * (Mathf.PI * 2f / RayCount);
                rays[i] = map.Raycast(Position, new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)), Radius);
            }
            Flicker = 0.82f + 0.18f * Mathf.PerlinNoise(time * 3f, flickerSeed);
        }

        /// <summary>Độ sáng (0..1) tại một điểm. opaque = điểm đó nằm trong ô tường (để mặt tường được chiếu sáng).</summary>
        public float LightAt(float wx, float wy, bool opaque)
        {
            float dx = wx - Position.x, dy = wy - Position.y;
            float d2 = dx * dx + dy * dy;
            if (d2 >= Radius * Radius) return 0f;

            float d = Mathf.Sqrt(d2);
            float angle = Mathf.Atan2(dy, dx);
            if (angle < 0f) angle += Mathf.PI * 2f;
            int index = (int)(angle * (RayCount / (Mathf.PI * 2f))) % RayCount;
            if (d > rays[index] + (opaque ? 1f : 0.02f)) return 0f;
            return (1f - d / Radius) * Brightness * Flicker;
        }

        /// <summary>Điểm p có đang được nến này chiếu tới không (dùng cho luật hóa đá của tượng).</summary>
        public bool Illuminates(SchoolMap map, Vector2 p)
        {
            float reach = Radius * 0.9f;
            return (p - Position).sqrMagnitude < reach * reach && map.HasLineOfSight(Position, p);
        }
    }
}
