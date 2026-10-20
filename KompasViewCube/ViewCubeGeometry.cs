using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Media3D;

namespace KompasViewCube
{
    /// <summary>
    /// Только геометрия и материалы ViewCube.
    ///
    /// Идея:
    /// 1. Сначала строится непрерывная тёмная оболочка rounded-box.
    /// 2. Поверх шести сторон кладутся светлые скруглённые панели.
    /// 3. 12 рёбер и 8 углов остаются отдельными GeometryModel3D,
    ///    поэтому старые 26 команд и hover продолжают работать.
    /// 4. Угол НЕ является обычной сферой:
    ///    внутри octant-патча радиус плавно увеличивается, а на границах
    ///    возвращается к радиусу ребра. Это даёт более "NURBS/blend"
    ///    форму без разрыва со смежными рёбрами.
    /// </summary>
    public sealed class ViewCubeGeometry
    {
        public sealed class BuildResult
        {
            public Model3DGroup Group { get; } = new Model3DGroup();

            public Dictionary<GeometryModel3D, string> Commands { get; } =
                new Dictionary<GeometryModel3D, string>();

            public Dictionary<GeometryModel3D, Material> NormalMaterials { get; } =
                new Dictionary<GeometryModel3D, Material>();

            public Dictionary<GeometryModel3D, Material> HoverMaterials { get; } =
                new Dictionary<GeometryModel3D, Material>();
        }

        // ============================================================
        // ОСНОВНЫЕ РАЗМЕРЫ
        // ============================================================

        private const double Outer = 1.0;

        // Радиус обычного ребра.
        private const double EdgeRadius = 0.22;
        private const double Inner = Outer - EdgeRadius;

        // Насколько угол становится "полнее" обычного сферического.
        //
        // ВАЖНО: теперь blend никогда не выходит за габарит Outer.
        // Поэтому угол визуально вливается в оболочку, а не образует
        // отдельную "пупырку".
        private const double CornerBulge = 0.12;

        // Светлая центральная панель.
        private const double PanelHalf = 0.715;
        private const double PanelCornerRadius = 0.275;
        private const double PanelLift = 0.004;

        private const int EdgeSegments = 8;
        private const int CornerSegments = 10;
        private const int PanelCornerSegments = 8;

        // ============================================================
        // ЦВЕТА
        // ============================================================

        private static readonly Color FaceColor =
            Color.FromRgb(226, 229, 232);

        private static readonly Color EdgeColor =
            Color.FromRgb(203, 206, 209);

        private static readonly Color CornerColor =
            Color.FromRgb(183, 185, 188);

        private static readonly Color HoverColor =
            Color.FromRgb(255, 170, 0);

        private static readonly Color TextColor =
            Color.FromRgb(69, 73, 77);


        private BuildResult _result;


        public BuildResult Build(string language = "en")
        {
            _result = new BuildResult();

            // Непрерывный непрозрачный каркас.
            BuildBaseFaceShells();

            // 12 интерактивных рёбер.
            BuildEdges();

            // 8 плавных blend-углов.
            BuildBlendCorners();

            // 6 светлых интерактивных панелей.
            BuildFacePanels(language);

            return _result;
        }


        // ============================================================
        // БАЗОВЫЕ ПЛОСКИЕ ЧАСТИ ОБОЛОЧКИ
        //
        // Они закрывают все места под светлой панелью/между рёбрами.
        // У них нет command: это именно визуальная подложка.
        // ============================================================

        private void BuildBaseFaceShells()
        {
            Material shell =
                CreateMaterial(EdgeColor);

            AddVisualQuad(
                P(-Inner, -Outer, -Inner),
                P( Inner, -Outer, -Inner),
                P( Inner, -Outer,  Inner),
                P(-Inner, -Outer,  Inner),
                new Vector3D(0, -1, 0),
                shell);

            AddVisualQuad(
                P( Inner, Outer, -Inner),
                P(-Inner, Outer, -Inner),
                P(-Inner, Outer,  Inner),
                P( Inner, Outer,  Inner),
                new Vector3D(0, 1, 0),
                shell);

            AddVisualQuad(
                P(Outer, -Inner, -Inner),
                P(Outer,  Inner, -Inner),
                P(Outer,  Inner,  Inner),
                P(Outer, -Inner,  Inner),
                new Vector3D(1, 0, 0),
                shell);

            AddVisualQuad(
                P(-Outer,  Inner, -Inner),
                P(-Outer, -Inner, -Inner),
                P(-Outer, -Inner,  Inner),
                P(-Outer,  Inner,  Inner),
                new Vector3D(-1, 0, 0),
                shell);

            AddVisualQuad(
                P(-Inner, -Inner, Outer),
                P( Inner, -Inner, Outer),
                P( Inner,  Inner, Outer),
                P(-Inner,  Inner, Outer),
                new Vector3D(0, 0, 1),
                shell);

            AddVisualQuad(
                P(-Inner,  Inner, -Outer),
                P( Inner,  Inner, -Outer),
                P( Inner, -Inner, -Outer),
                P(-Inner, -Inner, -Outer),
                new Vector3D(0, 0, -1),
                shell);
        }


