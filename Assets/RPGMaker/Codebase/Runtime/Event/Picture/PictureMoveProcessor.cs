using RPGMaker.Codebase.CoreSystem.Knowledge.DataModel.Event;
using RPGMaker.Codebase.Runtime.Common;

namespace RPGMaker.Codebase.Runtime.Event.Picture
{
    /// <summary>
    /// [ピクチャ]-[ピクチャの移動]
    /// </summary>
    public class PictureMoveProcessor : AbstractEventCommandProcessor
    {
        protected override void Process(string eventID, EventDataModel.EventCommand command) {
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
#else
            ProcessAsync(eventID, command);
        }

        private async void ProcessAsync(string eventID, EventDataModel.EventCommand command) {
#endif
            if (int.TryParse(command.parameters[0], out var result) == false ||
                HudDistributor.Instance.NowHudHandler().GetPicture(int.Parse(command.parameters[0])) == null)
            {
                ProcessEndAction();
                return;
            }

#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
            HudDistributor.Instance.NowHudHandler().PictureInit();
#else
            await HudDistributor.Instance.NowHudHandler().PictureInit();
#endif

            //画像の番号
            var pictureNumber = int.Parse(command.parameters[0]);

            //アンカー
            HudDistributor.Instance.NowHudHandler().SetAnchor(pictureNumber, int.Parse(command.parameters[2]));

            //幅,高さ
            HudDistributor.Instance.NowHudHandler().PlayPictureSize(pictureNumber,
                int.Parse(command.parameters[10]),
                int.Parse(command.parameters[6]),
                int.Parse(command.parameters[7]));

            //不透明度
            HudDistributor.Instance.NowHudHandler().PlayChangeColor(null,
                HudDistributor.Instance.NowHudHandler().GetPicture(int.Parse(command.parameters[0])).color * 255f,
                pictureNumber,
                int.Parse(command.parameters[8]),
                int.Parse(command.parameters[10]),
                false);

            //"通常", "加算", "乗算", "スクリーン";
            HudDistributor.Instance.NowHudHandler().SetProcessingType(pictureNumber, int.Parse(command.parameters[9]));

            //移動
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
            HudDistributor.Instance.NowHudHandler().PlayMove(
#else
            await HudDistributor.Instance.NowHudHandler().PlayMove(
#endif
                ProcessEndAction,
                pictureNumber,
                int.Parse(command.parameters[1]),
                int.Parse(command.parameters[3]),
                command.parameters[4], command.parameters[5],
                int.Parse(command.parameters[10]),
                command.parameters[11] == "1");
        }

        private void ProcessEndAction() {
            SendBackToLauncher.Invoke();
        }
    }
}