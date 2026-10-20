using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;
using KompasAPI7;

namespace KompasViewCube
{
    public sealed class KompasCamera
    {
        private readonly KompasApi7CameraAdapter _adapter;

        private CancellationTokenSource _animationCts;
        private int _animationGeneration;

        public bool IsViewCubeAnimating { get; private set; }
        public event Action AnimationCompleted;
        public event Action<double[]> OrientationFrameChanged;

        // Скорость поворота ViewCube.
        // Потом можем сделать настройкой.
        private const int AnimationDurationMs = 280;

        // Приблизительно 60 кадров/сек.
        private const int FrameDelayMs = 16;


        public KompasCamera()
        {
            IApplication application =
                (IApplication)Marshal.GetActiveObject("KOMPAS.Application.7");
            _adapter =
                new KompasApi7CameraAdapter(application);
        }

        public KompasCamera(IApplication application)
        {
            _adapter = new KompasApi7CameraAdapter(application);
        }

        // ============================================================
        // ДОСТУП К КОМПАСУ
        // ============================================================

        private double[] GetCurrentMatrix()
        {
            return _adapter.GetMatrix();
        }


        // ============================================================
        // 6 ГРАНЕЙ
        // ============================================================

        public Task Front()
            => AnimateToDirection(0, -1, 0);

        public Task Back()
            => AnimateToDirection(0, 1, 0);

        public Task Left()
            => AnimateToDirection(-1, 0, 0);

        public Task Right()
            => AnimateToDirection(1, 0, 0);

        public Task Top()
            => AnimateToDirection(0, 0, 1);

        public Task Bottom()
            => AnimateToDirection(0, 0, -1);


        // ============================================================
        // 12 РЁБЕР
        // ============================================================

        public Task FrontTop()
            => AnimateToDirection(0, -1, 1);

        public Task FrontBottom()
            => AnimateToDirection(0, -1, -1);

        public Task FrontLeft()
            => AnimateToDirection(-1, -1, 0);

        public Task FrontRight()
            => AnimateToDirection(1, -1, 0);


        public Task BackTop()
            => AnimateToDirection(0, 1, 1);

        public Task BackBottom()
            => AnimateToDirection(0, 1, -1);

        public Task BackLeft()
            => AnimateToDirection(-1, 1, 0);

        public Task BackRight()
            => AnimateToDirection(1, 1, 0);


        public Task LeftTop()
            => AnimateToDirection(-1, 0, 1);

        public Task LeftBottom()
            => AnimateToDirection(-1, 0, -1);

        public Task RightTop()
            => AnimateToDirection(1, 0, 1);

        public Task RightBottom()
            => AnimateToDirection(1, 0, -1);


        // ============================================================
        // 8 УГЛОВ
        // ============================================================

        public Task FrontTopRight()
            => AnimateToDirection(1, -1, 1);

        public Task FrontTopLeft()
            => AnimateToDirection(-1, -1, 1);

        public Task FrontBottomRight()
            => AnimateToDirection(1, -1, -1);

        public Task FrontBottomLeft()
            => AnimateToDirection(-1, -1, -1);


        public Task BackTopRight()
            => AnimateToDirection(1, 1, 1);

        public Task BackTopLeft()
            => AnimateToDirection(-1, 1, 1);

        public Task BackBottomRight()
            => AnimateToDirection(1, 1, -1);

        public Task BackBottomLeft()
            => AnimateToDirection(-1, 1, -1);


        // ============================================================
        // ROLL
        // ============================================================

        public Task RollClockwise()
        {
            return AnimateRoll(-90.0);
        }


        public Task RollCounterClockwise()
        {
            return AnimateRoll(90.0);
        }


        // ============================================================
        // ПЛАВНЫЙ ПЕРЕХОД К НАПРАВЛЕНИЮ
        // ============================================================

