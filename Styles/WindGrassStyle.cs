using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace WeavyTaskbar.Styles
{
    public static class WindGrassStyle
    {
        private static int[] _bgH, _bgP, _mdH, _mdP, _fgH, _fgP;
        private static float[] _bgC, _mdC, _fgC;
        private static bool _ok;
        private static Random _r;

        public static void Render(Bitmap bmp, float time, int alpha, float speed)
        {
            time *= speed;
            int w = bmp.Width, h = bmp.Height;
            if (h < 6) return;

            var data = bmp.LockBits(new Rectangle(0, 0, w, h), ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);
            int[] p = new int[w * h];

            // soil background
            for (int y = 0; y < h; y++)
            {
                float t = (float)y / (h - 1);
                int r = C((int)(90 + t * 40));
                int g = C((int)(110 + t * 55));
                int b = C((int)(50 + t * 25));
                int bg = (alpha << 24) | (r << 16) | (g << 8) | b;
                for (int x = 0; x < w; x++) p[y * w + x] = bg;
            }

            if (!_ok) Init(w, h);

            // cloud shadow
            float shadow = 1f - (float)(Math.Sin(time * 0.08f + 1.3f) * 0.5f + 0.5f) * 0.04f;

            // draw background layer first
            DrawLayer(p, w, h, _bgH, _bgP, _bgC, time, 0.12f, alpha, shadow);
            // middle layer
            DrawLayer(p, w, h, _mdH, _mdP, _mdC, time, 0.20f, alpha, shadow);
            // foreground layer on top
            DrawLayer(p, w, h, _fgH, _fgP, _fgC, time, 0.30f, alpha, shadow);

            // occasional flowers
            for (int i = 0; i < 5; i++)
            {
                int fx = (int)(H(i, 20) * w); if (fx >= w) fx = w - 1;
                int fy = h - 3 - (int)(H(i, 21) * h * 0.3f);
                if (H(i, 22) > 0.94f && fy > 1 && fy < h)
                {
                    int col = new int[] { 0xFF6B8A, 0xFFB347, 0xFFFFFF, 0xF7DC6F }[i % 4];
                    int fr = (col >> 16) & 0xFF, fg = (col >> 8) & 0xFF, fb = col & 0xFF;
                    for (int dy = -1; dy <= 1; dy++)
                        for (int dx = -1; dx <= 1; dx++)
                        {
                            int px = fx + dx, py = fy + dy;
                            if (px >= 0 && px < w && py >= 0 && py < h)
                            {
                                int idx = py * w + px;
                                int er = (p[idx] >> 16) & 0xFF, eg = (p[idx] >> 8) & 0xFF, eb = p[idx] & 0xFF;
                                p[idx] = (alpha << 24) | (C((int)(er + (fr - er) * 0.7f)) << 16) | (C((int)(eg + (fg - eg) * 0.7f)) << 8) | C((int)(eb + (fb - eb) * 0.7f));
                            }
                        }
                }
            }

            Marshal.Copy(p, 0, data.Scan0, p.Length);
            bmp.UnlockBits(data);
        }

        private static void Init(int w, int h)
        {
            _r = new Random();
            int bgCount = w / 2 + 1;
            int mdCount = w / 3 + 1;
            int fgCount = w / 5 + 1;
            _bgH = new int[bgCount]; _bgP = new int[bgCount]; _bgC = new float[bgCount];
            _mdH = new int[mdCount]; _mdP = new int[mdCount]; _mdC = new float[mdCount];
            _fgH = new int[fgCount]; _fgP = new int[fgCount]; _fgC = new float[fgCount];

            for (int i = 0; i < bgCount; i++)
            {
                int x = i * 2;
                _bgH[i] = (int)(h * (0.25f + H(x, 0) * 0.15f));
                _bgP[i] = (int)(H(x, 3) * 120f);
                _bgC[i] = H(x, 7) * 0.10f - 0.05f;
            }
            for (int i = 0; i < mdCount; i++)
            {
                int x = i * 3 + _r.Next(2);
                _mdH[i] = (int)(h * (0.45f + H(x, 1) * 0.18f));
                _mdP[i] = (int)(H(x, 4) * 160f);
                _mdC[i] = H(x, 8) * 0.12f - 0.06f;
            }
            for (int i = 0; i < fgCount; i++)
            {
                int x = i * 5 + _r.Next(3);
                _fgH[i] = (int)(h * (0.65f + H(x, 2) * 0.22f));
                _fgP[i] = (int)(H(x, 5) * 200f);
                _fgC[i] = H(x, 9) * 0.14f - 0.07f;
            }
            _ok = true;
        }

        private static void DrawLayer(int[] p, int w, int h, int[] heights, int[] phases, float[] colors, float time, float windSpeed, int alpha, float shadow)
        {
            int count = heights.Length;
            float spacing = (float)(w - 1) / (count - 1);

            for (int i = 0; i < count; i++)
            {
                int x = (int)(i * spacing + 0.5f);
                int bladeH = heights[i];
                int tipY = h - bladeH;
                if (tipY < 0) tipY = 0;
                float colorShift = colors[i];

                // primary wind + secondary gust
                float sway = (float)Math.Sin(x * 0.025f + time * windSpeed + phases[i] * 0.01f) * 3f;
                sway += (float)Math.Sin(x * 0.05f + time * windSpeed * 0.7f + phases[i] * 0.02f + 1.2f) * 1.2f;
                sway += (float)Math.Sin(x * 0.012f + time * windSpeed * 0.4f + 2.5f) * 1.5f;

                for (int y = h - 1; y >= tipY; y--)
                {
                    float t = (float)(h - 1 - y) / bladeH;
                    if (t > 1f) t = 1f;

                    // curve: quadratic bezier effect — sway strongest at tip
                    float curve = sway * t * t;

                    // width: wider at base, narrow at tip
                    float width = 1.8f - t * 1.3f;

                    // color: dark green base → bright tip
                    int r = C((int)(70 + t * 90 + colorShift * 50));
                    int g = C((int)(100 + t * 110 + colorShift * 60));
                    int b = C((int)(40 + t * 50 + colorShift * 30));

                    // apply cloud shadow
                    r = C((int)(r * shadow));
                    g = C((int)(g * shadow));
                    b = C((int)(b * shadow));

                    int cx = x + (int)(curve + 0.5f);
                    int iw = width > 1.5f ? 2 : 1;

                    for (int wi = 0; wi < iw; wi++)
                    {
                        int px = cx + wi - iw / 2;
                        if (px >= 0 && px < w)
                        {
                            int idx = y * w + px;
                            int er = (p[idx] >> 16) & 0xFF;
                            int eg = (p[idx] >> 8) & 0xFF;
                            int eb = p[idx] & 0xFF;
                            float blend = 0.5f + 0.2f * (1f - t);
                            p[idx] = (alpha << 24) |
                                (C((int)(er + (r - er) * blend)) << 16) |
                                (C((int)(eg + (g - eg) * blend)) << 8) |
                                C((int)(eb + (b - eb) * blend));
                        }
                    }
                }
            }
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
