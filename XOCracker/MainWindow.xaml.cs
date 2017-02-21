using System.Drawing;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Image = System.Windows.Controls.Image;

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
            SavePresetCommand.IsEnabled = _gamePreset.HasChanges;
            CancelPresetCommand.IsEnabled = _gamePreset.HasChanges;
            BoardRowsField.Text = _gamePreset.Rows.ToString();
            BoardColumnsField.Text = _gamePreset.Columns.ToString();
            UpdateSpiteControlls(StartSprite, StartSpriteText, _gamePreset.StartSprite);
            UpdateSpiteControlls(TurnSprite, TurnSpriteText, _gamePreset.TurnSprite);
            UpdateSpiteControlls(FreeCellSprite, FreeCellSpriteText, _gamePreset.FreeCellSprite);
            UpdateSpiteControlls(OCellSprite, OCellSpriteText, _gamePreset.OCellSprite);
            UpdateSpiteControlls(OCellSprite2, OCellSprite2Text, _gamePreset.OCellSprite2);
            UpdateSpiteControlls(XCellSprite, XCellSpriteText, _gamePreset.XCellSprite);
            UpdateSpiteControlls(XCellSprite2, XCellSprite2Text, _gamePreset.XCellSprite2);
            GameProcessTab.IsEnabled = _gamePreset.IsReady();
        }

        private void UpdateSpiteControlls(Image spriteControl, TextBlock spriteTextControl, Bitmap bitmap)
        {
            spriteControl.Source = ScreenshotRegion.BitmapToImageSource(bitmap);
            if (bitmap != null)
            {
                spriteControl.MaxHeight = bitmap.Height;
                spriteControl.MaxWidth = bitmap.Width;
            }

            spriteControl.Visibility = bitmap != null
                ? Visibility.Visible : Visibility.Collapsed;
            spriteTextControl.Visibility = bitmap != null
                ? Visibility.Collapsed : Visibility.Visible;
        }

        private void StartSpriteSelectionCommand_Click(object sender, RoutedEventArgs e)
        {
            ScreenshotRegion screener = new ScreenshotRegion(this);
            if (screener.ShowDialog() == true)
            {
                _gamePreset.StartSprite = screener.Picture;
                UpdatePresetControls();
            }
        }

        private void TurnSpriteSelectionCommand_Click(object sender, RoutedEventArgs e)
        {
            ScreenshotRegion screener = new ScreenshotRegion(this);
            if (screener.ShowDialog() == true)
            {
                _gamePreset.TurnSprite = screener.Picture;
                UpdatePresetControls();
            }
        }

        private void OCellSpriteSelectionCommand_Click(object sender, RoutedEventArgs e)
        {
            ScreenshotRegion screener = new ScreenshotRegion(this);
            if (screener.ShowDialog() == true)
            {
                _gamePreset.OCellSprite = screener.Picture;
                UpdatePresetControls();
            }
        }


        private void OCellSprite2SelectionCommand_Click(object sender, RoutedEventArgs e)
        {
            ScreenshotRegion screener = new ScreenshotRegion(this);
            if (screener.ShowDialog() == true)
            {
                _gamePreset.OCellSprite2 = screener.Picture;
                UpdatePresetControls();
            }
        }

        private void XCellSpriteSelectionCommand_Click(object sender, RoutedEventArgs e)
        {
            ScreenshotRegion screener = new ScreenshotRegion(this);
            if (screener.ShowDialog() == true)
            {
                _gamePreset.XCellSprite = screener.Picture;
                UpdatePresetControls();
            }
        }

        private void XCellSprite2SelectionCommand_Click(object sender, RoutedEventArgs e)
        {
            ScreenshotRegion screener = new ScreenshotRegion(this);
            if (screener.ShowDialog() == true)
            {
                _gamePreset.XCellSprite2 = screener.Picture;
                UpdatePresetControls();
            }
        }

        private void FreeCellSpriteSelectionCommand_Click(object sender, RoutedEventArgs e)
        {
            ScreenshotRegion screener = new ScreenshotRegion(this);
            if (screener.ShowDialog() == true)
            {
                _gamePreset.FreeCellSprite = screener.Picture;
                UpdatePresetControls();
            }
        }

        private void NumberValidationTextBox(object sender, TextCompositionEventArgs e)
        {
            Regex regex = new Regex("[^0-9]+");
            e.Handled = regex.IsMatch(e.Text);
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

        private void BoardRowsFieldChanged(object sender, TextChangedEventArgs e)
        {
            int rows;
            if (int.TryParse(BoardRowsField.Text, out rows))
            {
                _gamePreset.Rows = rows;
                UpdatePresetControls();
            }
        }

        private void BoardColumnsFieldChanged(object sender, TextChangedEventArgs e)
        {
            int columns;
            if (int.TryParse(BoardColumnsField.Text, out columns))
            {
                _gamePreset.Columns = columns;
                UpdatePresetControls();
            }
        }

        private void SavePresetCommand_Click(object sender, RoutedEventArgs e)
        {
            _gamePreset.Save();
            UpdatePresetControls();
        }

        private void CancelPresetCommand_Click(object sender, RoutedEventArgs e)
        {
            _gamePreset.Reload();
            UpdatePresetControls();
        }

        private void ResetPresetCommand_Click(object sender, RoutedEventArgs e)
        {
            _gamePreset.Reset();
            UpdatePresetControls();
        }
    }
}
