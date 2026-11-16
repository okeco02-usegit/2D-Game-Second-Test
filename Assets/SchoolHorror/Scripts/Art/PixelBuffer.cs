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