        // ============================================================
        // 12 РЁБЕР
        // ============================================================

        private void BuildEdges()
        {
            Material normal =
                CreateMaterial(EdgeColor);

            Material hover =
                CreateMaterial(HoverColor);

            AddRoundedEdgeX(-1,  1, "FrontTop",     normal, hover);
            AddRoundedEdgeX(-1, -1, "FrontBottom",  normal, hover);
            AddRoundedEdgeX( 1,  1, "BackTop",      normal, hover);
            AddRoundedEdgeX( 1, -1, "BackBottom",   normal, hover);

            AddRoundedEdgeY(-1,  1, "LeftTop",      normal, hover);
            AddRoundedEdgeY(-1, -1, "LeftBottom",   normal, hover);
            AddRoundedEdgeY( 1,  1, "RightTop",     normal, hover);
            AddRoundedEdgeY( 1, -1, "RightBottom",  normal, hover);

            AddRoundedEdgeZ(-1, -1, "FrontLeft",    normal, hover);
            AddRoundedEdgeZ( 1, -1, "FrontRight",   normal, hover);
            AddRoundedEdgeZ(-1,  1, "BackLeft",     normal, hover);
            AddRoundedEdgeZ( 1,  1, "BackRight",    normal, hover);
        }


        private void AddRoundedEdgeX(
            int ySign,
            int zSign,
            string command,
            Material normal,
            Material hover)
        {
            MeshGeometry3D mesh =
                new MeshGeometry3D();

            for (int i = 0; i <= EdgeSegments; i++)
            {
                double a =
                    (Math.PI / 2.0) *
                    i / EdgeSegments;

                double ny =
                    ySign * Math.Cos(a);

                double nz =
                    zSign * Math.Sin(a);

                double y =
                    ySign * Inner +
                    ny * EdgeRadius;

                double z =
                    zSign * Inner +
                    nz * EdgeRadius;

                AddVertex(
                    mesh,
                    P(-Inner, y, z),
                    Normalized(0, ny, nz));

                AddVertex(
                    mesh,
                    P(Inner, y, z),
                    Normalized(0, ny, nz));
            }

            AddStripTriangles(mesh, EdgeSegments);
            Register(mesh, command, normal, hover);
        }


        private void AddRoundedEdgeY(
            int xSign,
            int zSign,
            string command,
            Material normal,
            Material hover)
        {
            MeshGeometry3D mesh =
                new MeshGeometry3D();

            for (int i = 0; i <= EdgeSegments; i++)
            {
                double a =
                    (Math.PI / 2.0) *
                    i / EdgeSegments;

                double nx =
                    xSign * Math.Cos(a);

                double nz =
                    zSign * Math.Sin(a);

                double x =
                    xSign * Inner +
                    nx * EdgeRadius;

                double z =
                    zSign * Inner +
                    nz * EdgeRadius;

                AddVertex(
                    mesh,
                    P(x, -Inner, z),
                    Normalized(nx, 0, nz));

                AddVertex(
                    mesh,
                    P(x, Inner, z),
                    Normalized(nx, 0, nz));
            }

            AddStripTriangles(mesh, EdgeSegments);
            Register(mesh, command, normal, hover);
        }


        private void AddRoundedEdgeZ(
            int xSign,
            int ySign,
            string command,
            Material normal,
            Material hover)
        {
            MeshGeometry3D mesh =
                new MeshGeometry3D();

            for (int i = 0; i <= EdgeSegments; i++)
            {
                double a =
                    (Math.PI / 2.0) *
                    i / EdgeSegments;

                double nx =
                    xSign * Math.Cos(a);

                double ny =
                    ySign * Math.Sin(a);

                double x =
                    xSign * Inner +
                    nx * EdgeRadius;

                double y =
                    ySign * Inner +
                    ny * EdgeRadius;

                AddVertex(
                    mesh,
                    P(x, y, -Inner),
                    Normalized(nx, ny, 0));

                AddVertex(
                    mesh,
                    P(x, y, Inner),
                    Normalized(nx, ny, 0));
            }

            AddStripTriangles(mesh, EdgeSegments);
            Register(mesh, command, normal, hover);
        }