        private async Task AnimateToDirection(
            double x,
            double y,
            double z)
        {
            GetStandardBasis(
                out Vector3 standardRight,
                out Vector3 standardUp,
                out Vector3 standardView);

            Vector3 view =
                (standardRight * x +
                 standardView * -y +
                 standardUp * z)
                .Normalized();

            // Keep the closest quarter-turn of the selected view, rather than
            // carrying a small arbitrary roll into an otherwise exact view.
            double[] current = GetCurrentMatrix();
            Vector3 currentUp = new Vector3(current[4], current[5], current[6]).Normalized();
            Vector3 referenceUp = Math.Abs(Vector3.Dot(view, standardUp)) > 0.999999
                ? -standardView : standardUp;
            Vector3 referenceRight = Vector3.Cross(referenceUp, view).Normalized();
            referenceUp = Vector3.Cross(view, referenceRight).Normalized();

            Vector3 up = referenceUp;
            double bestScore = double.NegativeInfinity;
            foreach (Vector3 candidate in new[] { referenceUp, referenceRight, -referenceUp, -referenceRight })
            {
                double score = Vector3.Dot(currentUp, candidate);
                // If current up points along the new view, screen-right breaks
                // the otherwise indeterminate tie.
                if (Math.Abs(Vector3.Dot(currentUp, view)) > 0.999999)
                {
                    Vector3 currentRight = new Vector3(current[0], current[1], current[2]).Normalized();
                    score = Vector3.Dot(currentRight, Vector3.Cross(candidate, view));
                }
                if (score > bestScore)
                {
                    bestScore = score;
                    up = candidate;
                }
            }
            Vector3 right = Vector3.Cross(up, view).Normalized();
            await AnimateToBasis(
                right,
                up,
                view);
        }


        // ============================================================
        // ROLL — СОЗДАЁМ ЦЕЛЕВОЙ БАЗИС
        // ============================================================

        public Task Orbit(double horizontalDegrees, double verticalDegrees)
        {
            double[] m = GetCurrentMatrix();
            Vector3 r = new Vector3(m[0], m[1], m[2]).Normalized();
            Vector3 u = new Vector3(m[4], m[5], m[6]).Normalized();
            Vector3 v = new Vector3(m[8], m[9], m[10]).Normalized();
            double h = horizontalDegrees * Math.PI / 180.0;
            double p = verticalDegrees * Math.PI / 180.0;
            Vector3 nr = r * Math.Cos(h) + v * -Math.Sin(h);
            Vector3 nv = r * Math.Sin(h) + v * Math.Cos(h);
            Vector3 nu = u * Math.Cos(p) + nv * -Math.Sin(p);
            nv = u * Math.Sin(p) + nv * Math.Cos(p);
            return AnimateToBasis(nr.Normalized(), nu.Normalized(), nv.Normalized());
        }
        private async Task AnimateRoll(
            double angleDegrees)
        {
            double[] matrix =
                GetCurrentMatrix();


            Vector3 right =
                new Vector3(
                    matrix[0],
                    matrix[1],
                    matrix[2]).Normalized();

            Vector3 up =
                new Vector3(
                    matrix[4],
                    matrix[5],
                    matrix[6]).Normalized();

            Vector3 view =
                new Vector3(
                    matrix[8],
                    matrix[9],
                    matrix[10]).Normalized();


            double radians =
                angleDegrees *
                Math.PI / 180.0;

            double cos =
                Math.Cos(radians);

            double sin =
                Math.Sin(radians);


            Vector3 newRight =
                new Vector3(
                    right.X * cos + up.X * sin,
                    right.Y * cos + up.Y * sin,
                    right.Z * cos + up.Z * sin)
                .Normalized();


            Vector3 newUp =
                new Vector3(
                    up.X * cos - right.X * sin,
                    up.Y * cos - right.Y * sin,
                    up.Z * cos - right.Z * sin)
                .Normalized();


            await AnimateToBasis(
                newRight,
                newUp,
                view);
        }


