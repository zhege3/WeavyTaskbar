using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace WeavyTaskbar.Styles
{
    public static class SeaFishStyle
    {
        private static float[] _x, _y, _s, _sp, _bp;
        private static int[] _bodyTop, _bodyBot, _bodyMid, _finClr, _eyeW, _eyeD;
        private static int[] _dir;
        private static bool _ok;
        private static Random _r;

        public static void Render(Bitmap bmp, float time, int alpha, float speed)
        {
            time *= speed;
            int w = bmp.Width, h = bmp.Height;
            if (h < 10) return;

            var data = bmp.LockBits(new Rectangle(0, 0, w, h), ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);
            int[] p = new int[w * h];

            for (int y = 0; y < h; y++)
            {
                float d = (float)y / (h - 1);
                int r, g, b;
                if (d < 0.10f) { float t = d / 0.10f; r = L(135, 100, t); g = L(210, 185, t); b = L(238, 230, t); }
                else if (d < 0.25f) { float t = (d - 0.10f) / 0.15f; r = L(100, 55, t); g = L(185, 160, t); b = L(230, 215, t); }
                else if (d < 0.45f) { float t = (d - 0.25f) / 0.20f; r = L(55, 35, t); g = L(160, 130, t); b = L(215, 195, t); }
                else if (d < 0.70f) { float t = (d - 0.45f) / 0.25f; r = L(35, 22, t); g = L(130, 100, t); b = L(195, 165, t); }
                else { float t = (d - 0.70f) / 0.30f; r = L(22, 14, t); g = L(100, 72, t); b = L(165, 125, t); }
                int wc = (alpha << 24) | (r << 16) | (g << 8) | b;
                for (int x = 0; x < w; x++) p[y * w + x] = wc;
            }

            if (!_ok) { Init(w, h); _ok = true; }

            float dt = 1f / 45f;
            for (int i = 0; i < _x.Length; i++)
            {
                int d = _dir[i];
                _x[i] += _sp[i] * dt * d;
                _y[i] += (float)Math.Sin(time * 0.04f + _bp[i]) * 0.008f * _sp[i];
                if (_y[i] < _s[i] * 0.5f) _y[i] = _s[i] * 0.5f;
                if (_y[i] > h - _s[i] * 0.5f) _y[i] = h - _s[i] * 0.5f;
                if (d == 1 && _x[i] > w + _s[i] * 3f) { _x[i] = -_s[i] * 3f; _y[i] = _s[i] + (float)_r.NextDouble() * (h - _s[i] * 2f); }
                if (d == -1 && _x[i] < -_s[i] * 3f) { _x[i] = w + _s[i] * 3f; _y[i] = _s[i] + (float)_r.NextDouble() * (h - _s[i] * 2f); }

                DrawFish(p, w, h, i, time);
            }

            Marshal.Copy(p, 0, data.Scan0, p.Length);
            bmp.UnlockBits(data);
        }

        private static void Init(int w, int h)
        {
            _r = new Random();
            int n = w / 180; if (n < 3) n = 3; if (n > 12) n = 12;
            _x = new float[n]; _y = new float[n]; _s = new float[n];
            _sp = new float[n]; _bp = new float[n];
            _bodyTop = new int[n]; _bodyBot = new int[n]; _bodyMid = new int[n];
            _finClr = new int[n]; _eyeW = new int[n]; _eyeD = new int[n]; _dir = new int[n];

            int[][] schemes = new int[][] {
                new int[]{ 255,130,60,  255,200,140,  240,90,30,  220,40,20,  140,60,20 },  // clownfish orange
                new int[]{ 40,130,240,  120,210,255,  20,70,200,  20,40,140,  10,30,90 },    // blue tang
                new int[]{ 230,60,80,   255,160,160,  200,30,50,  160,20,40,  100,30,40 },    // red jewel
                new int[]{ 70,190,100,  160,235,170,  40,140,60,  30,90,40,  20,60,30 },      // green cichlid
                new int[]{ 200,160,60,  240,210,120,  170,120,30,  130,80,20,  90,50,15 },    // gold barb
            };

            for (int i = 0; i < n; i++)
            {
                _s[i] = h * 0.28f + (float)_r.NextDouble() * h * 0.22f;
                _x[i] = (float)_r.NextDouble() * w;
                _y[i] = _s[i] + (float)_r.NextDouble() * (h - _s[i] * 2f);
                _sp[i] = 5f + (float)_r.NextDouble() * 10f;
                _bp[i] = (float)_r.NextDouble() * 6.28f;
                _dir[i] = _r.Next(2) == 0 ? 1 : -1;
                int si = _r.Next(schemes.Length);
                var s = schemes[si];
                _bodyTop[i] = A(s[0], s[1], s[2]);
                _bodyMid[i] = A(s[3], s[4], s[5]);
                _bodyBot[i] = A(s[6], s[7], s[8]);
                _finClr[i] = A(C(s[0]-20), C(s[1]-20), C(s[2]-15));
                _eyeW[i] = A(255, 255, 255);
                _eyeD[i] = A(10, 10, 10);
            }
        }

        private static void DrawFish(int[] p, int w, int h, int idx, float time)
        {
            int ix = (int)_x[idx], iy = (int)_y[idx];
            float sz = _s[idx];
            int d = _dir[idx];
            int len = (int)(sz * 1.5f), hw = (int)(sz * 0.42f);
            if (hw < 2) hw = 2; if (len < 4) len = 4;

            float undu = (float)Math.Sin(time * 3.5f + _bp[idx] + (float)idx) * 0.6f;
            float tailW = (float)Math.Sin(time * 4f + _bp[idx] + idx) * (hw > 3 ? 1.8f : 1.2f);

            // ---- body: teardrop shape with countershading ----
            for (int dx = -len / 2; dx <= len / 2; dx++)
            {
                float pos = (float)dx / (len / 2f + 0.01f); // -1(tail) to 1(head)
                // body width curve: widest near head, tapers to tail
                float wr = pos < 0.3f ? (0.35f + 0.65f * (1f - Math.Abs(pos - 0.3f) / 1.3f))
                                      : 0.35f + 0.65f * (1f - Math.Abs(pos));
                wr *= hw;
                int r = (int)(wr + undu * (1f - Math.Abs(pos)) * 1.5f);
                if (r < 0) r = 0;

                for (int dy = -r; dy <= r; dy++)
                {
                    int px = ix + dx * d, py = iy + dy;
                    if (px < 0 || px >= w || py < 0 || py >= h) continue;

                    float shade = (float)dy / (r + 0.01f); // -1(top) to 1(bottom)
                    shade = (shade + 1f) * 0.5f; // 0(top) to 1(bottom)
                    int clr;
                    if (shade < 0.3f) clr = _bodyTop[idx];
                    else if (shade < 0.7f) clr = _bodyMid[idx];
                    else clr = _bodyBot[idx];
                    p[py * w + px] = clr;
                }
            }

            // ---- tail: forked with oscillation ----
            int tailBase = ix - (len / 2) * d;
            int tailLen = hw + 2;
            float split = (float)(hw * 0.6f);

            for (int ti = 1; ti <= tailLen; ti++)
            {
                float tp = (float)ti / tailLen;
                int px = tailBase - ti * d;
                if (px < 0 || px >= w) continue;

                for (int s = -1; s <= 1; s += 2)
                {
                    float offset = tp * tailW * s * 0.7f + s * split * tp * 0.5f;
                    int py = iy + (int)offset;
                    if (py >= 0 && py < h) p[py * w + px] = _finClr[idx];
                }
                // center spine
                if (iy >= 0 && iy < h) p[iy * w + px] = _finClr[idx];
            }

            // ---- dorsal fin (top) ----
            int dfStart = ix - (len / 6) * d;
            for (int di = 0; di <= len / 3; di++)
            {
                int fh = (int)(hw * 0.55f * (1f - (float)di / (len / 3 + 1)));
                int px = dfStart + di * d;
                for (int fj = 1; fj <= fh; fj++)
                {
                    int py = iy - hw - fj;
                    if (px >= 0 && px < w && py >= 0 && py < h)
                        p[py * w + px] = _finClr[idx];
                }
            }

            // ---- pectoral fin (side) ----
            int pfX = ix + (len / 5) * d;
            for (int pi = -1; pi <= 1; pi++)
            {
                int px = pfX + pi * d;
                int py = iy + (int)(hw * 0.8f) + 1;
                if (px >= 0 && px < w && py >= 0 && py < h)
                    p[py * w + px] = _finClr[idx];
            }

            // ---- eye ----
            int ex = ix + (len / 3) * d, eyBase = iy - hw / 3;
            for (int edy = -1; edy <= 0; edy++)
            {
                int ey = eyBase + edy;
                if (ex >= 0 && ex < w && ey >= 0 && ey < h) p[ey * w + ex] = _eyeW[idx];
            }
            int pupX = ex + d;
            int pupY = eyBase;
            if (pupX >= 0 && pupX < w && pupY >= 0 && pupY < h) p[pupY * w + pupX] = _eyeD[idx];

            // ---- mouth line ----
            int mouthX = ix + (len / 2 - 1) * d, mouthY = iy;
            if (mouthX >= 0 && mouthX < w && mouthY >= 0 && mouthY < h)
                p[mouthY * w + mouthX] = _eyeD[idx];
        }

        private static int A(int r, int g, int b) { return (255 << 24) | (r << 16) | (g << 8) | b; }
        private static int C(int v) { return v < 0 ? 0 : (v > 255 ? 255 : v); }
        private static int L(int a, int b, float t) { return C((int)(a + (b - a) * t)); }
    }
}
