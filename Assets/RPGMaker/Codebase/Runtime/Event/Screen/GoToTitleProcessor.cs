using RPGMaker.Codebase.CoreSystem.Knowledge.DataModel.Event;
using RPGMaker.Codebase.Runtime.Battle;
using RPGMaker.Codebase.Runtime.Common;
using RPGMaker.Codebase.Runtime.Map;
using System.Threading.Tasks;
using UnityEngine.SceneManagement;

namespace RPGMaker.Codebase.Runtime.Event.Screen
{
    /// <summary>
    /// [シーン制御]-[タイトル画面に戻す]
    /// </summary>
    public class GoToTitleProcessor : AbstractEventCommandProcessor
    {
        protected override void Process(string eventId, EventDataModel.EventCommand command) {
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
#else
            ProcessAsync(eventId, command);
        }

        private async void ProcessAsync(string eventId, EventDataModel.EventCommand command) {
#endif
            if (GameStateHandler.IsMap())
                //メニューの非表示
                MapManager.menu.MenuClose(false);

            //画面をフェードアウトする
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
            HudDistributor.Instance.NowHudHandler().DisplayInit();
            HudDistributor.Instance.NowHudHandler().FadeOut(GoTitle, UnityEngine.Color.black);
#else
            await HudDistributor.Instance.NowHudHandler().DisplayInit();
            HudDistributor.Instance.NowHudHandler().FadeOut(() => { _ = GoTitle(); }, UnityEngine.Color.black);
#endif
        }

        //タイトルに戻る
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
        private void GoTitle() {
#else
        private async Task GoTitle() {
#endif
            //ゲームオーバー表示
            if (GameStateHandler.IsMap())
            {
                //タイトルシーンへ
                SceneManager.LoadScene("Title");
                //次のイベントへ
                ProcessEndAction();
            }
            else
            {
                //タイトルシーンへ
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
                SceneBattle.BackTitle();
#else
                await SceneBattle.BackTitle();
#endif
            }
        }

        private void ProcessEndAction() {
            SendBackToLauncher.Invoke();
        }
    }
}