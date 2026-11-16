using System.Collections.Generic;
using UnityEngine;

namespace SchoolHorror
{
    /// <summary>
    /// Mặt nạ bóng tối phủ lên màn hình. Mặc định đen kín; chỉ những điểm ảnh mà tia sáng
    /// từ đèn pin / nến chạm tới (raycasting trên lưới) mới được "khoét" sáng.
    /// </summary>
    public sealed class LightingSystem : MonoBehaviour
    {
        const int TextureWidth = 432;  // đủ phủ tỉ lệ màn hình tới 2.4:1
        const int TextureHeight = 180;
        const int PlayerRayCount = 1024;
        const int ShadeLevels = 12;    // lượng tử hóa độ sáng cho hợp chất pixel-art
        const float TwoPi = Mathf.PI * 2f;

        SchoolMap map;
        Camera cam;
        Flashlight flashlight;
        List<CandleLight> candles;

        Texture2D texture;
        Color32[] pixels;
        readonly float[] playerRays = new float[PlayerRayCount];
        readonly List<CandleLight> visibleCandles = new List<CandleLight>();

        public void Init(SchoolMap schoolMap, Camera camera, Flashlight light, List<CandleLight> candleLights, Material material)
        {
            map = schoolMap;
            cam = camera;
            flashlight = light;
            candles = candleLights;

            texture = new Texture2D(TextureWidth, TextureHeight, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp
            };
            pixels = new Color32[TextureWidth * TextureHeight];

            var spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
            spriteRenderer.sprite = Sprite.Create(texture, new Rect(0, 0, TextureWidth, TextureHeight), new Vector2(0.5f, 0.5f), 1f);
            spriteRenderer.sharedMaterial = material;
            spriteRenderer.sortingOrder = 1000;
        }

        /// <summary>Điểm p có nằm trong vùng sáng của đèn pin hoặc một ngọn nến không.</summary>
        public bool IsIlluminated(Vector2 p)
        {
            if (flashlight.Illuminates(p)) return true;
            foreach (CandleLight candle in candles)
                if (candle.Illuminates(map, p)) return true;
            return false;
        }

        /// <summary>Tính lại mặt nạ cho khung hình hiện tại. Gọi sau khi camera đã về đúng vị trí.</summary>
        public void Render()
        {
            float viewHeight = cam.orthographicSize * 2f;
            // Màn hình rộng hơn 2.4:1 thì phóng to điểm ảnh mặt nạ để vẫn phủ kín bề ngang.
            float pixelSize = Mathf.Max(viewHeight / TextureHeight, viewHeight * cam.aspect / TextureWidth);
            Vector2 camPos = cam.transform.position;
            transform.position = new Vector3(camPos.x, camPos.y, 0f);
            transform.localScale = new Vector3(pixelSize, pixelSize, 1f);
            float originX = camPos.x - TextureWidth * pixelSize * 0.5f;
            float originY = camPos.y - TextureHeight * pixelSize * 0.5f;

            Vector2 lightPos = flashlight.Origin;
            bool beamOn = flashlight.IsOn;
            float beamRange = flashlight.Range;
            float beamHalf = flashlight.HalfAngleRad;
            float beamPower = flashlight.Intensity;
            float aim = Mathf.Atan2(flashlight.AimDirection.y, flashlight.AimDirection.x);
            float beamFar = flashlight.FarBrightness;
            float glow = GameConfig.PlayerGlowRadius;
            float maxReach = GameConfig.VisionRange;

            for (int i = 0; i < PlayerRayCount; i++)
            {
                float a = i * (TwoPi / PlayerRayCount);
                playerRays[i] = map.Raycast(lightPos, new Vector2(Mathf.Cos(a), Mathf.Sin(a)), maxReach);
            }

            CollectVisibleCandles(originX, originY, TextureWidth * pixelSize, TextureHeight * pixelSize);

            for (int y = 0; y < TextureHeight; y++)
            {
                float wy = originY + (y + 0.5f) * pixelSize;
                int tileY = Mathf.FloorToInt(wy);
                int row = y * TextureWidth;
                for (int x = 0; x < TextureWidth; x++)
                {
                    float wx = originX + (x + 0.5f) * pixelSize;
                    bool opaque = map.IsOpaque(Mathf.FloorToInt(wx), tileY);
                    float light = 0f;

                    float dx = wx - lightPos.x, dy = wy - lightPos.y;
                    float d = Mathf.Sqrt(dx * dx + dy * dy);
                    float angle = Mathf.Atan2(dy, dx);
                    float positive = angle < 0f ? angle + TwoPi : angle;
                    int index = (int)(positive * (PlayerRayCount / TwoPi)) % PlayerRayCount;

                    // Chỉ những điểm mắt người chơi nhìn thẳng tới được mới có thể hiện ra: tường và cửa đóng
                    // che hết phía sau, còn cửa mở / cửa sổ / tường sập thì nhìn xuyên qua được.
                    // Ô tường được cộng thêm 1 ô để chính mặt tường bị tia chạm vào vẫn hiện lên.
                    if (d <= playerRays[index] + (opaque ? 1f : 0.02f))
                    {
                        if (d < glow) light = (1f - d / glow) * 0.4f;
                        if (beamOn && d < beamRange)
                        {
                            float diff = Mathf.Abs(angle - aim);
                            if (diff > Mathf.PI) diff = TwoPi - diff;
                            if (diff < beamHalf)
                            {
                                float edge = Mathf.Clamp01((beamHalf - diff) / (beamHalf * 0.3f));
                                float falloff = Mathf.Lerp(1f, beamFar, d / beamRange);
                                light = Mathf.Max(light, beamPower * edge * falloff);
                            }
                        }
                        // Vùng sáng của nến ở xa cũng chỉ thấy được khi nằm trong tầm mắt.
                        for (int c = 0; c < visibleCandles.Count; c++)
                            light = Mathf.Max(light, visibleCandles[c].LightAt(wx, wy, opaque));
                    }

                    int level = Mathf.RoundToInt((1f - Mathf.Clamp01(light)) * ShadeLevels);
                    pixels[row + x] = new Color32(0, 0, 0, (byte)(level * 255 / ShadeLevels));
                }
            }

            texture.SetPixels32(pixels);
            texture.Apply(false);
        }

        void CollectVisibleCandles(float originX, float originY, float width, float height)
        {
            visibleCandles.Clear();
            foreach (CandleLight candle in candles)
            {
                Vector2 p = candle.Position;
                float r = candle.Radius;
                if (p.x < originX - r || p.x > originX + width + r || p.y < originY - r || p.y > originY + height + r) continue;
                candle.Refresh(map, Time.time);
                visibleCandles.Add(candle);
            }
        }
    }
}
