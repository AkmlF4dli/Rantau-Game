using RPGMaker.Codebase.CoreSystem.Knowledge.DataModel.CharacterActor;
using RPGMaker.Codebase.CoreSystem.Knowledge.DataModel.Event;
using RPGMaker.Codebase.CoreSystem.Knowledge.DataModel.Runtime;
using RPGMaker.Codebase.CoreSystem.Service.DatabaseManagement;
using RPGMaker.Codebase.Runtime.Battle.Objects;
using RPGMaker.Codebase.Runtime.Common.Component.Hud.Party;
using RPGMaker.Codebase.Runtime.Common.Enum;
using System;
using System.Collections.Generic;
using System.Linq;
using static RPGMaker.Codebase.CoreSystem.Knowledge.DataModel.Flag.FlagDataModel;
using static RPGMaker.Codebase.CoreSystem.Knowledge.DataModel.Runtime.RuntimeSaveDataModel;
using System.Threading.Tasks;

namespace RPGMaker.Codebase.Runtime.Common.Component.Hud.Actor
{
    public class ActorChangeExp
    {
        // データプロパティ
        //--------------------------------------------------------------------------------------------------------------
        private List<RuntimeActorDataModel> _runtimeActorDataModel;
        private List<Variable> _databaseVariables;
        private SaveDataVariablesData _saveDataVariablesData;
        private List<CharacterActorDataModel> _characterActorData;
        private List<string> _messages;
        private Action _callback;

        // 状態プロパティ
        //--------------------------------------------------------------------------------------------------------------
        public bool isLevelUp;

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
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
#else
            await UniteTask.Delay(0);
#endif
            _runtimeActorDataModel = runtimeActorData;
            _saveDataVariablesData = saveDataVariablesData;
            _characterActorData = characterActorData;
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
            _databaseVariables = new DatabaseManagementService().LoadFlags().variables;
#else
            _databaseVariables = (await new DatabaseManagementService().LoadFlags()).variables;
#endif
        }

#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
        public void ChangeEXP(EventDataModel.EventCommand command, Action callback) {
#else
        public async Task ChangeEXP(EventDataModel.EventCommand command, Action callback) {
            await UniteTask.Delay(0);
#endif
            var isFixedValue = command.parameters[0] == "0";
            var actorId = command.parameters[1];
            var isAddValue = command.parameters[2] == "0";
            var isConstant = command.parameters[3] == "0";
            var isLevelUpEvent = command.parameters[5] == "1";

            var index = 0;
            var value = 0;
            _callback = callback;

            if (isConstant)
            {
                value = int.Parse(command.parameters[4]);
            }
            else
            {
                if (_saveDataVariablesData.data.Count == 0)
                    return;

#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
                var variables = new DatabaseManagementService().LoadFlags().variables;
#else
                var variables = (await new DatabaseManagementService().LoadFlags()).variables;
#endif
                value = variables.FindIndex(x => x.id == command.parameters[4]);

                var variablesNum = int.Parse(_saveDataVariablesData.data[value]);
                value = variablesNum;
            }

            if (isFixedValue)
            {
                if (actorId == "-1") //パーティ全体
                {
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
                    ChangeEXPAllProcess(isAddValue ? value : -value);
#else
                    await ChangeEXPAllProcess(isAddValue ? value : -value);
#endif
                    if (isLevelUpEvent && isLevelUp)
                    {
                        TimeHandler.Instance.AddTimeActionFrame(1, ShowLevelupWindow, false);
                        return;
                    }
                }
                else //個々のキャラクター
                {
                    index = _runtimeActorDataModel.IndexOf(_runtimeActorDataModel.FirstOrDefault(c => c.actorId == actorId));
                    if (index >= 0)
                    {
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
                        ChangeEXPProcess(isAddValue ? value : -value, actorId);
#else
                        await ChangeEXPProcess(isAddValue ? value : -value, actorId);
#endif
                        if (isLevelUpEvent && isLevelUp)
                        {
                            TimeHandler.Instance.AddTimeActionFrame(1, ShowLevelupWindow, false);
                            return;
                        }
                    }
                    else
                    {
                        //存在しないため新規作成
                        PartyChange partyChange = new PartyChange();
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
                        RuntimeActorDataModel actor = partyChange.SetActorData(actorId);
                        ChangeEXPProcess(isAddValue ? value : -value, actorId);
#else
                        RuntimeActorDataModel actor = await partyChange.SetActorData(actorId);
                        await ChangeEXPProcess(isAddValue ? value : -value, actorId);
#endif
                        if (isLevelUpEvent && isLevelUp)
                        {
                            TimeHandler.Instance.AddTimeActionFrame(1, ShowLevelupWindow, false);
                            return;
                        }
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
                                ChangeEXPProcess(isAddValue ? value : -value, _runtimeActorDataModel[index].actorId);
#else
                                await ChangeEXPProcess(isAddValue ? value : -value, _runtimeActorDataModel[index].actorId);
#endif
                                if (isLevelUpEvent && isLevelUp)
                                {
                                    TimeHandler.Instance.AddTimeActionFrame(1, ShowLevelupWindow, false);
                                    return;
                                }
                            }
                            else
                            {
                                //存在しないため新規作成
                                PartyChange partyChange = new PartyChange();
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
                                RuntimeActorDataModel actor = partyChange.SetActorData(_characterActorData[indexActor].uuId);
                                ChangeEXPProcess(isAddValue ? value : -value, actor.actorId);
#else
                                RuntimeActorDataModel actor = await partyChange.SetActorData(_characterActorData[indexActor].uuId);
                                await ChangeEXPProcess(isAddValue ? value : -value, actor.actorId);
#endif
                                if (isLevelUpEvent && isLevelUp)
                                {
                                    TimeHandler.Instance.AddTimeActionFrame(1, ShowLevelupWindow, false);
                                    return;
                                }
                            }
                        }
                    }
                }
            }

