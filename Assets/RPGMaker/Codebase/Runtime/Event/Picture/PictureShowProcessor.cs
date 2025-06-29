using RPGMaker.Codebase.CoreSystem.Knowledge.DataModel.Event;
using RPGMaker.Codebase.Runtime.Common;

namespace RPGMaker.Codebase.Runtime.Event.Picture
{
    /// <summary>
    /// [ピクチャ]-[ピクチャの表示]
    /// </summary>
    public class PictureShowProcessor : AbstractEventCommandProcessor
    {
        protected override void Process(string eventID, EventDataModel.EventCommand command) {
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
#else
            ProcessAsync(eventID, command);
        }

        private async void ProcessAsync(string eventID, EventDataModel.EventCommand command) {
#endif
            //画像の番号
            var pictureNumber = int.Parse(command.parameters[0]);
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
            HudDistributor.Instance.NowHudHandler().PictureInit();
#else
            await HudDistributor.Instance.NowHudHandler().PictureInit();
#endif
            //画像の表示
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
            HudDistributor.Instance.NowHudHandler().AddPicture(pictureNumber, command.parameters[1]);
#else
            await HudDistributor.Instance.NowHudHandler().AddPicture(pictureNumber, command.parameters[1]);
#endif

            //アンカー
            HudDistributor.Instance.NowHudHandler().SetAnchor(pictureNumber, int.Parse(command.parameters[2]));

            //座標なのか、変数なのか
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
            HudDistributor.Instance.NowHudHandler().SetPosition(pictureNumber,
#else
            await HudDistributor.Instance.NowHudHandler().SetPosition(pictureNumber,
#endif
                int.Parse(command.parameters[3]),
                command.parameters[4], command.parameters[5]);

            //幅,高さ
            HudDistributor.Instance.NowHudHandler().SetPictureSize(pictureNumber,
                int.Parse(command.parameters[6]),
                int.Parse(command.parameters[7]));

            //不透明度
            HudDistributor.Instance.NowHudHandler().SetPictureOpacity(pictureNumber, int.Parse(command.parameters[8]));

            //"通常", "加算", "乗算", "スクリーン";
            HudDistributor.Instance.NowHudHandler().SetProcessingType(pictureNumber, int.Parse(command.parameters[9]));

            //セーブデータへの保存用
            HudDistributor.Instance.NowHudHandler().AddPictureParameter(pictureNumber, command.parameters);

            // クリックされた時に発火するコモンイベントの設定
            HudDistributor.Instance.NowHudHandler().SetExecuteCommonEvent(pictureNumber);

            ProcessEndAction();
        }

        private void ProcessEndAction() {
            SendBackToLauncher.Invoke();
        }
    }
}