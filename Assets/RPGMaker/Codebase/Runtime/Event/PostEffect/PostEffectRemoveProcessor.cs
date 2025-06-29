using RPGMaker.Codebase.CoreSystem.Knowledge.DataModel.Event;
using RPGMaker.Codebase.Runtime.Common;

namespace RPGMaker.Codebase.Runtime.Event.PostEffect
{
    public class PostEffectRemoveProcessor : AbstractEventCommandProcessor
    {
        protected override void Process(string eventID, EventDataModel.EventCommand command)
        {
            int[] param =
            {
                int.Parse(command.parameters[0]), // Dummy
            };
            
            HudDistributor.Instance.NowHudHandler().RemovePostEffect();
            TimeHandler.Instance.AddTimeAction(1f / 60f, ProcessEndAction, false);
        }

        private void ProcessEndAction()
        {
            SendBackToLauncher.Invoke();
        }
    }
}