        // ============================================================
        // 8 BLEND-УГЛОВ
        //
        // На границах патча угол точно совпадает с EdgeRadius,
        // поэтому бесшовно стыкуется с цилиндрическими рёбрами.
        //
        // Внутри патча радиус плавно увеличивается, но ограничивается
        // габаритом куба. Получаем "полный" blend-угол без выступов.
        // ============================================================

        private void BuildBlendCorners()
        {
            Material normal =
                CreateMaterial(CornerColor);

            Material hover =
                CreateMaterial(HoverColor);

            AddBlendCorner( 1, -1,  1, "FrontTopRight",     normal, hover);
            AddBlendCorner(-1, -1,  1, "FrontTopLeft",      normal, hover);
            AddBlendCorner( 1, -1, -1, "FrontBottomRight",  normal, hover);
            AddBlendCorner(-1, -1, -1, "FrontBottomLeft",   normal, hover);

            AddBlendCorner( 1,  1,  1, "BackTopRight",      normal, hover);
            AddBlendCorner(-1,  1,  1, "BackTopLeft",       normal, hover);
            AddBlendCorner( 1,  1, -1, "BackBottomRight",   normal, hover);
            AddBlendCorner(-1,  1, -1, "BackBottomLeft",    normal, hover);
        }


        private void AddBlendCorner(
            int xSign,
            int ySign,
            int zSign,
            string command,
            Material normal,
            Material hover)
        {
            MeshGeometry3D mesh =
                new MeshGeometry3D();

            int row =
                CornerSegments + 1;

            for (int p = 0; p <= CornerSegments; p++)
            {
                double phi =
                    (Math.PI / 2.0) *
                    p / CornerSegments;

                for (int t = 0; t <= CornerSegments; t++)
                {
                    double theta =
                        (Math.PI / 2.0) *
                        t / CornerSegments;

                    double sx =
                        Math.Sin(phi) *
                        Math.Cos(theta);

                    double sy =
                        Math.Sin(phi) *
                        Math.Sin(theta);

                    double sz =
                        Math.Cos(phi);

                    // Ноль на всех четырёх границах, максимум внутри.
                    double blend =
                        Math.Sin(2.0 * phi) *
                        Math.Sin(2.0 * theta);

                    /*
                     * Хотим сделать угол "полнее" сферы, но не позволяем
                     * ему выйти наружу за габарит куба.
                     *
                     * Базовая сфера:
                     *     radius = EdgeRadius
                     *
                     * В середине угла радиус немного растёт.
                     *
                     * maximumRadius — максимально допустимый радиус
                     * вдоль данного луча, при котором ни X, ни Y, ни Z
                     * не выйдет дальше плоскостей +/-Outer.
                     *
                     * Благодаря этому исчезают выступающие "пупырки",
                     * но сам угол остаётся более крупным и мягким,
                     * чем обычная сферическая восьмушка.
                     */
                    double maxDirection =
                        Math.Max(
                            Math.Abs(sx),
                            Math.Max(
                                Math.Abs(sy),
                                Math.Abs(sz)));

                    double maximumRadius =
                        maxDirection > 0.000001
                            ? EdgeRadius / maxDirection
                            : EdgeRadius;

                    double desiredRadius =
                        EdgeRadius +
                        CornerBulge * blend;

                    // Небольшой запас не даёт совпадающим поверхностям
                    // мерцать из-за z-fighting.
                    double radius =
                        Math.Min(
                            desiredRadius,
                            maximumRadius - 0.0005);

                    if (radius < EdgeRadius)
                    {
                        radius =
                            EdgeRadius;
                    }

                    double nx =
                        xSign * sx;

                    double ny =
                        ySign * sy;

                    double nz =
                        zSign * sz;

                    Point3D point =
                        P(
                            xSign * Inner +
                            nx * radius,

                            ySign * Inner +
                            ny * radius,

                            zSign * Inner +
                            nz * radius);

                    /*
                     * Для маленького ViewCube радиальный normal выглядит
                     * ровно и без бликов. Геометрия сама уже задаёт blend.
                     */
                    Vector3D n =
                        Normalized(nx, ny, nz);

                    AddVertex(
                        mesh,
                        point,
                        n);
                }
            }

            for (int p = 0; p < CornerSegments; p++)
            {
                for (int t = 0; t < CornerSegments; t++)
                {
                    int a =
                        p * row + t;

                    int b =
                        a + 1;

                    int c =
                        a + row;

                    int d =
                        c + 1;

                    mesh.TriangleIndices.Add(a);
                    mesh.TriangleIndices.Add(c);
                    mesh.TriangleIndices.Add(b);

                    mesh.TriangleIndices.Add(b);
                    mesh.TriangleIndices.Add(c);
                    mesh.TriangleIndices.Add(d);
                }
            }

            Register(
                mesh,
                command,
                normal,
                hover);
        }


