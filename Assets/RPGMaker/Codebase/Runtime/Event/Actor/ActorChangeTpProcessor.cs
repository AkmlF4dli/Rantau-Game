using RPGMaker.Codebase.CoreSystem.Knowledge.DataModel.Event;
using RPGMaker.Codebase.Runtime.Common;
using RPGMaker.Codebase.Runtime.Common.Component.Hud.Actor;

namespace RPGMaker.Codebase.Runtime.Event.Actor
{
    /// <summary>
    /// [アクター]-[TPの増減]
    /// </summary>
    public class ActorChangeTpProcessor : AbstractEventCommandProcessor
    {
        private ActorChangeTp _actor;

        protected override void Process(string eventID, EventDataModel.EventCommand command) {
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
#else
            ProcessAsync(eventID, command);
        }

        private async void ProcessAsync(string eventID, EventDataModel.EventCommand command) {
#endif
            if (_actor == null)
            {
                _actor = new ActorChangeTp();
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
                _actor.Init(DataManager.Self().GetRuntimeSaveDataModel());
#else
                await _actor.Init(DataManager.Self().GetRuntimeSaveDataModel());
#endif
            }

#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
            _actor.ChangeTP(command);
#else
            await _actor.ChangeTP(command);
#endif
            ProcessEndAction();
        }

        private void ProcessEndAction() {
            _actor = null;
            SendBackToLauncher.Invoke();
        }
    }
}