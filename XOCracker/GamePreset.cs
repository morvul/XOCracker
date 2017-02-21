using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Windows;
using XOCracker.Properties;

namespace XOCracker
{
    public class GamePreset
    {
        private const string GamePresetDir = "GamePreset";
        private Bitmap _startSprite;
        private int _columns;
        private int _rows;
        private Bitmap _turnSprite;
        private Bitmap _freeCellSprite;
        private Bitmap _oCellSprite;
        private Bitmap _xCellSprite;
        private Bitmap _oCellSprite2;
        private Bitmap _xCellSprite2;
        private Bitmap _oCellSprite3;
        private Bitmap _xCellSprite3;

        public bool HasChanges { get; set; }

        public Bitmap StartSprite
        {
            get { return _startSprite; }
            set
            {
                if (_startSprite != value)
                {
                    _startSprite = value;
                    HasChanges = true;
                }
            }
        }

        public Bitmap TurnSprite
        {
            get { return _turnSprite; }
            set
            {
                if (_turnSprite != value)
                {
                    _turnSprite = value;
                    HasChanges = true;
                }
            }
        }

        public Bitmap FreeCellSprite
        {
            get { return _freeCellSprite; }
            set
            {
                if (_freeCellSprite != value)
                {
                    _freeCellSprite = value;
                    HasChanges = true;
                }
            }
        }


        public Bitmap OCellSprite
        {
            get { return _oCellSprite; }
            set
            {
                if (_oCellSprite != value)
                {
                    _oCellSprite = value;
                    HasChanges = true;
                }
            }
        }

        public Bitmap OCellSprite2
        {
            get { return _oCellSprite2; }
            set
            {
                if (_oCellSprite2 != value)
                {
                    _oCellSprite2 = value;
                    HasChanges = true;
                }
            }
        }

        public Bitmap OCellSprite3
        {
            get { return _oCellSprite3; }
            set
            {
                if (_oCellSprite3 != value)
                {
                    _oCellSprite3 = value;
                    HasChanges = true;
                }
            }
        }



        public Bitmap XCellSprite
        {
            get { return _xCellSprite; }
            set
            {
                if (_xCellSprite != value)
                {
                    _xCellSprite = value;
                    HasChanges = true;
                }
            }
        }

        public Bitmap XCellSprite2
        {
            get { return _xCellSprite2; }
            set
            {
                if (_xCellSprite2 != value)
                {
                    _xCellSprite2 = value;
                    HasChanges = true;
                }
            }
        }

        public Bitmap XCellSprite3
        {
            get { return _xCellSprite3; }
            set
            {
                if (_xCellSprite3 != value)
                {
                    _xCellSprite3 = value;
                    HasChanges = true;
                }
            }
        }


        public int Columns
        {
            get { return _columns; }
            set
            {
                if (_columns != value)
                {
                    _columns = value;
                    HasChanges = true;
                }
            }
        }

        public int Rows
        {
            get { return _rows; }
            set
            {
                if (_rows != value)
                {
                    _rows = value;
                    HasChanges = true;
                }
            }
        }

        public bool IsReady()
        {
            return StartSprite != null && TurnSprite != null && FreeCellSprite != null
                && (OCellSprite != null || OCellSprite2 != null || OCellSprite3 != null)
                && (XCellSprite != null || XCellSprite2 != null || XCellSprite3 != null) 
                && Rows > 0 && Columns > 0;
        }

        public void Reset()
        {
            StartSprite = null;
            TurnSprite = null;
            FreeCellSprite = null;
            OCellSprite = null;
            OCellSprite2 = null;
            OCellSprite3 = null;
            XCellSprite = null;
            XCellSprite2 = null;
            XCellSprite3 = null;
            Rows = 0;
            Columns = 0;
        }

        public void Reload()
        {
            Rows = Settings.Default.Rows;
            Columns = Settings.Default.Columns;
            try
            {
                var presetDir = Path.Combine(Environment.CurrentDirectory, GamePresetDir);
                if (Directory.Exists(presetDir))
                {
                    StartSprite = FromFile(nameof(StartSprite));
                    TurnSprite = FromFile(nameof(TurnSprite));
                    FreeCellSprite = FromFile(nameof(FreeCellSprite));
                    OCellSprite = FromFile(nameof(OCellSprite));
                    OCellSprite2 = FromFile(nameof(OCellSprite2));
                    OCellSprite3 = FromFile(nameof(OCellSprite3));
                    XCellSprite = FromFile(nameof(XCellSprite));
                    XCellSprite2 = FromFile(nameof(XCellSprite2));
                    XCellSprite3 = FromFile(nameof(XCellSprite3));
                }
            }
            catch (Exception excpt)
            {
                MessageBox.Show(excpt.Message, "Data initialization error", MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }

            HasChanges = false;
        }

        public static GamePreset Initialize()
        {
            var gamePreset = new GamePreset();
            gamePreset.Reload();
            return gamePreset;
        }

        public void Save()
        {
            if (!HasChanges)
            {
                return;
            }

            Settings.Default.Rows = Rows;
            Settings.Default.Columns = Columns;
            Settings.Default.Save();
            var presetDir = Path.Combine(Environment.CurrentDirectory, GamePresetDir);
            if (!Directory.Exists(presetDir))
            {
                Directory.CreateDirectory(presetDir);
            }
            else
            {
                var startSpriteFileName = Path.Combine(presetDir, nameof(StartSprite) + ".bmp");
                StartSprite?.Save(startSpriteFileName, ImageFormat.Bmp);
                var turnSpriteFileName = Path.Combine(presetDir, nameof(TurnSprite) + ".bmp");
                TurnSprite?.Save(turnSpriteFileName);
                var freeCellSpriteFileName = Path.Combine(presetDir, nameof(FreeCellSprite) + ".bmp");
                FreeCellSprite?.Save(freeCellSpriteFileName);
                var oCellSpriteFileName = Path.Combine(presetDir, nameof(OCellSprite) + ".bmp");
                OCellSprite?.Save(oCellSpriteFileName);
                var oCellSprite2FileName = Path.Combine(presetDir, nameof(OCellSprite2) + ".bmp");
                OCellSprite2?.Save(oCellSprite2FileName);
                var oCellSprite3FileName = Path.Combine(presetDir, nameof(OCellSprite3) + ".bmp");
                OCellSprite3?.Save(oCellSprite3FileName);
                var xCellSpriteFileName = Path.Combine(presetDir, nameof(XCellSprite) + ".bmp");
                XCellSprite?.Save(xCellSpriteFileName);
                var xCellSprite2FileName = Path.Combine(presetDir, nameof(XCellSprite2) + ".bmp");
                XCellSprite2?.Save(xCellSprite2FileName);
                var xCellSprite3FileName = Path.Combine(presetDir, nameof(XCellSprite3) + ".bmp");
                XCellSprite3?.Save(xCellSprite3FileName);
            }

            HasChanges = false;

        }

        private Bitmap FromFile(string imgName)
        {
            var fileName = Path.Combine(GamePresetDir, imgName + ".bmp");
            if (File.Exists(fileName))
            {
                var bytes = File.ReadAllBytes(fileName);
                var ms = new MemoryStream(bytes);
                var img = Image.FromStream(ms);
                return (Bitmap)img;
            }

            return null;
        }
    }
}
