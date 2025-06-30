using RPGMaker.Codebase.CoreSystem.Knowledge.DataModel.Event;
using RPGMaker.Codebase.Runtime.Common.Component.Hud.GameProgress;

namespace RPGMaker.Codebase.Runtime.Event.GameProgress
{
    /// <summary>
    /// [ゲーム進行]-[変数の操作]
    /// </summary>
    public class GameValProcessor : AbstractEventCommandProcessor
    {
        private GameVal _gameVal;

        protected override void Process(string eventID, EventDataModel.EventCommand command) {
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
#else
            ProcessAsync(eventID, command);
        }

        private async void ProcessAsync(string eventID, EventDataModel.EventCommand command) {
#endif
            if (_gameVal == null) _gameVal = new GameVal();

            _gameVal.Init();
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
            _gameVal.SetGameVal(command, eventID);
#else
            await _gameVal.SetGameVal(command, eventID);
#endif
            ProcessEndAction();
        }

        private void ProcessEndAction() {
            _gameVal = null;
            SendBackToLauncher.Invoke();
        }
    }
}