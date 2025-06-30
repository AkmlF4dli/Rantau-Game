using RPGMaker.Codebase.CoreSystem.Knowledge.DataModel.Event;
using RPGMaker.Codebase.Runtime.Battle.Objects;
using RPGMaker.Codebase.Runtime.Common;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace RPGMaker.Codebase.Runtime.Event.Battle
{
    /// <summary>
    /// [バトル]-[戦闘アニメーションの表示]
    /// </summary>
    public class BattleShowAnimation : AbstractEventCommandProcessor
    {
        protected override void Process(string eventID, EventDataModel.EventCommand command) {
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
#else
            ProcessAsync(eventID, command);
        }

        private async void ProcessAsync(string eventID, EventDataModel.EventCommand command) {
#endif
            var memberNo = 0;
            if (int.TryParse(command.parameters[0], out memberNo))
            {
                memberNo -= 1; // 1から始まる番号で格納されているのでインデックス用に調整
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
                IterateEnemyIndex(memberNo, enemy =>
#else
                await IterateEnemyIndex(memberNo, enemy =>
#endif
                {
                    if (enemy.IsAlive()) enemy.StartAnimation(command.parameters[1], false, 0);
                });
            }

            //次のイベントへ
            ProcessEndAction();
        }

#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
        private void IterateEnemyIndex(int number, Action<GameBattler> callback) {
#else
        private async Task IterateEnemyIndex(int number, Action<GameBattler> callback) {
#endif
            if (number < 0)
            {
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
                DataManager.Self().GetGameTroop().Members().ForEach(callback);
#else
                (await DataManager.Self().GetGameTroop().Members()).ForEach(callback);
#endif
            }
            else
            {
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
                var enemy = DataManager.Self().GetGameTroop().Members().ElementAtOrDefault(number);
#else
                var enemy = (await DataManager.Self().GetGameTroop().Members()).ElementAtOrDefault(number);
#endif
                if (enemy != null) callback(enemy);
            }
        }

        private void ProcessEndAction() {
            SendBackToLauncher.Invoke();
        }
    }
}