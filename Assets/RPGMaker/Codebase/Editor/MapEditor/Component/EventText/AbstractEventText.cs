using RPGMaker.Codebase.CoreSystem.Knowledge.DataModel.Enemy;
using RPGMaker.Codebase.CoreSystem.Knowledge.DataModel.Event;
using RPGMaker.Codebase.CoreSystem.Knowledge.DataModel.EventBattle;
using RPGMaker.Codebase.CoreSystem.Knowledge.DataModel.EventMap;
using RPGMaker.Codebase.CoreSystem.Knowledge.Enum;
using RPGMaker.Codebase.CoreSystem.Service.DatabaseManagement;
using RPGMaker.Codebase.CoreSystem.Service.EventManagement;
using RPGMaker.Codebase.Editor.Common;
using RPGMaker.Codebase.Editor.MapEditor.Window.EventEdit;
using RPGMaker.Codebase.Runtime.Common;
using System;
using System.Collections.Generic;
using UnityEngine.UIElements;

namespace RPGMaker.Codebase.Editor.MapEditor.Component.EventText
{
    public class AbstractEventText
    {
        protected DatabaseManagementService DatabaseManagementService;
        protected VisualElement             Element;
        protected EventManagementService    EventManagementService;
        protected Label                     LabelElement;
        protected string                    ret;

        protected AbstractEventText() {
            Element = new VisualElement();
            LabelElement = new Label();
            ret = "";

            DatabaseManagementService = Editor.Hierarchy.Hierarchy.databaseManagementService;
            EventManagementService = new EventManagementService();
        }

        protected ExecutionContentsWindow  ExecutionContentsWindow =>
            (ExecutionContentsWindow)WindowLayoutManager.GetActiveWindow(
                WindowLayoutManager.WindowLayoutId.MapEventExecutionContentsWindow);

        public virtual VisualElement Invoke(string indent, EventDataModel.EventCommand eventCommand) {
            return Element;
        }

        protected void AddFoldout(string indent, EventDataModel.EventCommand eventCommand) {
            Element.style.flexDirection = FlexDirection.Row;
            var indentLabel = new VisualElement();
            indentLabel.style.width = indent.Length * 3;
            Element.Add(indentLabel);

            var smallWidthVe = new VisualElement();
            smallWidthVe.style.width = 18;
            var state = (GetFolderParameter(eventCommand) == "1");
            var ud = new FolderStateChangeDetectableUserData(state);
            Element.userData = ud;
            var foldout = new Foldout();
            foldout.value = state;
            foldout.RegisterValueChangedCallback((evt) =>
            {
                state = foldout.value;
                SetFolderParameter(eventCommand, (state ? "1" : "0"));
                ud.State = state;
                evt.StopPropagation();
            });
            smallWidthVe.Add(foldout);
            Element.Add(smallWidthVe);

            Element.style.left = -5;
        }

        /// <summary>
        /// 対象キャラクター名を取得する。
        /// </summary>
        /// <param name="targetCharacterId">
        /// 対象キャラクターid。以下のいずれか。
        ///   "-2" プレイヤー。
        /// 　"-1" このイベント。
        /// 　イベントid。
        /// </param>
        /// <returns>対象キャラクター名</returns>
        protected string GetTargetCharacterName(string targetCharacterId)
        {
            var targetCharacter = new Commons.TargetCharacter(targetCharacterId);
            return targetCharacter.TargetType switch
            {
                Commons.TargetType.Player => EditorLocalize.LocalizeText("WORD_0860"),
                Commons.TargetType.ThisEvent => EditorLocalize.LocalizeText("WORD_0920"),
                _ => GetEventDisplayName(targetCharacter.TargetEventId)
            };
        }

        /// <summary>
        /// 画面に表示するイベントの名称を返却
        /// </summary>
        /// <returns></returns>
        protected string GetEventDisplayName(EventMapDataModel data) {
            if (data.name == "")
                return "EV" + string.Format("{0:D4}", data.SerialNumber) + " " + EditorLocalize.LocalizeText("WORD_1518");
            return "EV" + string.Format("{0:D4}", data.SerialNumber) + " " + data.name;
        }

        /// <summary>
        /// 画面に表示するイベントの名称を返却
        /// </summary>
        /// <returns></returns>
        public string GetEventDisplayName(string eventId) {
            var allEventMap = EventManagementService.LoadEventMap();

            //イベントIDからマップIDを検索
            foreach (var EventMap in allEventMap)
                if (EventMap.eventId == eventId)
                    return GetEventDisplayName(EventMap);

            return "";
        }

        /// <summary>
        /// バトルイベント関連で利用する、敵データのリスト取得
        /// </summary>
        /// <returns></returns>
        protected List<EnemyDataModel> GetEnemyList() {
            var enemyDataModels = DatabaseManagementService.LoadEnemy();
            var fileNames = new List<EnemyDataModel>();
            for (var i = 0; i < enemyDataModels.Count; i++)
            {
                if (enemyDataModels[i].deleted == 0)
                {
                    fileNames.Add(enemyDataModels[i]);
                }
            }

            return fileNames;
        }

