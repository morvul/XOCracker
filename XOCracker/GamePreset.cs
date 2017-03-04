using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Windows;
using XOCracker.Enums;
using XOCracker.Properties;

namespace XOCracker
{
    public class GamePreset
    {
        private const string GamePresetDir = "GamePreset";
        private const string ImgExt = ".bmp";
        private Bitmap _startSprite;
        private int _columns;
        private int _rows;
        private Bitmap _turnSprite;
        private List<Bitmap> _oCellSprites;
        private List<Bitmap> _xCellSprites;
        private List<Bitmap> _freeCellSprites;
        private Rectangle _firstCell;
        private Rectangle _lastCell;
        private int _vinLength;

        private GamePreset()
        {
            FreeCellSprites = new List<Bitmap>();
            OCellSprites = new List<Bitmap>();
            XCellSprites = new List<Bitmap>();
        }

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

        public List<Bitmap> FreeCellSprites
        {
            get { return _freeCellSprites; }
            set
            {
                if (_freeCellSprites != value)
                {
                    _freeCellSprites = value;
                    HasChanges = true;
                }
            }
        }


        public List<Bitmap> XCellSprites
        {
            get { return _xCellSprites; }
            set
            {
                if (_xCellSprites != value)
                {
                    _xCellSprites = value;
                    HasChanges = true;
                }
            }
        }

