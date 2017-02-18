using System;
using System.Drawing;
using System.IO;
using System.Windows;

namespace XOCracker
{
    public class GamePreset
    {
        private Bitmap _startSprite;
        private const string GamePresetDir = "GamePreset";

        public Bitmap StartSprite
        {
            get { return _startSprite; }
            set
            {
                _startSprite = value;
                if (_startSprite != value)
                {
                    
                }
            }
        }

        public static GamePreset Initialize()
        {
            var presetDir = Path.Combine(Environment.CurrentDirectory, GamePresetDir);
            var gamePreset = new GamePreset();
            try
            {
                if (!Directory.Exists(presetDir))
                {
                    Directory.CreateDirectory(presetDir);
                }
                else
                {
                    var startSpriteFileName = Path.Combine(GamePresetDir, nameof(StartSprite) + ".bmp");

                    if (File.Exists(startSpriteFileName))
                    {
                        gamePreset.StartSprite = new Bitmap(startSpriteFileName);
                    }
                }
            }
            catch (Exception excpt)
            {
                MessageBox.Show(excpt.Message, "Data initialization error", MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }

            return gamePreset;
        }

        public bool IsReady()
        {
            return StartSprite != null;
        }
    }
}
