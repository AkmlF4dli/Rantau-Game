using RPGMaker.Codebase.CoreSystem.Knowledge.DataModel.CharacterActor;
using RPGMaker.Codebase.CoreSystem.Knowledge.DataModel.Event;
using RPGMaker.Codebase.Runtime.Battle.Objects;
using RPGMaker.Codebase.Runtime.Common.Component.Hud.Party;
using System.Linq;
using System.Threading.Tasks;

namespace RPGMaker.Codebase.Runtime.Common.Component.Hud.Actor
{
    public class ActorChangeClass
    {
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
        public void ChangeClass(CharacterActorDataModel actorData, EventDataModel.EventCommand command) {
#else
        public async Task ChangeClass(CharacterActorDataModel actorData, EventDataModel.EventCommand command) {
#endif
            var classId = command.parameters[1];
            var save = command.parameters[2] == "1" ? true : false;

            var party = DataManager.Self().GetGameParty();
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
            var index = party.Actors.IndexOf(party.Actors.FirstOrDefault(c => c.ActorId == actorData.uuId));
#else
            var partyActors = await party.GetActors();
            var index = partyActors.IndexOf(partyActors.FirstOrDefault(c => c.ActorId == actorData.uuId));
#endif
            if(index >= 0)
            {
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
                party.Actors[index].ChangeClass(classId, save);
#else
                await partyActors[index].ChangeClass(classId, save);
#endif
            }
            else
            {
                //パーティに存在しない場合
                //RuntimeActorDataModel取得
                var actor = DataManager.Self().GetRuntimeSaveDataModel().runtimeActorDataModels.FirstOrDefault(c => c.actorId == actorData.uuId);
                if (actor == null)
                {
                    //存在しないため新規作成
                    PartyChange partyChange = new PartyChange();
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
                    actor = partyChange.SetActorData(actorData.uuId);
#else
                    actor = await partyChange.SetActorData(actorData.uuId);
#endif
                }

                //GameActor生成
                GameActor gameActor = new GameActor(actor);
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
#else
                await gameActor.InitForConstructor(actor);
#endif

                //職業変更
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
                gameActor.ChangeClass(classId, save);
#else
                await gameActor.ChangeClass(classId, save);
#endif
            }
        }
    }
}