using RPGMaker.Codebase.CoreSystem.Knowledge.DataModel.Event;
using RPGMaker.Codebase.Runtime.Common;

namespace RPGMaker.Codebase.Runtime.Event.PostEffect
{
    public class PostEffectGlitchProcessor : AbstractEventCommandProcessor
    {
        protected override void Process(string eventID, EventDataModel.EventCommand command)
        {
            int type = 1; // TODO: マジックナンバー
            bool persistent = command.parameters[0] != "0"; // ChangeMap
            int flame = int.Parse(command.parameters[1]); // NumFrame
            bool wait = command.parameters[2] != "0"; // Wait
            int[] param =
            {
                int.Parse(command.parameters[3]), // Intensity
                int.Parse(command.parameters[4]), // BlockSize
                int.Parse(command.parameters[5]), // NoiseSpeed
                int.Parse(command.parameters[6]), // WaveWidth
                int.Parse(command.parameters[7]), // ChromaticAberration
                
            };
            
            HudDistributor.Instance.NowHudHandler().ApplyPostEffect(ProcessEndAction, type, persistent, flame, param, wait);
        }

        private void ProcessEndAction()
        {
            SendBackToLauncher.Invoke();
        }
    }
}