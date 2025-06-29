using RPGMaker.Codebase.CoreSystem.Knowledge.DataModel.Event;
using RPGMaker.Codebase.CoreSystem.Knowledge.DataModel.Runtime;
using RPGMaker.Codebase.Runtime.Common;
using RPGMaker.Codebase.Runtime.Map;
using RPGMaker.Codebase.Runtime.Map.Component.Character;
using System.Text.RegularExpressions;

namespace RPGMaker.Codebase.Runtime.Event.Map
{
    /// <summary>
    /// [マップ]-[ミニマップ表示の変更]
    /// </summary>
    public class MapChangeMinimapProcessor : AbstractEventCommandProcessor
    {
        public const int ParameterDisplayOnOff = 0;
        public const int ParameterPositionType = 1;
        public const int ParameterPositionX = 2;
        public const int ParameterPositionY = 3;
        public const int ParameterWidth = 4;
        public const int ParameterHeight = 5;
        public const int ParameterScale = 6;
        public const int ParameterOpacity = 7;
        public const int ParameterPassableColor = 8;
        public const int ParameterUnpassableColor = 9;
        public const int ParameterEventColor = 10;

        public const int PositionTypeTopLeft = 0;
        public const int PositionTypeTopRight = 1;
        public const int PositionTypeBottomLeft = 2;
        public const int PositionTypeBottomRight = 3;
        public const int PositionTypeXy = 4;

        public const int ScreenWidth = 1920;
        public const int ScreenHeight = 1080;

        public const int TileIndexPassable = 15;
        public const int TileIndexUnpassable = 9;
        public const int TileIndexEvent = 12;

        private RuntimePlayerDataModel _runtimePlayerDataModel;

        protected override void Process(string eventId, EventDataModel.EventCommand command) {
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
#else
            ProcessAsync(eventId, command);
        }

        private async void ProcessAsync(string eventId, EventDataModel.EventCommand command) {
#endif
            var show = command.parameters[ParameterDisplayOnOff] == "1";
            var width = int.Parse(command.parameters[ParameterWidth]);
            var height = int.Parse(command.parameters[ParameterHeight]);
            var scale = float.Parse(command.parameters[ParameterScale]);
            var opacity = int.Parse(command.parameters[ParameterOpacity]);
            var x = 0;
            var y = 0;
            var positionType = int.Parse(command.parameters[ParameterPositionType]);
            var margin = 8;
            switch (positionType)
            {
                case PositionTypeTopLeft:
                    x = margin;
                    y = margin;
                    break;

                case PositionTypeTopRight:
                    //メニューボタンにかからないよう調整。
                    x = ScreenWidth - width - margin - 104;
                    y = 8;
                    break;

                case PositionTypeBottomLeft:
                    x = 8;
                    y = ScreenHeight - height - margin;
                    break;

                case PositionTypeBottomRight:
                    x = ScreenWidth - width - margin;
                    y = ScreenHeight - height - margin;
                    break;

                case PositionTypeXy:
                    x = int.Parse(command.parameters[ParameterPositionX]);
                    y = int.Parse(command.parameters[ParameterPositionY]);
                    break;

            }

            var minimap = DataManager.Self().GetRuntimeSaveDataModel().runtimeScreenDataModel.minimap;
            minimap.show = show;
            minimap.x = x;
            minimap.y = y;
            minimap.width = width;
            minimap.height = height;
            minimap.scale = scale;
            minimap.opacity = opacity;
            minimap.passableColor = command.parameters[ParameterPassableColor];
            minimap.unpassableColor = command.parameters[ParameterUnpassableColor];
            minimap.eventColor = command.parameters[ParameterEventColor];
            var memo = MapEventExecutionController.Instance.GetEventMapGameObject(eventId).GetComponent<EventOnMap>()?.MapDataModelEvent?.note;
            minimap.frameName = null;
            minimap.maskName = null;
            if (!string.IsNullOrEmpty(memo))
            {
                var match = Regex.Match(memo, @"<minimap-frame:([^,>]*)(,([^,>]*))?>");
                if (match.Success)
                {
                    minimap.frameName = match.Groups[1].Value;
                    minimap.maskName = match.Groups[3].Value;
                }
            }

            //ミニマップ表示のセットアップ。
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
            HudDistributor.Instance.NowHudHandler().SetupMinimap();
#else
            await HudDistributor.Instance.NowHudHandler().SetupMinimap();
#endif

            ProcessEndAction();
        }

        private void ProcessEndAction() {
            SendBackToLauncher.Invoke();
        }
    }
}