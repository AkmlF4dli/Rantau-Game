using RPGMaker.Codebase.CoreSystem.Knowledge.DataModel.Event;
using RPGMaker.Codebase.Runtime.Battle;
using RPGMaker.Codebase.Runtime.Battle.Objects;
using RPGMaker.Codebase.Runtime.Common;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace RPGMaker.Codebase.Runtime.Event.Battle
{
    /// <summary>
    /// [バトル]-[敵キャラの出現]
    /// </summary>
    public class BattleAppear : AbstractEventCommandProcessor
    {
        protected override void Process(string eventID, EventDataModel.EventCommand command) {
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
#else
            ProcessAsync(eventID, command);
        }

        private async void ProcessAsync(string eventID, EventDataModel.EventCommand command) {
#endif
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
            IterateEnemyIndex(int.Parse(command.parameters[0]), enemy =>
#else
            await IterateEnemyIndex(int.Parse(command.parameters[0]), async enemy =>
#endif
            {
                enemy.Appear();
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
                DataManager.Self().GetGameTroop().MakeUniqueNames();
#else
                await DataManager.Self().GetGameTroop().MakeUniqueNames();
#endif

                //敵選択Windowを再生成する
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
                BattleManager.SceneBattle.CreateEnemyWindow();
#else
                await BattleManager.SceneBattle.CreateEnemyWindow();
#endif
            });

            //次のイベントへ
            ProcessEndAction();
        }

#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
        private void IterateEnemyIndex(int param, Action<GameBattler> callback) {
#else
        private async Task IterateEnemyIndex(int param, Func<GameBattler, Task> callback) {
#endif
            if (param < 0)
            {
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
                DataManager.Self().GetGameTroop().Members().ForEach(callback);
#else
                foreach (var member in await DataManager.Self().GetGameTroop().Members())
                {
                    await callback(member);
                }
#endif
            }
            else
            {
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
                var enemy = DataManager.Self().GetGameTroop().Members().ElementAtOrDefault(param);
                if (enemy != null) callback(enemy);
#else
                var enemy = (await DataManager.Self().GetGameTroop().Members()).ElementAtOrDefault(param);
                if (enemy != null) await callback(enemy);
#endif
            }
        }

        private void ProcessEndAction() {
            SendBackToLauncher.Invoke();
        }
    }
}