using System.Windows;
using System.Windows.Controls;

namespace KompasViewCube
{
    public partial class SettingsWindow : Window
    {
        private readonly ViewCubeSettings _settings;
        public SettingsWindow()
        {
            InitializeComponent();
            _settings = ViewCubeSettings.Load();
            bool ru = _settings.Language == "ru";
            Title = ru ? "Настройки ViewCube" : "ViewCube settings";
            LanguageLabel.Text = ru ? "Язык" : "Language";
            ScaleLabel.Text = ru ? "Размер куба" : "Cube size";
            PositionLabel.Text = ru ? "Положение" : "Position";
            CancelButton.Content = ru ? "Отмена" : "Cancel";
            CancelButton.Click += (sender, e) => Close();
            SaveButton.Content = ru ? "Сохранить" : "Save";
            PositionBox.Content = ru ? "Переместить мышью" : "Drag to position";
            LanguageBox.SelectedValue = _settings.Language;
            ScaleSlider.Value = _settings.Scale * 100.0;
            Closed += (sender, e) => ViewCubeSettings.PositionEditing = false;
        }
        private void PositionEdit_Changed(object sender, RoutedEventArgs e)
        {
            ViewCubeSettings.PositionEditing = PositionBox.IsChecked == true;
        }
        private void ScaleSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (ScaleValue != null) ScaleValue.Text = ((int)e.NewValue) + "%";
        }
        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            _settings.Language = (string)LanguageBox.SelectedValue ?? "ru";
            _settings.Scale = ScaleSlider.Value / 100.0;
            ViewCubeSettings position = ViewCubeSettings.Load();
            _settings.PositionX = position.PositionX;
            _settings.PositionY = position.PositionY;
            _settings.Save();
            Close();
        }
    }
}
