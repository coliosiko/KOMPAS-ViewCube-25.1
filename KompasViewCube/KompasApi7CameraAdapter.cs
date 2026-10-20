using System;
using System.Reflection;
using KompasAPI7;

namespace KompasViewCube
{
    internal sealed class KompasApi7CameraAdapter
    {
        private readonly IApplication _application;
        private IKompasDocument3D1 _cachedDocument;
        private int _cachedScheme = int.MinValue;
        private double[] _cachedFrontMatrix;

        public KompasApi7CameraAdapter(IApplication application)
        {
            _application = application ??
                throw new ArgumentNullException(nameof(application));
            int major, minor, build, revision;
            if (!_application.GetSystemVersion(out major, out minor, out build, out revision) || major != 25)
                throw new NotSupportedException("ViewCube поддерживает только КОМПАС-3D v25.");
        }

        public double[] GetMatrix()
        {
            double[] matrix = GetMatrix3D(GetManager()) as double[];
            if (!IsMatrix(matrix))
                throw new InvalidOperationException("Не удалось получить Matrix3D из КОМПАСа.");

            return (double[])matrix.Clone();
        }

        public double GetScale()
        {
            return GetManager().Scale;
        }

        public void SetMatrix(double[] matrix, double scale)
        {
            if (!IsMatrix(matrix))
                throw new ArgumentException("Ожидалась матрица 4x4.", nameof(matrix));

            GetManager().SetMatrix3D(matrix, scale);
        }

        public double[] GetFrontViewMatrix()
        {
            IKompasDocument3D1 document = GetDocument();
            IViewProjectionManager manager = document.ViewProjectionManager;
            int scheme = ReadProjectionScheme(manager);

            if (_cachedFrontMatrix != null &&
                SameDocument(_cachedDocument, document) &&
                _cachedScheme == scheme)
            {
                return (double[])_cachedFrontMatrix.Clone();
            }

            const int frontProjectionIndex = 1;
            IViewProjection7 front = manager.ViewProjection[frontProjectionIndex];
            double[] matrix = front?.Placement?.GetMatrix3D() as double[];

            bool schemeChanged =
                SameDocument(_cachedDocument, document) &&
                _cachedScheme != int.MinValue &&
                _cachedScheme != scheme;

            if (schemeChanged)
                matrix = RecalculateFrontMatrix(manager, front);

            if (!IsMatrix(matrix))
                throw new InvalidOperationException("Не удалось получить матрицу стандартного вида «Спереди».");

            _cachedDocument = document;
            _cachedScheme = scheme;
            _cachedFrontMatrix = (double[])matrix.Clone();

            return (double[])_cachedFrontMatrix.Clone();
        }

        private IKompasDocument3D1 GetDocument()
        {
            IKompasDocument3D1 document =
                _application.ActiveDocument as IKompasDocument3D1;

            if (document == null)
                throw new InvalidOperationException("В КОМПАСе нет активной 3D-модели.");

            return document;
        }

        private IViewProjectionManager GetManager()
        {
            return GetDocument().ViewProjectionManager;
        }

        private static int ReadProjectionScheme(IViewProjectionManager manager)
        {
            object value = manager.GetType().InvokeMember(
                "ViewProjectionScheme",
                BindingFlags.GetProperty,
                null,
                manager,
                null);

            return Convert.ToInt32(value);
        }

        private static double[] RecalculateFrontMatrix(
            IViewProjectionManager manager,
            IViewProjection7 front)
        {
            double[] savedMatrix = GetMatrix3D(manager) as double[];
            if (!IsMatrix(savedMatrix))
                throw new InvalidOperationException("Не удалось сохранить текущую ориентацию модели.");

            savedMatrix = (double[])savedMatrix.Clone();
            double savedScale = manager.Scale;

            try
            {
                front.Current = true;
                double[] matrix = GetMatrix3D(manager) as double[];
                return IsMatrix(matrix) ? (double[])matrix.Clone() : null;
            }
            finally
            {
                manager.SetMatrix3D(savedMatrix, savedScale);
            }
        }

        private static bool SameDocument(
            IKompasDocument3D1 left,
            IKompasDocument3D1 right)
        {
            return ReferenceEquals(left, right) ||
                   (left != null && right != null && left.Equals(right));
        }

        // The v25 typelib exposes get_Matrix3D as the Matrix3D property
        // in C#, paired with SetMatrix3D; there is no separate GetMatrix3D method.
        private static object GetMatrix3D(IViewProjectionManager manager) => manager.Matrix3D;

        private static bool IsMatrix(double[] matrix)
        {
            return matrix != null && matrix.Length == 16;
        }
    }
}
