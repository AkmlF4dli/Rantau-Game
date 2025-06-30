using RPGMaker.Codebase.CoreSystem.Knowledge.DataModel.Event;
using RPGMaker.Codebase.Runtime.Common;
using RPGMaker.Codebase.Runtime.Common.Component.Hud.Actor;
using System.Linq;

namespace RPGMaker.Codebase.Runtime.Event.Actor
{
    /// <summary>
    /// [アクター]-[職業の変更]
    /// </summary>
    public class ActorChangeClassProcessor : AbstractEventCommandProcessor
    {
        private ActorChangeClass _actor;

        protected override void Process(string eventID, EventDataModel.EventCommand command) {
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
#else
            ProcessAsync(eventID, command);
        }

        private async void ProcessAsync(string eventID, EventDataModel.EventCommand command) {
#endif
            if (_actor == null)
            {
                _actor = new ActorChangeClass();
            }

            var actorData = DataManager.Self().GetActorDataModels()
                .FirstOrDefault(c => c.uuId == command.parameters[0]);
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
            _actor.ChangeClass(actorData, command);
#else
            await _actor.ChangeClass(actorData, command);
#endif
            ProcessEndAction();
        }

        private void ProcessEndAction() {
            _actor = null;
            SendBackToLauncher.Invoke();
        }
    }
}