using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace WeavyTaskbar.Styles
{
    public static class StarShipStyle
    {
        private static float[] _x, _y, _s, _sp, _bp;
        private static int[] _type, _dir;
        private static bool _ok;
        private static Random _r;
        private static int[] _starX, _starY;
        private static float[] _starB;

        public static void Render(Bitmap bmp, float time, int alpha, float speed)
        {
            time *= speed;
            int w = bmp.Width, h = bmp.Height;
            if (h < 8) return;

            var data = bmp.LockBits(new Rectangle(0, 0, w, h), ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);
            int[] p = new int[w * h];

            // ---- deep space gradient ----
            int topR = 10, topG = 10, topB = 46;
            int botR = 30, botG = 30, botB = 80;
            for (int y = 0; y < h; y++)
            {
                float t = (float)y / (h - 1);
                int r = C((int)(topR + (botR - topR) * t));
                int g = C((int)(topG + (botG - topG) * t));
                int b = C((int)(topB + (botB - topB) * t));
                int bg = (alpha << 24) | (r << 16) | (g << 8) | b;
                for (int x = 0; x < w; x++) p[y * w + x] = bg;
            }

            // ---- init ----
            if (!_ok) Init(w, h);
            _ok = true;

            // ---- stars ----
            for (int i = 0; i < _starX.Length; i++)
            {
                float flicker = (float)(Math.Sin(time * _starB[i] * 10f + i * 2.7f) * 0.5f + 0.5f);
                float bright = 0.3f + flicker * 0.7f;
                int sr = C((int)(200 * bright));
                int sg = C((int)(210 * bright));
                int sb = C((int)(255 * bright));
                int sc = (alpha << 24) | (sr << 16) | (sg << 8) | sb;
                int sx = _starX[i], sy = _starY[i];
                if (sx < w && sy < h) p[sy * w + sx] = sc;
                // cross sparkle on bright stars
                if (flicker > 0.85f)
                {
                    int hsr = C((int)(255 * bright));
                    if (sx > 0) p[sy * w + sx - 1] = (alpha << 24) | (hsr << 16) | (hsr << 8) | hsr;
                    if (sx + 1 < w) p[sy * w + sx + 1] = (alpha << 24) | (hsr << 16) | (hsr << 8) | hsr;
                    if (sy > 0) p[(sy - 1) * w + sx] = (alpha << 24) | (hsr << 16) | (hsr << 8) | hsr;
                    if (sy + 1 < h) p[(sy + 1) * w + sx] = (alpha << 24) | (hsr << 16) | (hsr << 8) | hsr;
                }
            }

            // ---- ships ----
            float dt = 1f / 45f;
            for (int i = 0; i < _x.Length; i++)
            {
                int d = _dir[i];
                _x[i] += _sp[i] * dt * d;
                _y[i] += (float)Math.Sin(time * 0.03f + _bp[i]) * 0.006f * _sp[i];
                if (_y[i] < _s[i] * 0.5f) _y[i] = _s[i] * 0.5f;
                if (_y[i] > h - _s[i] * 0.5f) _y[i] = h - _s[i] * 0.5f;
                if (d == 1 && _x[i] > w + _s[i] * 4f) { _x[i] = -_s[i] * 4f; _y[i] = _s[i] + (float)_r.NextDouble() * (h - _s[i] * 2f); }
                if (d == -1 && _x[i] < -_s[i] * 4f) { _x[i] = w + _s[i] * 4f; _y[i] = _s[i] + (float)_r.NextDouble() * (h - _s[i] * 2f); }

                DrawShip(p, w, h, i, time);
            }

            Marshal.Copy(p, 0, data.Scan0, p.Length);
            bmp.UnlockBits(data);
        }

        private static void Init(int w, int h)
        {
            _r = new Random();
            int n = 3 + _r.Next(3);
            _x = new float[n]; _y = new float[n]; _s = new float[n];
            _sp = new float[n]; _bp = new float[n];
            _type = new int[n]; _dir = new int[n];

            for (int i = 0; i < n; i++)
            {
                _s[i] = h * 0.28f + (float)_r.NextDouble() * h * 0.22f;
                _x[i] = (float)_r.NextDouble() * w;
                _y[i] = _s[i] + (float)_r.NextDouble() * (h - _s[i] * 2f);
                _sp[i] = 8f + (float)_r.NextDouble() * 14f;
                _bp[i] = (float)_r.NextDouble() * 6.28f;
                _dir[i] = _r.Next(2) == 0 ? 1 : -1;
                _type[i] = _r.Next(4);
            }

            // stars
            int sn = Math.Min(80, w * h / 200);
            _starX = new int[sn]; _starY = new int[sn]; _starB = new float[sn];
            for (int i = 0; i < sn; i++)
            {
                _starX[i] = _r.Next(w);
                _starY[i] = _r.Next(h);
                _starB[i] = 0.3f + (float)_r.NextDouble() * 0.7f;
            }
        }

        private static void DrawShip(int[] p, int w, int h, int idx, float time)
        {
            int ix = (int)_x[idx], iy = (int)_y[idx];
            float sz = _s[idx]; int d = _dir[idx]; int tp = _type[idx];
            int len = (int)(sz * 1.5f), hw = (int)(sz * 0.4f);
            if (hw < 2) hw = 2; if (len < 4) len = 4;

            // color schemes per ship type
            int bodyR, bodyG, bodyB, wingR, wingG, wingB, glowR, glowG, glowB;
            switch (tp)
            {
                case 0: bodyR = 200; bodyG = 200; bodyB = 220; wingR = 80; wingG = 80; wingB = 140; glowR = 100; glowG = 200; glowB = 255; break;  // silver-blue
                case 1: bodyR = 220; bodyG = 160; bodyB = 60; wingR = 180; wingG = 100; wingB = 30; glowR = 255; glowG = 180; glowB = 40; break;   // gold
                case 2: bodyR = 80; bodyG = 190; bodyB = 120; wingR = 40; wingG = 140; wingB = 80; glowR = 60; glowG = 255; glowB = 140; break;    // teal-green
                case 3: bodyR = 230; bodyG = 80; bodyB = 80; wingR = 180; wingG = 40; wingB = 40; glowR = 255; glowG = 100; glowB = 60; break;     // red
                case 4: bodyR = 140; bodyG = 100; bodyB = 220; wingR = 100; wingG = 60; wingB = 180; glowR = 180; glowG = 140; glowB = 255; break; // purple
                default: bodyR = 220; bodyG = 220; bodyB = 240; wingR = 60; wingG = 60; wingB = 80; glowR = 255; glowG = 255; glowB = 200; break;  // white-black
            }
            int bodyC = A(bodyR, bodyG, bodyB);
            int wingC = A(wingR, wingG, wingB);
            int glowC = A(glowR, glowG, glowB);

            float flicker = (float)(Math.Sin(time * 8f + _bp[idx]) * 0.3f + 0.7f);

            // ---- body ----
            for (int dx = -len / 2; dx <= len / 2; dx++)
            {
                float pos = (float)dx / (len / 2f);
                int r = (int)(hw * (1f - pos * pos * 0.5f));
                if (r < 0) r = 0;
                for (int dy = -r; dy <= r; dy++)
                {
                    int px = ix + dx * d, py = iy + dy;
                    if (px >= 0 && px < w && py >= 0 && py < h) p[py * w + px] = bodyC;
                }
            }

            // ---- wings (swept back) ----
            int wingBase = ix - (len / 4) * d;
            for (int wi = 0; wi <= len / 3; wi++)
            {
                int wx = wingBase + wi * d;
                int wh = (int)(hw * 1.3f * (1f - (float)wi / (len / 3 + 1)));
                for (int sign = -1; sign <= 1; sign += 2)
                {
                    for (int wj = 1; wj <= wh; wj++)
                    {
                        int wy = iy + sign * (hw + wj);
                        if (wx >= 0 && wx < w && wy >= 0 && wy < h) p[wy * w + wx] = wingC;
                    }
                }
            }

            // ---- cockpit ----
            int cpx = ix + (len / 4) * d;
            for (int cdx = -hw / 2; cdx <= hw / 2; cdx++)
            {
                for (int cdy = -hw / 3; cdy <= hw / 3; cdy++)
                {
                    int px = cpx + cdx, py = iy + cdy;
                    if (px >= 0 && px < w && py >= 0 && py < h)
                        p[py * w + px] = A(180, 230, 255);
                }
            }

            // ---- engine glow ----
            int engX = ix - (len / 2) * d;
            for (int ei = 0; ei <= 2; ei++)
            {
                int glowLen = (int)(hw * 0.8f * flicker);
                for (int ej = 1; ej <= glowLen; ej++)
                {
                    int px = engX - ej * d;
                    int py = iy;
                    if (px >= 0 && px < w && py >= 0 && py < h)
                        p[py * w + px] = glowC;
                    if (py + 1 < h && px >= 0 && px < w)
                        p[(py + 1) * w + px] = glowC;
                    if (py - 1 >= 0 && px >= 0 && px < w)
                        p[(py - 1) * w + px] = glowC;
                }
            }
        }

        private static int A(int r, int g, int b) { return (255 << 24) | (r << 16) | (g << 8) | b; }
        private static int C(int v) { return v < 0 ? 0 : (v > 255 ? 255 : v); }
    }
}
