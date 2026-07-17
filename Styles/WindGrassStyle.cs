using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace WeavyTaskbar.Styles
{
    public static class WindGrassStyle
    {
        public static void Render(Bitmap bmp, float time, int alpha, float speed)
        {
            time *= speed;
            int w = bmp.Width, h = bmp.Height;
            if (h < 4) return;

            var data = bmp.LockBits(new Rectangle(0, 0, w, h), ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);
            int[] pixels = new int[w * h];

            // 背景：草地的土地底色，带深浅变化
            for (int y = 0; y < h; y++)
            {
                float t = (float)y / (h - 1);
                int r = C((int)(130 - t * 35));
                int g = C((int)(170 - t * 40));
                int b = C((int)(90 - t * 25));
                int color = (alpha << 24) | (r << 16) | (g << 8) | b;
                for (int x = 0; x < w; x++) pixels[y * w + x] = color;
            }

            // 草：每3px一根，颜色有差异
            int spacing = 3;
            int count = w / spacing + 1;

            for (int i = 0; i < count; i++)
            {
                int x = i * spacing;
                float height = 0.60f + H(x, 0) * 0.30f;
                float bladeH = h * height;
                int tipY = (int)(h - bladeH);
                if (tipY < 0) tipY = 0;

                // 每根草的颜色偏移
                float colorShift = H(x, 5) * 0.15f - 0.075f;

                // 草的摆动：风从左向右
                float windSway = (float)Math.Sin(x * 0.028 + time * 0.18) * 2.5f;
                windSway += (float)Math.Sin(x * 0.045 + time * 0.12 + 1.2f) * 1.0f;

                for (int y = h - 1; y >= tipY; y--)
                {
                    float t = (float)(h - 1 - y) / bladeH;
                    if (t > 1f) t = 1f;

                    // 从底部深绿到顶部亮绿黄
                    int r = C((int)(90 + t * 70 + colorShift * 40));
                    int g = C((int)(130 + t * 80 + colorShift * 50));
                    int b = C((int)(50 + t * 30 + colorShift * 20));
                    r = C(r); g = C(g); b = C(b);

                    // 草叶宽：底部粗顶部细
                    float width = 1.5f - t * 0.8f;
                    int drawW = Math.Max(1, (int)(width + 0.5f));

                    // 草尖弯曲
                    float tipSway = windSway * t * t * 1.5f;
                    int dx = (int)(x + tipSway + 0.5f);

                    for (int wi = 0; wi < drawW; wi++)
                    {
                        int px = dx + wi - drawW / 2;
                        if (px >= 0 && px < w)
                        {
                            int idx = y * w + px;
                            int er = (pixels[idx] >> 16) & 0xFF;
                            int eg = (pixels[idx] >> 8) & 0xFF;
                            int eb = pixels[idx] & 0xFF;
                            float blend = 0.4f + 0.2f * (1f - t);
                            pixels[idx] = (alpha << 24) |
                                (C((int)(er + (r - er) * blend)) << 16) |
                                (C((int)(eg + (g - eg) * blend)) << 8) |
                                C((int)(eb + (b - eb) * blend));
                        }
                    }
                }
            }

            // 小花点缀
            for (int i = 0; i < 6; i++)
            {
                int fx = (int)(H(i, 20) * w);
                int fy = h - 2 - (int)(H(i, 21) * h * 0.35f);
                if (H(i, 22) > 0.94f && fy > 2)
                {
                    int col = new int[] { 0xFF6B8A, 0xFFB347, 0xFF85A1, 0xF7DC6F }[(int)(H(i, 23) * 3)];
                    int r = (col >> 16) & 0xFF, g = (col >> 8) & 0xFF, b = col & 0xFF;
                    for (int dy = -1; dy <= 1; dy++) for (int dx = -1; dx <= 1; dx++)
                        {
                            int px = fx + dx, py = fy + dy;
                            if (px >= 0 && px < w && py >= 0 && py < h)
                            {
                                int idx = py * w + px;
                                int er = (pixels[idx] >> 16) & 0xFF, eg = (pixels[idx] >> 8) & 0xFF, eb = pixels[idx] & 0xFF;
                                float blend = 0.6f;
                                pixels[idx] = (alpha << 24) |
                                    (C((int)(er + (r - er) * blend)) << 16) |
                                    (C((int)(eg + (g - eg) * blend)) << 8) |
                                    C((int)(eb + (b - eb) * blend));
                            }
                        }
                }
            }

            Marshal.Copy(pixels, 0, data.Scan0, pixels.Length);
            bmp.UnlockBits(data);
        }

        private static float H(int x, int y)
        {
            unchecked
            {
                int n = (x * 1619 + y * 31337) & 0x7FFFFFFF;
                n = (n << 13) ^ n;
                return (float)((n * (n * n * 15731 + 789221) + 1376312589) & 0x7FFFFFFF) / 0x7FFFFFFF;
            }
        }

        private static int C(int v) { return v < 0 ? 0 : (v > 255 ? 255 : v); }
    }
}