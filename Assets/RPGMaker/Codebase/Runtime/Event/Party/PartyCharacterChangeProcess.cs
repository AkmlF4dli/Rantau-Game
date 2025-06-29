using RPGMaker.Codebase.CoreSystem.Knowledge.DataModel.Event;
using RPGMaker.Codebase.Runtime.Map;
using System.Threading.Tasks;

namespace RPGMaker.Codebase.Runtime.Event.Party
{
    /// <summary>
    /// [パーティ]-[隊列メンバーの集合]
    /// </summary>
    public class PartyCharacterChangeProcess : AbstractEventCommandProcessor
    {
        protected override void Process(string eventID, EventDataModel.EventCommand command) {
            ProcessExecute();
        }

        private async void ProcessExecute() {
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
            await Task.Delay(1);
#else
            await UniteTask.Delay(1);
#endif
            MapManager.PartyMemberAllInCoordinate(MapManager.ReasonForPartyMemberAllIn.Event);
            ProcessEndAction();
        }

        private void ProcessEndAction() {
            SendBackToLauncher.Invoke();
        }
    }
}