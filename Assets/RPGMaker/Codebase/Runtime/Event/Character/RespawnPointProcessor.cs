using RPGMaker.Codebase.CoreSystem.Knowledge.DataModel.Event;
using RPGMaker.Codebase.CoreSystem.Service.MapManagement;
using RPGMaker.Codebase.Runtime.Common;
using RPGMaker.Codebase.Runtime.Common.Component.Hud.Character;
using RPGMaker.Codebase.Runtime.Map;
using UnityEngine; //バトルでは本コマンドは利用しない
using RPGMaker.Codebase.CoreSystem.Knowledge.DataModel.Runtime;

namespace RPGMaker.Codebase.Runtime.Event.Character
{
    /// <summary>
    /// [キャラクター]-[復活ポイント]
    /// </summary>
    public class RespawnPointProcessor : AbstractEventCommandProcessor
    {
        private MovePlace _movePlace;

        protected override void Process(string eventID, EventDataModel.EventCommand command) {
            //移動処理実施
            RespawnPoint(eventID, command);
        }

        private void ProcessEndAction() {
            SendBackToLauncher.Invoke();
        }

        //復活ポイントを設定する
        private void RespawnPoint(string eventId, EventDataModel.EventCommand command)
        {
            var SaveData = DataManager.Self().GetRuntimeSaveDataModel();
            //セーブデータに復活ポイントを設定する
            int RespawnPointType = int.Parse(command.parameters[0]);
            //復活時の状態設定
            SaveData.respawnPointData.eStatus = (RuntimeSaveDataModel.RespawnPointData.EStatus) int.Parse(command.parameters[5]);
            //ゲームオーバー
            if (RespawnPointType == 0)
            {
                SaveData.respawnPointData.mapID = "";
                SaveData.respawnPointData.x = 0;
                SaveData.respawnPointData.y = 0;

            }else
            //現在を設定する
            if (RespawnPointType == 1)
            {
                //現在地を取得する
                // プレイヤーの現在位置取得
                var MapPos = new Commons.TargetCharacter(Commons.TargetType.Player, null).GetTilePositionOnTile();
                //現在のマップ
                var MapData = MapManager.CurrentMapDataModel;
                //MapData.id;
                SaveData.respawnPointData.x = MapPos.x;
                SaveData.respawnPointData.y = MapPos.y;
                SaveData.respawnPointData.mapID = MapData.id;

                if (command.parameters[6].ToLower() == "false" || command.parameters[6] == "")
                {
                    SaveData.respawnPointData.gameOverScreenFlg = false;
                }
                else
                if (command.parameters[6].ToLower() == "true")
                {
                    SaveData.respawnPointData.gameOverScreenFlg = true;
                }
            }
            else
            //設定された位置を登録する
            if (RespawnPointType == 2)
            {
                //直接指定 設定された位置をセーブデータに設定する
                if (command.parameters[2] == "-1" || command.parameters[2] == "")
                {
                    SaveData.respawnPointData.mapID = "";
                    SaveData.respawnPointData.x = 0;
                    SaveData.respawnPointData.y = 0;
                }
                else
                {
                    // マップID
                    SaveData.respawnPointData.mapID = (command.parameters[2]);
                    var pos = new Vector2(0, 0);
                    // 座標
                    if (float.TryParse(command.parameters[3], out pos.x))
                    {
                        SaveData.respawnPointData.x = (int) Mathf.Abs(pos.x);
                    }
                    if (float.TryParse(command.parameters[4], out pos.y))
                    {
                        SaveData.respawnPointData.y = (int) (Mathf.Abs(pos.y) * -1f);
                    }
                }
                if (command.parameters[6] == "false" || command.parameters[6] == "")
                {
                    SaveData.respawnPointData.gameOverScreenFlg = false;
                }
                else
                if (command.parameters[6] == "true")
                {
                    SaveData.respawnPointData.gameOverScreenFlg = true;
                }
            }
            //次のイベント実行
            ProcessEndAction();
        }
    }
}