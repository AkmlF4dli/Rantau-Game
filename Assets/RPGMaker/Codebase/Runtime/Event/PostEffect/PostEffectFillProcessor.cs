using RPGMaker.Codebase.CoreSystem.Knowledge.DataModel.Event;
using RPGMaker.Codebase.Runtime.Common;

namespace RPGMaker.Codebase.Runtime.Event.PostEffect
{
    public class PostEffectFillProcessor : AbstractEventCommandProcessor
    {
        protected override void Process(string eventID, EventDataModel.EventCommand command)
        {
            int type = 4; // TODO: マジックナンバー
            bool persistent = command.parameters[0] != "0"; // ChangeMap
            int flame = int.Parse(command.parameters[1]); // NumFrame
            bool wait = command.parameters[2] != "0"; // Wait
            int[] param =
            {
                int.Parse(command.parameters[3]), // Mono | Gradient
                int.Parse(command.parameters[4]), // Color1.R
                int.Parse(command.parameters[5]), // Color1.G
                int.Parse(command.parameters[6]), // Color1.B
                int.Parse(command.parameters[7]), // Color1.A
                int.Parse(command.parameters[8]), // GradientDirection
                int.Parse(command.parameters[9]), // Color2.R
                int.Parse(command.parameters[10]), // Color2.G
                int.Parse(command.parameters[11]), // Color2.B
                int.Parse(command.parameters[12]), // Color2.A
                int.Parse(command.parameters[13]), // Color3.R
                int.Parse(command.parameters[14]), // Color3.G
                int.Parse(command.parameters[15]), // Color3.B
                int.Parse(command.parameters[16]), // Color3.A
                int.Parse(command.parameters[17]), // Blend
            };

            HudDistributor.Instance.NowHudHandler()
                .ApplyPostEffect(ProcessEndAction, type, persistent, flame, param, wait);
        }

        private void ProcessEndAction()
        {
            SendBackToLauncher.Invoke();
        }
    }
}