using RPGMaker.Codebase.CoreSystem.Knowledge.DataModel.Event;
using RPGMaker.Codebase.CoreSystem.Knowledge.DataModel.SkillCustom;
using RPGMaker.Codebase.Runtime.Common;
using RPGMaker.Codebase.Runtime.Common.Component.Hud.Actor;

namespace RPGMaker.Codebase.Runtime.Event.Actor
{
    /// <summary>
    /// [アクター]-[スキルの増減]
    /// </summary>
    public class ActorChangeSkillProcessor : AbstractEventCommandProcessor
    {
        private ActorChangeSkill _actor;

        protected override void Process(string eventID, EventDataModel.EventCommand command) {
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
#else
            ProcessAsync(eventID, command);
        }

        private async void ProcessAsync(string eventID, EventDataModel.EventCommand command) {
#endif
            if (_actor == null)
            {
                _actor = new ActorChangeSkill();
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
                _actor.Init(
#else
                await _actor.Init(
#endif
                    DataManager.Self().GetRuntimeSaveDataModel().runtimeActorDataModels,
                    DataManager.Self().GetRuntimeSaveDataModel().variables,
                    DataManager.Self().GetActorDataModels()
                );
            }

            var skills = DataManager.Self().GetSkillCustomDataModels();
            SkillCustomDataModel skillsData = null;
            for (int i = 0; i < skills.Count; i++)
                if (skills[i].basic.id == command.parameters[3])
                {
                    skillsData = skills[i];
                    break;
                }
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
            _actor.ChangeSkill(skillsData, command);
#else
            await _actor.ChangeSkill(skillsData, command);
#endif
            ProcessEndAction();
        }

        private void ProcessEndAction() {
            _actor = null;
            SendBackToLauncher.Invoke();
        }
    }
}