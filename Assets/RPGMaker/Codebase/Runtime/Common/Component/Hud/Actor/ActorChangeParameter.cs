using RPGMaker.Codebase.CoreSystem.Knowledge.DataModel.CharacterActor;
using RPGMaker.Codebase.CoreSystem.Knowledge.DataModel.Event;
using RPGMaker.Codebase.CoreSystem.Knowledge.DataModel.Runtime;
using RPGMaker.Codebase.CoreSystem.Service.DatabaseManagement;
using RPGMaker.Codebase.Runtime.Battle.Objects;
using RPGMaker.Codebase.Runtime.Common.Component.Hud.Party;
using System.Collections.Generic;
using System.Linq;
using static RPGMaker.Codebase.CoreSystem.Knowledge.DataModel.Flag.FlagDataModel;
using static RPGMaker.Codebase.CoreSystem.Knowledge.DataModel.Runtime.RuntimeSaveDataModel;
using System.Threading.Tasks;

namespace RPGMaker.Codebase.Runtime.Common.Component.Hud.Actor
{
    /// <summary>
    ///     [能力値の増減]の適応処理
    /// </summary>
    public class ActorChangeParameter
    {
        // データプロパティ
        //--------------------------------------------------------------------------------------------------------------
        private List<Variable> _databaseVariables;
        private List<RuntimeActorDataModel> _runtimeActorDataModels;
        private SaveDataVariablesData _saveDataVariablesData;
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
        public void ChangeParameter(EventDataModel.EventCommand command) {
#else
        public async Task ChangeParameter(EventDataModel.EventCommand command) {
#endif
            var isFixedValue = command.parameters[0] == "0";
            var parameter = int.Parse(command.parameters[2]);
            var isAddValue = command.parameters[3] == "0";
            var isConstant = command.parameters[4] == "0";

            var value = 0;
            if (isConstant)
            {
                if (!int.TryParse(command.parameters[5], out value))
                    return;
            }
            else
            {
                var index = _databaseVariables.FindIndex(v => v.id == command.parameters[5]);
                if (index == -1)
                    return;

                if (!int.TryParse(_saveDataVariablesData.data[index], out value))
                    return;
            }

            value = isAddValue ? value : -value;

            if (isFixedValue)
            {
                var actorId = command.parameters[1];
                if (actorId == "-1")
                {
                    // パーティ全体
                    foreach (var actorDataModel in _runtimeActorDataModels)
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
                        ChangeParameterProcess(parameter, actorDataModel, value);
#else
                        await ChangeParameterProcess(parameter, actorDataModel, value);
#endif

                    return;
                }

                // 個々のキャラクター
                var actor = _runtimeActorDataModels.FirstOrDefault(c => c.actorId == actorId);
                if (actor != null)
                {
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
                    ChangeParameterProcess(parameter, actor, value);
#else
                    await ChangeParameterProcess(parameter, actor, value);
#endif
                }
                else
                {
                    //存在しないため新規作成
                    PartyChange partyChange = new PartyChange();
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
                    actor = partyChange.SetActorData(actorId);
                    ChangeParameterProcess(parameter, actor, value);
#else
                    actor = await partyChange.SetActorData(actorId);
                    await ChangeParameterProcess(parameter, actor, value);
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
                                ChangeParameterProcess(parameter, _runtimeActorDataModels[index], value);
#else
                                await ChangeParameterProcess(parameter, _runtimeActorDataModels[index], value);
#endif
                            }
                            else
                            {
                                //存在しないため新規作成
                                PartyChange partyChange = new PartyChange();
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
                                RuntimeActorDataModel actor = partyChange.SetActorData(_characterActorData[indexActor].uuId);
                                ChangeParameterProcess(parameter, actor, value);
#else
                                RuntimeActorDataModel actor = await partyChange.SetActorData(_characterActorData[indexActor].uuId);
                                await ChangeParameterProcess(parameter, actor, value);
#endif
                            }
                        }
                    }
                }
            }
        }

#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
        private void ChangeParameterProcess(int parameter, RuntimeActorDataModel targetDataModel, int value) {
#else
        private async Task ChangeParameterProcess(int parameter, RuntimeActorDataModel targetDataModel, int value) {
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

#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
            actor.AddParam(parameter, value);
            actor.ResetActorData();
#else
            await actor.AddParam(parameter, value);
            await actor.ResetActorData();
#endif
        }
    }
}