            //ここまで到達した場合にはCBを即実行
            _callback();
        }

        private void ShowLevelupWindow() {
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
#else
            _ = ShowLevelupWindowAsymc();
        }
        private async Task ShowLevelupWindowAsymc() {
#endif
            //次に表示するメッセージ
            var text = _messages[0];
            _messages.RemoveAt(0);

            //表示処理
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
            HudDistributor.Instance.NowHudHandler().OpenMessageWindow();
#else
            await HudDistributor.Instance.NowHudHandler().OpenMessageWindow();
#endif
            HudDistributor.Instance.NowHudHandler().SetShowMessage(text);

            //InputHandlerへの登録
            InputDistributor.AddInputHandler(GameStateHandler.IsMap() ? GameStateHandler.GameState.EVENT : GameStateHandler.GameState.BATTLE_EVENT, HandleType.Decide, DecideEvent);
            InputDistributor.AddInputHandler(GameStateHandler.IsMap() ? GameStateHandler.GameState.EVENT : GameStateHandler.GameState.BATTLE_EVENT, HandleType.LeftClick, DecideEvent);
        }

        private void DecideEvent() {
            if (HudDistributor.Instance.NowHudHandler().IsInputWait())
            {
                HudDistributor.Instance.NowHudHandler().Next();
                return;
            }
            if (!HudDistributor.Instance.NowHudHandler().IsInputEnd())
            {
                return;
            }

            //InputHandler削除
            InputDistributor.RemoveInputHandler(GameStateHandler.IsMap() ? GameStateHandler.GameState.EVENT : GameStateHandler.GameState.BATTLE_EVENT, HandleType.Decide, DecideEvent);
            InputDistributor.RemoveInputHandler(GameStateHandler.IsMap() ? GameStateHandler.GameState.EVENT : GameStateHandler.GameState.BATTLE_EVENT, HandleType.LeftClick, DecideEvent);

            if (_messages.Count > 0)
            {
                TimeHandler.Instance.AddTimeActionFrame(1, ShowLevelupWindow, false);
            }
            else
            {
                HudDistributor.Instance.NowHudHandler().CloseMessageWindow();
                _callback();
            }
        }

