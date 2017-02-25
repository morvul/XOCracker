using System;

namespace XOCracker
{
    public class GameProcess
    {
        private GamePreset _gamePreset;

        private GameProcess(){ }

        private GameProcess(GamePreset gamePreset)
        {
            _gamePreset = gamePreset;
            Update();
        }

        public event Action OnGameStateUpdated;

        public void Update()
        {
            OnGameStateUpdated?.Invoke();
        }

        internal static GameProcess Initialize(GamePreset gamePreset)
        {
            var gameProcess = new GameProcess(gamePreset);
            return gameProcess;
        }

        public void Stop()
        {
        }

        public void Start()
        {
            Update();
        }
    }
}