        // ============================================================
        // ОСНОВНАЯ АНИМАЦИЯ
        // ============================================================

        private async Task AnimateToBasis(
            Vector3 targetRight,
            Vector3 targetUp,
            Vector3 targetView)
        {
            /*
             * Если пользователь во время вращения
             * нажал другой вид —
             * старая анимация сразу прекращается.
             */
            _animationCts?.Cancel();
            _animationCts?.Dispose();

            _animationCts =
                new CancellationTokenSource();

            CancellationToken token =
                _animationCts.Token;

            int animationGeneration =
                Interlocked.Increment(ref _animationGeneration);

            IsViewCubeAnimating = true;

            try
            {
            // --------------------------------------------------------
            // Запоминаем состояние окна
            // --------------------------------------------------------

            double[] sourceMatrix =
                GetCurrentMatrix();

            GetStandardBasis(
                out Vector3 displayRight,
                out Vector3 displayUp,
                out Vector3 displayView);

            double scale =
                _adapter.GetScale();


            Vector3 sourceRight =
                new Vector3(
                    sourceMatrix[0],
                    sourceMatrix[1],
                    sourceMatrix[2])
                .Normalized();

            Vector3 sourceUp =
                new Vector3(
                    sourceMatrix[4],
                    sourceMatrix[5],
                    sourceMatrix[6])
                .Normalized();

            Vector3 sourceView =
                new Vector3(
                    sourceMatrix[8],
                    sourceMatrix[9],
                    sourceMatrix[10])
                .Normalized();


            // --------------------------------------------------------
            // Матрицы → кватернионы
            // --------------------------------------------------------

            Quaternion start =
                Quaternion.FromBasis(
                    sourceRight,
                    sourceUp,
                    sourceView);

            Quaternion end =
                Quaternion.FromBasis(
                    targetRight,
                    targetUp,
                    targetView);


            Stopwatch clock = Stopwatch.StartNew();


            while (clock.ElapsedMilliseconds <
                   AnimationDurationMs)
            {
                if (token.IsCancellationRequested)
                    return;


                double t =
                    (double)clock.ElapsedMilliseconds /
                    AnimationDurationMs;


                /*
                 * EaseInOut.
                 *
                 * Вместо линейной скорости получаем:
                 *
                 * медленно → быстрее → медленно.
                 */
                double smoothT =
                    SmoothStep(t);


                Quaternion current =
                    Quaternion.Slerp(
                        start,
                        end,
                        smoothT);


                current.ToBasis(
                    out Vector3 right,
                    out Vector3 up,
                    out Vector3 view);


                /*
                 * На каждом кадре берём КОПИЮ
                 * исходной Matrix3D.
                 *
                 * Поэтому положение объекта,
                 * центр вида и остальные параметры
                 * не интерполируются и не портятся.
                 */
                double[] frame =
                    (double[])sourceMatrix.Clone();


                frame[0] = right.X;
                frame[1] = right.Y;
                frame[2] = right.Z;

                frame[4] = up.X;
                frame[5] = up.Y;
                frame[6] = up.Z;

                frame[8] = view.X;
                frame[9] = view.Y;
                frame[10] = view.Z;


                _adapter.SetMatrix(
                    frame,
                    scale);

                OrientationFrameChanged?.Invoke(
                    ToCubeOrientationMatrix(
                        frame,
                        displayRight,
                        displayUp,
                        displayView));


                await Task.Delay(
                    FrameDelayMs);


            }


            if (token.IsCancellationRequested)
                return;


            // --------------------------------------------------------
            // Последний кадр ставим ТОЧНО
            // --------------------------------------------------------

            double[] finalMatrix =
                (double[])sourceMatrix.Clone();


            finalMatrix[0] =
                targetRight.X;

            finalMatrix[1] =
                targetRight.Y;

            finalMatrix[2] =
                targetRight.Z;


            finalMatrix[4] =
                targetUp.X;

            finalMatrix[5] =
                targetUp.Y;

            finalMatrix[6] =
                targetUp.Z;


            finalMatrix[8] =
                targetView.X;

            finalMatrix[9] =
                targetView.Y;

            finalMatrix[10] =
                targetView.Z;


            _adapter.SetMatrix(
                finalMatrix,
                scale);

            OrientationFrameChanged?.Invoke(
                ToCubeOrientationMatrix(
                    finalMatrix,
                    displayRight,
                    displayUp,
                    displayView));
            }
            finally
            {
                // Старая отменённая анимация не должна снять блокировку,
                // если новая уже успела начаться.
                if (animationGeneration == _animationGeneration)
                {
                    IsViewCubeAnimating = false;
                    AnimationCompleted?.Invoke();
                }
            }
        }


