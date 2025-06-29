using RPGMaker.Codebase.CoreSystem.Knowledge.DataModel.Event;
using RPGMaker.Codebase.Runtime.Battle.Objects;
using RPGMaker.Codebase.Runtime.Common;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace RPGMaker.Codebase.Runtime.Event.Battle
{
    /// <summary>
    /// [バトル]-[敵からの変身]
    /// </summary>
    public class BattleTransform : AbstractEventCommandProcessor
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
                await IterateEnemyIndex(memberNo, async enemy =>
#endif
                {
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
                    ((GameEnemy) enemy).Transform(command.parameters[1]);
                    DataManager.Self().GetGameTroop().MakeUniqueNames();
#else
                    await ((GameEnemy) enemy).Transform(command.parameters[1]);
                    await DataManager.Self().GetGameTroop().MakeUniqueNames();
#endif
                });
            }

            //次のイベントへ
            ProcessEndAction();
        }

#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
        private void IterateEnemyIndex(int number, Action<GameBattler> callback) {
#else
        private async Task IterateEnemyIndex(int number, Func<GameBattler, Task> callback) {
#endif
            if (number < 0)
            {
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
                DataManager.Self().GetGameTroop().Members().ForEach(callback);
#else
                foreach (var enemy in await DataManager.Self().GetGameTroop().Members())
                {
                    await callback(enemy);
                }
#endif
            }
            else
            {
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
                var enemy = DataManager.Self().GetGameTroop().Members().ElementAtOrDefault(number);
                if (enemy != null) callback(enemy);
#else
                var enemy = (await DataManager.Self().GetGameTroop().Members()).ElementAtOrDefault(number);
                if (enemy != null) await callback(enemy);
#endif
            }
        }

        private void ProcessEndAction() {
            SendBackToLauncher.Invoke();
        }
    }
}