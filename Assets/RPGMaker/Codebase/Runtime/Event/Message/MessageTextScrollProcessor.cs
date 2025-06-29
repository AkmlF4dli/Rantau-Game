using RPGMaker.Codebase.CoreSystem.Knowledge.DataModel.Event;
using RPGMaker.Codebase.Runtime.Common;

namespace RPGMaker.Codebase.Runtime.Event.Message
{
    /// <summary>
    /// [メッセージ]-[文章のスクロール]
    /// </summary>
    public class MessageTextScrollProcessor : AbstractEventCommandProcessor
    {
        protected override void Process(string eventID, EventDataModel.EventCommand command) {
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
#else
            ProcessAsync(eventID, command);
        }

        private async void ProcessAsync(string eventID, EventDataModel.EventCommand command) {
#endif
            if (!HudDistributor.Instance.NowHudHandler().IsMessageScrollWindowActive())
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
                HudDistributor.Instance.NowHudHandler().OpenMessageScrollWindow();
#else
                await HudDistributor.Instance.NowHudHandler().OpenMessageScrollWindow();
#endif

            //加速数値
            HudDistributor.Instance.NowHudHandler().SetScrollSpeed(int.Parse(command.parameters[0]));
            HudDistributor.Instance.NowHudHandler().SetScrollNoFast(command.parameters[1] == "1");
            ProcessEndAction();
        }


        private void ProcessEndAction() {
            SendBackToLauncher.Invoke();
        }
    }
}