        // ============================================================
        // EASING
        // ============================================================

        private static double SmoothStep(
            double t)
        {
            if (t < 0)
                t = 0;

            if (t > 1)
                t = 1;

            return
                t * t *
                (3.0 - 2.0 * t);
        }


        // ============================================================
        // VECTOR3
        // ============================================================
        // ============================================================
        // ПУБЛИЧНЫЙ ДОСТУП ДЛЯ VIEWCUBE
        // ============================================================

        public double[] GetOrientationMatrix()
        {
            double[] matrix = GetCurrentMatrix();

            GetStandardBasis(
                out Vector3 standardRight,
                out Vector3 standardUp,
                out Vector3 standardView);

            return ToCubeOrientationMatrix(
                matrix,
                standardRight,
                standardUp,
                standardView);
        }

        private void GetStandardBasis(
            out Vector3 right,
            out Vector3 up,
            out Vector3 view)
        {
            double[] matrix =
                _adapter.GetFrontViewMatrix();

            right = new Vector3(
                matrix[0], matrix[1], matrix[2]).Normalized();

            up = new Vector3(
                matrix[4], matrix[5], matrix[6]).Normalized();

            view = new Vector3(
                matrix[8], matrix[9], matrix[10]).Normalized();
        }

        private static double[] ToCubeOrientationMatrix(
            double[] matrix,
            Vector3 standardRight,
            Vector3 standardUp,
            Vector3 standardView)
        {
            Vector3 worldRight = new Vector3(
                matrix[0], matrix[1], matrix[2]).Normalized();

            Vector3 worldUp = new Vector3(
                matrix[4], matrix[5], matrix[6]).Normalized();

            Vector3 worldView = new Vector3(
                matrix[8], matrix[9], matrix[10]).Normalized();

            Vector3 cubeRight = ToCubeVector(
                worldRight, standardRight, standardUp, standardView);

            Vector3 cubeUp = ToCubeVector(
                worldUp, standardRight, standardUp, standardView);

            Vector3 cubeView = ToCubeVector(
                worldView, standardRight, standardUp, standardView);

            double[] result = (double[])matrix.Clone();

            result[0] = cubeRight.X;
            result[1] = cubeRight.Y;
            result[2] = cubeRight.Z;
            result[4] = cubeUp.X;
            result[5] = cubeUp.Y;
            result[6] = cubeUp.Z;
            result[8] = cubeView.X;
            result[9] = cubeView.Y;
            result[10] = cubeView.Z;

            return result;
        }

        private static Vector3 ToCubeVector(
            Vector3 world,
            Vector3 standardRight,
            Vector3 standardUp,
            Vector3 standardView)
        {
            // Канонический базис геометрии куба:
            // Right = +X, Top = +Z, Front = -Y.
            return new Vector3(
                Vector3.Dot(world, standardRight),
                -Vector3.Dot(world, standardView),
                Vector3.Dot(world, standardUp));
        }

        private struct Vector3
        {
            public double X;
            public double Y;
            public double Z;


            public Vector3(
                double x,
                double y,
                double z)
            {
                X = x;
                Y = y;
                Z = z;
            }


