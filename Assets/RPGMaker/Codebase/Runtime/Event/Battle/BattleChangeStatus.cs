using RPGMaker.Codebase.CoreSystem.Knowledge.DataModel.Event;
using RPGMaker.Codebase.CoreSystem.Knowledge.DataModel.Flag;
using RPGMaker.Codebase.CoreSystem.Service.DatabaseManagement;
using RPGMaker.Codebase.Runtime.Battle.Objects;
using RPGMaker.Codebase.Runtime.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace RPGMaker.Codebase.Runtime.Event.Battle
{
    /// <summary>
    /// [バトル]-[敵キャラのステート変更]
    /// </summary>
    public class BattleChangeStatus : AbstractEventCommandProcessor
    {
        protected override void Process(string eventID, EventDataModel.EventCommand command) {
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
#else
            ProcessAsync(eventID, command);
        }

        private async void ProcessAsync(string eventID, EventDataModel.EventCommand command) {
#endif
            //U382 各処理をコマンド通りの処理へ
            //HP設定
            if (command.parameters[1] == "True")
            {
                int value = 0;
                //up/down設定
                int Operate = command.parameters[2] == "up" ? 1 : -1;
                bool bRate = false;
                //定数
                if (command.parameters[4] == "True")
                {
                    value = int.Parse(command.parameters[5]) * Operate;
                }
                else
                //割合
                if (command.parameters[6] == "True")
                {
                    value = int.Parse(command.parameters[7]);
                    bRate = true;
                }
                else
                //変数
                if (command.parameters[8] == "True")
                {
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
                    value = GetVariables(command.parameters[9]) * Operate;
#else
                    value = await GetVariables(command.parameters[9]) * Operate;
#endif
                }

#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
                IterateEnemyIndex(int.Parse(command.parameters[0]),
                    enemy => { ChangeHp(enemy, value, command.parameters[3] == "True", bRate); });
#else
                Func<GameBattler, Task> ChangeHpCallback = async (enemy) =>
                {
                    await ChangeHp(enemy, value, command.parameters[3] == "True",bRate);
                };
                await IterateEnemyIndex(int.Parse(command.parameters[0]), ChangeHpCallback);
#endif
            }
            //MP 設定
            if (command.parameters[10] == "True")
            {
                int value = 0;
                //up/down設定
                int Operate = command.parameters[11] == "up" ? 1 : -1;
                bool bRate = false;
                //定数
                if (command.parameters[13] == "True")
                {
                    value = int.Parse(command.parameters[14]) * Operate;
                }
                else
                //割合
                if (command.parameters[15] == "True")
                {
                    value = int.Parse(command.parameters[16]);
                    bRate = true;
                }
                else
                //変数
                if (command.parameters[17] == "True")
                {
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
                    value = GetVariables(command.parameters[18]) * Operate;
#else
                    value = await GetVariables(command.parameters[18]) * Operate;
#endif
                }

#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
                IterateEnemyIndex(int.Parse(command.parameters[0]),
                    enemy => { ChangeMp(enemy, value, bRate); });
#else
                    Func<GameBattler, Task> ChangeMpCallback = async (enemy) =>
                {
                    ChangeMp(enemy, value, bRate);
                };
                await IterateEnemyIndex(int.Parse(command.parameters[0]), ChangeMpCallback);
#endif
            }
            //TP設定
            if (command.parameters[19] == "True")
            {
                int value = 0;
                //up/down設定
                int Operate = command.parameters[20] == "up" ? 1 : -1;
                //定数
                if (command.parameters[22] == "True")
                {
                    value = int.Parse(command.parameters[23]) * Operate;
                }
                else
                //変数
                if (command.parameters[24] == "True")
                {
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
                    value = GetVariables(command.parameters[25]) * Operate;
#else
                    value = await GetVariables(command.parameters[25]) * Operate;
#endif
                }

#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
                IterateEnemyIndex(int.Parse(command.parameters[0]),
                    enemy => { enemy.GainTp(value); });
#else
                Func<GameBattler, Task> GainTpCallback = async (enemy) =>
                {
                    await UniteTask.Delay(0);
                    enemy.GainTp(value);
                };
                await IterateEnemyIndex(int.Parse(command.parameters[0]), GainTpCallback);
#endif
            }

            //次のイベントへ
            ProcessEndAction();
        }

        private int OperateValue(int operation, int operandType, int operand) {
            var value = operandType == 0
                ? operand
                : int.Parse(DataManager.Self().GetRuntimeSaveDataModel().variables.data[operand]);
            return operation == 0 ? value : -value;
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
                if (enemy != null)
                {
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
                    callback(enemy);
#else
                    await callback(enemy);
#endif
                }
            }
        }

        private void ProcessEndAction() {
            SendBackToLauncher.Invoke();
        }
        

        //U382 各取得関数とHP,MPの計算処理を追加
        //変数値を取得する
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
        private int GetVariables(string ID) {
#else
        private async Task<int> GetVariables(string ID) {
#endif
            var dm = new DatabaseManagementService();
            var data = DataManager.Self().GetRuntimeSaveDataModel();
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
            var vari = dm.LoadFlags().variables;
#else
            var vari = (await dm.LoadFlags()).variables;
#endif
            for (var i = 0; i < vari.Count; i++)
            {
                if (vari[i].id == ID)
                {
                    return int.Parse(data.variables.data[i]);
                }
            }
            return 0;
        }

        //HP操作
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
        public void ChangeHp(GameBattler target, int value, bool allowDeath,bool bRate) {
#else
        public async Task ChangeHp(GameBattler target, int value, bool allowDeath,bool bRate) {
#endif
            if (target.IsAlive())
            {
                var minHP = allowDeath ? 0 : 1;
                if (bRate == false)
                {
                    target.Hp = Math.Max(minHP, Math.Min(target.Mhp, target.Hp + value));
                }
                else
                {
                    //割合
                    target.Hp = Math.Max(minHP, (int)((target.Mhp * (float)value)/100.0f));
                }

                if (allowDeath && target.Hp == 0)
                {
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
                    target.Refresh();
#else
                    await target.Refresh();
#endif
                    target.PerformCollapse();
                }
            }
        }

        //MP操作
        public void ChangeMp(GameBattler target, int value,bool bRate) 
        {
            if (target.IsAlive())
            {
                if (bRate == false)
                {
                    var maxMP = target.Mmp;
                    target.Mp = Math.Max(0, Math.Min(maxMP, target.Mp + value));
                }
                else
                {
                    float maxMP = target.Mmp;
                    //割合
                    target.Mp = Math.Max(0, (int) ((maxMP * ((float) value)) / 100.0f));
                }
            }
        }

    }
}