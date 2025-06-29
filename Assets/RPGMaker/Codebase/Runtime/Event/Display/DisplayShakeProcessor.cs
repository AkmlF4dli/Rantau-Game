using RPGMaker.Codebase.CoreSystem.Knowledge.DataModel.Event;
using RPGMaker.Codebase.Runtime.Common;

namespace RPGMaker.Codebase.Runtime.Event.Display
{
    /// <summary>
    /// [画面]-[画面のシェイク]
    /// </summary>
    public class DisplayShakeProcessor : AbstractEventCommandProcessor
    {
        protected override void Process(string eventID, EventDataModel.EventCommand command) {
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
#else
            ProcessAsync(eventID, command);
        }

        private async void ProcessAsync(string eventID, EventDataModel.EventCommand command) {
#endif
            var type = int.Parse(command.parameters[0]);
            var value = int.Parse(command.parameters[1]);
            var flame = int.Parse(command.parameters[2]);
            var wait = command.parameters[3] == "0" ? false : true;
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
            HudDistributor.Instance.NowHudHandler().DisplayInit();
#else
            await HudDistributor.Instance.NowHudHandler().DisplayInit();
#endif
            HudDistributor.Instance.NowHudHandler().Shake(ProcessEndAction, type, value, flame, wait);
        }

        private void ProcessEndAction() {
            SendBackToLauncher.Invoke();
        }
    }
}