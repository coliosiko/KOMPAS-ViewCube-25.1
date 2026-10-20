using System;
using Microsoft.Win32;

namespace KompasViewCube
{
    public enum ViewCubeCorner { TopRight, BottomRight }

    public sealed class ViewCubeSettings
    {
        private const string RegistryPath = @"Software\KOMPAS-ViewCube";
        public static event Action Changed;
        public static bool PositionEditing { get; set; }
        public double PositionX { get; set; } = double.NaN;
        public double PositionY { get; set; } = double.NaN;

        public string Language { get; set; } = "ru";
        public double Scale { get; set; } = 1.0;
        public ViewCubeCorner Corner { get; set; } = ViewCubeCorner.TopRight;

        public static ViewCubeSettings Load()
        {
            ViewCubeSettings settings = new ViewCubeSettings();
            using (RegistryKey key = Registry.CurrentUser.OpenSubKey(RegistryPath))
            {
                if (key == null) return settings;
                double x, y;
                if (double.TryParse(key.GetValue("PositionX") as string, System.Globalization.NumberStyles.Float,
                    System.Globalization.CultureInfo.InvariantCulture, out x) && !double.IsNaN(x) && !double.IsInfinity(x))
                    settings.PositionX = Math.Max(0, Math.Min(1, x));
                if (double.TryParse(key.GetValue("PositionY") as string, System.Globalization.NumberStyles.Float,
                    System.Globalization.CultureInfo.InvariantCulture, out y) && !double.IsNaN(y) && !double.IsInfinity(y))
                    settings.PositionY = Math.Max(0, Math.Min(1, y));
                string language = key.GetValue("Language") as string;
                if (language == "ru" || language == "en") settings.Language = language;
                double scale;
                if (double.TryParse(key.GetValue("Scale") as string,
                    System.Globalization.NumberStyles.Float,
                    System.Globalization.CultureInfo.InvariantCulture, out scale))
                    settings.Scale = Math.Max(0.5, Math.Min(1.0, scale));
                ViewCubeCorner corner;
                if (Enum.TryParse(key.GetValue("Corner") as string, out corner)) settings.Corner = corner;
            }
            return settings;
        }

        public void Save()
        {
            using (RegistryKey key = Registry.CurrentUser.CreateSubKey(RegistryPath))
            {
                key.SetValue("PositionX", PositionX.ToString(System.Globalization.CultureInfo.InvariantCulture));
                key.SetValue("PositionY", PositionY.ToString(System.Globalization.CultureInfo.InvariantCulture));
                key.SetValue("Language", Language);
                key.SetValue("Scale", Scale.ToString(System.Globalization.CultureInfo.InvariantCulture));
                key.SetValue("Corner", Corner.ToString());
            }
            Changed?.Invoke();
        }
    }
}