            public double Length
            {
                get
                {
                    return Math.Sqrt(
                        X * X +
                        Y * Y +
                        Z * Z);
                }
            }


            public Vector3 Normalized()
            {
                double length =
                    Length;

                if (length <
                    0.0000001)
                {
                    throw new InvalidOperationException(
                        "Нельзя нормализовать нулевой вектор.");
                }

                return new Vector3(
                    X / length,
                    Y / length,
                    Z / length);
            }

            public static Vector3 operator +(
                Vector3 a,
                Vector3 b)
            {
                return new Vector3(
                    a.X + b.X,
                    a.Y + b.Y,
                    a.Z + b.Z);
            }

            public static Vector3 operator *(
                Vector3 value,
                double scale)
            {
                return new Vector3(
                    value.X * scale,
                    value.Y * scale,
                    value.Z * scale);
            }

            public static Vector3 operator -(
                Vector3 value)
            {
                return new Vector3(
                    -value.X,
                    -value.Y,
                    -value.Z);
            }


            public static double Dot(
                Vector3 a,
                Vector3 b)
            {
                return
                    a.X * b.X +
                    a.Y * b.Y +
                    a.Z * b.Z;
            }


            public static Vector3 Cross(
                Vector3 a,
                Vector3 b)
            {
                return new Vector3(
                    a.Y * b.Z -
                    a.Z * b.Y,

                    a.Z * b.X -
                    a.X * b.Z,

                    a.X * b.Y -
                    a.Y * b.X);
            }
        }


        // ============================================================
        // QUATERNION
        // ============================================================

        private struct Quaternion
        {
            public double X;
            public double Y;
            public double Z;
            public double W;


            public Quaternion(
                double x,
                double y,
                double z,
                double w)
            {
                X = x;
                Y = y;
                Z = z;
                W = w;
            }


            public Quaternion Normalized()
            {
                double length =
                    Math.Sqrt(
                        X * X +
                        Y * Y +
                        Z * Z +
                        W * W);

                if (length <
                    0.0000001)
                {
                    return new Quaternion(
                        0, 0, 0, 1);
                }

                return new Quaternion(
                    X / length,
                    Y / length,
                    Z / length,
                    W / length);
            }


            public static Quaternion operator -(
                Quaternion q)
            {
                return new Quaternion(
                    -q.X,
                    -q.Y,
                    -q.Z,
                    -q.W);
            }


            public static double Dot(
                Quaternion a,
                Quaternion b)
            {
                return
                    a.X * b.X +
                    a.Y * b.Y +
                    a.Z * b.Z +
                    a.W * b.W;
            }


            // --------------------------------------------------------
            // BASIS → QUATERNION
            // --------------------------------------------------------

            public static Quaternion FromBasis(
                Vector3 right,
                Vector3 up,
                Vector3 view)
            {
                /*
                 * Наши базисные векторы лежат
                 * в строках Matrix3D:
                 *
                 * Right
                 * Up
                 * View
                 */

                double m00 = right.X;
                double m01 = right.Y;
                double m02 = right.Z;

                double m10 = up.X;
                double m11 = up.Y;
                double m12 = up.Z;

                double m20 = view.X;
                double m21 = view.Y;
                double m22 = view.Z;


                double trace =
                    m00 + m11 + m22;


                double x;
                double y;
                double z;
                double w;


                if (trace > 0)
                {
                    double s =
                        Math.Sqrt(
                            trace + 1.0) *
                        2.0;

                    w = 0.25 * s;

                    x =
                        (m21 - m12) / s;

                    y =
                        (m02 - m20) / s;

                    z =
                        (m10 - m01) / s;
                }
                else if (
                    m00 > m11 &&
                    m00 > m22)
                {
                    double s =
                        Math.Sqrt(
                            1.0 +
                            m00 -
                            m11 -
                            m22) *
                        2.0;

                    w =
                        (m21 - m12) / s;

                    x =
                        0.25 * s;

                    y =
                        (m01 + m10) / s;

                    z =
                        (m02 + m20) / s;
                }
                else if (
                    m11 > m22)
                {
                    double s =
                        Math.Sqrt(
                            1.0 +
                            m11 -
                            m00 -
                            m22) *
                        2.0;

                    w =
                        (m02 - m20) / s;

                    x =
                        (m01 + m10) / s;

                    y =
                        0.25 * s;

                    z =
                        (m12 + m21) / s;
                }
                else
                {
                    double s =
                        Math.Sqrt(
                            1.0 +
                            m22 -
                            m00 -
                            m11) *
                        2.0;

                    w =
                        (m10 - m01) / s;

                    x =
                        (m02 + m20) / s;

                    y =
                        (m12 + m21) / s;

                    z =
                        0.25 * s;
                }


                return new Quaternion(
                    x,
                    y,
                    z,
                    w)
                    .Normalized();
            }


