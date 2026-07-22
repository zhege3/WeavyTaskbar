using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace WeavyTaskbar.Styles
{
    public static class WildWestStyle
    {
        private static int[] _cactX, _cactH, _cactW;
        private static float[] _trwX, _trwY, _trwS, _trwSp, _trwR;
        private static float[,] _bullets; // [i,0]=x, [i,1]=y, [i,2]=dx, [i,3]=dy
        private static float _bulletTimer;
        private static bool _ok;
        private static Random _r;

        public static void Render(Bitmap bmp, float time, int alpha, float speed)
        {
            time *= speed;
            int w = bmp.Width, h = bmp.Height;
            if (h < 10) return;

            var data = bmp.LockBits(new Rectangle(0, 0, w, h), ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);
            int[] p = new int[w * h];

            int skyTopR = 232, skyTopG = 117, skyTopB = 58;
            int skyBotR = 240, skyBotG = 160, skyBotB = 96;
            int gndTopR = 212, gndTopG = 167, gndTopB = 106;
            int gndBotR = 180, gndBotG = 135, gndBotB = 85;
            float gndLine = h * 0.55f;

            for (int y = 0; y < h; y++)
            {
                int r, g, b;
                if (y < gndLine)
                {
                    float t = y / gndLine;
                    r = C((int)(skyTopR + (skyBotR - skyTopR) * t));
                    g = C((int)(skyTopG + (skyBotG - skyTopG) * t));
                    b = C((int)(skyTopB + (skyBotB - skyTopB) * t));
                }
                else
                {
                    float t = (y - gndLine) / (h - gndLine);
                    r = C((int)(gndTopR + (gndBotR - gndTopR) * t));
                    g = C((int)(gndTopG + (gndBotG - gndTopG) * t));
                    b = C((int)(gndTopB + (gndBotB - gndTopB) * t));
                }
                int bg = (alpha << 24) | (r << 16) | (g << 8) | b;
                for (int x = 0; x < w; x++) p[y * w + x] = bg;
            }

            if (!_ok) Init(w, h, (int)gndLine);

            float dt = 1f / 45f;

            // cacti
            for (int i = 0; i < _cactX.Length; i++)
                DrawCactus(p, w, h, _cactX[i], _cactH[i], _cactW[i], (int)gndLine);

            // tumbleweeds (cross screen in ~3 seconds)
            for (int i = 0; i < _trwX.Length; i++)
            {
                _trwX[i] += _trwSp[i] * dt;
                _trwR[i] += _trwSp[i] * dt * 0.12f;
                _trwY[i] = gndLine + 4f + (float)Math.Sin(time * 0.3f + i * 3f) * 1.5f;
                if (_trwX[i] > w + _trwS[i] * 3f) { _trwX[i] = -_trwS[i] * 3f; _trwY[i] = gndLine + 4f + (float)_r.NextDouble() * 3f; }
                DrawTumbleweed(p, w, h, _trwX[i], _trwY[i], _trwS[i], _trwR[i]);
            }

            // bullets: 5s salvo, 6 from left + 6 from right
            _bulletTimer += dt;
            if (_bulletTimer > 5f)
            {
                _bulletTimer = 0f;
                SpawnBullets(w, h, gndLine);
            }
            for (int i = 0; i < _bullets.GetLength(0); i++)
            {
                _bullets[i, 0] += _bullets[i, 2] * dt;
                _bullets[i, 1] += _bullets[i, 3] * dt;
                int bx = (int)_bullets[i, 0], by = (int)_bullets[i, 1];
                if (bx >= -2 && bx < w + 2 && by >= -2 && by < h + 2 && (float)Math.Sqrt(_bullets[i, 2] * _bullets[i, 2] + _bullets[i, 3] * _bullets[i, 3]) > 0.1f)
                {
                    // bullet: bright 3x3 dot
                    int bc = (255 << 24) | (255 << 16) | (240 << 8) | 100;
                    for (int bdx = -1; bdx <= 1; bdx++)
                        for (int bdy = -1; bdy <= 1; bdy++)
                        {
                            int px = bx + bdx, py = by + bdy;
                            if (px >= 0 && px < w && py >= 0 && py < h)
                                p[py * w + px] = bc;
                        }
                    // bright core
                    if (bx >= 0 && bx < w && by >= 0 && by < h)
                        p[by * w + bx] = (255 << 24) | (255 << 16) | (255 << 8) | 255;
                    // trail (3 pixels long)
                    for (int tj = 1; tj <= 3; tj++)
                    {
                        int tx = bx - (int)(_bullets[i, 2] * 0.015f * tj);
                        int ty = by - (int)(_bullets[i, 3] * 0.015f * tj);
                        if (tx >= 0 && tx < w && ty >= 0 && ty < h)
                            p[ty * w + tx] = (255 << 24) | (220 << 16) | (180 << 8) | (100 - tj * 25);
                    }
                    // out of bounds → deactivate
                    if (bx < -5 || bx > w + 5 || by < -5 || by > h + 5)
                    { _bullets[i, 2] = 0; _bullets[i, 3] = 0; }
                }
            }

            Marshal.Copy(p, 0, data.Scan0, p.Length);
            bmp.UnlockBits(data);
        }

        private static void SpawnBullets(int w, int h, float gnd)
        {
            float cy = gnd + 2f + (float)_r.NextDouble() * (h - gnd) * 0.5f;
            for (int i = 0; i < 6; i++)
            {
                bool fromLeft = i < 3;
                _bullets[i, 0] = fromLeft ? -3f : w + 3f;
                _bullets[i, 1] = cy + 1f - i % 3 * 2f + (float)_r.NextDouble() * 1f;
                float spd = w * 2.8f + (float)_r.NextDouble() * w * 1.2f; // cross in ~0.75-1.3s
                _bullets[i, 2] = fromLeft ? spd : -spd;
                _bullets[i, 3] = (float)_r.NextDouble() * 3f - 1.5f;
            }
        }

        private static void Init(int w, int h, int gnd)
        {
            _r = new Random();

            // cacti
            int cn = 5 + _r.Next(4);
            _cactX = new int[cn]; _cactH = new int[cn]; _cactW = new int[cn];
            for (int i = 0; i < cn; i++)
            {
                _cactX[i] = (int)(w * 0.05f + _r.NextDouble() * w * 0.9f);
                _cactH[i] = (int)((h - gnd) * 0.4f + _r.NextDouble() * (h - gnd) * 0.55f);
                _cactW[i] = 2 + _r.Next(3);
            }

            // tumbleweeds (speed = cross in ~3s)
            int tn = 2;
            _trwX = new float[tn]; _trwY = new float[tn]; _trwS = new float[tn]; _trwSp = new float[tn]; _trwR = new float[tn];
            for (int i = 0; i < tn; i++)
            {
                _trwS[i] = (int)(h * 0.15f + _r.NextDouble() * h * 0.15f);
                _trwX[i] = (float)_r.NextDouble() * w;
                _trwY[i] = gnd + 4f;
                _trwSp[i] = w * 0.12f + (float)_r.NextDouble() * w * 0.06f;
                _trwR[i] = (float)_r.NextDouble() * 6.28f;
            }

            // bullets
            _bullets = new float[6, 4];
            _bulletTimer = 0f;

            _ok = true;
        }

        private static void DrawCactus(int[] p, int w, int h, int cx, int ch, int cw, int ground)
        {
            int baseY = ground - 1;
            for (int y = baseY - ch; y <= baseY; y++)
            {
                int dy = baseY - y;
                for (int dx = -cw / 2; dx <= cw / 2; dx++)
                {
                    int px = cx + dx, py = y;
                    if (px >= 0 && px < w && py >= 0 && py < h && py < ground)
                    {
                        bool edge = (dx == -cw / 2 || dx == cw / 2);
                        int r = edge ? 40 : 60;
                        int g = edge ? 100 : 140;
                        int b_ = edge ? 30 : 50;
                        p[py * w + px] = (255 << 24) | (r << 16) | (g << 8) | b_;
                    }
                }
                if (dy == ch * 3 / 4 && cw >= 2)
                {
                    int armLen = ch / 3;
                    int armY = y - armLen / 2;
                    for (int ax = 1; ax <= armLen; ax++)
                        for (int ay = 0; ay < armLen / 2; ay++)
                        {
                            int px = cx - cw / 2 - ax, py = armY + ay;
                            if (px >= 0 && px < w && py >= 0 && py < h && py < ground)
                                p[py * w + px] = (255 << 24) | (55 << 16) | (130 << 8) | 45;
                        }
                }
                if (dy == ch * 2 / 3 && cw >= 2)
                {
                    int armLen = ch / 3;
                    int armY = y - armLen / 2;
                    for (int ax = 1; ax <= armLen; ax++)
                        for (int ay = 0; ay < armLen / 2; ay++)
                        {
                            int px = cx + cw / 2 + ax, py = armY + ay;
                            if (px >= 0 && px < w && py >= 0 && py < h && py < ground)
                                p[py * w + px] = (255 << 24) | (55 << 16) | (130 << 8) | 45;
                        }
                }
            }
        }

        private static void DrawTumbleweed(int[] p, int w, int h, float tx, float ty, float ts, float rot)
        {
            int ix = (int)tx, iy = (int)ty;
            int r = (int)(ts * 0.5f); if (r < 2) r = 2;
            for (int dy = -r; dy <= r; dy++)
                for (int dx = -r; dx <= r; dx++)
                {
                    float dist = (float)Math.Sqrt(dx * dx + dy * dy);
                    if (dist <= r)
                    {
                        float angle = (float)Math.Atan2(dy, dx) + rot;
                        float spokes = (float)Math.Abs(Math.Sin(angle * 6f));
                        float cover = spokes * (1f - dist / r) * 0.6f + (1f - dist / r) * 0.15f;
                        if (cover < 0.15f) continue;
                        int px = ix + dx, py = iy + dy;
                        if (px >= 0 && px < w && py >= 0 && py < h)
                        {
                            int br = C((int)(120 + cover * 40));
                            int bg_ = C((int)(80 + cover * 30));
                            int bb = C((int)(40 + cover * 20));
                            p[py * w + px] = (255 << 24) | (br << 16) | (bg_ << 8) | bb;
                        }
                    }
                }
        }

        private static int C(int v) { return v < 0 ? 0 : (v > 255 ? 255 : v); }
    }
}