        public List<Bitmap> OCellSprites
        {
            get { return _oCellSprites; }
            set
            {
                if (_oCellSprites != value)
                {
                    _oCellSprites = value;
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

        public string DirectoryPath => GamePresetDir;

        public Rectangle FirstCell
        {
            get { return _firstCell; }
            set
            {
                if (_firstCell != value)
                {
                    _firstCell = value;
                    HasChanges = true;
                }
            }
        }

        public Rectangle LastCell
        {
            get { return _lastCell; }
            set
            {
                if (_lastCell != value)
                {
                    _lastCell = value;
                    HasChanges = true;
                }
            }
        }

        public int VinLength
        {
            get { return _vinLength; }
            set
            {
                if (_vinLength != value)
                {
                    _vinLength = value;
                    HasChanges = true;
                }
            }
        }

        public bool IsReady()
        {
            return StartSprite != null && TurnSprite != null 
                && FreeCellSprites.Count > 0 && OCellSprites.Count > 0 && XCellSprites.Count > 0
                && Rows > 0 && Columns > 0 && FirstCell != Rectangle.Empty && LastCell != Rectangle.Empty
                && VinLength > 0;
        }

        public void Reset()
        {
            StartSprite = null;
            TurnSprite = null;
            Rows = 0;
            Columns = 0;
            LastCell = Rectangle.Empty;
            FirstCell = Rectangle.Empty;
            VinLength = 0;
            if (FreeCellSprites.Count > 0)
            {
                FreeCellSprites.Clear();
                HasChanges = true;
            }

            if (OCellSprites.Count > 0)
            {
                OCellSprites.Clear();
                HasChanges = true;
            }

            if (XCellSprites.Count > 0)
            {
                XCellSprites.Clear();
                HasChanges = true;
            }
        }

        public void Reload()
        {
            Rows = Settings.Default.Rows;
            FirstCell = Settings.Default.FirstCell;
            LastCell = Settings.Default.LastCell;
            Columns = Settings.Default.Columns;
            VinLength = Settings.Default.VinLength;
            try
            {
                var presetDir = Path.Combine(Environment.CurrentDirectory, GamePresetDir);
                if (Directory.Exists(presetDir))
                {
                    StartSprite = FromFile(nameof(StartSprite), presetDir);
                    TurnSprite = FromFile(nameof(TurnSprite), presetDir);
                    var freeCellsDir = Path.Combine(presetDir, nameof(FreeCellSprites));
                    if (Directory.Exists(freeCellsDir))
                    {
                        FreeCellSprites.Clear();
                        var files = Directory.GetFiles(freeCellsDir);
                        foreach (var file in files)
                        {
                            var freeCellSprite = FromFile(file);
                            if (freeCellSprite != null)
                            {
                                FreeCellSprites.Add(freeCellSprite);
                            }
                        }
                    }

                    var oCellsDir = Path.Combine(presetDir, nameof(OCellSprites));
                    if (Directory.Exists(oCellsDir))
                    {
                        OCellSprites.Clear();
                        var files = Directory.GetFiles(oCellsDir);
                        foreach (var file in files)
                        {
                            var oCellSprite = FromFile(file);
                            if (oCellSprite != null)
                            {
                                OCellSprites.Add(oCellSprite);
                            }
                        }
                    }

                    var xCellsDir = Path.Combine(presetDir, nameof(XCellSprites));
                    if (Directory.Exists(xCellsDir))
                    {
                        XCellSprites.Clear();
                        var files = Directory.GetFiles(xCellsDir);
                        foreach (var file in files)
                        {
                            var xCellSprite = FromFile(file);
                            if (xCellSprite != null)
                            {
                                XCellSprites.Add(xCellSprite);
                            }
                        }
                    }
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
            Settings.Default.LastCell = LastCell;
            Settings.Default.FirstCell = FirstCell;
            Settings.Default.VinLength = VinLength;
            Settings.Default.Save();

            var presetDir = Path.Combine(Environment.CurrentDirectory, GamePresetDir);
            if (!Directory.Exists(presetDir))
            {
                Directory.CreateDirectory(presetDir);
            }

            var startSpriteFileName = Path.Combine(presetDir, nameof(StartSprite) + ImgExt);
            StartSprite?.Save(startSpriteFileName, ImageFormat.Bmp);
            var turnSpriteFileName = Path.Combine(presetDir, nameof(TurnSprite) + ImgExt);
            TurnSprite?.Save(turnSpriteFileName);

            var freeCellsDir = Path.Combine(presetDir, nameof(FreeCellSprites));
            if (!Directory.Exists(freeCellsDir))
            {
                Directory.CreateDirectory(freeCellsDir);
            }
            else
            {
                DeleteSpriteFiles(freeCellsDir);
            }

            var index = 0;
            foreach (var freeCellSprite in FreeCellSprites)
            {
                var oCellSpriteFileName = Path.Combine(freeCellsDir, nameof(freeCellSprite) + ++index + ImgExt);
                freeCellSprite.Save(oCellSpriteFileName);
            }

            var oCellsDir = Path.Combine(presetDir, nameof(OCellSprites));
            if (!Directory.Exists(oCellsDir))
            {
                Directory.CreateDirectory(oCellsDir);
            }
            else
            {
                DeleteSpriteFiles(oCellsDir);
            }

            index = 0;
            foreach (var oCellSprite in OCellSprites)
            {
                var oCellSpriteFileName = Path.Combine(oCellsDir, nameof(oCellSprite) + ++index + ImgExt);
                oCellSprite.Save(oCellSpriteFileName);
            }

            var xCellsDir = Path.Combine(presetDir, nameof(XCellSprites));
            if (!Directory.Exists(xCellsDir))
            {
                Directory.CreateDirectory(xCellsDir);
            }
            else
            {
                DeleteSpriteFiles(xCellsDir);
            }

            index = 0;
            foreach (var xCellSprite in XCellSprites)
            {
                var xCellSpriteFileName = Path.Combine(xCellsDir, nameof(xCellSprite) + ++index + ImgExt);
                xCellSprite.Save(xCellSpriteFileName);
            }

            HasChanges = false;

        }

        private void DeleteSpriteFiles(string dir)
        {
            var files = Directory.GetFiles(dir);
            foreach (var file in files)
            {
                var xCellSprite = FromFile(file);
                if (xCellSprite != null)
                {
                    File.Delete(file);
                }
            }
        }

        private Bitmap FromFile(string imgName, string dirName = null)
        {
            var fileName = dirName == null ? imgName : Path.Combine(dirName, imgName + ImgExt);
            if (fileName.EndsWith(ImgExt) && File.Exists(fileName))
            {
                var bytes = File.ReadAllBytes(fileName);
                var ms = new MemoryStream(bytes);
                var img = (Bitmap)Image.FromStream(ms);
                return img;
            }

            return null;
        }
    }
}
