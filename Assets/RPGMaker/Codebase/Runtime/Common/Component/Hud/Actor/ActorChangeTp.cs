using RPGMaker.Codebase.CoreSystem.Knowledge.DataModel.CharacterActor;
using RPGMaker.Codebase.CoreSystem.Knowledge.DataModel.Event;
using RPGMaker.Codebase.CoreSystem.Knowledge.DataModel.Runtime;
using RPGMaker.Codebase.CoreSystem.Service.DatabaseManagement;
using System;
using System.Collections.Generic;
using System.Linq;
using static RPGMaker.Codebase.CoreSystem.Knowledge.DataModel.Flag.FlagDataModel;
using static RPGMaker.Codebase.CoreSystem.Knowledge.DataModel.Runtime.RuntimeSaveDataModel;
using System.Threading.Tasks;

namespace RPGMaker.Codebase.Runtime.Common.Component.Hud.Actor
{
    public class ActorChangeTp
    {
        // const
        //--------------------------------------------------------------------------------------------------------------
        private const int            TP_MIN = 0;
        private const int            TP_MAX = 100;
        private       List<Variable> _databaseVariables;

        // データプロパティ
        //--------------------------------------------------------------------------------------------------------------
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
        public void ChangeTP(EventDataModel.EventCommand command) {
#else
        public async Task ChangeTP(EventDataModel.EventCommand command) {
#endif
            var isFixedValue = command.parameters[0] == "0" ? true : false;
            var isAddValue = command.parameters[2] == "0" ? true : false;
            var isConstant = command.parameters[3] == "0" ? true : false;

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
                        ChangeTpProcess(actorDataModel, value);
#else
                        await ChangeTpProcess(actorDataModel, value);
#endif
                    return;
                }

                // 個々のキャラクター
                var actor = _runtimeActorDataModels.FirstOrDefault(c => c.actorId == actorId);
                if(actor != null)
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
                    ChangeTpProcess(actor, value);
#else
                    await ChangeTpProcess(actor, value);
#endif
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
                                ChangeTpProcess(_runtimeActorDataModels[index], value);
#else
                                await ChangeTpProcess(_runtimeActorDataModels[index], value);
#endif
                            }
                        }
                    }
                }
            }
        }

#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
        private void ChangeTpProcess(RuntimeActorDataModel targetDataModel, int value) {
#else
        private async Task ChangeTpProcess(RuntimeActorDataModel targetDataModel, int value) {
#endif
            //パーティに指定したキャラが存在しない
            if (targetDataModel == null) return;
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
            var actorsWork = DataManager.Self().GetGameParty().Actors;
#else
            var actorsWork = await DataManager.Self().GetGameParty().GetActors();
#endif
            var actor = actorsWork.FirstOrDefault(c => c.ActorId == targetDataModel?.actorId);
            if (actor == null) return;

            targetDataModel.tp = Math.Max(TP_MIN, Math.Min(TP_MAX, targetDataModel.tp + value));
        }
    }
}