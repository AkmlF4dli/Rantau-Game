using RPGMaker.Codebase.CoreSystem.Knowledge.DataModel.CharacterActor;
using RPGMaker.Codebase.CoreSystem.Knowledge.DataModel.Class;
using RPGMaker.Codebase.CoreSystem.Knowledge.DataModel.Runtime;
using RPGMaker.Codebase.CoreSystem.Service.DatabaseManagement;
using RPGMaker.Codebase.Runtime.Battle.Objects;
using System.Collections.Generic;
using System.Linq;
using static RPGMaker.Codebase.CoreSystem.Knowledge.DataModel.Flag.FlagDataModel;
using static RPGMaker.Codebase.CoreSystem.Knowledge.DataModel.Runtime.RuntimeSaveDataModel;
using System.Threading.Tasks;

namespace RPGMaker.Codebase.Runtime.Common.Component.Hud.Actor
{
    public class ActorHeal
    {
        // データプロパティ
        //--------------------------------------------------------------------------------------------------------------
        private List<Variable> _databaseVariables;
        private List<ClassDataModel>        _classDataModels;
        private List<RuntimeActorDataModel> _runtimeActorDataModel;
        private List<CharacterActorDataModel> _characterActorData;
        private SaveDataVariablesData _saveDataVariablesData;


        /**
         * 初期化
         */
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
        public void Init(
#else
        public async Task Init(
#endif
            List<ClassDataModel> classDatas,
            List<RuntimeActorDataModel> runtimeActorData,
            SaveDataVariablesData saveDataVariablesData
        ) {
            _classDataModels = classDatas;
            _runtimeActorDataModel = runtimeActorData;
            _saveDataVariablesData = saveDataVariablesData;

            var databaseManagementService = new DatabaseManagementService();
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
            _databaseVariables = databaseManagementService.LoadFlags().variables;
#else
            _databaseVariables = (await databaseManagementService.LoadFlags()).variables;
#endif

            _characterActorData = DataManager.Self().GetActorDataModels();
        }

#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
        public void Heal(int type, string id) {
#else
        public async Task Heal(int type, string id) {
#endif
            if (type == 0)
            {
                if (id == "-1")
                {
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
                    AllHeal();
#else
                    await AllHeal();
#endif
                }
                else
                {
                    var index = _runtimeActorDataModel.IndexOf(_runtimeActorDataModel.FirstOrDefault(c => c.actorId == id));
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
                    if(index >= 0) HealProcess(index);
#else
                    if (index >= 0) await HealProcess(index);
#endif
                }
            }
            else
            {
                //MVの挙動から
                //変数内の数値によって経験値を変動させるのは、該当のIDのユーザー（=SerialNoが一致するアクター）
                int variableIndex = _databaseVariables.FindIndex(v => v.id == id);
                if (variableIndex >= 0)
                {
                    int actorSerialNo = int.Parse(_saveDataVariablesData.data[variableIndex]);
                    if (actorSerialNo >= 0)
                    {
                        int indexActor = _characterActorData.IndexOf(_characterActorData.FirstOrDefault(c => c.SerialNumber == actorSerialNo));
                        if (indexActor >= 0)
                        {
                            int index = _runtimeActorDataModel.IndexOf(_runtimeActorDataModel.FirstOrDefault(c => c.actorId == _characterActorData[indexActor].uuId));
                            if (index >= 0)
                            {
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
                                HealProcess(index);
#else
                                await HealProcess(index);
#endif
                            }
                        }
                    }
                }
            }
        }

#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
        private void AllHeal() {
#else
        private async Task AllHeal() {
#endif
            for (var i = 0; i < _runtimeActorDataModel.Count; i++)
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
                HealProcess(i);
#else
                await HealProcess(i);
#endif
        }

#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
        private void HealProcess(int index) {            
#else
        private async Task HealProcess(int index) {
#endif
            var selectClass = _classDataModels.FirstOrDefault(c => c.id == _runtimeActorDataModel[index].classId);
            var maxHP = _runtimeActorDataModel[index].GetCurrentMaxHp(selectClass);
            var maxMP = _runtimeActorDataModel[index].GetCurrentMaxMp(selectClass);
            _runtimeActorDataModel[index].hp = maxHP;
            _runtimeActorDataModel[index].mp = maxMP;
            _runtimeActorDataModel[index].states.Clear();

            //GameActorを検索する
            bool flg = false;
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
                    actors[i].ClearStates();
                    flg = true;
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
                actor.ClearStates();
            }
        }
    }
}