using RPGMaker.Codebase.CoreSystem.Knowledge.DataModel.Event;
using RPGMaker.Codebase.Runtime.Common;
using RPGMaker.Codebase.Runtime.Common.Component.Hud.Actor;

namespace RPGMaker.Codebase.Runtime.Event.Actor
{
    /// <summary>
    /// [アクター]-[全回復]
    /// </summary>
    public class ActorHealProcessor : AbstractEventCommandProcessor
    {
        private ActorHeal  _actor;

        protected override void Process(string eventID, EventDataModel.EventCommand command) {
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
#else
            ProcessAsync(eventID, command);
        }

        private async void ProcessAsync(string eventID, EventDataModel.EventCommand command) {
#endif
            if (_actor == null)
            {
                _actor = new ActorHeal();
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
                _actor.Init(
#else
                await _actor.Init(
#endif
                    DataManager.Self().GetClassDataModels(),
                    DataManager.Self().GetRuntimeSaveDataModel().runtimeActorDataModels,
                    DataManager.Self().GetRuntimeSaveDataModel().variables);
            }

#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
            _actor.Heal(int.Parse(command.parameters[0]), command.parameters[1]);
#else
            await _actor.Heal(int.Parse(command.parameters[0]), command.parameters[1]);
#endif
            ProcessEndAction();
        }

        private void ProcessEndAction() {
            _actor = null;
            SendBackToLauncher.Invoke();
        }
    }
}