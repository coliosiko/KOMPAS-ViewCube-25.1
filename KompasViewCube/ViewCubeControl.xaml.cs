using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using System.Windows.Threading;

namespace KompasViewCube
{
    public partial class ViewCubeControl : UserControl
    {
        private readonly KompasCamera _camera;
        private readonly DispatcherTimer _orientationTimer;

        private Model3DGroup _cubeGroup;
        private GeometryModel3D _hovered;

        private double[] _lastKompasMatrix;
        private string _language = "en";

        private readonly Dictionary<GeometryModel3D, string> _commands =
            new Dictionary<GeometryModel3D, string>();

        private readonly Dictionary<GeometryModel3D, Material> _normalMaterials =
            new Dictionary<GeometryModel3D, Material>();

        private readonly Dictionary<GeometryModel3D, Material> _hoverMaterials =
            new Dictionary<GeometryModel3D, Material>();


        public ViewCubeControl()
            : this(new KompasCamera())
        {
        }

        public ViewCubeControl(
            KompasAPI7.IApplication application)
            : this(new KompasCamera(application))
        {
        }

        private ViewCubeControl(
            KompasCamera camera)
        {
            InitializeComponent();

            _camera = camera ??
                throw new ArgumentNullException(nameof(camera));

            _orientationTimer =
                new DispatcherTimer(
                    DispatcherPriority.Render)
                {
                    Interval = TimeSpan.FromMilliseconds(16)
                };

            _orientationTimer.Tick +=
                OrientationTimer_Tick;

            BuildCube();

            Loaded +=
                ViewCubeControl_Loaded;

            Unloaded +=
                ViewCubeControl_Unloaded;
        }


        private void BuildCube()
        {
            _commands.Clear();
            _normalMaterials.Clear();
            _hoverMaterials.Clear();

            ViewCubeGeometry.BuildResult geometry =
                new ViewCubeGeometry().Build(_language);

            _cubeGroup =
                geometry.Group;

            foreach (KeyValuePair<GeometryModel3D, string> pair
                in geometry.Commands)
            {
                _commands.Add(
                    pair.Key,
                    pair.Value);
            }

            foreach (KeyValuePair<GeometryModel3D, Material> pair
                in geometry.NormalMaterials)
            {
                _normalMaterials.Add(
                    pair.Key,
                    pair.Value);
            }

            foreach (KeyValuePair<GeometryModel3D, Material> pair
                in geometry.HoverMaterials)
            {
                _hoverMaterials.Add(
                    pair.Key,
                    pair.Value);
            }

            _cubeGroup.Transform =
                Transform3D.Identity;

            CubeVisual.Content =
                _cubeGroup;
        }

        public void SetLanguage(string language)
        {
            string normalized = language == "ru" ? "ru" : "en";
            if (_language == normalized) return;
            _language = normalized;
            BuildCube();
            RollLeftButton.ToolTip = _language == "ru"
                ? "Повернуть вид против часовой стрелки" : "Roll view counterclockwise";
            RollRightButton.ToolTip = _language == "ru"
                ? "Повернуть вид по часовой стрелке" : "Roll view clockwise";
        }


        // ============================================================
        // HIT TEST + HOVER
        // ============================================================

        private GeometryModel3D GetModelUnderMouse(
            Point point)
        {
            GeometryModel3D result = null;

            VisualTreeHelper.HitTest(
                CubeViewport,
                null,
                hit =>
                {
                    RayMeshGeometry3DHitTestResult rayHit =
                        hit as RayMeshGeometry3DHitTestResult;

                    GeometryModel3D model =
                        rayHit?.ModelHit as GeometryModel3D;

                    if (model != null &&
                        _commands.ContainsKey(model))
                    {
                        result = model;

                        return
                            HitTestResultBehavior.Stop;
                    }

                    return
                        HitTestResultBehavior.Continue;
                },
                new PointHitTestParameters(point));

            return result;
        }

