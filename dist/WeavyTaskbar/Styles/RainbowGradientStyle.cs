using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace WeavyTaskbar.Styles
{
    public static class RainbowGradientStyle
    {
        public static void Render(Bitmap bmp, float offset, int alpha, float speed)
        {
            offset *= speed;
            int w = bmp.Width, h = bmp.Height;
            var data = bmp.LockBits(
                new Rectangle(0, 0, w, h),
                ImageLockMode.WriteOnly,
                PixelFormat.Format32bppArgb);

            int[] pixels = new int[w * h];

            for (int px = 0; px < w; px++)
            {
                float hue = ((float)px / w * 360f + offset) % 360f;
                if (hue < 0) hue += 360f;

                Color c = HsvToRgb(hue, 1f, 1f);
                int argb = (alpha << 24) | (c.R << 16) | (c.G << 8) | c.B;

                for (int py = 0; py < h; py++)
                    pixels[py * w + px] = argb;
            }

            Marshal.Copy(pixels, 0, data.Scan0, pixels.Length);
            bmp.UnlockBits(data);
        }

        public static Color HsvToRgb(float h, float s, float v)
        {
            float c = v * s;
            float x = c * (1f - Math.Abs((h / 60f) % 2f - 1f));
            float m = v - c;
            float r, g, b;

            if (h < 60f) { r = c; g = x; b = 0f; }
            else if (h < 120f) { r = x; g = c; b = 0f; }
            else if (h < 180f) { r = 0f; g = c; b = x; }
            else if (h < 240f) { r = 0f; g = x; b = c; }
            else if (h < 300f) { r = x; g = 0f; b = c; }
            else { r = c; g = 0f; b = x; }

            return Color.FromArgb(
                (int)((r + m) * 255f),
                (int)((g + m) * 255f),
                (int)((b + m) * 255f));
        }
    }
}
