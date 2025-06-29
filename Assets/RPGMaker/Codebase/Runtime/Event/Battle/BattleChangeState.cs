using RPGMaker.Codebase.CoreSystem.Knowledge.DataModel.Event;
using RPGMaker.Codebase.Runtime.Battle.Objects;
using RPGMaker.Codebase.Runtime.Common;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace RPGMaker.Codebase.Runtime.Event.Battle
{
    /// <summary>
    /// [バトル]-[敵キャラのステート変更]
    /// </summary>
    public class BattleChangeState : AbstractEventCommandProcessor
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
            await IterateEnemyIndex(int.Parse(command.parameters[0]), async (enemy) =>
#endif
            {
                var alreadyDead = enemy.IsDead();
                if (command.parameters[1] == "0")
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
                    enemy.AddState(command.parameters[2]);
#else
                    await enemy.AddState(command.parameters[2]);
#endif
                else
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
                    enemy.RemoveState(command.parameters[2]);
#else
                    await enemy.RemoveState(command.parameters[2]);
#endif

                if (enemy.IsDead() && !alreadyDead) enemy.PerformCollapse();

                enemy.ClearResult();
            });

            //次のイベントへ
            ProcessEndAction();
        }

#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
        public void IterateEnemyIndex(int param, Action<GameBattler> callback) {
#else
        public async Task IterateEnemyIndex(int param, Func<GameBattler, Task> callback) {
#endif
            if (param == 0)
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
                var enemy = DataManager.Self().GetGameTroop().Members().ElementAtOrDefault(param - 1);
                if (enemy != null) callback(enemy);
#else
                var enemy = (await DataManager.Self().GetGameTroop().Members()).ElementAtOrDefault(param - 1);
                if (enemy != null) await callback(enemy);
#endif
            }
        }

        private void ProcessEndAction() {
            SendBackToLauncher.Invoke();
        }
    }
}