using RPGMaker.Codebase.CoreSystem.Knowledge.DataModel.Event;
using RPGMaker.Codebase.Runtime.Common;
using RPGMaker.Codebase.Runtime.Common.Component.Hud.GameProgress;

namespace RPGMaker.Codebase.Runtime.Event.GameProgress
{
    /// <summary>
    /// [ゲーム進行]-[タイマーの操作]
    /// </summary>
    public class GameTimerProcessor : AbstractEventCommandProcessor
    {
        protected override void Process(string eventID, EventDataModel.EventCommand command) {
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
#else
            ProcessAsync(eventID, command);
        }

        private async void ProcessAsync(string eventID, EventDataModel.EventCommand command) {
#endif
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
            GameTimer gameTimer = HudDistributor.Instance.NowHudHandler().CreateGameTimer();
#else
            GameTimer gameTimer = await HudDistributor.Instance.NowHudHandler().CreateGameTimer();
#endif
            gameTimer.SetGameTimer(command.parameters[0] == "1" ? true : false, int.Parse(command.parameters[1]));
            ProcessEndAction();
        }

        private void ProcessEndAction() {
            SendBackToLauncher.Invoke();
        }
    }
}