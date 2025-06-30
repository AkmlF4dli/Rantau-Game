using RPGMaker.Codebase.CoreSystem.Knowledge.DataModel.Event;
using RPGMaker.Codebase.Runtime.Common;

namespace RPGMaker.Codebase.Runtime.Event.Display
{
    /// <summary>
    /// [画面]-[画面のフェードイン]
    /// </summary>
    public class DisplayFadeInProcessor : AbstractEventCommandProcessor
    {
        protected override void Process(string eventID, EventDataModel.EventCommand command) {
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
#else
            ProcessAsync(eventID, command);
        }

        private async void ProcessAsync(string eventID, EventDataModel.EventCommand command) {
#endif
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
            HudDistributor.Instance.NowHudHandler().DisplayInit();
#else
            await HudDistributor.Instance.NowHudHandler().DisplayInit();
#endif
            HudDistributor.Instance.NowHudHandler().FadeIn(ProcessEndAction);
        }

        private void ProcessEndAction() {
            SendBackToLauncher.Invoke();
        }
    }
}