using RPGMaker.Codebase.CoreSystem.Knowledge.DataModel.Event;
using RPGMaker.Codebase.Runtime.Common.Component.Hud.Character;
using System.Threading.Tasks;
using UnityEngine;

namespace RPGMaker.Codebase.Runtime.Event.Character
{
    /// <summary>
    /// [キャラクター]-[フキダシアイコンの表示]
    /// </summary>
    public class ShowIconProcessor : AbstractEventCommandProcessor
    {
        protected override void Process(string eventID, EventDataModel.EventCommand command) {
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
#else
            ProcessAsync(eventID, command);
        }

        private async void ProcessAsync(string eventID, EventDataModel.EventCommand command) {
#endif
            //0 イベントの番号
            //1 フキダシアイコンアセットid
            //2 ウェイト

#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
            CharacterShowIcon(
#else
            await CharacterShowIcon(
#endif
                command.parameters[0],
                command.parameters[1],
                command.parameters[2] == "1" ? true : false,
                eventID);
        }

        private void ProcessEndAction() {
            SendBackToLauncher.Invoke();
        }

#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
        private void CharacterShowIcon(
#else
        private async Task CharacterShowIcon(
#endif
            string eventId,
            string popupIconAssetId,
            bool waitToggle,
            string currentEventID
        ) {
            ShowIcon showIcon = new GameObject("ShowIcon").AddComponent<ShowIcon>();
            showIcon.Init();
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
            showIcon.PlayAnimation(ProcessEndAction, CloseCharacterShowIcon, eventId, popupIconAssetId, waitToggle,
#else
            await showIcon.PlayAnimation(ProcessEndAction, CloseCharacterShowIcon, eventId, popupIconAssetId, waitToggle,
#endif
                currentEventID);
        }

        private void CloseCharacterShowIcon(ShowIcon showIcon) {
            Object.Destroy(showIcon.gameObject);
        }
    }
}