#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
        private void ChangeEXPAllProcess(int exp) {
#else
        private async Task ChangeEXPAllProcess(int exp) {
#endif
            _messages = new List<string>();
            List<int> beforeLevel = new List<int>();
            List<int> afterLevel = new List<int>();

#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
            DataManager.Self().GetGameParty().AllMembers().ForEach(actor =>
#else
            foreach (var actor in await DataManager.Self().GetGameParty().AllMembers())
#endif
            {
                //事前のレベルの取得
                beforeLevel.Add(actor.Level);
                //経験値変動
                actor.LevelupMessage = new List<string>();
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
                actor.GainExp(exp, false);
#else
                await actor.GainExp(exp, false);
#endif
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
            });
#else
            };
#endif

            //事後のレベルの取得
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
            DataManager.Self().GetGameParty().AllMembers().ForEach(actor => { 
#else
            (await DataManager.Self().GetGameParty().AllMembers()).ForEach(actor => { 
#endif
                afterLevel.Add(actor.Level);
            });

            //レベルが上がったかの確認
            isLevelUp = false;
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
#else
            var actors = await DataManager.Self().GetGameParty().GetActors();
#endif
            for (int i = 0; i < beforeLevel.Count; i++)
            {
                if (beforeLevel[i] < afterLevel[i])
                {
                    isLevelUp = true;
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
                    var text = DataManager.Self().GetGameParty().Actors[i].LevelUpText();
                    for (int j = 0; j < DataManager.Self().GetGameParty().Actors[i].LevelupMessage.Count; j++)
#else
                    var text = actors[i].LevelUpText();
                    for (int j = 0; j < actors[i].LevelupMessage.Count; j++)
#endif
                    {
                        if ((j + 1) % 4 == 0)
                        {
                            _messages.Add(text);
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
                            text = DataManager.Self().GetGameParty().Actors[i].LevelupMessage[j];
#else
                            text = actors[i].LevelupMessage[j];
#endif
                        }
                        else
                        {
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
                            text += "\\!\n" + DataManager.Self().GetGameParty().Actors[i].LevelupMessage[j];
#else
                            text += "\\!\n" + actors[i].LevelupMessage[j];
#endif
                        }
                    }
                    _messages.Add(text);
                }
            }
        }

#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
        private void ChangeEXPProcess(int exp, string actorId) {
#else
        private async Task ChangeEXPProcess(int exp, string actorId) {
#endif
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
            var index = DataManager.Self().GetGameParty().Actors
                .IndexOf(DataManager.Self().GetGameParty().Actors.FirstOrDefault(c => c.ActorId == actorId));
#else
            var actors = await DataManager.Self().GetGameParty().GetActors();
            var index = actors
                .IndexOf(actors.FirstOrDefault(c => c.ActorId == actorId));
#endif

            GameActor gameActor;
            if (index != -1)
            {
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
                gameActor = DataManager.Self().GetGameParty().Actors[index];
#else
                gameActor = actors[index];
#endif
            }
            else
            {
                //パーティに存在しない場合
                //RuntimeActorDataModel取得
                var actor = DataManager.Self().GetRuntimeSaveDataModel().runtimeActorDataModels.FirstOrDefault(c => c.actorId == actorId);

                //GameActor生成
                gameActor = new GameActor(actor);
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
#else
                await gameActor.InitForConstructor(actor);
#endif
            }

            //事前のレベルを取得
            var beforeLevel = gameActor.Level;
            //経験値変動
            gameActor.LevelupMessage = new List<string>();
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
            gameActor.GainExp(exp, false);
#else
            await gameActor.GainExp(exp, false);
#endif
            //レベルが上がったかの確認
            isLevelUp = beforeLevel < gameActor.Level;

            _messages = new List<string>();
            if (isLevelUp)
            {
               var text = gameActor.LevelUpText();
                for (int i = 0; i < gameActor.LevelupMessage.Count; i++)
                {
                    if ((i + 1) % 4 == 0)
                    {
                        _messages.Add(text);
                        text = gameActor.LevelupMessage[i];
                    }
                    else
                    {
                        text += "\\!\n" + gameActor.LevelupMessage[i];
                    }
                }
                _messages.Add(text);
            }
        }
    }
}