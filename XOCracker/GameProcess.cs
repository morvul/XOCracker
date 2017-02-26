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
using Point = System.Drawing.Point;
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
        private int _cellShift;

        private GameProcess()
        {
            _cellShift = 3;
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
                    .Select(x => new KeyValuePair<ImageContainer, CellType>(new ImageContainer(x, _cellShift), CellType.OCell))
                .Union(_gamePreset.XCellSprites
                    .Select(x => new KeyValuePair<ImageContainer, CellType>(new ImageContainer(x, _cellShift), CellType.XCell)))
                .Union(new[] { new KeyValuePair<ImageContainer, CellType>(new ImageContainer(_gamePreset.FreeCellSprite, _cellShift), CellType.Free) })
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
            var isBoardUpdated = false;
            var screen = Screen.FromHandle(_winHandle);
            Cell firstCell = new Cell(CellType.Unknown, 0, 0);
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
                if (firstCell.CellType != CellType.Unknown)
                {
                    firstCell = UpdateCellType(firstCell, searchScreenObj);
                }

                if (firstCell.CellType == CellType.Unknown)
                {
                    firstCell = GetFirstCell(searchScreenObj);
                }

                var boardCells = new List<Cell> { firstCell };
                if (firstCell.CellType != CellType.Unknown)
                {
                    boardCells.AddRange(GetBoardCells(firstCell, searchScreenObj));
                }

                using (var graphics = Graphics.FromImage(screenShot))
                {
                    foreach (var boardCell in boardCells)
                    {
                        isBoardUpdated |= SetBoardCell(boardCell);
                        graphics.FillRectangle(Brushes.Crimson, boardCell.X, boardCell.Y,
                        boardCell.Width, boardCell.Height);
                    }

                    screenShot.Save("ss.bmp");
                }

                if (isBoardUpdated)
                {
                    isBoardUpdated = false;
                    OnGameStateUpdated?.Invoke();
                }

                Thread.Sleep(Delay);
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
                var x = cellCenterX - possibleCell.Key.GetWidth() / 2;
                var y = cellCenterY - possibleCell.Key.GetHeight() / 2;
                var image = possibleCell.Key;
                var diff = screen.Difference(ref image, x, y, x + possibleCell.Key.GetWidth(), y + possibleCell.Key.GetHeight(),
                    new Size(possibleCell.Key.GetWidth(), possibleCell.Key.GetHeight()), AnalysisAccuracy);
                if (minDiff == null || minDiff.Value > diff)
                {
                    cell.X = x;
                    cell.Y = y;
                    cell.Width = possibleCell.Key.GetWidth();
                    cell.Height = possibleCell.Key.GetHeight();
                    cell.CellType = diff < Dispersion ? possibleCell.Value : CellType.Unknown;
                    minDiff = diff;
                }
            }

            return cell;
        }

        private Cell GetFirstCell(ImageContainer screen)
        {
            Rectangle firstCell = new Rectangle(screen.GetWidth(), screen.GetHeight(), 0, 0);
            CellType firstCellType = CellType.Unknown;
            foreach (var possibleCell in PossibleCells)
            {
                var cellPoint = screen.Find(possibleCell.Key, AnalysisAccuracy, Dispersion, _cellShift);
                if (cellPoint == Point.Empty)
                {
                    continue;
                }

                if (firstCell.Y > cellPoint.Y || (firstCell.Y == cellPoint.Y && firstCell.X > cellPoint.X))
                {
                    firstCell.X = cellPoint.X;
                    firstCell.Y = cellPoint.Y;
                    firstCell.Width = possibleCell.Key.GetWidth();
                    firstCell.Height = possibleCell.Key.GetHeight();
                    firstCellType = possibleCell.Value;
                }
            }

            return new Cell(firstCellType, 0, 0, firstCell);
        }


        private List<Cell> GetBoardCells(Cell firstCell, ImageContainer screen)
        {
            var result = new List<Cell>();
            var prewCell = firstCell;
            for (int row = 0; row < _gamePreset.Rows; row++)
            {
                for (int column = 0; column < _gamePreset.Columns; column++)
                {
                    if (column == 0 && row == 0)
                    {
                        continue;
                    }

                    Cell nextCell = new Cell();

                    if (column > 0)
                    {
                        var cellCenterX = prewCell.X + prewCell.Width / 2 + _cellShift;
                        var cellCenterY = prewCell.Y + prewCell.Height / 2;
                        int? minDiff = null;

                        for (int x = cellCenterX; x < cellCenterX + prewCell.Width + _cellShift; x++)
                        {
                            foreach (var possibleCell in PossibleCells)
                            {
                                var y = cellCenterY - possibleCell.Key.GetHeight() / 2;
                                var image = possibleCell.Key;
                                var diff = screen.Difference(ref image, x, y, x + possibleCell.Key.GetWidth(),
                                    y + possibleCell.Key.GetHeight(),
                                    new Size(possibleCell.Key.GetWidth(), possibleCell.Key.GetHeight()),
                                    AnalysisAccuracy);
                                if (minDiff == null || minDiff.Value >= diff)
                                {
                                    nextCell.X = x;
                                    nextCell.Y = y;
                                    nextCell.Width = possibleCell.Key.GetWidth();
                                    nextCell.Height = possibleCell.Key.GetHeight();
                                    nextCell.CellType = diff <= Dispersion ? possibleCell.Value : CellType.Unknown;
                                    minDiff = diff;
                                }
                            }
                        }
                    }
                    else
                    {
                        var cellCenterX = firstCell.X + firstCell.Width / 2;
                        var cellCenterY = firstCell.Y + firstCell.Height / 2;
                        int? minDiff = null;

                        for (int y = cellCenterY; y < cellCenterY + firstCell.Height + _cellShift; y++)
                        {
                            foreach (var possibleCell in PossibleCells)
                            {
                                var x = cellCenterX - possibleCell.Key.GetWidth() / 2;
                                var image = possibleCell.Key;
                                var diff = screen.Difference(ref image, x, y, x + possibleCell.Key.GetWidth(),
                                    y + possibleCell.Key.GetHeight(),
                                    new Size(possibleCell.Key.GetWidth(), possibleCell.Key.GetHeight()),
                                    AnalysisAccuracy);
                                if (minDiff == null || minDiff.Value >= diff)
                                {
                                    nextCell.X = x;
                                    nextCell.Y = y;
                                    nextCell.Width = possibleCell.Key.GetWidth();
                                    nextCell.Height = possibleCell.Key.GetHeight();
                                    nextCell.CellType = diff <= Dispersion ? possibleCell.Value : CellType.Unknown;
                                    minDiff = diff;
                                }
                            }
                        }

                        firstCell = nextCell;
                    }

                    nextCell.Row = row;
                    nextCell.Column = column;
                    prewCell = nextCell;
                    result.Add(nextCell);
                }
            }

            return result;
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
