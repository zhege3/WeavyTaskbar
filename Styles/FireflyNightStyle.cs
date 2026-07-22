using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace WeavyTaskbar.Styles
{
    public static class FireflyNightStyle
    {
        private static float[] _fx, _fy, _fb, _fp, _fs;
        private static bool _ok;
        private static Random _r;

        public static void Render(Bitmap bmp, float time, int alpha, float speed)
        {
            time *= speed;
            int w = bmp.Width, h = bmp.Height;
            if (h < 8) return;

            var data = bmp.LockBits(new Rectangle(0, 0, w, h), ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);
            int[] p = new int[w * h];

            // night sky + dark grass
            int skyR = 8, skyG = 8, skyB = 24;
            int gndR = 18, gndG = 36, gndB = 10;
            float gndLine = h * 0.65f;

            for (int y = 0; y < h; y++)
            {
                int r, g, b;
                if (y < gndLine)
                {
                    float t = y / gndLine;
                    r = C((int)(skyR + (gndR - skyR) * t));
                    g = C((int)(skyG + (gndG - skyG) * t));
                    b = C((int)(skyB + (gndB - skyB) * t));
                }
                else
                {
                    float t = (y - gndLine) / (h - gndLine);
                    r = C((int)(gndR + t * 5));
                    g = C((int)(gndG + t * 8));
                    b = C((int)(gndB + t * 3));
                }
                int bg = (alpha << 24) | (r << 16) | (g << 8) | b;
                for (int x = 0; x < w; x++) p[y * w + x] = bg;
            }

            if (!_ok) Init(w, h, (int)gndLine);

            for (int i = 0; i < _fx.Length; i++)
            {
                // drifting flight (slow)
                float dx = (float)Math.Sin(time * _fs[i] * 0.25f + _fp[i]) * _fs[i] * 0.25f;
                float dy = (float)Math.Cos(time * _fs[i] * 0.20f + _fp[i] + 1.7f) * _fs[i] * 0.20f;
                _fx[i] += dx;
                _fy[i] += dy;

                // wrap around edges
                if (_fx[i] < -7f) _fx[i] = w + 7f;
                if (_fx[i] > w + 7f) _fx[i] = -7f;
                if (_fy[i] < -7f) _fy[i] = h + 7f;
                if (_fy[i] > h + 7f) _fy[i] = -7f;

                // 4-second pulse (2s fade in, 2s fade out)
                float pulse = (float)(Math.Sin(time * 0.035f + _fp[i]) * 0.5f + 0.5f);
                float bright = 0.25f + pulse * 0.75f;

                int ix = (int)_fx[i], iy = (int)_fy[i];
                DrawGlow(p, w, h, ix, iy, bright);
            }

            // subtle grass blades at bottom
            int gc = (alpha << 24) | (24 << 16) | (48 << 8) | 14;
            for (int x = 0; x < w; x += 4)
            {
                int gh = (int)((h - gndLine) * (0.4f + (float)((x * 479) & 63) / 63f * 0.5f));
                for (int dy = 0; dy < gh; dy++)
                {
                    int py = (int)gndLine + gh - dy;
                    int px = x + dy / 4;
                    if (px >= 0 && px < w && py >= 0 && py < h)
                        p[py * w + px] = gc;
                }
            }

            Marshal.Copy(p, 0, data.Scan0, p.Length);
            bmp.UnlockBits(data);
        }

        private static void DrawGlow(int[] p, int w, int h, int cx, int cy, float bright)
        {
            // soft circular glow
            int r = 7;
            for (int dy = -r; dy <= r; dy++)
            {
                for (int dx = -r; dx <= r; dx++)
                {
                    float dist = (float)Math.Sqrt(dx * dx + dy * dy);
                    if (dist <= r)
                    {
                        float cover = (1f - dist / r) * bright;
                        if (cover < 0.05f) continue;
                        cover = cover * cover;
                        int px = cx + dx, py = cy + dy;
                        if (px < 0 || px >= w || py < 0 || py >= h) continue;
                        int ex = p[py * w + px];
                        int er = (ex >> 16) & 0xFF, eg = (ex >> 8) & 0xFF, eb = ex & 0xFF;
                        int gr = C((int)(er + (255 - er) * cover));
                        int gg = C((int)(eg + (255 - eg) * cover));
                        int gb = C((int)(eb + (140 - eb) * cover));
                        p[py * w + px] = (255 << 24) | (gr << 16) | (gg << 8) | gb;
                    }
                }
            }
            // bright core (always visible, brighter when pulsing)
            if (cx >= 0 && cx < w && cy >= 0 && cy < h && bright > 0.25f)
                p[cy * w + cx] = (255 << 24) | (255 << 16) | (255 << 8) | C((int)(130 + bright * 125));
        }

        private static void Init(int w, int h, int gnd)
        {
            _r = new Random();
            int n = 14;
            _fx = new float[n]; _fy = new float[n]; _fb = new float[n]; _fp = new float[n]; _fs = new float[n];
            for (int i = 0; i < n; i++)
            {
                _fx[i] = 5f + (float)_r.NextDouble() * (w - 10f);
                _fy[i] = 5f + (float)_r.NextDouble() * (h - 10f);
                _fb[i] = 1.5f + (float)_r.NextDouble() * 2f;
                _fp[i] = (float)_r.NextDouble() * 6.28f;
                _fs[i] = 2f + (float)_r.NextDouble() * 4f;
            }
            _ok = true;
        }

        private static int C(int v) { return v < 0 ? 0 : (v > 255 ? 255 : v); }
    }
}
