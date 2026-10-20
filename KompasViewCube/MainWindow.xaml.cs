using System;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Threading;

namespace KompasViewCube
{
    public partial class MainWindow : Window
    {
        private Point _dragStart;
        private Point _dragOrigin;
        private KompasOverlayPlacement _overlayPlacement;
        private ViewCubeControl _viewCube;
        private KompasAPI7.IApplication _application;
        private DispatcherTimer _documentTimer;

        public MainWindow()
        {
            InitializeComponent();
            _viewCube = new ViewCubeControl();
            ViewCubeHost.Children.Add(_viewCube);
            ApplySettings();
        }

        public MainWindow(
            object applicationInterface,
            IntPtr kompasWindowHandle)
        {
            InitializeComponent();

            KompasAPI7.IApplication application =
                applicationInterface as KompasAPI7.IApplication;

            if (application == null)
            {
                throw new ArgumentException(
                    "Ожидался интерфейс API7 КОМПАС-3D.",
                    nameof(applicationInterface));
            }

            _application = application;

            ConfigureAsOverlay(kompasWindowHandle);
            ApplySettings();
            ViewCubeSettings.Changed += Settings_Changed;

            _documentTimer = new DispatcherTimer(DispatcherPriority.Background)
            {
                Interval = TimeSpan.FromMilliseconds(250)
            };
            _documentTimer.Tick += DocumentTimer_Tick;
            _documentTimer.Start();
            RefreshDocumentState();
        }

        private void ConfigureAsOverlay(
            IntPtr kompasWindowHandle)
        {
            WindowStyle = WindowStyle.None;
            ResizeMode = ResizeMode.NoResize;
            ShowInTaskbar = false;
            WindowStartupLocation = WindowStartupLocation.Manual;
            AllowsTransparency = true;
            Background = Brushes.Transparent;
            ViewCubeHost.Background = null;

            new WindowInteropHelper(this).Owner =
                kompasWindowHandle;

            _overlayPlacement =
                new KompasOverlayPlacement(
                    this,
                    kompasWindowHandle,
                    () => ViewCubeSettings.Load().Corner);

            PreviewMouseLeftButtonDown += (sender, e) =>
            {
                if (!ViewCubeSettings.PositionEditing) return;
                _dragStart = PointToScreen(e.GetPosition(this));
                _dragOrigin = new Point(Left, Top);
                _overlayPlacement.IsDragging = true;
                CaptureMouse();
                e.Handled = true;
            };
            PreviewMouseMove += (sender, e) =>
            {
                if (!_overlayPlacement.IsDragging) return;
                Point now = PointToScreen(e.GetPosition(this));
                var transform = PresentationSource.FromVisual(this).CompositionTarget.TransformFromDevice;
                Vector delta = transform.Transform(now - _dragStart);
                _overlayPlacement.DragTo(_dragOrigin.X + delta.X, _dragOrigin.Y + delta.Y, false);
                e.Handled = true;
            };
            PreviewMouseLeftButtonUp += (sender, e) =>
            {
                if (!_overlayPlacement.IsDragging) return;
                _overlayPlacement.IsDragging = false;
                _overlayPlacement.DragTo(Left, Top, true);
                ReleaseMouseCapture();
                e.Handled = true;
            };
            LostMouseCapture += (sender, e) =>
            {
                if (!_overlayPlacement.IsDragging) return;
                _overlayPlacement.IsDragging = false;
                _overlayPlacement.DragTo(Left, Top, true);
            };
            Closed += MainWindow_Closed;
        }

        private void MainWindow_Closed(
            object sender,
            EventArgs e)
        {
            Closed -= MainWindow_Closed;
            ViewCubeSettings.Changed -= Settings_Changed;
            if (_documentTimer != null)
            {
                _documentTimer.Stop();
                _documentTimer.Tick -= DocumentTimer_Tick;
                _documentTimer = null;
            }
            RemoveViewCube();
            _overlayPlacement?.Dispose();
            _overlayPlacement = null;
        }

        private void Settings_Changed() => Dispatcher.BeginInvoke(new Action(ApplySettings));

        private void ApplySettings()
        {
            ViewCubeSettings settings = ViewCubeSettings.Load();
            Width = 266 * settings.Scale;
            Height = 266 * settings.Scale;
            _viewCube?.SetLanguage(settings.Language);
            _overlayPlacement?.Refresh();
        }

        public bool HasActive3DDocument =>
            _application?.ActiveDocument is KompasAPI7.IKompasDocument3D1;

        private void DocumentTimer_Tick(object sender, EventArgs e) =>
            RefreshDocumentState();

        private void RefreshDocumentState()
        {
            if (HasActive3DDocument)
            {
                if (_viewCube == null)
                {
                    _viewCube = new ViewCubeControl(_application);
                    ViewCubeHost.Children.Add(_viewCube);
                    _viewCube.SetLanguage(ViewCubeSettings.Load().Language);
                }

                if (!IsVisible)
                    Show();
            }
            else
            {
                RemoveViewCube();
                if (IsVisible)
                    Hide();
            }
        }

        private void RemoveViewCube()
        {
            if (_viewCube == null)
                return;

            ViewCubeHost.Children.Remove(_viewCube);
            _viewCube = null;
        }
    }
}