        // ============================================================
        // СВЕТЛЫЕ ГРАНИ
        // ============================================================

        private void BuildFacePanels(string language)
        {
            AddFacePanel(
                new Point3D(0, -Outer, 0),
                new Vector3D(1, 0, 0),
                new Vector3D(0, 0, 1),
                new Vector3D(0, -1, 0),
                FaceText(language, "FRONT", "СПЕРЕДИ"),
                "Front");

            AddFacePanel(
                new Point3D(0, Outer, 0),
                new Vector3D(-1, 0, 0),
                new Vector3D(0, 0, 1),
                new Vector3D(0, 1, 0),
                FaceText(language, "BACK", "СЗАДИ"),
                "Back");

            AddFacePanel(
                new Point3D(Outer, 0, 0),
                new Vector3D(0, 1, 0),
                new Vector3D(0, 0, 1),
                new Vector3D(1, 0, 0),
                FaceText(language, "RIGHT", "СПРАВА"),
                "Right");

            AddFacePanel(
                new Point3D(-Outer, 0, 0),
                new Vector3D(0, -1, 0),
                new Vector3D(0, 0, 1),
                new Vector3D(-1, 0, 0),
                FaceText(language, "LEFT", "СЛЕВА"),
                "Left");

            AddFacePanel(
                new Point3D(0, 0, Outer),
                new Vector3D(1, 0, 0),
                new Vector3D(0, 1, 0),
                new Vector3D(0, 0, 1),
                FaceText(language, "TOP", "СВЕРХУ"),
                "Top");

            AddFacePanel(
                new Point3D(0, 0, -Outer),
                new Vector3D(1, 0, 0),
                new Vector3D(0, -1, 0),
                new Vector3D(0, 0, -1),
                FaceText(language, "BOTTOM", "СНИЗУ"),
                "Bottom");
        }

        private static string FaceText(string language, string english, string russian)
        {
            return language == "ru" ? russian : english;
        }


        private void AddFacePanel(
            Point3D center,
            Vector3D axisU,
            Vector3D axisV,
            Vector3D normal,
            string text,
            string command)
        {
            MeshGeometry3D mesh =
                new MeshGeometry3D();

            List<Point> boundary =
                BuildRoundedRectangleBoundary(
                    PanelHalf,
                    PanelCornerRadius,
                    PanelCornerSegments);

            // Center of triangle fan.
            mesh.Positions.Add(
                LocalPoint(
                    center,
                    axisU,
                    axisV,
                    normal,
                    0,
                    0,
                    PanelLift));

            mesh.Normals.Add(normal);

            mesh.TextureCoordinates.Add(
                new Point(0.5, 0.5));

            foreach (Point p in boundary)
            {
                mesh.Positions.Add(
                    LocalPoint(
                        center,
                        axisU,
                        axisV,
                        normal,
                        p.X,
                        p.Y,
                        PanelLift));

                mesh.Normals.Add(normal);

                double u =
                    (p.X + PanelHalf) /
                    (2.0 * PanelHalf);

                double v =
                    1.0 -
                    (p.Y + PanelHalf) /
                    (2.0 * PanelHalf);

                mesh.TextureCoordinates.Add(
                    new Point(u, v));
            }

            for (int i = 0; i < boundary.Count; i++)
            {
                int current =
                    i + 1;

                int next =
                    ((i + 1) % boundary.Count) + 1;

                mesh.TriangleIndices.Add(0);
                mesh.TriangleIndices.Add(current);
                mesh.TriangleIndices.Add(next);
            }

            Register(
                mesh,
                command,
                CreateFaceMaterial(text, FaceColor),
                CreateFaceMaterial(text, HoverColor));
        }


        // ============================================================
        // HELPERS
        // ============================================================

        private void Register(
            MeshGeometry3D mesh,
            string command,
            Material normal,
            Material hover)
        {
            GeometryModel3D model =
                new GeometryModel3D(
                    mesh,
                    normal)
                {
                    BackMaterial = normal
                };

            _result.Group.Children.Add(model);
            _result.Commands[model] = command;
            _result.NormalMaterials[model] = normal;
            _result.HoverMaterials[model] = hover;
        }


