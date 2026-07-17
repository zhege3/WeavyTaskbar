using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Microsoft.CSharp;
using Microsoft.Win32;

namespace WeavyTaskbar
{
    static class NativeMethods
    {
        public const int WS_EX_LAYERED = 0x00080000;
        public const int WS_EX_TRANSPARENT = 0x00000020;
        public const int WS_EX_TOOLWINDOW = 0x00000080;
        public const int WS_EX_NOACTIVATE = 0x08000000;

        public const int GW_HWNDNEXT = 2;

        public const int SWP_NOACTIVATE = 0x0010;
        public const int SWP_SHOWWINDOW = 0x0040;
        public const int SWP_NOZORDER = 0x0004;

        public const byte AC_SRC_OVER = 0x00;
        public const byte AC_SRC_ALPHA = 0x01;
        public const int ULW_ALPHA = 0x02;

        public const int WM_HOTKEY = 0x0312;
        public const uint MOD_ALT = 0x0001;
        public const uint MOD_CONTROL = 0x0002;
        public const uint VK_D = 0x44;

        [DllImport("user32.dll")]
        public static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

        [DllImport("user32.dll")]
        public static extern bool IsWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        public static extern bool IsWindowVisible(IntPtr hWnd);

        [DllImport("user32.dll")]
        public static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

        [DllImport("user32.dll")]
        public static extern IntPtr GetWindow(IntPtr hWnd, uint uCmd);

        [DllImport("user32.dll")]
        public static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter,
            int X, int Y, int cx, int cy, uint uFlags);

        [DllImport("user32.dll")]
        public static extern bool RegisterHotKey(IntPtr hWnd, int id,
            uint fsModifiers, uint vk);

        [DllImport("user32.dll")]
        public static extern bool UnregisterHotKey(IntPtr hWnd, int id);

        [DllImport("user32.dll")]
        public static extern IntPtr GetDC(IntPtr hWnd);

        [DllImport("user32.dll")]
        public static extern bool ReleaseDC(IntPtr hWnd, IntPtr hDC);

        [DllImport("gdi32.dll")]
        public static extern IntPtr CreateCompatibleDC(IntPtr hDC);

        [DllImport("gdi32.dll")]
        public static extern bool DeleteDC(IntPtr hDC);

        [DllImport("gdi32.dll")]
        public static extern IntPtr SelectObject(IntPtr hDC, IntPtr hObject);

        [DllImport("gdi32.dll")]
        public static extern bool DeleteObject(IntPtr hObject);

        [DllImport("user32.dll")]
        public static extern bool UpdateLayeredWindow(IntPtr hwnd, IntPtr hdcDst,
            ref POINT pptDst, ref SIZE psize, IntPtr hdcSrc, ref POINT pptSrc,
            int crKey, ref BLENDFUNCTION pblend, int dwFlags);

        [DllImport("user32.dll")]
        public static extern int SetWindowCompositionAttribute(IntPtr hwnd,
            ref WindowCompositionAttributeData data);

        public static readonly IntPtr HWND_BOTTOM = new IntPtr(1);

