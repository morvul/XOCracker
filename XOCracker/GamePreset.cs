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
                && Rows > 0 && Columns > 0;
        }

        public void Reset()
        {
            StartSprite = null;
            TurnSprite = null;
            FreeCellSprite = null;
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
