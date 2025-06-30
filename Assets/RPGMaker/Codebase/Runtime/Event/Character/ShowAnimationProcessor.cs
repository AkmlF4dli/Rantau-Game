using RPGMaker.Codebase.CoreSystem.Knowledge.DataModel.Event;
using RPGMaker.Codebase.Runtime.Common.Component.Hud.Character;
using System.Threading.Tasks;
using UnityEngine;

namespace RPGMaker.Codebase.Runtime.Event.Character
{
    /// <summary>
    /// [キャラクター]-[アニメーションの表示]
    /// </summary>
    public class ShowAnimationProcessor : AbstractEventCommandProcessor
    {
        private CharacterAnimation _characterAnimation;
        private GameObject _characterObject;

        protected override void Process(string eventID, EventDataModel.EventCommand command) {
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
#else
            ProcessAsync(eventID, command);
        }

        private async void ProcessAsync(string eventID, EventDataModel.EventCommand command) {
#endif
            //0 イベントの番号
            //1 アニメーションの種類
            //2 ウェイト
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
            CharacterShowAnimation(
#else
            await CharacterShowAnimation(
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
        private void CharacterShowAnimation(string eventId, string animName, bool waitToggle, string currentEventID) {
#else
        private async Task CharacterShowAnimation(string eventId, string animName, bool waitToggle, string currentEventID) {
#endif
            _characterObject = new GameObject {name = "AnimationObject"};
            _characterAnimation = _characterObject.AddComponent<CharacterAnimation>();
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
            _characterAnimation.Init();
#else
            await _characterAnimation.Init();
#endif

#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
            _characterAnimation.PlayAnimation(ProcessEndAction, CloseCharacterShowAnimation, eventId, animName,
#else
            await _characterAnimation.PlayAnimation(ProcessEndAction, CloseCharacterShowAnimation, eventId, animName,
#endif
                waitToggle, currentEventID);
        }

        private void CloseCharacterShowAnimation() {
            if (_characterAnimation == null) return;
            _characterObject = null;
            _characterAnimation = null;
            ProcessEndAction();
        }
    }
}