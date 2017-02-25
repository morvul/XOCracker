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
using XOCracker.Properties;
using Point = System.Drawing.Point;

namespace XOCracker
{
    public class GameProcess
    {
        private readonly GamePreset _gamePreset;
        private readonly Window _appWindow;
        private CancellationTokenSource _monitoringCancellation;
        private IntPtr _winHandle;
        private Rectangle _winParams;

        private GameProcess() { }

        private GameProcess(GamePreset gamePreset, Window appWindow)
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

        public Queue<CellType> UpdatedCells { get; set; }

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
            _monitoringCancellation?.Cancel();
            Stop();
        }

        private void Monitoring()
        {
            var screen = Screen.FromHandle(_winHandle);
            var possibleCells = _gamePreset.OCellSprites
                    .Select(x => new KeyValuePair<ImageContainer, CellType>(new ImageContainer(x), CellType.OCell))
                .Union(_gamePreset.XCellSprites
                    .Select(x => new KeyValuePair<ImageContainer, CellType>(new ImageContainer(x), CellType.XCell)))
                .Union(new[] { new KeyValuePair<ImageContainer, CellType>(new ImageContainer(_gamePreset.FreeCellSprite), CellType.Free) })
                .ToList();
            while (!_monitoringCancellation.IsCancellationRequested)
            {
                var screenShot = SearchHelper.CaptureScreen(SearchHelper.MagicShift, SearchHelper.MagicShift,
                    screen.Bounds.Width, screen.Bounds.Height, _winHandle);
                Rectangle winParams;
                lock (this)
                {
                    winParams = _winParams;
                }

                using (var graphics = Graphics.FromImage(screenShot))
                {
                    graphics.FillRectangle(Brushes.Black, winParams.X, winParams.Y, winParams.Width, winParams.Height);
                }

                var searchScreenObj = new ImageContainer(screenShot);
                var firstCell = GetFirstCell(searchScreenObj, possibleCells);
                Thread.Sleep(Delay);
            }
        }

        private KeyValuePair<Point, CellType> GetFirstCell(ImageContainer screen, List<KeyValuePair<ImageContainer, CellType>> possibleCells)
        {
            Point firstCellPoint = new Point(screen.GetWidth(), screen.GetHeight());
            CellType firstCellType = CellType.Unknown;
            
            foreach (var possibleCell in possibleCells)
            {
                var cellPoint = screen.Find(possibleCell.Key, AnalysisAccuracy, Dispersion);
                if (cellPoint == Point.Empty)
                {
                    continue;
                }

                if (firstCellPoint.Y > cellPoint.Y || firstCellPoint.Y == cellPoint.Y && firstCellPoint.X > cellPoint.X)
                {
                    firstCellPoint = cellPoint;
                    firstCellType = possibleCell.Value;
                }
            }

            return new KeyValuePair<Point, CellType>(firstCellPoint, firstCellType);
        }

        public void RunMonitoring()
        {
            _monitoringCancellation?.Dispose();
            _monitoringCancellation = new CancellationTokenSource();
            _winHandle = new WindowInteropHelper(_appWindow).Handle;
            _winParams = new Rectangle((int)_appWindow.Left, (int)_appWindow.Top,
                 (int)_appWindow.ActualWidth, (int)_appWindow.ActualHeight);
            Task.Factory.StartNew(Monitoring, _monitoringCancellation.Token);
        }

        public void Start()
        {
            RunMonitoring();
            Update();
        }
    }
}