        private void CubeViewport_MouseMove(
            object sender,
            MouseEventArgs e)
        {
            GeometryModel3D model =
                GetModelUnderMouse(
                    e.GetPosition(
                        CubeViewport));

            if (model == _hovered)
                return;

            ClearHover();

            if (model == null)
                return;

            _hovered = model;

            Material hoverMaterial;

            if (_hoverMaterials.TryGetValue(
                model,
                out hoverMaterial))
            {
                model.Material =
                    hoverMaterial;

                model.BackMaterial =
                    hoverMaterial;
            }

            Cursor = Cursors.Hand;
        }

        private void CubeViewport_MouseLeave(
            object sender,
            MouseEventArgs e)
        {
            ClearHover();
        }

        private void ClearHover()
        {
            if (_hovered != null)
            {
                Material normal;

                if (_normalMaterials.TryGetValue(
                    _hovered,
                    out normal))
                {
                    _hovered.Material =
                        normal;

                    _hovered.BackMaterial =
                        normal;
                }

                _hovered = null;
            }

            Cursor =
                Cursors.Arrow;
        }

        private async void CubeViewport_MouseLeftButtonDown(
            object sender,
            MouseButtonEventArgs e)
        {
            GeometryModel3D model =
                GetModelUnderMouse(
                    e.GetPosition(
                        CubeViewport));

            if (model == null)
                return;

            string command =
                _commands[model];

            await ExecuteSafely(
                () => ExecuteCommand(command));
        }

        // ============================================================
        // 26 КОМАНД ВИДА
        // ============================================================

        private Task ExecuteCommand(
            string command)
        {
            switch (command)
            {
                case "Front": return _camera.Front();
                case "Back": return _camera.Back();
                case "Left": return _camera.Left();
                case "Right": return _camera.Right();
                case "Top": return _camera.Top();
                case "Bottom": return _camera.Bottom();

                case "FrontTop": return _camera.FrontTop();
                case "FrontBottom": return _camera.FrontBottom();
                case "FrontLeft": return _camera.FrontLeft();
                case "FrontRight": return _camera.FrontRight();

                case "BackTop": return _camera.BackTop();
                case "BackBottom": return _camera.BackBottom();
                case "BackLeft": return _camera.BackLeft();
                case "BackRight": return _camera.BackRight();

                case "LeftTop": return _camera.LeftTop();
                case "LeftBottom": return _camera.LeftBottom();
                case "RightTop": return _camera.RightTop();
                case "RightBottom": return _camera.RightBottom();

                case "FrontTopRight": return _camera.FrontTopRight();
                case "FrontTopLeft": return _camera.FrontTopLeft();
                case "FrontBottomRight": return _camera.FrontBottomRight();
                case "FrontBottomLeft": return _camera.FrontBottomLeft();

                case "BackTopRight": return _camera.BackTopRight();
                case "BackTopLeft": return _camera.BackTopLeft();
                case "BackBottomRight": return _camera.BackBottomRight();
                case "BackBottomLeft": return _camera.BackBottomLeft();
            }

            return Task.CompletedTask;
        }

        // ============================================================
        // ROLL
        // ============================================================

        private async void Orbit_Click(object sender, RoutedEventArgs e)
        {
            string direction = (string)((System.Windows.Controls.Button)sender).Tag;
            await ExecuteSafely(() => _camera.Orbit(
                direction == "Left" ? -90 : direction == "Right" ? 90 : 0,
                direction == "Up" ? 90 : direction == "Down" ? -90 : 0));
        }

        private async void RollLeft_Click(
            object sender,
            RoutedEventArgs e)
        {
            await ExecuteSafely(
                _camera.RollClockwise);
        }

        private async void RollRight_Click(
            object sender,
            RoutedEventArgs e)
        {
            await ExecuteSafely(
                _camera.RollCounterClockwise);
        }

