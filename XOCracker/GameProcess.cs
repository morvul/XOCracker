using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Interop;
using XOCracker.Enums;
using XOCracker.Models;
using XOCracker.Properties;
using Size = System.Drawing.Size;

namespace XOCracker
{
    public class GameProcess
    {
        private readonly GamePreset _gamePreset;
        private readonly Window _appWindow;
        private IntPtr _winHandle;
        private Rectangle _winParams;
        private Task _monitoringProcess;
        private bool _isMonitoringRinning;

        private GameProcess()
        {
            _isMonitoringRinning = false;
            _monitoringProcess = new Task(Monitoring);
            UpdatedCells = new Queue<Cell>();
        }

        private GameProcess(GamePreset gamePreset, Window appWindow)
            : this()
        {
            _gamePreset = gamePreset;
            _appWindow = appWindow;
            _appWindow.LocationChanged += WindowChanged;
            _appWindow.SizeChanged += WindowChanged;
            Board = new CellType[_gamePreset.Rows, _gamePreset.Columns];
        }

        private void WindowChanged(object sender, EventArgs e)
        {
            lock (this)
            {
                _winParams = new Rectangle((int)_appWindow.Left, (int)_appWindow.Top,
                 (int)_appWindow.Width, (int)_appWindow.Height);
            }
        }


        public CellType[,] Board { get; set; }

        private List<KeyValuePair<ImageContainer, CellType>> PossibleCells => _gamePreset.OCellSprites
                    .Select(x => new KeyValuePair<ImageContainer, CellType>(new ImageContainer(x), CellType.OCell))
                .Union(_gamePreset.XCellSprites
                    .Select(x => new KeyValuePair<ImageContainer, CellType>(new ImageContainer(x), CellType.XCell)))
                .Union(new[] { new KeyValuePair<ImageContainer, CellType>(new ImageContainer(_gamePreset.FreeCellSprite), CellType.Free) })
                .ToList();

        public Queue<Cell> UpdatedCells { get; set; }

        public int Delay { get; set; }

        public int AnalysisAccuracy { get; set; }

        public int Dispersion { get; set; }

        public event Action OnGameStateUpdated;

        private void Update()
        {
            OnGameStateUpdated?.Invoke();
        }

        internal static GameProcess Initialize(GamePreset gamePreset, Window appWindow)
        {
            var gameProcess = new GameProcess(gamePreset, appWindow);
            gameProcess.Load();
            return gameProcess;
        }

        private void Load()
        {
            Delay = Settings.Default.Delay;
            AnalysisAccuracy = Settings.Default.Accuracy;
            Dispersion = Settings.Default.Dispersion;
        }

        public void Save()
        {
            Settings.Default.Delay = Delay;
            Settings.Default.Accuracy = AnalysisAccuracy;
            Settings.Default.Dispersion = Dispersion;
            Settings.Default.Save();
        }

        public void Stop()
        {
        }

        public void StopMonitoring()
        {
            _isMonitoringRinning = false;
            Stop();
        }

        private void Monitoring()
        {
            var screen = Screen.FromHandle(_winHandle);
            while (_isMonitoringRinning)
            {
                var screenShot = SearchHelper.CaptureScreen(SearchHelper.MagicShift, SearchHelper.MagicShift,
                    screen.Bounds.Width, screen.Bounds.Height, _winHandle);

                #region Hide Application window on the screenshot

                Rectangle winParams;
                lock (this)
                {
                    winParams = _winParams;
                }

                using (var graphics = Graphics.FromImage(screenShot))
                {
                    graphics.FillRectangle(Brushes.Black, winParams.X, winParams.Y, winParams.Width,
                        winParams.Height);
                }

                #endregion

                var searchScreenObj = new ImageContainer(screenShot);
                UpdateBoard(searchScreenObj);

                Thread.Sleep(Delay);
            }
        }

        private void UpdateBoard(ImageContainer screen)
        {
            bool isBoardUpdated = false;
            var firstCellPos = _gamePreset.FirstCell;
            var lastCellPos = _gamePreset.LastCell;
            var horCellDistance = (lastCellPos.X - firstCellPos.X) / (_gamePreset.Columns - 2);
            var verCellDistance = (lastCellPos.Y - firstCellPos.Y) / (_gamePreset.Rows - 2);
            if (Board.GetLength(0) != _gamePreset.Rows || Board.GetLength(1) != _gamePreset.Columns)
            {
                Board = new CellType[_gamePreset.Rows, _gamePreset.Columns];
            }

            for (int row = 0; row < _gamePreset.Rows; row++)
            {
                for (int column = 0; column < _gamePreset.Columns; column++)
                {
                    var cellType = Board[row, column];
                    var x = firstCellPos.X + horCellDistance * column;
                    var y = firstCellPos.Y + verCellDistance * row;

                    var cell = new Cell(cellType, row, column, x, y, _gamePreset.FirstCell.Height, _gamePreset.FirstCell.Width);
                    cell = UpdateCellType(cell, screen);
                    isBoardUpdated |= SetBoardCell(cell);
                }
            }

            if (isBoardUpdated)
            {
                OnGameStateUpdated?.Invoke();
            }
        }

        private bool SetBoardCell(Cell cell)
        {
            if (Board[cell.Row, cell.Column] != cell.CellType)
            {
                Board[cell.Row, cell.Column] = cell.CellType;
                UpdatedCells.Enqueue(cell);
                return true;
            }

            return false;
        }

        private Cell UpdateCellType(Cell cell, ImageContainer screen)
        {
            if (cell.Width == 0 || cell.Height == 0)
            {
                cell.CellType = CellType.Unknown;
                return cell;
            }

            var cellCenterX = cell.X + cell.Width / 2;
            var cellCenterY = cell.Y + cell.Height / 2;
            int? minDiff = null;
            foreach (var possibleCell in PossibleCells)
            {
                var x = cellCenterX - possibleCell.Key.Width / 2;
                var y = cellCenterY - possibleCell.Key.Height / 2;
                var image = possibleCell.Key;
                var diff = screen.Difference(ref image, x, y, x + image.Width, y + image.Height,
                    new Size(image.Width, image.Height), AnalysisAccuracy);
                if (minDiff == null || minDiff.Value > diff)
                {
                    cell.X = x;
                    cell.Y = y;
                    cell.Width = possibleCell.Key.Width;
                    cell.Height = possibleCell.Key.Height;
                    cell.CellType = diff < Dispersion ? possibleCell.Value : CellType.Unknown;
                    minDiff = diff;
                }
            }

            var r = screen.Find(new ImageContainer(_gamePreset.FreeCellSprite), 1, 1);
            return cell;
        }

        public void RunMonitoring()
        {
            _isMonitoringRinning = true;
            _winHandle = new WindowInteropHelper(_appWindow).Handle;
            _winParams = new Rectangle((int)_appWindow.Left, (int)_appWindow.Top,
                (int)_appWindow.ActualWidth, (int)_appWindow.ActualHeight);
            Board = new CellType[_gamePreset.Rows, _gamePreset.Columns];
            if (_monitoringProcess.Status != TaskStatus.Running)
            {
                _monitoringProcess = Task.Factory.StartNew(Monitoring);
            }
        }

        public void Start()
        {
            RunMonitoring();
            Update();
        }
    }
}