        [StructLayout(LayoutKind.Sequential)]
        public struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct POINT
        {
            public int x;
            public int y;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct SIZE
        {
            public int cx;
            public int cy;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct BLENDFUNCTION
        {
            public byte BlendOp;
            public byte BlendFlags;
            public byte SourceConstantAlpha;
            public byte AlphaFormat;
        }
    }

    enum AccentState
    {
        ACCENT_DISABLED = 0,
        ACCENT_ENABLE_TRANSPARENTGRADIENT = 2,
    }

    [StructLayout(LayoutKind.Sequential)]
    struct AccentPolicy
    {
        public AccentState AccentState;
        public int AccentFlags;
        public int GradientColor;
        public int AnimationId;
    }

    enum WindowCompositionAttribute
    {
        WCA_ACCENT_POLICY = 19
    }

    [StructLayout(LayoutKind.Sequential)]
    struct WindowCompositionAttributeData
    {
        public WindowCompositionAttribute Attribute;
        public IntPtr Data;
        public int SizeOfData;
    }

    class StyleInfo
    {
        public string Name;
        public MethodInfo Method;
    }

    class OverlayWindow : NativeWindow, IDisposable
    {
        private bool _disposed;

        public void Create()
        {
            if (Handle != IntPtr.Zero) return;

            var cp = new CreateParams
            {
                ExStyle = NativeMethods.WS_EX_LAYERED
                        | NativeMethods.WS_EX_TRANSPARENT
                        | NativeMethods.WS_EX_TOOLWINDOW
                        | NativeMethods.WS_EX_NOACTIVATE,
                Style = unchecked((int)0x80000000),
                X = 0,
                Y = 0,
                Width = 100,
                Height = 40,
                Parent = IntPtr.Zero
            };
            CreateHandle(cp);
        }

        public void Destroy()
        {
            if (Handle != IntPtr.Zero)
            {
                DestroyHandle();
            }
        }

        public void ShowAt(int x, int y, int w, int h, Bitmap bitmap)
        {
            if (Handle == IntPtr.Zero || bitmap == null) return;

            IntPtr screenDc = NativeMethods.GetDC(IntPtr.Zero);
            IntPtr memDc = NativeMethods.CreateCompatibleDC(screenDc);
            IntPtr hBitmap = bitmap.GetHbitmap(Color.FromArgb(0));
            IntPtr oldBitmap = NativeMethods.SelectObject(memDc, hBitmap);

            var size = new NativeMethods.SIZE { cx = w, cy = h };
            var ptSrc = new NativeMethods.POINT { x = 0, y = 0 };
            var ptDst = new NativeMethods.POINT { x = x, y = y };
            var blend = new NativeMethods.BLENDFUNCTION
            {
                BlendOp = NativeMethods.AC_SRC_OVER,
                BlendFlags = 0,
                SourceConstantAlpha = 255,
                AlphaFormat = NativeMethods.AC_SRC_ALPHA
            };

            NativeMethods.UpdateLayeredWindow(
                Handle, screenDc, ref ptDst, ref size,
                memDc, ref ptSrc, 0, ref blend, NativeMethods.ULW_ALPHA);

            NativeMethods.SelectObject(memDc, oldBitmap);
            NativeMethods.DeleteObject(hBitmap);
            NativeMethods.DeleteDC(memDc);
            NativeMethods.ReleaseDC(IntPtr.Zero, screenDc);
        }

        public void Reposition(int x, int y, int w, int h)
        {
            if (Handle == IntPtr.Zero) return;
            NativeMethods.SetWindowPos(Handle, IntPtr.Zero,
                x, y, w, h,
                NativeMethods.SWP_NOACTIVATE | NativeMethods.SWP_NOZORDER);
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                Destroy();
                _disposed = true;
            }
        }
    }

    class MessageForm : Form
    {
        private AppContext _ctx;

