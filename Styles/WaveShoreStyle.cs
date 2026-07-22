using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace WeavyTaskbar.Styles
{
    public static class WaveShoreStyle
    {
        private static bool _hasHadWave = false;

        public static void Render(Bitmap bmp, float time, int alpha, float speed)
        {
            time *= speed;
            int w = bmp.Width, h = bmp.Height;
            if (h < 4) return;

            var data = bmp.LockBits(new Rectangle(0, 0, w, h), ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);
            int[] pixels = new int[w * h];

            float ct = time % 350f;
            float maxWater = h * 0.75f;
            float wetY = h - maxWater;
            float waterLevel, wetFade;
            bool hasWater;

            if (ct < 196f)
            {
                waterLevel = 0f; hasWater = false;
                wetFade = 1f - Math.Min(1f, ct / 50f);
            }
            else if (ct < 266f)
            {
                float t = (ct - 196f) / 70f;
                t = 1f - (1f - t) * (1f - t);
                waterLevel = maxWater * t; hasWater = true; wetFade = 0f;
            }
            else if (ct < 280f)
            {
                waterLevel = maxWater; hasWater = true; wetFade = 0f;
            }
            else
            {
                float t = (ct - 280f) / 70f;
                t = t * t;
                waterLevel = maxWater * (1f - t);
                hasWater = waterLevel > 0f; wetFade = 1f;
                _hasHadWave = true;
            }

            for (int x = 0; x < w; x++)
            {
                float surfaceX = h - waterLevel;
                if (hasWater)
                {
                    float ripple = (float)Math.Sin(x * 0.006f + ct * 0.02f) * 2.5f;
                    ripple += (float)Math.Sin(x * 0.015f + ct * 0.04f + 0.7f) * 1.2f;
                    surfaceX += ripple;
                    surfaceX = Math.Max(2, Math.Min(h - 1, surfaceX));
                }

                float edgeRipple = (float)Math.Sin(x * 0.008f + ct * 0.015f) * 2f;

                for (int y = 0; y < h; y++)
                {
                    float dy = y - surfaceX;
                    int color;

                    if (hasWater && dy >= -6f)
                    {
                        if (dy < 1f && dy > -6f)
                        {
                            float fuzz = H(x * 17 + y * 11 + (int)(ct * 5), 19);
                            if (fuzz > 0.30f)
                                color = Foam(x, y, alpha, ct);
                            else
                                color = Sand(x, y, alpha);
                        }
                        else if (dy < 4f)
                        {
                            float t = (dy - 1f) / 3f;
                            t = Math.Min(1f, Math.Max(0f, t));
                            color = Blend(Foam(x, y, alpha, ct), Shallow(x, alpha, ct), t);
                        }
                        else
                        {
                            float depth = (dy - 4f) / (h - 4f - surfaceX);
                            depth = Math.Min(1f, Math.Max(0f, depth));
                            color = Water(depth, alpha, x, y, ct);
                        }
                    }
                    else if (y >= wetY && wetFade > 0.01f && _hasHadWave)
                    {
                        int dry = Sand(x, y, alpha);
                        int wet = Wet(x, y, alpha);
                        float dft = y - (wetY + edgeRipple);
                        float fr = Math.Max(6f, (h - wetY) * 0.5f);

                        if (dft < 0f) { color = dry; }
                        else
                        {
                            float t = dft / fr; if (t > 1f) t = 1f;
                            float td = 1f - wetFade;
                            float rd = td + (1f - t) * (1f - td);
                            if (rd > 1f) rd = 1f;
                            color = Blend(wet, dry, rd);
                        }
                    }
                    else
                    {
                        color = Sand(x, y, alpha);
                    }

                    pixels[y * w + x] = color;
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

        private static int Sand(int x, int y, int alpha)
        {
            float grain = (H(x, y) + H(x + 50, y + 50) * 0.3f) / 1.3f;
            int r = C(242 + (int)(grain * 10 - 5));
            int g = C(228 + (int)(grain * 8 - 4));
            int b = C(205 + (int)(grain * 8 - 4));

            int sx = x / 140, sy_c = y / 10;
            if (H(sx * 7, sy_c * 13) > 0.997f && y < 28 && y > 4)
            {
                float sv = H(sx * 23, sy_c * 31);
                int sr = sv > 0.6f ? 255 : (sv > 0.2f ? 249 : 232);
                int sg = sv > 0.6f ? 145 : (sv > 0.2f ? 217 : 168);
                int sb = sv > 0.6f ? 103 : (sv > 0.2f ? 118 : 124);
                return (alpha << 24) | (sr << 16) | (sg << 8) | sb;
            }
            return (alpha << 24) | (r << 16) | (g << 8) | b;
        }

        private static int Wet(int x, int y, int alpha)
        {
            float grain = H(x * 3, y * 7) * 0.2f;
            int r = C(190 + (int)(grain * 12 - 6));
            int g = C(160 + (int)(grain * 10 - 5));
            int b = C(130 + (int)(grain * 8 - 4));
            return (alpha << 24) | (r << 16) | (g << 8) | b;
        }

        private static int Foam(int x, int y, int alpha, float ct)
        {
            float hsh = H(x * 11 + y * 17, (int)(ct * 4));
            float bright = 0.92f + hsh * 0.08f;
            int bc = C((int)(248 + bright * 7));
            int a = (int)(alpha * (0.92f + hsh * 0.08f));
            return (a << 24) | (bc << 16) | (C(bc - 1) << 8) | C(Math.Min(255, bc + 14));
        }

        private static int Shallow(int x, int alpha, float ct)
        {
            float shimmer = (float)(Math.Sin(x * 0.025f + ct * 0.5f) * 0.5f + 0.5f) * 0.08f;
            return (alpha << 24) | (C(110 + (int)(shimmer * 20)) << 16) | (C(195 + (int)(shimmer * 20)) << 8) | C(235 + (int)(shimmer * 20));
        }

        private static int Water(float depth, int alpha, int x, int y, float ct)
        {
            depth = Math.Min(1f, Math.Max(0f, depth));
            int r, g, b;

            if (depth < 0.10f)
            { float t = depth / 0.10f; r = L(135, 100, t); g = L(210, 185, t); b = L(238, 230, t); }
            else if (depth < 0.25f)
            { float t = (depth - 0.10f) / 0.15f; r = L(100, 55, t); g = L(185, 160, t); b = L(230, 215, t); }
            else if (depth < 0.45f)
            { float t = (depth - 0.25f) / 0.20f; r = L(55, 35, t); g = L(160, 130, t); b = L(215, 195, t); }
            else if (depth < 0.70f)
            { float t = (depth - 0.45f) / 0.25f; r = L(35, 22, t); g = L(130, 100, t); b = L(195, 165, t); }
            else
            { float t = (depth - 0.70f) / 0.30f; r = L(22, 14, t); g = L(100, 72, t); b = L(165, 125, t); }

            float caustic = (float)(Math.Sin(x * 0.03f + y * 0.10f + ct * 1.2f) * 0.5f + 0.5f) * 0.6f +
                            (float)(Math.Sin(x * 0.05f + y * 0.07f + ct * 1.8f + 0.8f) * 0.5f + 0.5f) * 0.4f;
            float boost = 1f + caustic * 0.12f * (1f - depth * 0.5f);
            r = C((int)(r * boost));
            g = C((int)(g * boost));
            b = C((int)(b * boost));
            b = Math.Max(b, r + 15);

            return (alpha << 24) | (r << 16) | (g << 8) | b;
        }

        private static int Blend(int c1, int c2, float t)
        {
            if (t < 0f) t = 0f; if (t > 1f) t = 1f;
            int a1 = (c1 >> 24) & 0xFF, r1 = (c1 >> 16) & 0xFF, g1 = (c1 >> 8) & 0xFF, b1 = c1 & 0xFF;
            int a2 = (c2 >> 24) & 0xFF, r2 = (c2 >> 16) & 0xFF, g2 = (c2 >> 8) & 0xFF, b2 = c2 & 0xFF;
            return (C((int)(a1 + (a2 - a1) * t)) << 24) |
                   (C((int)(r1 + (r2 - r1) * t)) << 16) |
                   (C((int)(g1 + (g2 - g1) * t)) << 8) |
                    C((int)(b1 + (b2 - b1) * t));
        }

        private static int C(int v) { return v < 0 ? 0 : (v > 255 ? 255 : v); }
        private static int L(int a, int b, float t) { return C((int)(a + (b - a) * t)); }
    }
}