        private void AddVisualQuad(
            Point3D p0,
            Point3D p1,
            Point3D p2,
            Point3D p3,
            Vector3D normal,
            Material material)
        {
            MeshGeometry3D mesh =
                new MeshGeometry3D
                {
                    Positions =
                        new Point3DCollection
                        {
                            p0, p1, p2, p3
                        },

                    Normals =
                        new Vector3DCollection
                        {
                            normal, normal, normal, normal
                        },

                    TriangleIndices =
                        new Int32Collection
                        {
                            0, 1, 2,
                            0, 2, 3
                        }
                };

            GeometryModel3D model =
                new GeometryModel3D(
                    mesh,
                    material)
                {
                    BackMaterial = material
                };

            _result.Group.Children.Add(model);
        }


        private static void AddStripTriangles(
            MeshGeometry3D mesh,
            int segments)
        {
            for (int i = 0; i < segments; i++)
            {
                int a = i * 2;
                int b = a + 1;
                int c = a + 2;
                int d = a + 3;

                mesh.TriangleIndices.Add(a);
                mesh.TriangleIndices.Add(c);
                mesh.TriangleIndices.Add(b);

                mesh.TriangleIndices.Add(b);
                mesh.TriangleIndices.Add(c);
                mesh.TriangleIndices.Add(d);
            }
        }


        private static void AddVertex(
            MeshGeometry3D mesh,
            Point3D point,
            Vector3D normal)
        {
            mesh.Positions.Add(point);
            mesh.Normals.Add(normal);
        }


        private static Material CreateMaterial(
            Color color)
        {
            SolidColorBrush brush =
                new SolidColorBrush(color);

            brush.Freeze();

            return new DiffuseMaterial(brush);
        }


        private static Material CreateFaceMaterial(
            string text,
            Color background)
        {
            Border border =
                new Border
                {
                    Width = 512,
                    Height = 512,

                    Background =
                        new SolidColorBrush(background),

                    Child =
                        new TextBlock
                        {
                            Text = text,

                            Foreground =
                                new SolidColorBrush(TextColor),

                            FontFamily =
                                new FontFamily("Bahnschrift Condensed"),

                            FontStretch =
                                FontStretches.Condensed,

                            FontSize = 92,

                            FontWeight =
                                FontWeights.Normal,

                            HorizontalAlignment =
                                HorizontalAlignment.Center,

                            VerticalAlignment =
                                VerticalAlignment.Center,

                            TextAlignment =
                                TextAlignment.Center
                        }
                };

            border.Measure(
                new Size(512, 512));

            border.Arrange(
                new Rect(
                    0,
                    0,
                    512,
                    512));

            VisualBrush visualBrush =
                new VisualBrush(border)
                {
                    Stretch = Stretch.Fill
                };

            return new DiffuseMaterial(
                visualBrush);
        }


        private static List<Point> BuildRoundedRectangleBoundary(
            double half,
            double radius,
            int segments)
        {
            List<Point> points =
                new List<Point>();

            double c =
                half - radius;

            AddArc(points,  c, -c, -90,   0, radius, segments);
            AddArc(points,  c,  c,   0,  90, radius, segments);
            AddArc(points, -c,  c,  90, 180, radius, segments);
            AddArc(points, -c, -c, 180, 270, radius, segments);

            return points;
        }


        private static void AddArc(
            List<Point> points,
            double cx,
            double cy,
            double startDegrees,
            double endDegrees,
            double radius,
            int segments)
        {
            for (int i = 0; i <= segments; i++)
            {
                if (points.Count > 0 &&
                    i == 0)
                {
                    continue;
                }

                double t =
                    (double)i / segments;

                double a =
                    (startDegrees +
                     (endDegrees - startDegrees) * t) *
                    Math.PI / 180.0;

                points.Add(
                    new Point(
                        cx + Math.Cos(a) * radius,
                        cy + Math.Sin(a) * radius));
            }
        }


        private static Point3D LocalPoint(
            Point3D center,
            Vector3D axisU,
            Vector3D axisV,
            Vector3D normal,
            double u,
            double v,
            double offset)
        {
            return new Point3D(
                center.X +
                axisU.X * u +
                axisV.X * v +
                normal.X * offset,

                center.Y +
                axisU.Y * u +
                axisV.Y * v +
                normal.Y * offset,

                center.Z +
                axisU.Z * u +
                axisV.Z * v +
                normal.Z * offset);
        }


        private static Vector3D Normalized(
            double x,
            double y,
            double z)
        {
            Vector3D v =
                new Vector3D(
                    x,
                    y,
                    z);

            if (v.LengthSquared >
                0.0000001)
            {
                v.Normalize();
            }

            return v;
        }


        private static Point3D P(
            double x,
            double y,
            double z)
        {
            return new Point3D(
                x,
                y,
                z);
        }
    }
}
