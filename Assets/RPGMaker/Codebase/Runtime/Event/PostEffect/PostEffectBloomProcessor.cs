using RPGMaker.Codebase.CoreSystem.Knowledge.DataModel.Event;
using RPGMaker.Codebase.Runtime.Common;

namespace RPGMaker.Codebase.Runtime.Event.PostEffect
{
    public class PostEffectBloomProcessor : AbstractEventCommandProcessor
    {
        protected override void Process(string eventID, EventDataModel.EventCommand command)
        {
            int type = 3; // TODO: マジックナンバー
            bool persistent = command.parameters[0] != "0"; // ChangeMap
            int flame = int.Parse(command.parameters[1]); // NumFrame
            bool wait = command.parameters[2] != "0"; // Wait
            int[] param =
            {
                int.Parse(command.parameters[3]), // Intensity
                
            };
            
            HudDistributor.Instance.NowHudHandler().ApplyPostEffect(ProcessEndAction, type, persistent, flame, param, wait);
        }

        private void ProcessEndAction()
        {
            SendBackToLauncher.Invoke();
        }
    }
}