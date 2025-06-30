using RPGMaker.Codebase.CoreSystem.Knowledge.DataModel.Event;
using RPGMaker.Codebase.Runtime.Common;

namespace RPGMaker.Codebase.Runtime.Event.AudioVideo
{
    /// <summary>
    /// ムービーの再生
    /// </summary>
    public class MoviePlayProcessor : AbstractEventCommandProcessor
    {
        protected override void Process(string eventID, EventDataModel.EventCommand command) {
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
#else
            ProcessAsync(eventID, command);
        }

        private async void ProcessAsync(string eventID, EventDataModel.EventCommand command) {
#endif
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
            if (SoundManager.Self().IsMovie(command.parameters[0]))
#endif
            {
                //初期化
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
                HudDistributor.Instance.NowHudHandler().MovieInit();
#else
                await HudDistributor.Instance.NowHudHandler().MovieInit();
#endif
                
                //読み込む動画名を入れて再生
                HudDistributor.Instance.NowHudHandler().AddMovie(command.parameters[0], ()=>
                {
                    //次のイベントへ
                    ProcessEndAction();
                });
            }
        }

        private void ProcessEndAction() {
            SendBackToLauncher.Invoke();
        }
        
    }
}