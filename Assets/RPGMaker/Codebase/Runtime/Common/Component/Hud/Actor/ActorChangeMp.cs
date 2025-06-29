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
    public class ActorChangeMp
    {
        // const
        //--------------------------------------------------------------------------------------------------------------
        private const int            MP_MIN = 0;
        private       List<Variable> _databaseVariables;

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
        public void ChangeMP(EventDataModel.EventCommand command) {
#else
        public async Task ChangeMP(EventDataModel.EventCommand command) {
#endif
            var isFixedValue = command.parameters[0] == "0";
            var isAddValue = command.parameters[2] == "0";
            var isConstant = command.parameters[3] == "0";

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
                        ChangeMpProcess(actorDataModel, value);
#else
                        await ChangeMpProcess(actorDataModel, value);
#endif
                    return;
                }

                // 個々のキャラクター
                var actor = _runtimeActorDataModels.FirstOrDefault(c => c.actorId == actorId);
                if(actor != null)
                {
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
                    ChangeMpProcess(actor, value);
#else
                    await ChangeMpProcess(actor, value);
#endif
                }
                else
                {
                    //存在しないため新規作成
                    PartyChange partyChange = new PartyChange();
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
                    actor = partyChange.SetActorData(actorId);
                    ChangeMpProcess(actor, value);
#else
                    actor = await partyChange.SetActorData(actorId);
                    await ChangeMpProcess(actor, value);
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
                                ChangeMpProcess(_runtimeActorDataModels[index], value);
#else
                                await ChangeMpProcess(_runtimeActorDataModels[index], value);
#endif
                            }
                            else
                            {
                                //存在しないため新規作成
                                PartyChange partyChange = new PartyChange();
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
                                RuntimeActorDataModel actor = partyChange.SetActorData(_characterActorData[indexActor].uuId);
                                ChangeMpProcess(actor, value);
#else
                                RuntimeActorDataModel actor = await partyChange.SetActorData(_characterActorData[indexActor].uuId);
                                await ChangeMpProcess(actor, value);
#endif
                            }
                        }
                    }
                }
            }
        }

#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
        private void ChangeMpProcess(RuntimeActorDataModel targetDataModel, int value) {
#else
        private async Task ChangeMpProcess(RuntimeActorDataModel targetDataModel, int value) {
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
            var maxMP = targetDataModel.GetCurrentMaxMp(selectClass);

            targetDataModel.mp = Math.Max(MP_MIN, Math.Min(maxMP, targetDataModel.mp + value));
        }
    }
}