        public MessageForm(AppContext ctx)
        {
            _ctx = ctx;
            FormBorderStyle = FormBorderStyle.None;
            ShowInTaskbar = false;
            Width = 1;
            Height = 1;
            Location = new Point(-100, -100);
        }

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == NativeMethods.WM_HOTKEY)
                _ctx.Toggle();
            base.WndProc(ref m);
        }
    }

    class AppContext : ApplicationContext
    {
        private MessageForm _msgForm;
        private OverlayWindow _overlay;
        private NotifyIcon _trayIcon;
        private IntPtr _taskbar;
        private bool _active;
        private Timer _renderTimer;
        private Timer _posTimer;
        private Bitmap _buffer;
        private float _hueOffset;
        private float _speedMultiplier = 1.0f;
        private int _lastX, _lastY, _lastW, _lastH;
        private volatile bool _cleanedUp;

        private const int HOTKEY_ID = 1;
        private const int FPS = 15;
        private const int OVERLAY_ALPHA = 220;

        private List<StyleInfo> _styles = new List<StyleInfo>();
        private int _currentIndex;

        public AppContext()
        {
            _taskbar = NativeMethods.FindWindow("Shell_TrayWnd", null);
            if (_taskbar == IntPtr.Zero)
            {
                MessageBox.Show("Cannot find Shell_TrayWnd.", "WeavyTaskbar",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                ExitThread();
                return;
            }

            LoadStyles();
            _speedMultiplier = LoadSpeed();

            _msgForm = new MessageForm(this);
            var h = _msgForm.Handle;

            if (!NativeMethods.RegisterHotKey(_msgForm.Handle, HOTKEY_ID,
                NativeMethods.MOD_CONTROL | NativeMethods.MOD_ALT, NativeMethods.VK_D))
            {
                MessageBox.Show(
                    "Ctrl+Alt+D hotkey registration failed.\n" +
                    "It may already be in use by another application.\n\n" +
                    "The overlay will still start.",
                    "WeavyTaskbar",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }

            _msgForm.FormClosing += (s, e) => { Cleanup(); ExitThread(); };

            _trayIcon = new NotifyIcon
            {
                Text = "WeavyTaskbar - Ctrl+Alt+D to toggle",
                Visible = true
            };
            _trayIcon.MouseClick += (s, e) =>
            {
                if (e.Button == MouseButtons.Left) Toggle();
            };

            BuildTrayMenu();

            _overlay = new OverlayWindow();

            _renderTimer = new Timer { Interval = 1000 / FPS };
            _renderTimer.Tick += OnRender;

            _posTimer = new Timer { Interval = 500 };
            _posTimer.Tick += OnPositionCheck;

            GetRect(out _lastX, out _lastY, out _lastW, out _lastH);
            _buffer = new Bitmap(Math.Max(_lastW, 1), Math.Max(_lastH, 1),
                PixelFormat.Format32bppArgb);

            Enable();
            _renderTimer.Start();
            _posTimer.Start();
        }

        private void LoadStyles()
        {
            string stylesDir = Path.Combine(Application.StartupPath, "Styles");
            if (!Directory.Exists(stylesDir))
            {
                Directory.CreateDirectory(stylesDir);
            }

            var csFiles = Directory.GetFiles(stylesDir, "*.cs");
            if (csFiles.Length == 0)
            {
                _styles.Add(new StyleInfo { Name = "Default (no .cs found)", Method = null });
                _currentIndex = 0;
                return;
            }

            try
            {
                using (var provider = new CSharpCodeProvider(
                    new Dictionary<string, string> { { "CompilerVersion", "v4.0" } }))
                {
                    var parms = new CompilerParameters
                    {
                        GenerateInMemory = true,
                        GenerateExecutable = false,
                        TreatWarningsAsErrors = false,
                    };
                    parms.ReferencedAssemblies.Add("System.dll");
                    parms.ReferencedAssemblies.Add("System.Drawing.dll");
                    parms.ReferencedAssemblies.Add(typeof(AppContext).Assembly.Location);

                    var result = provider.CompileAssemblyFromFile(parms, csFiles);

                    if (result.Errors.HasErrors)
                    {
                        string msg = "Style compilation errors:\n";
                        int count = 0;
                        foreach (CompilerError err in result.Errors)
                        {
                            if (!err.IsWarning && count < 8)
                            {
                                msg += string.Format("  {0}({1}): {2}\n",
                                    Path.GetFileName(err.FileName), err.Line, err.ErrorText);
                                count++;
                            }
                        }
                        MessageBox.Show(msg, "WeavyTaskbar - Style Error",
                            MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }

                    if (result.CompiledAssembly != null)
                    {
                        foreach (var type in result.CompiledAssembly.GetTypes())
                        {
                            var method = type.GetMethod("Render",
                                BindingFlags.Public | BindingFlags.Static, null,
                                new[] { typeof(Bitmap), typeof(float), typeof(int), typeof(float) }, null);
                            if (method != null)
                            {
                                _styles.Add(new StyleInfo
                                {
                                    Name = type.Name.Replace("Style", "").Replace("Render", ""),
                                    Method = method
                                });
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Failed to compile styles:\n" + ex.Message,
                    "WeavyTaskbar", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }

            if (_styles.Count == 0)
            {
                _styles.Add(new StyleInfo { Name = "Default (compile failed)", Method = null });
            }

            string saved = LoadStyleName();
            _currentIndex = 0;
            for (int i = 0; i < _styles.Count; i++)
            {
                if (_styles[i].Name == saved) { _currentIndex = i; break; }
            }
        }

        private void BuildTrayMenu()
        {
            var trayMenu = new ContextMenuStrip();

            var styleMenu = new ToolStripMenuItem("Change Style");
            for (int i = 0; i < _styles.Count; i++)
            {
                var item = new ToolStripMenuItem(_styles[i].Name);
                item.Tag = i;
                item.Click += (s, e) =>
                {
                    _currentIndex = (int)((ToolStripMenuItem)s).Tag;
                    SaveStyleName(_styles[_currentIndex].Name);
                };
                item.Checked = (i == _currentIndex);
                styleMenu.DropDownItems.Add(item);
            }
            styleMenu.DropDownOpening += (s, e) =>
            {
                for (int i = 0; i < _styles.Count; i++)
                    ((ToolStripMenuItem)styleMenu.DropDownItems[i]).Checked = (i == _currentIndex);
            };
            trayMenu.Items.Add(styleMenu);

            var speedMenu = new ToolStripMenuItem("Speed");
            float[] speeds = { 0.5f, 0.75f, 1.0f, 1.25f, 1.5f, 2.0f, 2.5f, 3.0f };
            for (int i = 0; i < speeds.Length; i++)
            {
                var item = new ToolStripMenuItem(speeds[i].ToString("0.##") + "x");
                item.Tag = speeds[i];
                item.Click += (s2, e2) =>
                {
                    _speedMultiplier = (float)((ToolStripMenuItem)s2).Tag;
                    SaveSpeed(_speedMultiplier);
                };
                speedMenu.DropDownItems.Add(item);
            }
            speedMenu.DropDownOpening += (s2, e2) =>
            {
                for (int i = 0; i < speeds.Length; i++)
                {
                    var it = (ToolStripMenuItem)speedMenu.DropDownItems[i];
                    it.Checked = Math.Abs((float)it.Tag - _speedMultiplier) < 0.001f;
                }
            };
            trayMenu.Items.Add(speedMenu);

            trayMenu.Items.Add(new ToolStripSeparator());
            trayMenu.Items.Add("Toggle Effect (Ctrl+Alt+D)", null, (s, e) => Toggle());

            trayMenu.Items.Add(new ToolStripSeparator());

            var startupItem = new ToolStripMenuItem("Start with Windows");
            startupItem.Checked = CheckStartup();
            startupItem.Click += (s, e) =>
            {
                bool set = !startupItem.Checked;
                SetStartup(set);
                startupItem.Checked = set;
            };
            trayMenu.Items.Add(startupItem);

            trayMenu.Items.Add(new ToolStripSeparator());
            trayMenu.Items.Add("Exit", null, (s, e) =>
            {
                Cleanup();
                ExitThread();
            });
            _trayIcon.ContextMenuStrip = trayMenu;
            _trayIcon.Icon = CreateAppIcon();
        }

        public void Toggle()
        {
            if (_active) Disable(); else Enable();
        }

        private void Enable()
        {
            if (_active) return;
            _active = true;

            SetAccent(true);
            _overlay.Create();
            PositionOverlay();
        }

        private void Disable()
        {
            if (!_active) return;
            _active = false;

            _overlay.Destroy();
            SetAccent(false);
        }

        public void Cleanup()
        {
            if (_cleanedUp) return;
            _cleanedUp = true;

            try
            {
                if (_renderTimer != null) _renderTimer.Stop();
                if (_posTimer != null) _posTimer.Stop();

                if (_active)
                {
                    if (_overlay != null) _overlay.Destroy();
                    SetAccent(false);
                    _active = false;
                }

                if (_overlay != null) _overlay.Dispose();
                if (_buffer != null) _buffer.Dispose();

                if (_trayIcon != null)
                {
                    _trayIcon.Visible = false;
                    _trayIcon.Dispose();
                    _trayIcon = null;
                }

                if (_msgForm != null && _msgForm.Handle != IntPtr.Zero)
                    NativeMethods.UnregisterHotKey(_msgForm.Handle, HOTKEY_ID);

                if (_msgForm != null) _msgForm.Dispose();
            }
            catch { }
        }

        private bool GetRect(out int x, out int y, out int w, out int h)
        {
            x = y = w = h = 0;
            if (_taskbar == IntPtr.Zero) return false;

            NativeMethods.RECT r;
            if (!NativeMethods.GetWindowRect(_taskbar, out r)) return false;

            x = r.Left;
            y = r.Top;
            w = r.Right - r.Left;
            h = r.Bottom - r.Top;
            return true;
        }

        private void PositionOverlay()
        {
            int x, y, w, h;
            GetRect(out x, out y, out w, out h);
            if (w <= 0 || h <= 0) return;

            IntPtr below = NativeMethods.GetWindow(_taskbar, NativeMethods.GW_HWNDNEXT);
            if (below == IntPtr.Zero) below = NativeMethods.HWND_BOTTOM;

            NativeMethods.SetWindowPos(_overlay.Handle, below,
                x, y, w, h,
                NativeMethods.SWP_NOACTIVATE | NativeMethods.SWP_SHOWWINDOW);
        }

        private void OnRender(object sender, EventArgs e)
        {
            if (!_active) return;

            int x, y, w, h;
            GetRect(out x, out y, out w, out h);
            if (w <= 0 || h <= 0) return;

            bool needsRepair = false;

            if (_overlay.Handle == IntPtr.Zero || !NativeMethods.IsWindow(_overlay.Handle))
            {
                _overlay.Destroy();
                _overlay.Create();
                needsRepair = true;
            }
            else
            {
                IntPtr below = NativeMethods.GetWindow(_taskbar, NativeMethods.GW_HWNDNEXT);
                if (below != _overlay.Handle)
                    needsRepair = true;

                if (!NativeMethods.IsWindowVisible(_overlay.Handle))
                    needsRepair = true;
            }

            if (needsRepair)
                PositionOverlay();

            if (_buffer.Width != w || _buffer.Height != h)
            {
                _buffer.Dispose();
                _buffer = new Bitmap(w, h, PixelFormat.Format32bppArgb);
            }

            RenderFrame(_buffer, _hueOffset);
            _overlay.ShowAt(x, y, w, h, _buffer);

            _hueOffset += 3f;
        }

        private void OnPositionCheck(object sender, EventArgs e)
        {
            if (!_active) return;

            int x, y, w, h;
            GetRect(out x, out y, out w, out h);
            if (x != _lastX || y != _lastY || w != _lastW || h != _lastH)
            {
                _lastX = x; _lastY = y; _lastW = w; _lastH = h;
                _overlay.Reposition(x, y, w, h);
            }
        }

        private void RenderFrame(Bitmap bmp, float offset)
        {
            if (_currentIndex < 0 || _currentIndex >= _styles.Count) return;
            var method = _styles[_currentIndex].Method;
            if (method != null)
            {
                try
                {
                    method.Invoke(null, new object[] { bmp, offset, OVERLAY_ALPHA, _speedMultiplier });
                }
                catch { }
            }
        }

        private bool CheckStartup()
        {
            try
            {
                using (var key = Registry.CurrentUser.OpenSubKey(
                    @"Software\Microsoft\Windows\CurrentVersion\Run", false))
                {
                    return key != null && key.GetValue("WeavyTaskbar") != null;
                }
            }
            catch { return false; }
        }

        private void SetStartup(bool enable)
        {
            try
            {
                using (var key = Registry.CurrentUser.OpenSubKey(
                    @"Software\Microsoft\Windows\CurrentVersion\Run", true))
                {
                    if (key == null) return;
                    if (enable)
                        key.SetValue("WeavyTaskbar", Application.ExecutablePath);
                    else
                        key.DeleteValue("WeavyTaskbar", false);
                }
            }
            catch { }
        }

        private string LoadStyleName()
        {
            try
            {
                using (var key = Registry.CurrentUser.OpenSubKey(@"Software\WeavyTaskbar", false))
                {
                    if (key != null)
                        return key.GetValue("Style") as string ?? "";
                }
            }
            catch { }
            return "CloudDrift";
        }

        private void SaveStyleName(string name)
        {
            try
            {
                using (var key = Registry.CurrentUser.CreateSubKey(@"Software\WeavyTaskbar"))
                {
                    if (key != null) key.SetValue("Style", name);
                }
            }
            catch { }
        }

        private float LoadSpeed()
        {
            try
            {
                using (var key = Registry.CurrentUser.OpenSubKey(@"Software\WeavyTaskbar", false))
                {
                    if (key != null)
                    {
                        object v = key.GetValue("Speed");
                        if (v != null)
                        {
                            float s;
                            if (float.TryParse(v.ToString(), out s))
                                return Math.Max(0.5f, Math.Min(3.0f, s));
                        }
                    }
                }
            }
            catch { }
            return 1.0f;
        }

        private void SaveSpeed(float speed)
        {
            try
            {
                using (var key = Registry.CurrentUser.CreateSubKey(@"Software\WeavyTaskbar"))
                {
                    if (key != null) key.SetValue("Speed", speed.ToString("0.##"));
                }
            }
            catch { }
        }

        private void SetAccent(bool enable)
        {
            if (_taskbar == IntPtr.Zero) return;

            try
            {
                int gradientColor = enable ? 0x00000000 : 0;
                var accent = new AccentPolicy
                {
                    AccentState = enable
                        ? AccentState.ACCENT_ENABLE_TRANSPARENTGRADIENT
                        : AccentState.ACCENT_DISABLED,
                    AccentFlags = 0,
                    GradientColor = gradientColor,
                    AnimationId = 0
                };

                int sz = Marshal.SizeOf(accent);
                IntPtr ptr = Marshal.AllocHGlobal(sz);
                Marshal.StructureToPtr(accent, ptr, false);

                var wcad = new WindowCompositionAttributeData
                {
                    Attribute = WindowCompositionAttribute.WCA_ACCENT_POLICY,
                    Data = ptr,
                    SizeOfData = sz
                };

                NativeMethods.SetWindowCompositionAttribute(_taskbar, ref wcad);
                Marshal.FreeHGlobal(ptr);
            }
            catch { }
        }

        private static Icon CreateAppIcon()
        {
            var bmp = new Bitmap(32, 32);
            using (var g = Graphics.FromImage(bmp))
            {
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                g.Clear(Color.Transparent);

                g.FillEllipse(Brushes.White, 9, 12, 8, 8);
                g.FillEllipse(Brushes.White, 14, 8, 10, 10);
                g.FillEllipse(Brushes.White, 21, 13, 7, 7);
                g.FillRectangle(Brushes.White, 8, 16, 19, 6);
            }
            IntPtr hIcon = bmp.GetHicon();
            var icon = Icon.FromHandle(hIcon);
            return icon;
        }
    }
}
