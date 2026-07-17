using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace WeavyTaskbar.Styles
{
    public static class CloudDriftStyle
    {
        private class CloudData
        {
            public float x;
            public float pixelSpeed;
            public float size;
            public float baseY;
            public float[] circles;
            public float[] softness;
            public int circleCount;
        }

        private enum State
        {
            Idle,
            Generating,
            Stopping
        }

        private static List<CloudData> clouds = new List<CloudData>();
        private static State state = State.Idle;
        private static float stateStartTime = 0f;
        private static float nextGenerateTime = 0f;
        private static float lastFrameTime = 0f;
        private static Random rand = new Random();

        private const float IDLE_DURATION = 2f;
        private const float GENERATE_DURATION = 300f;
        private const float MIN_GEN_INTERVAL = 30f;
        private const float MAX_GEN_INTERVAL = 60f;

        public static void Reset()
        {
            clouds.Clear();
            state = State.Idle;
            stateStartTime = 0f;
            nextGenerateTime = 0f;
            lastFrameTime = 0f;
        }

        public static void Render(Bitmap bmp, float time, int alpha, float speed)
        {
            int w = bmp.Width, h = bmp.Height;
            if (h < 4) return;

            time = time / 45f;

            float rawDelta = time - lastFrameTime;
            float deltaTime = rawDelta * speed;
            if (rawDelta <= 0f || rawDelta > 0.5f) { rawDelta = 0.016f; deltaTime = 0.016f * speed; }
            lastFrameTime = time;

            if (stateStartTime == 0f) stateStartTime = time;

            var data = bmp.LockBits(new Rectangle(0, 0, w, h), ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);
            int[] pixels = new int[w * h];

            // ---- 天空 ----
            for (int y = 0; y < h; y++)
            {
                float t = (float)y / (h - 1);
                int r = C(74 - (int)(t * 15));
                int g = C(144 - (int)(t * 10));
                int b = C(217 + (int)(t * 20));
                int color = (alpha << 24) | (r << 16) | (g << 8) | b;
                for (int x = 0; x < w; x++)
                    pixels[y * w + x] = color;
            }

            float elapsed = time - stateStartTime;

            switch (state)
            {
                case State.Idle:
                    if (elapsed >= IDLE_DURATION)
                    {
                        state = State.Generating;
                        stateStartTime = time;
                        nextGenerateTime = time;
                        clouds.Clear();
                    }
                    break;
                case State.Generating:
                    if (elapsed >= GENERATE_DURATION)
                    {
                        state = State.Stopping;
                        stateStartTime = time;
                    }
                    else
                    {
                        if (time >= nextGenerateTime)
                        {
                            clouds.Add(NewCloud(w, h));
                            float interval = MIN_GEN_INTERVAL + (float)rand.NextDouble() * (MAX_GEN_INTERVAL - MIN_GEN_INTERVAL);
                            nextGenerateTime = time + interval;
                        }
                    }
                    break;
                case State.Stopping:
                    break;
            }

            for (int i = clouds.Count - 1; i >= 0; i--)
            {
                CloudData cloud = clouds[i];
                cloud.x += cloud.pixelSpeed * deltaTime;
                if (cloud.x > w + cloud.size * 2f)
                {
                    clouds.RemoveAt(i);
                }
            }

            if (state == State.Stopping && clouds.Count == 0)
            {
                state = State.Idle;
                stateStartTime = time;
            }

            foreach (var cloud in clouds)
                DrawCloud(pixels, w, h, cloud);

            Marshal.Copy(pixels, 0, data.Scan0, pixels.Length);
            bmp.UnlockBits(data);
        }

        private static CloudData NewCloud(int w, int h)
        {
            float size = 25f + (float)rand.NextDouble() * 50f;
            float travelTime = 120f;
            float pixelSpeed = (w + size) / travelTime;

            float baseY;
            if (rand.NextDouble() < 0.25f)
                baseY = h * (0.02f + (float)rand.NextDouble() * 0.33f);
            else
                baseY = h * (0.65f + (float)rand.NextDouble() * 0.30f);

            // ---- 圆的数量：5~8个，所有圆紧密连接 ----
            int circleCount = 5 + rand.Next(4);
            float[] circles = new float[circleCount * 3];
            float[] softness = new float[circleCount];

            // 主圆（中心）
            circles[0] = 0f;
            circles[1] = size * 0.08f;
            circles[2] = size * 0.50f;
            softness[0] = 0.8f + (float)rand.NextDouble() * 0.4f;

            // ---- 子圆：每个圆心必须落在某个已存在圆的内部 ----
            for (int j = 1; j < circleCount; j++)
            {
                int pick = rand.Next(j);
                float tx = circles[pick * 3 + 0];
                float ty = circles[pick * 3 + 1];
                float tr = circles[pick * 3 + 2];

                float newR = (0.30f + (float)rand.NextDouble() * 0.20f) * size;
                float angle = (float)(rand.NextDouble() * 6.2832);
                float maxDist = tr * 0.65f;
                float dist = (float)rand.NextDouble() * maxDist;

                circles[j * 3 + 0] = tx + (float)Math.Cos(angle) * dist * 1.3f;
                circles[j * 3 + 1] = ty + (float)Math.Sin(angle) * dist * 0.7f;
                circles[j * 3 + 2] = newR;
                softness[j] = 0.6f + (float)rand.NextDouble() * 1.0f;
            }

            float startX = -size - 20f - (float)rand.NextDouble() * 50f;

            return new CloudData
            {
                x = startX,
                pixelSpeed = pixelSpeed,
                size = size,
                baseY = baseY,
                circles = circles,
                softness = softness,
                circleCount = circleCount
            };
        }

        private static void DrawCloud(int[] pixels, int w, int h, CloudData cloud)
        {
            float baseX = cloud.x;
            float baseY = cloud.baseY;
            float size = cloud.size;
            int count = cloud.circleCount;
            float[] circles = cloud.circles;
            float[] softness = cloud.softness;

            int bx0 = C2((int)(baseX - size * 1.3f), 0, w - 1);
            int bx1 = C2((int)(baseX + size * 1.3f), 0, w - 1);
            int by0 = C2((int)(baseY - size * 0.8f), 0, h - 1);
            int by1 = C2((int)(baseY + size * 0.8f), 0, h - 1);

            if (bx0 >= w || bx1 < 0 || by0 >= h || by1 < 0) return;

            const int cloudR = 255, cloudG = 254, cloudB = 251;

            for (int x = bx0; x <= bx1; x++)
            {
                for (int y = by0; y <= by1; y++)
                {
                    // ---- Step 1: shape (max cover from all circles) ----
                    float cover = 0f;

                    for (int ci = 0; ci < count; ci++)
                    {
                        int idx = ci * 3;
                        float cx = baseX + circles[idx];
                        float cy = baseY + circles[idx + 1];
                        float cr = circles[idx + 2];

                        float dx = x - cx;
                        float dy = y - cy;
                        float dist = (float)Math.Sqrt(dx * dx + dy * dy);

                        if (dist < cr)
                        {
                            float t = 1f - dist / cr;
                            float s = softness[ci];
                            float exponent = 0.8f + s * 1.2f;
                            float c = 1f - (float)Math.Pow(1f - t, exponent);
                            if (c > cover) cover = c;
                        }
                    }

                    if (cover < 0.03f) continue;

                    // ---- Step 2: edge noise (non-core pixels only) ----
                    if (cover < 0.8f)
                    {
                        float noise = 0.88f + 0.12f * (float)(
                            Math.Sin(x * 0.035f + y * 0.055f + cloud.x * 0.02f) * 0.5f +
                            Math.Cos(x * 0.025f + y * 0.065f + cloud.x * 0.018f + 1.2f) * 0.5f
                        );
                        cover *= noise;
                    }

                    // ---- Step 3: feather edges (wide transition, smoothstep) ----
                    float ft = (cover - 0.03f) / 0.82f;
                    ft = Math.Max(0f, Math.Min(1f, ft));
                    float blend = ft * ft * (3f - 2f * ft); // smoothstep

                    if (blend <= 0.01f) continue;

                    // ---- Step 4: blend uniform color with sky ----
                    int pixelIdx = y * w + x;
                    int sky = pixels[pixelIdx];
                    int sr = (sky >> 16) & 0xFF;
                    int sg = (sky >> 8) & 0xFF;
                    int sb = sky & 0xFF;

                    pixels[pixelIdx] = (255 << 24) |
                        (C((int)(sr + (cloudR - sr) * blend)) << 16) |
                        (C((int)(sg + (cloudG - sg) * blend)) << 8) |
                        C((int)(sb + (cloudB - sb) * blend));
                }
            }
        }

        private static int C(int v) { return v < 0 ? 0 : (v > 255 ? 255 : v); }
        private static int C2(int v, int lo, int hi) { return v < lo ? lo : (v > hi ? hi : v); }
    }
}