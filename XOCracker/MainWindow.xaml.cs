using System.Diagnostics;
using System.Drawing;
using System.IO;
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
        private GameProcess _gameProcess;

        public MainWindow()
        {
            InitializeComponent();
            InitializePresetTab();
            InitializeGameProcessTab();
        }

        #region Game process tab

        private void InitializeGameProcessTab()
        {
            _gameProcess = GameProcess.Initialize(_gamePreset);
            _gameProcess.OnGameStateUpdated += OnGameStateUpdated;
            if (!GameProcessTab.IsSelected)
            {
                TabControl.SelectedIndex = 1;
            }
        }

        private void OnGameStateUpdated()
        {
           UpdateBoard();
        }

        private void UpdateBoard()
        {
            var needRebuild = Board.Columns != _gamePreset.Columns || Board.Rows != _gamePreset.Rows;
            if (needRebuild)
            {
                Board.Children.Clear();
                Board.Columns = _gamePreset.Columns;
                Board.Rows = _gamePreset.Rows;
                for (int rowId = 0; rowId < _gamePreset.Rows; rowId++)
                {
                    for (int column = 0; column < _gamePreset.Columns; column++)
                    {
                        var cell = new Image
                        {
                            Source = ScreenshotRegion.BitmapToImageSource(_gamePreset.FreeCellSprite)
                        };
                        Board.Children.Add(cell);
                    }
                }
            }
            //Board.UpdateLayout();
        }

        private void UpdateGameProcessState()
        {
            if (!_gamePreset.IsReady())
            {
                TabControl.SelectedIndex = 0;
                _gameProcess.Stop();
                return;
            }

            UpdateBoard();
            if (GameProcessTab.IsSelected)
            {
                _gameProcess.Update();
            }
            else
            {
                _gameProcess.Stop();
            }
        }

        private void TabControl_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateGameProcessState();
        }


        private void StartCommand_OnClick(object sender, RoutedEventArgs e)
        {
            _gameProcess.Start();
        }

        #endregion

        #region Preset tab

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
            GameProcessTab.IsEnabled = _gamePreset.IsReady();
            OCellList.ItemsSource = _gamePreset.OCellSprites;
            XCellList.ItemsSource = _gamePreset.XCellSprites;
            OCellList.Items.Refresh();
            XCellList.Items.Refresh();
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
                ? Visibility.Visible
                : Visibility.Collapsed;
            spriteTextControl.Visibility = bitmap != null
                ? Visibility.Collapsed
                : Visibility.Visible;
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
            if (e.DataObject.GetDataPresent(typeof (string)))
            {
                string text = (string) e.DataObject.GetData(typeof (string));
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

        private void OCellSpriteSelectionCommand_Click(object sender, RoutedEventArgs e)
        {
            var image = (Bitmap) ((Button) sender).Tag;
            ScreenshotRegion screener = new ScreenshotRegion(this);
            if (screener.ShowDialog() == true)
            {
                var itemIndex = _gamePreset.OCellSprites.IndexOf(image);
                _gamePreset.OCellSprites.RemoveAt(itemIndex);
                _gamePreset.OCellSprites.Insert(itemIndex, screener.Picture);
                _gamePreset.HasChanges = true;
                UpdatePresetControls();
            }
        }

        private void XCellSpriteSelectionCommand_Click(object sender, RoutedEventArgs e)
        {
            var image = (Bitmap) ((Button) sender).Tag;
            ScreenshotRegion screener = new ScreenshotRegion(this);
            if (screener.ShowDialog() == true)
            {
                var itemIndex = _gamePreset.XCellSprites.IndexOf(image);
                _gamePreset.XCellSprites.RemoveAt(itemIndex);
                _gamePreset.XCellSprites.Insert(itemIndex, screener.Picture);
                _gamePreset.HasChanges = true;
                UpdatePresetControls();
            }
        }

        private void AddNewOCellSprite_Click(object sender, RoutedEventArgs e)
        {
            ScreenshotRegion screener = new ScreenshotRegion(this);
            if (screener.ShowDialog() == true)
            {
                _gamePreset.OCellSprites.Add(screener.Picture);
                _gamePreset.HasChanges = true;
                UpdatePresetControls();
            }
        }

        private void AddNewXCellSprite_Click(object sender, RoutedEventArgs e)
        {
            ScreenshotRegion screener = new ScreenshotRegion(this);
            if (screener.ShowDialog() == true)
            {
                _gamePreset.XCellSprites.Add(screener.Picture);
                _gamePreset.HasChanges = true;
                UpdatePresetControls();
            }
        }

        private void RemoveOCell_Click(object sender, RoutedEventArgs e)
        {
            var image = (Bitmap) ((MenuItem) sender).Tag;
            _gamePreset.OCellSprites.Remove(image);
            _gamePreset.HasChanges = true;
            UpdatePresetControls();
        }

        private void RemoveXCell_Click(object sender, RoutedEventArgs e)
        {
            var image = (Bitmap) ((MenuItem) sender).Tag;
            _gamePreset.XCellSprites.Remove(image);
            _gamePreset.HasChanges = true;
            UpdatePresetControls();
        }

        private void OpenPresetDirCommand_Click(object sender, RoutedEventArgs e)
        {
            if (!Directory.Exists(_gamePreset.DirectoryPath))
            {
                Directory.CreateDirectory(_gamePreset.DirectoryPath);
            }

            Process.Start(_gamePreset.DirectoryPath);
        }

        #endregion
    }
}
