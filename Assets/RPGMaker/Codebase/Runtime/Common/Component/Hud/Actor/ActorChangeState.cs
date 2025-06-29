using RPGMaker.Codebase.CoreSystem.Knowledge.DataModel.CharacterActor;
using RPGMaker.Codebase.CoreSystem.Knowledge.DataModel.Event;
using RPGMaker.Codebase.CoreSystem.Knowledge.DataModel.Runtime;
using RPGMaker.Codebase.CoreSystem.Knowledge.DataModel.State;
using RPGMaker.Codebase.CoreSystem.Knowledge.DataModel.SystemSetting;
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
    public class ActorChangeState
    {
        private List<Variable> _databaseVariables;

        // データプロパティ
        //--------------------------------------------------------------------------------------------------------------
        private List<RuntimeActorDataModel> _runtimeActorDataModel;
        private SaveDataVariablesData _saveDataVariablesData;
        private List<CharacterActorDataModel> _characterActorData;

        /**
         * 初期化
         */
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
        public void Init(
#else
        public async Task Init(
#endif
            List<RuntimeActorDataModel> runtimeActorData,
            SaveDataVariablesData saveDataVariablesData,
            List<CharacterActorDataModel> characterActorData
        ) {
            _runtimeActorDataModel = runtimeActorData;
            _saveDataVariablesData = saveDataVariablesData;
            _characterActorData = characterActorData;

            var databaseManagementService = new DatabaseManagementService();
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
            _databaseVariables = databaseManagementService.LoadFlags().variables;
#else
            _databaseVariables = (await databaseManagementService.LoadFlags()).variables;
#endif
        }

#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
        public void ChangeState(StateDataModel state, EventDataModel.EventCommand command) {
#else
        public async Task ChangeState(StateDataModel state, EventDataModel.EventCommand command) {
#endif
            var isFixedValue = command.parameters[0] == "0" ? true : false;
            var actorId = command.parameters[1];
            var isAddValue = command.parameters[2] == "0" ? true : false;
            var index = 0;

            if (isFixedValue)
            {
                if (actorId == "-1") //パーティ全体
                {
                    for (var i = 0; i < _runtimeActorDataModel.Count; i++)
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
                        ChangeStateProcess(isAddValue, i, state);
#else
                        await ChangeStateProcess(isAddValue, i, state);
#endif
                }
                else //個々のキャラクター
                {
                    index = _runtimeActorDataModel.IndexOf(_runtimeActorDataModel.FirstOrDefault(c => c.actorId == actorId));
                    if (index != -1)
                    {
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
                        ChangeStateProcess(isAddValue, index, state);
#else
                        await ChangeStateProcess(isAddValue, index, state);
#endif
                    }
                    else
                    {
                        //存在しないため新規作成
                        PartyChange partyChange = new PartyChange();
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
                        partyChange.SetActorData(actorId);
                        ChangeStateProcess(isAddValue,
#else
                        await partyChange.SetActorData(actorId);
                        await ChangeStateProcess(isAddValue,
#endif
                            _runtimeActorDataModel.IndexOf(_runtimeActorDataModel.FirstOrDefault(c => c.actorId == actorId)), state);
                    }
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
                            index = _runtimeActorDataModel.IndexOf(_runtimeActorDataModel.FirstOrDefault(c => c.actorId == _characterActorData[indexActor].uuId));
                            if (index >= 0)
                            {
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
                                ChangeStateProcess(isAddValue, index, state);
#else
                                await ChangeStateProcess(isAddValue, index, state);
#endif
                            }
                            else
                            {
                                //存在しないため新規作成
                                PartyChange partyChange = new PartyChange();
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
                                partyChange.SetActorData(actorId);
                                ChangeStateProcess(isAddValue,
#else
                                await partyChange.SetActorData(actorId);
                                await ChangeStateProcess(isAddValue,
#endif
                                    _runtimeActorDataModel.IndexOf(_runtimeActorDataModel.FirstOrDefault(c => c.actorId == _characterActorData[indexActor].uuId)), state);
                            }
                        }
                    }
                }
            }
        }

#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
        private void ChangeStateProcess(bool isAddValue, int index, StateDataModel value) {
#else
        private async Task ChangeStateProcess(bool isAddValue, int index, StateDataModel value) {
#endif
            bool flg = false;
            if (isAddValue)
            {
                //GameActorを検索する
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
                var actors = DataManager.Self().GetGameParty().Actors;
#else
                var actors = await DataManager.Self().GetGameParty().GetActors();
#endif
                for (int i = 0; i < actors.Count; i++)
                {
                    if (actors[i].ActorId == _runtimeActorDataModel[index].actorId)
                    {
                        //ステートが付与可能なタイミングかどうかのチェック
                        if (actors[i].IsStateTiming(value.id))
                        {
                            //ステート付与
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
                            actors[i].AddState(value.id);
#else
                            await actors[i].AddState(value.id);
#endif
                            flg = true;
                            //装備の反映
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
                            ResetEquipment(index, actors[i]);
#else
                            await ResetEquipment(index, actors[i]);
#endif
                        }
                    }
                }
                if (!flg)
                {
                    //パーティに存在しない場合
                    GameActor actor = new GameActor(_runtimeActorDataModel[index]);
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
#else
                    await actor.InitForConstructor(_runtimeActorDataModel[index]);
#endif
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
                    actor.AddState(value.id);
#else
                    await actor.AddState(value.id);
#endif
                }
            }
            else
            {
                //GameActorを検索する
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
                var actors = DataManager.Self().GetGameParty().Actors;
#else
                var actors = await DataManager.Self().GetGameParty().GetActors();
#endif
                for (int i = 0; i < actors.Count; i++)
                {
                    if (actors[i].ActorId == _runtimeActorDataModel[index].actorId)
                    {
                        //ステート解除
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
                        actors[i].RemoveState(value.id);
#else
                        await actors[i].RemoveState(value.id);
#endif
                        flg = true;
                        //装備の反映
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
                        ResetEquipment(index, actors[i]);
#else
                        await ResetEquipment(index, actors[i]);
#endif
                    }
                }
                if (!flg)
                {
                    //パーティに存在しない場合
                    GameActor actor = new GameActor(_runtimeActorDataModel[index]);
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
#else
                    await actor.InitForConstructor(_runtimeActorDataModel[index]);
#endif
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
                    actor.RemoveState(value.id);
#else
                    await actor.RemoveState(value.id);
#endif
                }
            }
        }

#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
        private void ResetEquipment(int index, GameActor actor) {
#else
        private async Task ResetEquipment(int index, GameActor actor) {
#endif
            //アクターが装備するものを頭から順にチェックしなおし
            SystemSettingDataModel systemSettingDataModel = DataManager.Self().GetSystemDataModel();
            for (var j = 0; j < _runtimeActorDataModel[index].equips.Count; j++)
            {
                //装備種別を取得
                SystemSettingDataModel.EquipType equipType = null;
                for (int j2 = 0; j2 < systemSettingDataModel.equipTypes.Count; j2++)
                    if (systemSettingDataModel.equipTypes[j2].id == _runtimeActorDataModel[index].equips[j].equipType)
                    {
                        equipType = systemSettingDataModel.equipTypes[j2];
                        break;
                    }

                //装備が封印されているかどうか
                bool ret = actor.IsEquipTypeSealed(j);
                if (ret)
                {
                    //装備を外す
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
                    ItemManager.RemoveEquipment(_runtimeActorDataModel[index], equipType, j);
#else
                    await ItemManager.RemoveEquipment(_runtimeActorDataModel[index], equipType, j);
#endif
                }
            }
            //GameActorへ反映
            //装備封印以外の要因で、装備を外すことになった場合は、以下の処理内で外れる
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
            actor.ResetActorData();
#else
            await actor.ResetActorData();
#endif
        }
    }
}