using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Threading;

namespace KompasViewCube
{
    internal sealed class KompasOverlayPlacement : IDisposable
    {
        private const int Margin = 14;

        private readonly Window _window;
        private readonly IntPtr _kompasWindow;
        private readonly DispatcherTimer _timer;
        private readonly Func<ViewCubeCorner> _getCorner;
        public bool IsDragging { get; set; }

        public KompasOverlayPlacement(
            Window window,
            IntPtr kompasWindow,
            Func<ViewCubeCorner> getCorner)
        {
            _window = window ??
                throw new ArgumentNullException(nameof(window));
            _kompasWindow = kompasWindow;
            _getCorner = getCorner ?? (() => ViewCubeCorner.TopRight);

            _timer = new DispatcherTimer(
                DispatcherPriority.Background)
            {
                Interval = TimeSpan.FromMilliseconds(200)
            };

            _timer.Tick += Timer_Tick;
            _window.Loaded += Window_Loaded;
        }

        private void Window_Loaded(
            object sender,
            RoutedEventArgs e)
        {
            PlaceWindow();
            _timer.Start();
        }

        private void Timer_Tick(
            object sender,
            EventArgs e)
        {
            PlaceWindow();
        }

        private void PlaceWindow()
        {
            if (IsDragging) return;
            NativeRect workspace = FindWorkspaceRect();
            if (workspace.Width <= 0 || workspace.Height <= 0)
                return;

            uint dpi = GetDpiForWindow(_kompasWindow);
            double scale = dpi > 0 ? dpi / 96.0 : 1.0;

            ViewCubeCorner corner = _getCorner();
            bool bottom = corner == ViewCubeCorner.BottomRight;

            ViewCubeSettings settings = ViewCubeSettings.Load();
            double rangeX = Math.Max(0, workspace.Width / scale - _window.Width);
            double rangeY = Math.Max(0, workspace.Height / scale - _window.Height);
            double x = double.IsNaN(settings.PositionX) ? Math.Max(0, rangeX - Margin) : settings.PositionX * rangeX;
            double y = double.IsNaN(settings.PositionY) ? (bottom ? Math.Max(0, rangeY - Margin) : Margin) : settings.PositionY * rangeY;
            _window.Left = workspace.Left / scale + Math.Max(0, Math.Min(rangeX, x));
            _window.Top = workspace.Top / scale + Math.Max(0, Math.Min(rangeY, y));
        }

        public void DragTo(double left, double top, bool save)
        {
            NativeRect rect = FindWorkspaceRect();
            if (rect.Width <= 0 || rect.Height <= 0) return;
            uint dpi = GetDpiForWindow(_kompasWindow);
            double scale = dpi > 0 ? dpi / 96.0 : 1.0;
            double rx = Math.Max(0, rect.Width / scale - _window.Width);
            double ry = Math.Max(0, rect.Height / scale - _window.Height);
            _window.Left = rect.Left / scale + Math.Max(0, Math.Min(rx, left - rect.Left / scale));
            _window.Top = rect.Top / scale + Math.Max(0, Math.Min(ry, top - rect.Top / scale));
            if (save)
            {
                ViewCubeSettings settings = ViewCubeSettings.Load();
                settings.PositionX = rx > 0 ? (_window.Left - rect.Left / scale) / rx : 0;
                settings.PositionY = ry > 0 ? (_window.Top - rect.Top / scale) / ry : 0;
                settings.Save();
            }
        }
        public void Refresh() => PlaceWindow();

        private NativeRect FindWorkspaceRect()
        {
            NativeRect main;
            if (!GetWindowRect(_kompasWindow, out main))
                return default(NativeRect);

            long mainArea = main.Area;
            NativeRect best = default(NativeRect);
            long bestArea = 0;

            EnumChildWindows(
                _kompasWindow,
                (handle, parameter) =>
                {
                    NativeRect candidate;
                    if (!IsWindowVisible(handle) ||
                        !GetWindowRect(handle, out candidate))
                    {
                        return true;
                    }

                    long area = candidate.Area;
                    bool looksLikeWorkspace =
                        area > mainArea / 5 &&
                        candidate.Top > main.Top + 60 &&
                        candidate.Right > main.Right - 120 &&
                        candidate.Bottom > main.Bottom - 120;

                    if (looksLikeWorkspace && area > bestArea)
                    {
                        best = candidate;
                        bestArea = area;
                    }

                    return true;
                },
                IntPtr.Zero);

            if (bestArea > 0)
                return best;

            main.Top += Math.Max(100, main.Height / 6);
            return main;
        }

        public void Dispose()
        {
            _timer.Stop();
            _timer.Tick -= Timer_Tick;
            _window.Loaded -= Window_Loaded;
        }

        private delegate bool EnumWindowProc(
            IntPtr window,
            IntPtr parameter);

        [DllImport("user32.dll")]
        private static extern bool EnumChildWindows(
            IntPtr parent,
            EnumWindowProc callback,
            IntPtr parameter);

        [DllImport("user32.dll")]
        private static extern bool GetWindowRect(
            IntPtr window,
            out NativeRect rectangle);

        [DllImport("user32.dll")]
        private static extern bool IsWindowVisible(
            IntPtr window);

        [DllImport("user32.dll")]
        private static extern uint GetDpiForWindow(
            IntPtr window);

        [StructLayout(LayoutKind.Sequential)]
        private struct NativeRect
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;

            public int Width => Right - Left;
            public int Height => Bottom - Top;
            public long Area =>
                (long)Math.Max(0, Width) *
                Math.Max(0, Height);
        }
    }
}
