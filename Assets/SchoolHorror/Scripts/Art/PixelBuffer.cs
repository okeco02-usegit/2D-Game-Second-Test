using UnityEngine;

namespace SchoolHorror
{
    /// <summary>Vùng đệm điểm ảnh để vẽ pixel-art bằng mã. Gốc tọa độ ở góc dưới-trái.</summary>
    public sealed class PixelBuffer
    {
        public readonly int Width;
        public readonly int Height;
        readonly Color32[] pixels;

        public PixelBuffer(int width, int height)
        {
            Width = width;
            Height = height;
            pixels = new Color32[width * height]; // trong suốt
        }

        public void Set(int x, int y, Color32 c)
        {
            if (x < 0 || y < 0 || x >= Width || y >= Height) return;
            pixels[y * Width + x] = c;
        }

        public void Fill(int x0, int y0, int w, int h, Color32 c)
        {
            for (int y = y0; y < y0 + h; y++)
                for (int x = x0; x < x0 + w; x++)
                    Set(x, y, c);
        }

        /// <summary>Làm tối một điểm ảnh theo hệ số (0..1).</summary>
        public void Darken(int x, int y, float factor)
        {
            if (x < 0 || y < 0 || x >= Width || y >= Height) return;
            Color32 c = pixels[y * Width + x];
            pixels[y * Width + x] = new Color32((byte)(c.r * factor), (byte)(c.g * factor), (byte)(c.b * factor), c.a);
        }

        /// <summary>Dán một vùng sprite từ atlas (tọa độ gốc trên-trái) vào đệm (tọa độ gốc dưới-trái) có trộn alpha.</summary>
        public void BlitAtlas(Color32[] atlas, int atlasW, int atlasH, int srcX, int srcYTop, int w, int h, int dstX, int dstYBottom)
        {
            if (atlas == null) return;
            for (int dy = 0; dy < h; dy++)
            {
                int py = dstYBottom + dy;
                if (py < 0 || py >= Height) continue;
                int imgY = srcYTop + (h - 1 - dy);
                int texY = atlasH - 1 - imgY;
                if (texY < 0 || texY >= atlasH) continue;
                for (int dx = 0; dx < w; dx++)
                {
                    int px = dstX + dx;
                    if (px < 0 || px >= Width) continue;
                    int sx = srcX + dx;
                    if (sx < 0 || sx >= atlasW) continue;
                    Color32 s = atlas[texY * atlasW + sx];
                    if (s.a == 0) continue;
                    if (s.a == 255) pixels[py * Width + px] = s;
                    else
                    {
                        Color32 d = pixels[py * Width + px];
                        int a = s.a, inv = 255 - a;
                        pixels[py * Width + px] = new Color32(
                            (byte)((s.r * a + d.r * inv) / 255),
                            (byte)((s.g * a + d.g * inv) / 255),
                            (byte)((s.b * a + d.b * inv) / 255), 255);
                    }
                }
            }
        }

        /// <summary>Vẽ từ "tranh ký tự": mỗi chuỗi là một hàng (từ trên xuống), mỗi ký tự tra màu trong bảng.</summary>
        public static PixelBuffer FromArt(string[] rows, System.Func<char, Color32> colorOf)
        {
            var buffer = new PixelBuffer(rows[0].Length, rows.Length);
            for (int row = 0; row < rows.Length; row++)
                for (int x = 0; x < rows[row].Length; x++)
                    if (rows[row][x] != '.')
                        buffer.Set(x, rows.Length - 1 - row, colorOf(rows[row][x]));
            return buffer;
        }

        public Sprite ToSprite(Vector2 pivot)
        {
            var texture = new Texture2D(Width, Height, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp
            };
            texture.SetPixels32(pixels);
            texture.Apply();
            return Sprite.Create(texture, new Rect(0, 0, Width, Height), pivot, GameConfig.PixelsPerTile);
        }
    }
}
