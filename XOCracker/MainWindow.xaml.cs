using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Input;

namespace XOCracker
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow
    {
        private GamePreset _gamePreset;
        public MainWindow()
        {
            InitializeComponent();
            InitializePresetTab();
        }

        private void InitializePresetTab()
        {
            _gamePreset = GamePreset.Initialize();
            UpdatePresetControls();
        }

        private void UpdatePresetControls()
        {
            StartSprite.Source = ScreenshotRegion.BitmapToImageSource(_gamePreset.StartSprite);
            if (_gamePreset.StartSprite != null)
            {
                StartSprite.MaxHeight = _gamePreset.StartSprite.Height;
                StartSprite.MaxWidth = _gamePreset.StartSprite.Width;
            }

            StartSprite.Visibility = _gamePreset.StartSprite != null
                ? Visibility.Visible : Visibility.Collapsed;
            StartSpriteText.Visibility = _gamePreset.StartSprite != null
                ? Visibility.Collapsed : Visibility.Visible;
            GameProcessTab.IsEnabled = _gamePreset.IsReady();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            ScreenshotRegion screener = new ScreenshotRegion();
            if (screener.ShowDialog() == true)
            {
                _gamePreset.StartSprite = screener.Picture;
                UpdatePresetControls();
            }
        }

        private void NumberValidationTextBox(object sender, TextCompositionEventArgs e)
        {
            
        }

        private void TextBoxPasting(object sender, DataObjectPastingEventArgs e)
        {
            if (e.DataObject.GetDataPresent(typeof(string)))
            {
                string text = (string)e.DataObject.GetData(typeof(string));
                Regex regex = new Regex("[^0-9]+");
                if (text != null && regex.IsMatch(text))
                {
                    e.CancelCommand();
                }
            }
            else
            {
                e.CancelCommand();
            }
        }
    }
}
