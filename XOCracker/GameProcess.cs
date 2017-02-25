namespace XOCracker
{
    public class GameProcess
    {
        private GamePreset _gamePreset;

        private GameProcess(){ }

        private GameProcess(GamePreset gamePreset)
        {
            _gamePreset = gamePreset;
        }

        private void Reload()
        {
            throw new System.NotImplementedException();
        }

        internal static GameProcess Initialize(GamePreset gamePreset)
        {
            var gameProcess = new GameProcess(gamePreset);
            gameProcess.Reload();
            return gameProcess;
        }
    }
}
