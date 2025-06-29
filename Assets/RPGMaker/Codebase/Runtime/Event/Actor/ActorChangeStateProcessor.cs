using RPGMaker.Codebase.CoreSystem.Knowledge.DataModel.Event;
using RPGMaker.Codebase.Runtime.Common;
using RPGMaker.Codebase.Runtime.Common.Component.Hud.Actor;

namespace RPGMaker.Codebase.Runtime.Event.Actor
{
    /// <summary>
    /// [アクター]-[ステートの変更]
    /// </summary>
    public class ActorChangeStateProcessor : AbstractEventCommandProcessor
    {
        private ActorChangeState _actor;

        protected override void Process(string eventId, EventDataModel.EventCommand command) {
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
#else
            ProcessAsync(eventId, command);
        }

        private async void ProcessAsync(string eventId, EventDataModel.EventCommand command) {
#endif
            if (_actor == null)
            {
                _actor = new ActorChangeState();
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

            var state = DataManager.Self().GetStateDataModel(command.parameters[3]);
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
            _actor.ChangeState(state, command);
#else
            await _actor.ChangeState(state, command);
#endif
            ProcessWait();
        }

        private void ProcessWait() {
            DataManager.Self().IsGameOverCheck = true;
            ProcessEndAction();
        }

        private void ProcessEndAction() {
            _actor = null;
            SendBackToLauncher.Invoke();
        }
    }
}