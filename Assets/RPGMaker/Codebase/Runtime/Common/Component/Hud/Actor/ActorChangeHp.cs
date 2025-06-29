using RPGMaker.Codebase.CoreSystem.Knowledge.DataModel.CharacterActor;
using RPGMaker.Codebase.CoreSystem.Knowledge.DataModel.Event;
using RPGMaker.Codebase.CoreSystem.Knowledge.DataModel.Runtime;
using RPGMaker.Codebase.CoreSystem.Service.DatabaseManagement;
using RPGMaker.Codebase.Runtime.Battle.Objects;
using RPGMaker.Codebase.Runtime.Common.Component.Hud.Party;
using System;
using System.Collections.Generic;
using System.Linq;
using static RPGMaker.Codebase.CoreSystem.Knowledge.DataModel.Flag.FlagDataModel;
using static RPGMaker.Codebase.CoreSystem.Knowledge.DataModel.Runtime.RuntimeSaveDataModel;
using System.Threading.Tasks;

namespace RPGMaker.Codebase.Runtime.Common.Component.Hud.Actor
{
    public class ActorChangeHp
    {
        private List<Variable> _databaseVariables;

        // データプロパティ
        //--------------------------------------------------------------------------------------------------------------
        private List<RuntimeActorDataModel> _runtimeActorDataModels;
        private SaveDataVariablesData       _saveDataVariablesData;
        private List<CharacterActorDataModel> _characterActorData;