        /// <summary>
        /// バトルイベント関連で利用する、敵の名称リスト取得
        /// </summary>
        /// <returns></returns>
        protected List<string> GetEnemyNameList(bool needAllEnemy = false) {
            var executionContentsWindowParam = ExecutionContentsWindow.ExecutionContentsWindowParam.instance;
            var enemies = new List<string>()
            {
                EditorLocalize.LocalizeText("WORD_1104")
            };
            if (!needAllEnemy) enemies.Clear();


            var battleEvent = EventManagementService.LoadEventBattle();
            EventBattleDataModel battleEventDataModel = null;
            for (int i = 0; i < battleEvent.Count; i++)
            {
                for (int j = 0; j < battleEvent[i].pages.Count; j++)
                {
                    if (battleEvent[i].pages[j].eventId == executionContentsWindowParam.eventId)
                    {
                        battleEventDataModel = battleEvent[i];
                        break;
                    }
                }
            }
            if (battleEventDataModel == null)
            {
                if (needAllEnemy)
                {
                    return new List<string> { EditorLocalize.LocalizeText("WORD_1104"), "", "", "", "", "", "", "", "" };
                }
                else
                {
                    return new List<string> { "", "", "", "", "", "", "", "" };
                }
            }

            var troopList = DatabaseManagementService.LoadTroop().Find(troop => troop.battleEventId == battleEventDataModel.eventId);
            var viewtype = DataManager.Self().GetSystemDataModel().battleScene.viewType;
            if (viewtype == 0)
            {
                //フロントビュー
                for (int i = 0; i < troopList?.frontViewMembers.Count; i++)
                {
                    var enemy = DatabaseManagementService.LoadEnemy()
                        .Find(e => e.id == troopList.frontViewMembers[i].enemyId);
                    enemies.Add(enemy.name);
                }
            }
            else
            {
                //サイドビュー
                for (int i = 0; i < troopList?.sideViewMembers.Count; i++)
                {
                    var enemy = DatabaseManagementService.LoadEnemy()
                        .Find(e => e.id == troopList.sideViewMembers[i].enemyId);
                    enemies.Add(enemy.name);
                }
            }

            return enemies;
        }

        static readonly Dictionary<int, int> CommandIdFolerParameterIndexDic = new Dictionary<int, int>()
        {
            {(int)EventEnum.EVENT_CODE_FLOW_IF, 51 },
            {(int)EventEnum.EVENT_CODE_FLOW_ELSE, 0 },
            {(int)EventEnum.EVENT_CODE_FLOW_LOOP, 1 },
            {(int)EventEnum.EVENT_CODE_FLOW_CUSTOM_MOVE, 0 },
            {(int)EventEnum.EVENT_CODE_SCENE_SET_BATTLE_CONFIG, 4 },
            {(int)EventEnum.EVENT_CODE_SCENE_SET_BATTLE_CONFIG_WIN, 0 },
            {(int)EventEnum.EVENT_CODE_SCENE_SET_BATTLE_CONFIG_LOSE, 0 },
            {(int)EventEnum.EVENT_CODE_SCENE_SET_BATTLE_CONFIG_ESCAPE, 0 },
            {(int)EventEnum.EVENT_CODE_MESSAGE_TEXT, 10 },
            {(int)EventEnum.EVENT_CODE_MESSAGE_TEXT_SCROLL, 3 },
            {(int)EventEnum.EVENT_CODE_SCENE_SET_SHOP_CONFIG, 5 },
            {(int)EventEnum.EVENT_CODE_ACTOR_CHANGE_NAME, 7 },
            {(int)EventEnum.EVENT_CODE_MESSAGE_INPUT_SELECT, 6 },
            {(int)EventEnum.EVENT_CODE_MESSAGE_INPUT_SELECT_SELECTED, 3 },
            {(int)EventEnum.EVENT_CODE_MESSAGE_INPUT_SELECT_CANCELED, 0 },
            {(int)EventEnum.EVENT_CODE_BATTLE_CHANGE_STATUS, 26 },
        };
        private static int GetFolderParameterIndex(EventDataModel.EventCommand eventCommand) {
            if (!CommandIdFolerParameterIndexDic.ContainsKey(eventCommand.code))
            {
                return -1;
            }
            return CommandIdFolerParameterIndexDic[eventCommand.code];
        }

        public static string GetFolderParameter(EventDataModel.EventCommand eventCommand) {
            var index = GetFolderParameterIndex(eventCommand);
            if (index < 0 || index >= eventCommand.parameters.Count) return "1";
            return eventCommand.parameters[index];
        }

        public static void SetFolderParameter(EventDataModel.EventCommand eventCommand, string value) {
            var index = GetFolderParameterIndex(eventCommand);
            if (index < 0) return;
            while (index >= eventCommand.parameters.Count)
            {
                eventCommand.parameters.Add("");
            }
            eventCommand.parameters[index] = value;
        }

        public class FolderStateChangeDetectableUserData
        {
            private bool _state;
            public event Action<bool> OnFolderStateChanged;

            public FolderStateChangeDetectableUserData(bool state) {
                _state = state;
            }

            public bool State
            {
                get => _state;
                set
                {
                    if (_state != value)
                    {
                        _state = value;
                        OnFolderStateChanged?.Invoke(_state);
                    }
                }
            }
        }
    }
}