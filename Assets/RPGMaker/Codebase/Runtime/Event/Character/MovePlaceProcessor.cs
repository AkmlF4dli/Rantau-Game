using RPGMaker.Codebase.CoreSystem.Knowledge.DataModel.Event;
using RPGMaker.Codebase.Runtime.Common;
using RPGMaker.Codebase.Runtime.Common.Component.Hud.Character;
using RPGMaker.Codebase.Runtime.Map;
using UnityEngine; //バトルでは本コマンドは利用しない

namespace RPGMaker.Codebase.Runtime.Event.Character
{
    /// <summary>
    /// [キャラクター]-[場所移動]
    /// </summary>
    public class MovePlaceProcessor : AbstractEventCommandProcessor
    {
        private MovePlace _movePlace;

        protected override void Process(string eventID, EventDataModel.EventCommand command) {
            //移動処理実施
            MovePlace(eventID, command);
        }

        private void ProcessEndAction() {
            SendBackToLauncher.Invoke();
        }

#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
        private void MovePlace(string eventId, EventDataModel.EventCommand command) {
#else
        private async void MovePlace(string eventId, EventDataModel.EventCommand command) {
#endif
            if (_movePlace == null)
            {
                _movePlace = new GameObject().AddComponent<MovePlace>();
                _movePlace.Init();
            }

#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
            _movePlace.MovePlaceProcess(eventId, command, CloseMovePlace);
#else
            await _movePlace.MovePlaceProcess(eventId, command, CloseMovePlace);
#endif
        }

        private void CloseMovePlace() {
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
#else
            CloseMovePlaceAsync();
        }
        private async void CloseMovePlaceAsync() {
#endif
            if (_movePlace == null) return;
            Object.Destroy(_movePlace.gameObject);
            _movePlace = null;

            //マップ変更時にOFFにする
            MapManager.IsDisplayName = 0;
            //場所移動成功時にマップ名の表示を行う
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
            HudDistributor.Instance.NowHudHandler().PlayChangeMapName();
#else
            await HudDistributor.Instance.NowHudHandler().PlayChangeMapName();
#endif

            //移動前にデータを破棄
            //MapEventExecuteController.Instance.RemoveCarryEventOnMap();

            //次のイベント実行
            ProcessEndAction();
        }
    }
}