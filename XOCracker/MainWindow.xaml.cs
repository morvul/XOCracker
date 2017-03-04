﻿using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using XOCracker.Enums;
using XOCracker.Properties;
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

        #region Main window

        public MainWindow()
        {
            InitializeComponent();
            InitializeWindow();
            InitializePresetTab();
            InitializeGameProcessTab();
        }

        private void InitializeWindow()
        {
            Width = Settings.Default.WinWidth;
            Height = Settings.Default.WinHeight;
            Top = Settings.Default.WinTop;
            Left = Settings.Default.WinLeft;
            Topmost = Settings.Default.TopMost;
            SetOnTopFlag.IsChecked = Topmost;
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            _gameProcess.Save();
            Settings.Default.WinWidth = Width;
            Settings.Default.WinHeight = Height;
            Settings.Default.WinTop = Top;
            Settings.Default.WinLeft = Left;
            Settings.Default.TopMost = Topmost;
            Settings.Default.Save();
            base.OnClosing(e);
        }

        #endregion

        #region Game process tab

        private void InitializeGameProcessTab()
        {
            _gameProcess = GameProcess.Initialize(_gamePreset, this);
            DelayField.Text = _gameProcess.Delay.ToString();
            AccuracyField.Text = _gameProcess.AnalysisAccuracy.ToString();
            DispersionField.Text = _gameProcess.Dispersion.ToString();
            MouseSpeedField.Text = _gameProcess.MouseSpeed.ToString();
            MaxTurnDelayField.Text = _gameProcess.MaxTurnDelay.ToString();
            _gameProcess.OnGameStateUpdated += OnGameStateUpdated;
            if (!GameProcessTab.IsSelected)
            {
                TabControl.SelectedIndex = 1;
            }
        }

        private void OnGameStateUpdated()
        {
            Dispatcher.BeginInvoke(new Action(UpdateBoard));
        }

        private void UpdateBoard()
        {
            var needRebuild = Board.Columns != _gamePreset.Columns || Board.Rows != _gamePreset.Rows;
            if (needRebuild)
            {
                Board.Children.Clear();
                Board.Columns = _gamePreset.Columns;
                Board.Rows = _gamePreset.Rows;
                for (var row = 0; row < _gamePreset.Rows; row++)
                {
                    for (var column = 0; column < _gamePreset.Columns; column++)
                    {
                        var cell = new Image();
                        Board.Children.Add(cell);
                        SetBoardCell(row, column, CellType.Unknown);
                    }
                }
            }

            if (_gameProcess == null)
            {
                return;
            }

            BoardInfoField.Text = _gameProcess.BoardInfo;
            while (_gameProcess.UpdatedCells.Count > 0)
            {
                var updatedCell = _gameProcess.UpdatedCells.Dequeue();
                SetBoardCell(updatedCell.Row, updatedCell.Column, updatedCell.CellType);
            }
        }

        private void SetBoardCell(int row, int column, CellType cellType)
        {
            var cell = (Image)Board.Children[Board.Columns * row + column];
            switch (cellType)
            {
                case CellType.Free:
                    cell.Source = SearchHelper.BitmapToImageSource(_gamePreset.FreeCellSprites.FirstOrDefault());
                    break;
                case CellType.OCell:
                    cell.Source = SearchHelper.BitmapToImageSource(_gamePreset.OCellSprites.FirstOrDefault());
                    break;
                case CellType.XCell:
                    cell.Source = SearchHelper.BitmapToImageSource(_gamePreset.XCellSprites.FirstOrDefault());
                    break;
                case CellType.Unknown:
                    cell.Source = new BitmapImage(new Uri("/Resources/11.gif", UriKind.Relative));
                    break;
            }
        }

        private void UpdateGameProcessState()
        {
            if (_gameProcess == null)
            { return; }
            if (!_gamePreset.IsReady())
            {
                TabControl.SelectedIndex = 0;
                _gameProcess.StopMonitoring();
                return;
            }

            UpdateBoard();
            if (GameProcessTab.IsSelected)
            {
                _gameProcess.RunMonitoring();
            }
            else
            {
                _gameProcess.StopMonitoring();
            }

            StartCommand.Visibility = _gameProcess.IsGameStarted ? Visibility.Collapsed : Visibility.Visible;
            StopCommand.Visibility = _gameProcess.IsGameStarted ? Visibility.Visible : Visibility.Collapsed;
        }

        private void TabControl_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateGameProcessState();
        }


        private void StartCommand_OnClick(object sender, RoutedEventArgs e)
        {
            StartCommand.Visibility = Visibility.Collapsed;
            StopCommand.Visibility = Visibility.Visible;
            _gameProcess.Start();
        }


        private void StopCommand_OnClick(object sender, RoutedEventArgs e)
        {
            StopCommand.Visibility = Visibility.Collapsed;
            StartCommand.Visibility = Visibility.Visible;
            _gameProcess.Stop();
        }

        private void DelayChanged(object sender, TextChangedEventArgs e)
        {
            if (DelayField.Text == "")
            {
                DelayField.Text = "0";
            }

            _gameProcess.Delay = int.Parse(DelayField.Text);
        }


        private void AccuracyChanged(object sender, TextChangedEventArgs e)
        {
            if (AccuracyField.Text == "")
            {
                AccuracyField.Text = "0";
            }

            _gameProcess.AnalysisAccuracy = int.Parse(AccuracyField.Text);
        }


        private void DispersionChanged(object sender, TextChangedEventArgs e)
        {
            if (DispersionField.Text == "")
            {
                DispersionField.Text = "0";
            }

            _gameProcess.Dispersion = int.Parse(DispersionField.Text);
        }

        private void MouseSpeedChanged(object sender, TextChangedEventArgs e)
        {
            if (MouseSpeedField.Text == "")
            {
                MouseSpeedField.Text = "0";
            }

            _gameProcess.MouseSpeed = int.Parse(MouseSpeedField.Text);
        }


        private void MaxTurnDelayFieldChanged(object sender, TextChangedEventArgs e)
        {
            if (MaxTurnDelayField.Text == "")
            {
                MaxTurnDelayField.Text = "0";
            }

            _gameProcess.MaxTurnDelay = int.Parse(MaxTurnDelayField.Text);
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
            GameProcessTab.IsEnabled = _gamePreset.IsReady();
            FreeCellList.ItemsSource = _gamePreset.FreeCellSprites;
            OCellList.ItemsSource = _gamePreset.OCellSprites;
            XCellList.ItemsSource = _gamePreset.XCellSprites;
            FreeCellList.Items.Refresh();
            OCellList.Items.Refresh();
            XCellList.Items.Refresh();
            FirstCellField.Text = _gamePreset.FirstCell.ToString();
            LastCellField.Text = _gamePreset.LastCell.ToString();
            WinLengthField.Text = _gamePreset.VinLength.ToString();
        }

        private void UpdateSpiteControlls(Image spriteControl, TextBlock spriteTextControl, Bitmap bitmap)
        {
            spriteControl.Source = SearchHelper.BitmapToImageSource(bitmap);
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

        private void FirstCellField_OnMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            ScreenshotRegion screener = new ScreenshotRegion(this);
            if (screener.ShowDialog() == true)
            {
                _gamePreset.FirstCell = screener.Rectangle;
                UpdatePresetControls();
            }
        }

        private void LastCellField_OnMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            ScreenshotRegion screener = new ScreenshotRegion(this);
            if (screener.ShowDialog() == true)
            {
                _gamePreset.LastCell = screener.Rectangle;
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

        private void WinLengthFieldChanged(object sender, TextChangedEventArgs e)
        {
            int vinLength;
            if (int.TryParse(WinLengthField.Text, out vinLength))
            {
                _gamePreset.VinLength = vinLength;
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
            var image = (Bitmap)((Button)sender).Tag;
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
            var image = (Bitmap)((Button)sender).Tag;
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


        private void FreeCellSpriteSelectionCommand_Click(object sender, RoutedEventArgs e)
        {
            var image = (Bitmap)((Button)sender).Tag;
            ScreenshotRegion screener = new ScreenshotRegion(this);
            if (screener.ShowDialog() == true)
            {
                var itemIndex = _gamePreset.FreeCellSprites.IndexOf(image);
                _gamePreset.FreeCellSprites.RemoveAt(itemIndex);
                _gamePreset.FreeCellSprites.Insert(itemIndex, screener.Picture);
                _gamePreset.HasChanges = true;
                UpdatePresetControls();
            }
        }
        
        private void AddNewFreeCellSprite_Click(object sender, RoutedEventArgs e)
        {
            ScreenshotRegion screener = new ScreenshotRegion(this);
            if (screener.ShowDialog() == true)
            {
                _gamePreset.FreeCellSprites.Add(screener.Picture);
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
            var image = (Bitmap)((MenuItem)sender).Tag;
            _gamePreset.OCellSprites.Remove(image);
            _gamePreset.HasChanges = true;
            UpdatePresetControls();
        }

        private void RemoveXCell_Click(object sender, RoutedEventArgs e)
        {
            var image = (Bitmap)((MenuItem)sender).Tag;
            _gamePreset.XCellSprites.Remove(image);
            _gamePreset.HasChanges = true;
            UpdatePresetControls();
        }


        private void RemoveFreeCell_Click(object sender, RoutedEventArgs e)
        {
            var image = (Bitmap)((MenuItem)sender).Tag;
            _gamePreset.FreeCellSprites.Remove(image);
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


        private void SetOnTopFlag_Click(object sender, RoutedEventArgs e)
        {
            Topmost = SetOnTopFlag.IsChecked == true;
        }
    }
}