        private static async Task ExecuteSafely(
            Func<Task> command)
        {
            try
            {
                await command();
            }
            catch (Exception error)
            {
                MessageBox.Show(
                    error.Message,
                    "Ошибка ViewCube");
            }
        }

        // ============================================================
        // СОБЫТИЯ КОМПАС
        // ============================================================

        private void ViewCubeControl_Loaded(
            object sender,
            RoutedEventArgs e)
        {
            try
            {
                _camera.AnimationCompleted +=
                    KompasAnimationCompleted;

                _camera.OrientationFrameChanged +=
                    KompasOrientationFrameChanged;

                _lastKompasMatrix = null;
                UpdateCubeFromKompas();
                _orientationTimer.Start();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    ex.ToString(),
                    "Ошибка подключения событий КОМПАС");
            }
        }

        private void ViewCubeControl_Unloaded(
            object sender,
            RoutedEventArgs e)
        {
            _orientationTimer.Stop();

            _camera.AnimationCompleted -=
                KompasAnimationCompleted;

            _camera.OrientationFrameChanged -=
                KompasOrientationFrameChanged;

        }

        private void OrientationTimer_Tick(
            object sender,
            EventArgs e)
        {
            UpdateCubeFromKompas();
        }

        private void KompasAnimationCompleted()
        {
            // После последнего кадра один раз читаем фактическое состояние.
            _lastKompasMatrix = null;

            Dispatcher.BeginInvoke(
                new Action(UpdateCubeFromKompas),
                DispatcherPriority.Render);
        }

        private void KompasOrientationFrameChanged(
            double[] matrix)
        {
            // Эти кадры уже интерполированы камерой с easing, поэтому
            // показываем их сразу, без дополнительного запаздывания.
            SetCubeOrientation(matrix);
        }

        // ============================================================
        // КОМПАС -> VIEWCUBE
        // ============================================================

        private void UpdateCubeFromKompas()
        {
            if (_camera.IsViewCubeAnimating)
                return;

            try
            {
                double[] matrix =
                    _camera.GetOrientationMatrix();

                if (matrix == null ||
                    matrix.Length != 16)
                {
                    return;
                }

                if (MatricesAreEqual(
                    matrix,
                    _lastKompasMatrix))
                {
                    return;
                }

                _lastKompasMatrix =
                    (double[])matrix.Clone();

                SetCubeOrientation(matrix);
            }
            catch
            {
                // Документ может закрываться/переключаться.
            }
        }

        private static bool MatricesAreEqual(
            double[] a,
            double[] b)
        {
            if (a == null ||
                b == null ||
                a.Length != 16 ||
                b.Length != 16)
            {
                return false;
            }

            const double epsilon =
                0.00001;

            int[] indices =
            {
                0, 1, 2,
                4, 5, 6,
                8, 9, 10
            };

            foreach (int i in indices)
            {
                if (Math.Abs(
                    a[i] - b[i]) > epsilon)
                {
                    return false;
                }
            }

            return true;
        }

        private void SetCubeOrientation(double[] m)
        {
            if (m == null ||
                m.Length != 16)
            {
                return;
            }

            Vector3D up =
                new Vector3D(
                    m[4],
                    m[5],
                    m[6]);

            Vector3D view =
                new Vector3D(
                    m[8],
                    m[9],
                    m[10]);

            if (up.LengthSquared < 0.000001 ||
                view.LengthSquared < 0.000001)
            {
                return;
            }

            up.Normalize();
            view.Normalize();

            ApplyCubeOrientation(up, view);
        }

        private void ApplyCubeOrientation(
            Vector3D up,
            Vector3D view)
        {

            const double distance =
                6.0;

            CubeCamera.Position =
                new Point3D(
                    view.X * distance,
                    view.Y * distance,
                    view.Z * distance);

            CubeCamera.LookDirection =
                new Vector3D(
                    -view.X * distance,
                    -view.Y * distance,
                    -view.Z * distance);

            CubeCamera.UpDirection = up;
        }
    }
}
