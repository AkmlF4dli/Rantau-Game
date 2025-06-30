using RPGMaker.Codebase.CoreSystem.Knowledge.DataModel.Event;
using RPGMaker.Codebase.Runtime.Battle.Objects;
using RPGMaker.Codebase.Runtime.Common;
using RPGMaker.Codebase.Runtime.Common.Component.Hud.Party;
using System.Linq;

namespace RPGMaker.Codebase.Runtime.Event.Actor
{
    /// <summary>
    /// [アクター]-[装備の変更]
    /// </summary>
    public class ActorChangeEquipmentProcessor : AbstractEventCommandProcessor
    {
        protected override void Process(string eventID, EventDataModel.EventCommand command) {
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
#else
            ProcessAsync(eventID, command);
        }

        private async void ProcessAsync(string eventID, EventDataModel.EventCommand command) {
#endif
            //装備変更イベントは、以下のパラメータで動作する
            //0:アクターID
            //1:装備タイプ（武器、盾、頭…）
            //2:武器または防具のID

            //対象のアクターを取得
            var actorData = DataManager.Self().GetRuntimeSaveDataModel().runtimeActorDataModels
                .FirstOrDefault(c => c.actorId == command.parameters[0]);
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
            var actors = DataManager.Self().GetGameParty().Actors;
#else
            var actors = await DataManager.Self().GetGameParty().GetActors();
#endif
            var actor = actors.FirstOrDefault(c => c.ActorId == actorData?.actorId);
            if (actor == null)
            {
                //パーティに存在しない場合
                //RuntimeActorDataModel取得
                actorData = DataManager.Self().GetRuntimeSaveDataModel().runtimeActorDataModels.FirstOrDefault(c => c.actorId == command.parameters[0]);
                if (actorData == null)
                {
                    //存在しないため新規作成
                    PartyChange partyChange = new PartyChange();
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
                    actorData = partyChange.SetActorData(command.parameters[0]);
#else
                    actorData = await partyChange.SetActorData(command.parameters[0]);
#endif
                }

                //GameActor生成
                actor = new GameActor(actorData);
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
#else
                await actor.InitForConstructor(actorData);
#endif
            }

            //装備変更処理実施
            var equipTypes = DataManager.Self().GetSystemDataModel().equipTypes;
            var equipTypeIndex = equipTypes.IndexOf(equipTypes.FirstOrDefault(c => c.id == command.parameters[1]));

            //-1以外の場合は装備変更を行う
            if (command.parameters[2] != "-1")
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
                ItemManager.ChangeEquipment(actorData, equipTypes[equipTypeIndex], command.parameters[2], equipTypeIndex);
#else
                await ItemManager.ChangeEquipment(actorData, equipTypes[equipTypeIndex], command.parameters[2], equipTypeIndex);
#endif
            //-1指定の場合は装備を外す
            else
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
                ItemManager.RemoveEquipment(actorData, equipTypes[equipTypeIndex], equipTypeIndex);
#else
                await ItemManager.RemoveEquipment(actorData, equipTypes[equipTypeIndex], equipTypeIndex);
#endif

            for (int i = 0; i < actors.Count; i++)
                if (actors[i].ActorId == actorData.actorId)
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
                    actors[i].ResetActorData();
#else
                    await actors[i].ResetActorData();
#endif

            ProcessEndAction();
        }

        private void ProcessEndAction() {
            SendBackToLauncher.Invoke();
        }
    }
}