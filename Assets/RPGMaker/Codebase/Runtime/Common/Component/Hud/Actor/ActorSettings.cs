using RPGMaker.Codebase.CoreSystem.Knowledge.DataModel.CharacterActor;
using RPGMaker.Codebase.CoreSystem.Knowledge.DataModel.Event;
using RPGMaker.Codebase.CoreSystem.Knowledge.DataModel.Runtime;
using RPGMaker.Codebase.CoreSystem.Knowledge.Enum;
using RPGMaker.Codebase.Runtime.Common.Component.Hud.Party;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace RPGMaker.Codebase.Runtime.Common.Component.Hud.Actor
{
    public class ActorSettings
    {
        // データプロパティ
        //--------------------------------------------------------------------------------------------------------------
        private List<RuntimeActorDataModel> _runtimeActorDataModels;

        /**
         * 初期化
         */
        public void Init(
            List<RuntimeActorDataModel> runtimeActorData,
            List<CharacterActorDataModel> characterActorData
        ) {
            _runtimeActorDataModels = runtimeActorData;
        }

#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
        public void ChangeActorSetting(EventDataModel.EventCommand command) {
#else
        public async Task ChangeActorSetting(EventDataModel.EventCommand command) {
#endif
            var actor = _runtimeActorDataModels.FirstOrDefault(c => c.actorId == command.parameters[0]);
            if (actor == null)
            {
                //存在しないため新規作成
                PartyChange partyChange = new PartyChange();
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
                actor = partyChange.SetActorData(command.parameters[0]);
#else
                actor = await partyChange.SetActorData(command.parameters[0]);
#endif
            }

            var eventCode = (EventEnum) System.Enum.ToObject(typeof(EventEnum), command.code);
            //名前の変更
            if (eventCode == EventEnum.EVENT_CODE_ACTOR_CHANGE_NAME && command.parameters[1] == "1")
                actor.name = command.parameters[2];
            //二つ名の変更
            else if (eventCode == EventEnum.EVENT_CODE_ACTOR_CHANGE_NICKNAME && command.parameters[3] == "1")
                actor.nickname = command.parameters[4];
            //プロフィールの変更
            else if (eventCode == EventEnum.EVENT_CODE_ACTOR_CHANGE_PROFILE && command.parameters[5] == "1")
                actor.profile = command.parameters[6];
        }
    }
}