            // --------------------------------------------------------
            // QUATERNION → BASIS
            // --------------------------------------------------------

            public void ToBasis(
                out Vector3 right,
                out Vector3 up,
                out Vector3 view)
            {
                Quaternion q =
                    Normalized();


                double xx = q.X * q.X;
                double yy = q.Y * q.Y;
                double zz = q.Z * q.Z;

                double xy = q.X * q.Y;
                double xz = q.X * q.Z;
                double yz = q.Y * q.Z;

                double wx = q.W * q.X;
                double wy = q.W * q.Y;
                double wz = q.W * q.Z;


                right =
                    new Vector3(
                        1.0 -
                        2.0 * (yy + zz),

                        2.0 *
                        (xy - wz),

                        2.0 *
                        (xz + wy));


                up =
                    new Vector3(
                        2.0 *
                        (xy + wz),

                        1.0 -
                        2.0 * (xx + zz),

                        2.0 *
                        (yz - wx));


                view =
                    new Vector3(
                        2.0 *
                        (xz - wy),

                        2.0 *
                        (yz + wx),

                        1.0 -
                        2.0 * (xx + yy));
            }


            // --------------------------------------------------------
            // SLERP
            // --------------------------------------------------------

            public static Quaternion Slerp(
                Quaternion a,
                Quaternion b,
                double t)
            {
                a = a.Normalized();
                b = b.Normalized();


                double dot =
                    Dot(a, b);


                if (dot < 0.0)
                {
                    b = -b;
                    dot = -dot;
                }


                /*
                 * Если кватернионы почти одинаковые,
                 * обычный SLERP становится
                 * численно нестабильным.
                 *
                 * Тогда используем LERP.
                 */
                if (dot > 0.9995)
                {
                    Quaternion result =
                        new Quaternion(
                            a.X +
                            t * (b.X - a.X),

                            a.Y +
                            t * (b.Y - a.Y),

                            a.Z +
                            t * (b.Z - a.Z),

                            a.W +
                            t * (b.W - a.W));

                    return
                        result.Normalized();
                }


                dot =
                    Math.Max(
                        -1.0,
                        Math.Min(
                            1.0,
                            dot));


                double theta0 =
                    Math.Acos(dot);

                double theta =
                    theta0 * t;


                double sinTheta =
                    Math.Sin(theta);

                double sinTheta0 =
                    Math.Sin(theta0);


                double s0 =
                    Math.Cos(theta) -
                    dot *
                    sinTheta /
                    sinTheta0;

                double s1 =
                    sinTheta /
                    sinTheta0;


                return new Quaternion(
                    s0 * a.X +
                    s1 * b.X,

                    s0 * a.Y +
                    s1 * b.Y,

                    s0 * a.Z +
                    s1 * b.Z,

                    s0 * a.W +
                    s1 * b.W)
                    .Normalized();
            }
        }
    }
}
