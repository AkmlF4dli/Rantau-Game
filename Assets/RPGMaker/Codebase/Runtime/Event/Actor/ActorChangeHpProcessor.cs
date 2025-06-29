using RPGMaker.Codebase.CoreSystem.Knowledge.DataModel.Event;
using RPGMaker.Codebase.Runtime.Common;
using RPGMaker.Codebase.Runtime.Common.Component.Hud.Actor;

namespace RPGMaker.Codebase.Runtime.Event.Actor
{
    /// <summary>
    /// [アクター]-[HPの増減]
    /// </summary>
    public class ActorChangeHpProcessor : AbstractEventCommandProcessor
    {
        private ActorChangeHp _actor;

        protected override void Process(string eventId, EventDataModel.EventCommand command) {
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
#else
            ProcessAsync(eventId, command);
        }

        private async void ProcessAsync(string eventId, EventDataModel.EventCommand command) {
#endif
            if (_actor == null)
            {
                _actor = new ActorChangeHp();
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
                _actor.Init(DataManager.Self().GetRuntimeSaveDataModel());
#else
                await _actor.Init(DataManager.Self().GetRuntimeSaveDataModel());
#endif
            }

#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
            _actor.ChangeHP(command);
#else
            await _actor.ChangeHP(command);
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