using RPGMaker.Codebase.CoreSystem.Knowledge.DataModel.Event;
using RPGMaker.Codebase.Runtime.Common;

namespace RPGMaker.Codebase.Runtime.Event.Picture
{
    /// <summary>
    /// [ピクチャ]-[ピクチャの回転]
    /// </summary>
    public class PictureRotateProcessor : AbstractEventCommandProcessor
    {
        protected override void Process(string eventID, EventDataModel.EventCommand command) {
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
#else
            ProcessAsync(eventID, command);
        }

        private async void ProcessAsync(string eventID, EventDataModel.EventCommand command) {
#endif
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
            HudDistributor.Instance.NowHudHandler().PictureInit();
#else
            await HudDistributor.Instance.NowHudHandler().PictureInit();
#endif
            HudDistributor.Instance.NowHudHandler().PlayRotation(
                int.Parse(command.parameters[0]),
                int.Parse(command.parameters[1]));
            SendBackToLauncher.Invoke();
        }
    }
}