using RPGMaker.Codebase.CoreSystem.Knowledge.DataModel.Event;
using RPGMaker.Codebase.Runtime.Common;
using RPGMaker.Codebase.Runtime.Map;

namespace RPGMaker.Codebase.Runtime.Event.Screen
{
    /// <summary>
    /// [シーン制御]-[メニュー画面を開く]
    /// </summary>
    public class OpenMenuWindowProcessor : AbstractEventCommandProcessor
    {
        protected override void Process(string eventID, EventDataModel.EventCommand command) {
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
#else
            ProcessAsync(eventID, command);
        }

        private async void ProcessAsync(string eventID, EventDataModel.EventCommand command) {
#endif
            //一時的にイベントを中断し、メニューへ遷移する
            MapEventExecutionController.Instance.PauseEvent(ProcessEndAction);
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
            MapManager.menu.MenuOpen(true);
#else
            await MapManager.menu.MenuOpenAsync(true);
#endif
        }

        private void ProcessEndAction() {
            //キーイベント破棄のため、若干待つ
            TimeHandler.Instance.AddTimeActionFrame(1, ProcessEndActionWait, false);
        }

        private void ProcessEndActionWait() {
            SendBackToLauncher.Invoke();
        }
    }
}