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
    /// [バトル]-[戦闘行動の強制]
    /// </summary>
    public class BattleExecCommand : AbstractEventCommandProcessor
    {
        protected override void Process(string eventID, EventDataModel.EventCommand command) {
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
#else
            ProcessAsync(eventID, command);
        }

        private async void ProcessAsync(string eventID, EventDataModel.EventCommand command) {
#endif
            int ActorOrEnemy;
            ActorOrEnemy = command.parameters[0] == "0" ? 0 : 1;

            bool isAction = false;
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
            IterateBattler(ActorOrEnemy, command.parameters[1], battler =>
#else
            await IterateBattler(ActorOrEnemy, command.parameters[1], async battler =>
#endif
            {
                if (!battler.IsDeathStateAffected())
                {
                    if (command.parameters.Count >= 6 && command.parameters[5] == "NEWDATA")
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
                        battler.ForceAction(command.parameters[2], int.Parse(command.parameters[4]));
#else
                        await battler.ForceAction(command.parameters[2], int.Parse(command.parameters[4]));
#endif
                    else
                    {
                        if (command.parameters[3] == "0" || command.parameters[3] == "ラストターゲット" || command.parameters[3] == "Last Target" || command.parameters[3] == "最后一个目标")
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
                            battler.ForceAction(command.parameters[2], -2);
#else
                            await battler.ForceAction(command.parameters[2], -2);
#endif
                        else if (command.parameters[3] == "ランダム" || command.parameters[3] == "Random" || command.parameters[3] == "随机")
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
                            battler.ForceAction(command.parameters[2], -1);
#else
                            await battler.ForceAction(command.parameters[2], -1);
#endif
                        else
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
                            battler.ForceAction(command.parameters[2], int.Parse(command.parameters[3].Substring(command.parameters[3].Length - 1, 1)) - 1);
#else
                            await battler.ForceAction(command.parameters[2], int.Parse(command.parameters[3].Substring(command.parameters[3].Length - 1, 1)) - 1);
#endif
                    }
                    BattleEventCommandChainLauncher.PauseEvent(ProcessEndAction);
                    BattleManager.ForceAction(battler);
                    isAction = true;
                }
            });

            if (!isAction)
            {
                //戦闘行動の強制が行われなかった場合
                ProcessEndAction();
            }
        }

#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
        private void IterateBattler(int param1, string param2, Action<GameBattler> callback) {
#else
        private async Task IterateBattler(int param1, string param2, Func<GameBattler, Task> callback) {
#endif
            if (DataManager.Self().GetGameParty().InBattle())
            {
                if (param1 == 0)
                {
                    var memberNo = 0;
                    if (int.TryParse(param2, out memberNo))
                    {
                        memberNo -= 1; // 1から始まる番号で格納されているのでインデックス用に調整
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
                        IterateEnemyIndex(memberNo, callback);
#else
                        await IterateEnemyIndex(memberNo, callback);
#endif
                    }
                }
                else
                {
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
                    IterateActorId(param2, callback);
#else
                    await IterateActorId(param2, callback);
#endif
                }
            }
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
                foreach (var enemy in await DataManager.Self().GetGameTroop().Members())
                {
                    await callback(enemy);
                }
#endif
            }
            else
            {
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
                var enemy = DataManager.Self().GetGameTroop().Members().ElementAtOrDefault(param);
#else
                var enemy = (await DataManager.Self().GetGameTroop().Members()).ElementAtOrDefault(param);
#endif
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
                if (enemy != null) callback(enemy);
#else
                if (enemy != null) await callback(enemy);
#endif
            }
        }

#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
        private void IterateActorId(string param, Action<GameBattler> callback) {
#else
        private async Task IterateActorId(string param, Func<GameBattler, Task> callback) {
#endif
            if (param == "")
            {
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
                DataManager.Self().GetGameParty().Members().ForEach(callback);
#else
                foreach (var actor in await DataManager.Self().GetGameParty().Members())
                {
                    await callback(actor);
                }
#endif
            }
            else
            {
                var actors = DataManager.Self().GetRuntimeSaveDataModel().runtimeActorDataModels;
                for (var i = 0; i < actors.Count; i++)
                    if (param == actors[i].actorId)
                    {
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
                        var actor = DataManager.Self().GetGameActors().Actor(actors[i]);
                        if (actor != null) callback(actor);
#else
                        var actor = await DataManager.Self().GetGameActors().Actor(actors[i]);
                        if (actor != null) await callback(actor);
#endif
                    }
            }
        }

        private async void ProcessEndAction() {
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
            await Task.Delay(1);
#else
            await UniteTask.Delay(1);
#endif
            SendBackToLauncher.Invoke();
        }
    }
}