        /**
         * 初期化
         */
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
        public void Init(RuntimeSaveDataModel saveDataModel) {
#else
        public async Task Init(RuntimeSaveDataModel saveDataModel) {
            await UniteTask.Delay(0);
#endif
            _runtimeActorDataModels = saveDataModel.runtimeActorDataModels;
            _saveDataVariablesData = saveDataModel.variables;

            var databaseManagementService = new DatabaseManagementService();
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
            _databaseVariables = databaseManagementService.LoadFlags().variables;
#else
            _databaseVariables = (await databaseManagementService.LoadFlags()).variables;
#endif
            _characterActorData = DataManager.Self().GetActorDataModels();
        }

#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
        public void ChangeHP(EventDataModel.EventCommand command) {
#else
        public async Task ChangeHP(EventDataModel.EventCommand command) {
#endif
            var isFixedValue = command.parameters[0] == "0";
            var isAddValue = command.parameters[2] == "0";
            var isConstant = command.parameters[3] == "0";
            var isKnockOut = command.parameters[5] == "1";

            var value = 0;
            if (isConstant)
            {
                if (!int.TryParse(command.parameters[4], out value))
                    return;
            }
            else
            {
                var index = _databaseVariables.FindIndex(v => v.id == command.parameters[4]);
                if (index == -1) return;

                if (!int.TryParse(_saveDataVariablesData.data[index], out value))
                    return;
            }

            value = isAddValue ? value : -value;

            if (isFixedValue)
            {
                var actorId = command.parameters[1];
                if (actorId == "-1") //パーティ全体
                {
                    // パーティ全体
                    foreach (var actorDataModel in _runtimeActorDataModels)
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
                        ChangeHpProcess(isKnockOut, actorDataModel, value);
#else
                        await ChangeHpProcess(isKnockOut, actorDataModel, value);
#endif
                    return;
                }

                // 個々のキャラクター
                var actor = _runtimeActorDataModels.FirstOrDefault(c => c.actorId == actorId);
                if(actor != null)
                {
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
                    ChangeHpProcess(isKnockOut, actor, value);
#else
                    await ChangeHpProcess(isKnockOut, actor, value);
#endif
                }
                else
                {
                    //存在しないため新規作成
                    PartyChange partyChange = new PartyChange();
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
                    actor = partyChange.SetActorData(actorId);
                    ChangeHpProcess(isKnockOut, actor, value);
#else
                    actor = await partyChange.SetActorData(actorId);
                    await ChangeHpProcess(isKnockOut, actor, value);
#endif
                }
            }
            else
            {
                //MVの挙動から
                //変数内の数値によって経験値を変動させるのは、該当のIDのユーザー（=SerialNoが一致するアクター）
                int variableIndex = _databaseVariables.FindIndex(v => v.id == command.parameters[1]);
                if (variableIndex >= 0)
                {
                    int actorSerialNo = int.Parse(_saveDataVariablesData.data[variableIndex]);
                    if (actorSerialNo >= 0)
                    {
                        int indexActor = _characterActorData.IndexOf(_characterActorData.FirstOrDefault(c => c.SerialNumber == actorSerialNo));
                        if (indexActor >= 0)
                        {
                            int index = _runtimeActorDataModels.IndexOf(_runtimeActorDataModels.FirstOrDefault(c => c.actorId == _characterActorData[indexActor].uuId));
                            if (index >= 0)
                            {
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
                                ChangeHpProcess(isKnockOut, _runtimeActorDataModels[index], value);
#else
                                await ChangeHpProcess(isKnockOut, _runtimeActorDataModels[index], value);
#endif
                            }
                            else
                            {
                                //存在しないため新規作成
                                PartyChange partyChange = new PartyChange();
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
                                RuntimeActorDataModel actor = partyChange.SetActorData(_characterActorData[indexActor].uuId);
                                ChangeHpProcess(isKnockOut, actor, value);
#else
                                RuntimeActorDataModel actor = await partyChange.SetActorData(_characterActorData[indexActor].uuId);
                                await ChangeHpProcess(isKnockOut, actor, value);
#endif
                            }
                        }
                    }
                }
            }
        }

#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
        private void ChangeHpProcess(bool knockout, RuntimeActorDataModel targetDataModel, int value) {
#else
        private async Task ChangeHpProcess(bool knockout, RuntimeActorDataModel targetDataModel, int value) {
#endif
            if (targetDataModel == null) return;
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
            var actorsWork = DataManager.Self().GetGameParty().Actors;
#else
            var actorsWork = await DataManager.Self().GetGameParty().GetActors();
#endif
            var actor = actorsWork.FirstOrDefault(c => c.ActorId == targetDataModel?.actorId);
            if (actor == null)
            {
                //パーティに存在しない場合
                //RuntimeActorDataModel取得
                var runtimeActorData = DataManager.Self().GetRuntimeSaveDataModel().runtimeActorDataModels.FirstOrDefault(c => c.actorId == targetDataModel?.actorId);

                //GameActor生成
                actor = new GameActor(runtimeActorData);
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
#else
                await actor.InitForConstructor(runtimeActorData);
#endif
            }

            var selectClass = DataManager.Self().GetClassDataModels()
                .FirstOrDefault(c => c.id == targetDataModel.classId);
            var maxHP = targetDataModel.GetCurrentMaxHp(selectClass);
            var minHP = knockout ? 0 : 1;
            targetDataModel.hp = Math.Max(minHP, Math.Min(maxHP, targetDataModel.hp + value));

            if (knockout && targetDataModel.hp == 0)
            {
                // HP=0の場合は戦闘不能のステートを付与 戦闘不能は固定で0
                // ただし、Uniteではステートを付与できるタイミングに制限を設けられるため、ステート付与可能かどうかをチェックする
                bool isKnockout = false;
                var stateDataModels = DataManager.Self().GetStateDataModels();

#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
                var actors = DataManager.Self().GetGameParty().Actors;
#else
                var actors = await DataManager.Self().GetGameParty().GetActors();
#endif
                for (int i = 0; i < actors.Count; i++)
                {
                    if (actors[i].ActorId == targetDataModel.actorId)
                    {
                        //ステートが付与可能なタイミングかどうかのチェック
                        if (actors[i].IsStateTiming(stateDataModels[0].id))
                        {
                            //ステート付与
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
                            actors[i].AddState(stateDataModels[0].id);
#else
                            await actors[i].AddState(stateDataModels[0].id);
#endif
                            isKnockout = true;
                        }
                    }
                }

                if (!isKnockout)
                {
                    //戦闘不能ステートを付与できなかったため、HPを1に戻す
                    targetDataModel.hp = 1;
                }
            }
        }
    }
}