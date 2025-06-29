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
    public class ActorChangeLevel
    {
        // データプロパティ
        //--------------------------------------------------------------------------------------------------------------
        private List<Variable> _databaseVariables;
        private SaveDataVariablesData _saveDataVariablesData;
        private List<CharacterActorDataModel> _characterActorData;
        private List<string> _messages;
        private Action _callback;

        /**
         * 初期化
         */
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
        public void Init(
#else
        public async Task Init(
#endif
            SaveDataVariablesData saveDataVariablesData,
            List<CharacterActorDataModel> characterActorData
        ) {
            _saveDataVariablesData = saveDataVariablesData;
            _characterActorData = characterActorData;
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
            _databaseVariables = new DatabaseManagementService().LoadFlags().variables;
#else
            _databaseVariables = (await new DatabaseManagementService().LoadFlags()).variables;
#endif
        }

#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
        public void ChangeLevel(EventDataModel.EventCommand command, Action callback) {
#else
        public async Task ChangeLevel(EventDataModel.EventCommand command, Action callback) {
#endif
            var isActorFixed = command.parameters[0] == "0";    // アクター(0:固定 1:変数)
            var actorParam = command.parameters[1];             // アクターID(-1:全員)
            var isAddValue = command.parameters[2] == "0";      // 操作(0:増やす 1:減らす)
            var isConstant = command.parameters[3] == "0";      // オペランドタイプ(0:定数 1:変数)
            var levelParam = command.parameters[4];             // オペランド(設定値 or 変数番号) 実際は変数番号ではなく変数uuid
            var isLevelUpEvent = command.parameters[5] != "0";  // レベルアップを表示(true or false)				

            _callback = callback;

            int levelValue = GetLevelValue();

            if (actorParam == "-1")
            {
                if (isLevelUpEvent)
                {
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
                    HudDistributor.Instance.NowHudHandler().OpenMessageWindow();
#else
                    await HudDistributor.Instance.NowHudHandler().OpenMessageWindow();
#endif
                }

#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
                ChangeLevelAllProcess(isAddValue ? levelValue : -levelValue, isLevelUpEvent);
#else
                await ChangeLevelAllProcess(isAddValue ? levelValue : -levelValue, isLevelUpEvent);
#endif

                if (isLevelUpEvent)
                {
                    TimeHandler.Instance.AddTimeActionFrame(1, ShowLevelupWindow, false);
                    return;
                }
            }
            else
            {
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
                var actorId = GetActorId();
#else
                var actorId = await GetActorId();
#endif
                if (actorId != null)
                {
                    if (isLevelUpEvent)
                    {
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
                        HudDistributor.Instance.NowHudHandler().OpenMessageWindow();
#else
                        await HudDistributor.Instance.NowHudHandler().OpenMessageWindow();
#endif
                    }

#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
                    ChangeLevelProcess(isAddValue ? levelValue : -levelValue, isLevelUpEvent, actorId);
#else
                    await ChangeLevelProcess(isAddValue ? levelValue : -levelValue, isLevelUpEvent, actorId);
#endif

                    if (isLevelUpEvent)
                    {
                        TimeHandler.Instance.AddTimeActionFrame(1, ShowLevelupWindow, false);
                        return;
                    }
                }
            }

            //ここまで到達した場合にはCBを即実行
            _callback();

            int GetLevelValue()
            {
                if (isConstant)
                {
                    // 定数
                    return int.Parse(levelParam);
                }
                else
                {
                    // 変数
                    var variableValue = GetSaveDataVariableValue(levelParam);
                    return variableValue != null ? int.Parse(variableValue) : 0/*変数が存在しなければ増減0とする*/;
                }
            }

#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
            string GetActorId()
#else
            async Task<string> GetActorId()
#endif
            {
                if (isActorFixed)
                {
                    // 固定
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
                    var runtimePartyMember = DataManager.Self().GetGameParty().Actors.FirstOrDefault(c => c.ActorId == actorParam);
#else
                    var runtimePartyMember = (await DataManager.Self().GetGameParty().GetActors()).FirstOrDefault(c => c.ActorId == actorParam);
#endif
                    if (runtimePartyMember == null)
                    {
                        //存在しないため新規作成
                        PartyChange partyChange = new PartyChange();
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
                        partyChange.SetActorData(actorParam);
#else
                        await partyChange.SetActorData(actorParam);
#endif
                    }

                    return actorParam;
                }
                else
                {
                    // 変数
                    var variableValue = GetSaveDataVariableValue(actorParam);
                    if (variableValue == null)
                    {
                        return null;
                    }

                    int actorSerialNo = int.Parse(variableValue);
                    var uuId = _characterActorData.FirstOrDefault(c => c.SerialNumber == actorSerialNo)?.uuId;
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
                    var runtimePartyMember = DataManager.Self().GetGameParty().Actors.FirstOrDefault(c => c.ActorId == uuId);
#else
                    var runtimePartyMember = (await DataManager.Self().GetGameParty().GetActors()).FirstOrDefault(c => c.ActorId == uuId);
#endif
                    if (runtimePartyMember == null)
                    {
                        //存在しないため新規作成
                        PartyChange partyChange = new PartyChange();
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
                        RuntimeActorDataModel actor = partyChange.SetActorData(uuId);
#else
                        RuntimeActorDataModel actor = await partyChange.SetActorData(uuId);
#endif

                        //GameActor生成
                        runtimePartyMember = new GameActor(actor);
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
#else
                        await runtimePartyMember.InitForConstructor(actor);
#endif
                    }
                    return runtimePartyMember?.ActorId;
                }
            }

            string GetSaveDataVariableValue(string variableId)
            {
                int variableIndex = _databaseVariables.FindIndex(v => v.id == variableId);
                return
                    variableIndex >= 0 || variableIndex < _saveDataVariablesData.data.Count ?
                        _saveDataVariablesData.data[variableIndex] : null;
            }
        }

        private void ShowLevelupWindow() {
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
#else
            _ = ShowLevelupWindowAsync();
        }
        private async Task ShowLevelupWindowAsync() {
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
        private void ChangeLevelAllProcess(int level, bool show) {
#else
        private async Task ChangeLevelAllProcess(int level, bool show) {
#endif
            _messages = new List<string>();

#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
            DataManager.Self().GetGameParty().AllMembers().ForEach(actor => {
#else
            foreach (var actor in await DataManager.Self().GetGameParty().AllMembers()) {
#endif
                actor.LevelupMessage = new List<string>();
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
                actor.ChangeLevel(level, false);
#else
                await actor.ChangeLevel(level, false);
#endif
                var text = actor.LevelUpText();
                for (int i = 0; i < actor.LevelupMessage.Count; i++)
                {
                    if ((i + 1) % 4 == 0)
                    {
                        _messages.Add(text);
                        text = actor.LevelupMessage[i];
                    }
                    else
                    {
                        text += "\\!\n" + actor.LevelupMessage[i];
                    }
                }
                _messages.Add(text);
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
            });
#else
            };
#endif
        }

#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
        private void ChangeLevelProcess(int level, bool show, string actorId) {
#else
        private async Task ChangeLevelProcess(int level, bool show, string actorId) {
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

            _messages = new List<string>();
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
            gameActor.ChangeLevel(level, false);
#else
            await gameActor.ChangeLevel(level, false);
#endif
            gameActor.LevelupMessage = new List<string>();
            var text = gameActor.LevelUpText();
            for (int i = 0; i < gameActor.LevelupMessage.Count; i++)
            {
                if ((i + 1) % 4 == 0)
                {
                    _messages.Add(text);
                    text += gameActor.LevelupMessage[i];
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