using RPGMaker.Codebase.CoreSystem.Knowledge.DataModel.Event;
using RPGMaker.Codebase.Runtime.Common;
using UnityEngine;

namespace RPGMaker.Codebase.Runtime.Event.Display
{
    /// <summary>
    /// [画面]-[画面の色調変更]
    /// </summary>
    public class DisplayChangeColorProcessor : AbstractEventCommandProcessor
    {
        private int frame;

        protected override void Process(string eventID, EventDataModel.EventCommand command) {
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
#else
            ProcessAsync(eventID, command);
        }

        private async void ProcessAsync(string eventID, EventDataModel.EventCommand command) {
#endif
            var color = new Color(
                int.Parse(command.parameters[0]),
                int.Parse(command.parameters[1]),
                int.Parse(command.parameters[2])
            );
            var alpha = int.Parse(command.parameters[3]);

            frame = int.Parse(command.parameters[4]);
            var wait = command.parameters[5] != "0";
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
            HudDistributor.Instance.NowHudHandler().DisplayInit();
#else
            await HudDistributor.Instance.NowHudHandler().DisplayInit();
#endif
            HudDistributor.Instance.NowHudHandler().ChangeColor(ProcessEndAction, color, alpha, frame, wait);
        }

        private void ProcessEndAction() {
            SendBackToLauncher.Invoke();
        }
    }
}