using RPGMaker.Codebase.CoreSystem.Knowledge.DataModel.Event;
using RPGMaker.Codebase.Runtime.Common.Component.Hud.Party;

namespace RPGMaker.Codebase.Runtime.Event.Party
{
    /// <summary>
    /// [パーティ]-[メンバーの入れ替え]
    /// </summary>
    public class PartyChangeProcess : AbstractEventCommandProcessor
    {
        private PartyChange _partyChange;

        protected override void Process(string eventID, EventDataModel.EventCommand command) {
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
#else
            ProcessAsync(eventID, command);
        }
        private async void ProcessAsync(string eventID, EventDataModel.EventCommand command) {
#endif
            var actorID = command.parameters[0];
            var isAdd = command.parameters[1] == "1";
            var isInit = command.parameters[2] == "1";

            _partyChange = new PartyChange();
            _partyChange.Init(actorID, isAdd, isInit);
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
            _partyChange.SetPartyChange();
#else
            await _partyChange.SetPartyChange();
#endif
            ProcessEndAction();
        }

        private void ProcessEndAction() {
            _partyChange = null;
            SendBackToLauncher.Invoke();
        }
    }
}