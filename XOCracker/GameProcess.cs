using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
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
        private const string ScreensDir = "Screenshots"; 
        private static readonly Random Rd = new Random();
        private readonly GamePreset _gamePreset;
        private readonly Window _appWindow;
        private IntPtr _winHandle;
        private Rectangle _winParams;
        private Task _monitoringProcess;
        private bool _isMonitoringRinning;
        private CellType _playerSide;

        private GameProcess()
        {
            _isMonitoringRinning = false;
            _monitoringProcess = new Task(Monitoring);
            _playerSide = CellType.XCell;
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
                if (_winHandle.ToInt32() == 0)
                {
                    _winHandle = new WindowInteropHelper(_appWindow).Handle;
                }
            }
        }

        public bool IsGameStarted { get; private set; }

        public CellType[,] Board { get; set; }

        private List<KeyValuePair<ImageContainer, CellType>> PossibleCells { get; set; } = new List<KeyValuePair<ImageContainer, CellType>>();

        public Queue<Cell> UpdatedCells { get; set; }

        public int Delay { get; set; }

        public int AnalysisAccuracy { get; set; }

        public int Dispersion { get; set; }

        public int MouseSpeed { get; set; }

        public string BoardInfo { get; set; }

        public int MaxTurnDelay { get; set; }

        public event Action OnGameStateUpdated;

        private void Update()
        {
            PossibleCells = _gamePreset.OCellSprites
                .Select(x => new KeyValuePair<ImageContainer, CellType>(new ImageContainer(x), CellType.OCell))
                .Union(_gamePreset.XCellSprites
                    .Select(x => new KeyValuePair<ImageContainer, CellType>(new ImageContainer(x), CellType.XCell)))
                .Union(_gamePreset.FreeCellSprites
                    .Select(x => new KeyValuePair<ImageContainer, CellType>(new ImageContainer(x), CellType.Free)))
                .ToList();
            TurnImage = new ImageContainer(_gamePreset.TurnSprite);
            StartImage = new ImageContainer(_gamePreset.StartSprite);
            OnGameStateUpdated?.Invoke();
        }

        public ImageContainer StartImage { get; set; }

        public ImageContainer TurnImage { get; set; }

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
            MouseSpeed = Settings.Default.MouseSpeed;
            MaxTurnDelay = Settings.Default.MaxTurnDelay;
        }

        public void Save()
        {
            Settings.Default.Delay = Delay;
            Settings.Default.Accuracy = AnalysisAccuracy;
            Settings.Default.Dispersion = Dispersion;
            Settings.Default.MouseSpeed = MouseSpeed;
            Settings.Default.MaxTurnDelay = MaxTurnDelay;
            Settings.Default.Save();
        }

        public void Stop()
        {
            IsGameStarted = false;
        }

        public void StopMonitoring()
        {
            _isMonitoringRinning = false;
            Stop();
        }

        private void Monitoring()
        {
            Update();
            var isBoardUpdated = false;
            while (_isMonitoringRinning)
            {
                var searchScreenObj = GetScreenShot();
                var turnPoint = searchScreenObj.Find(TurnImage, 1, 0);
                if (turnPoint != Point.Empty)
                {
                    isBoardUpdated = UpdateBoard(searchScreenObj);
                    if (IsGameStarted)
                    {
                        MakeTurn();
                    }
               } 
                else if(IsGameStarted)
                {
                    var startPoint = searchScreenObj.Find(StartImage, 1, 0);
                    if (startPoint != Point.Empty)
                    {

                        if (!Directory.Exists(ScreensDir))
                        {
                            Directory.CreateDirectory(ScreensDir);
                        }

                        var screen = Screen.FromHandle(_winHandle);
                        var screenShot = SearchHelper.CaptureScreen(SearchHelper.MagicShift, SearchHelper.MagicShift,
                            screen.Bounds.Width, screen.Bounds.Height, _winHandle);
                        var fileName = $"{DateTime.Now.ToString("yyyy.MM.dd HH.mm.ss")}.bmp";
                        fileName = Path.Combine(Environment.CurrentDirectory, ScreensDir, fileName);
                        screenShot.Save(fileName);
                        startPoint.X -= _gamePreset.StartSprite.Width / 2;
                        startPoint.Y -= _gamePreset.StartSprite.Height / 2;
                        Mouse.ClickIt(startPoint, MouseSpeed, Mouse.Buttons.Left);
                    }
                }

                if (isBoardUpdated)
                {
                    OnGameStateUpdated?.Invoke();
                }
                Thread.Sleep(Delay);
            }
        }

        private ImageContainer GetScreenShot()
        {
            var screen = Screen.FromHandle(_winHandle);
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
            //
            return new ImageContainer(screenShot);
        }

        private void MakeTurn()
        {
            double stepCost;
            var goodCell = BotLogic.GetStep(Board, _gamePreset.VinLength, _playerSide, out stepCost);
            if (goodCell != null)
            {
                if (stepCost >= 4 && stepCost < 100500)
                {
                    Thread.Sleep(Rd.Next(MaxTurnDelay));
                }

                if (IsGameStarted)
                {
                    ClickCell(goodCell.Value);
                    _playerSide = GetPlayerSide(goodCell.Value);
                    SetBoardCell(new Cell(_playerSide, goodCell.Value.Y, goodCell.Value.X));
                }
            }

            BoardInfo = $"Last step cost: {stepCost}";
        }

        private CellType GetPlayerSide(Point turnCell)
        {
            Thread.Sleep(500);
            var screen = GetScreenShot();
            var cellType = UpdateCellType(turnCell.Y, turnCell.X, screen).CellType;
            if (cellType != _playerSide)
            {
            }
            return cellType;
        }

        private void ClickCell(Point goodCell)
        {
            var screenPoint = CellPosToPoint(goodCell);
            Mouse.ClickIt(screenPoint, MouseSpeed, Mouse.Buttons.Left);
        }

        private Point CellPosToPoint(Point goodCell)
        {
            var firstCellPos = _gamePreset.FirstCell;
            var lastCellPos = _gamePreset.LastCell;
            var horCellDistance = (lastCellPos.X - firstCellPos.X) / (_gamePreset.Columns - 1);
            var verCellDistance = (lastCellPos.Y - firstCellPos.Y) / (_gamePreset.Rows - 1);
            var x = firstCellPos.X + horCellDistance * goodCell.X + horCellDistance / 2;
            var y = firstCellPos.Y + verCellDistance * goodCell.Y + verCellDistance / 2;
            return new Point(x, y);
        }

        private bool UpdateBoard(ImageContainer screen)
        {
            bool isBoardUpdated = false;
            if (Board.GetLength(0) != _gamePreset.Rows || Board.GetLength(1) != _gamePreset.Columns)
            {
                Board = new CellType[_gamePreset.Rows, _gamePreset.Columns];
            }

            for (int row = 0; row < _gamePreset.Rows; row++)
            {
                for (int column = 0; column < _gamePreset.Columns; column++)
                {
                    var cell = UpdateCellType(row, column, screen);
                    isBoardUpdated |= SetBoardCell(cell);
                }
            }

            return isBoardUpdated;
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

        private Cell UpdateCellType(int row, int column, ImageContainer screen)
        {
            var horCellDistance = (_gamePreset.LastCell.X - _gamePreset.FirstCell.X) / (_gamePreset.Columns - 1);
            var verCellDistance = (_gamePreset.LastCell.Y - _gamePreset.FirstCell.Y) / (_gamePreset.Rows - 1);
            var x = _gamePreset.FirstCell.X + horCellDistance * column;
            var y = _gamePreset.FirstCell.Y + verCellDistance * row;
            var width = _gamePreset.FirstCell.Width;
            var height = _gamePreset.FirstCell.Height;
            var cell = new Cell(CellType.Unknown, row, column, x, y, height, width);
            if (width == 0 || height == 0)
            {
                return cell;
            }

            var cellCenterX = cell.X + cell.Width / 2;
            var cellCenterY = cell.Y + cell.Height / 2;
            int? minDiff = null;
            foreach (var possibleCell in PossibleCells)
            {
                var image = possibleCell.Key;
                x = cellCenterX - image.Width / 2;
                y = cellCenterY - image.Height / 2;
                width = image.Width - 1;
                height = image.Height - 1;
                var diff = screen.Difference(ref image, x, y, x + width, y + height, new Size(width, height), AnalysisAccuracy);
                if (minDiff == null || minDiff.Value > diff)
                {
                    cell.X = x;
                    cell.Y = y;
                    cell.Width = possibleCell.Key.Width;
                    cell.Height = possibleCell.Key.Height;
                    cell.CellType = diff <= Dispersion ? possibleCell.Value : CellType.Unknown;
                    minDiff = diff;
                }
            }

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
            IsGameStarted = true;
            RunMonitoring();
            Update();
        }
    }
}
