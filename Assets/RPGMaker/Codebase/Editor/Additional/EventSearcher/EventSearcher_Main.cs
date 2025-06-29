using System;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEditor.UIElements;
using UnityEngine.UIElements;
using RPGMaker.Codebase.CoreSystem.Service.EventManagement;
using RPGMaker.Codebase.CoreSystem.Knowledge.Enum;
using RPGMaker.Codebase.CoreSystem.Knowledge.DataModel.EventMap;
using RPGMaker.Codebase.Editor.Hierarchy.Common;
using RPGMaker.Codebase.CoreSystem.Knowledge.DataModel.Event;
using RPGMaker.Codebase.CoreSystem.Service.MapManagement.Repository;
using RPGMaker.Codebase.CoreSystem.Service.EventManagement.Repository;
using RPGMaker.Codebase.CoreSystem.Service.DatabaseManagement.Repository;
using RPGMaker.Codebase.Editor.Common;
using RPGMaker.Codebase.Editor.Common.View;

[Serializable]
public class EventSearcher_Main : EditorWindow
{
    [MenuItem("RPG Maker/EventSearcher", priority = 803)]
    public static void ShowEventSearcher() {
        EventSearcher_Main wnd = GetWindow<EventSearcher_Main>();
        var title = EditorLocalize.LocalizeText("WORD_1642");
        wnd.titleContent = new GUIContent(title);
    }

    // 検索モード
    enum SearchMode
    {
        Switch,
        Variable,
        Type,
        FreeWord
    }

    // 検索結果
    [Serializable]
    class SearchResult : ScriptableObject
    {
        // 親のイベントID
        public string EventID = "";
        // イベント種類(コモン/マップ/バトル)
        public string EventType = "";
        // イベント名
        public string EventName = "";
        // イベント情報
        public string EventParam = "";
        // イベント情報
        public string EventInformaton = "";
        // イベントのヒエラルキー上のボタン名称
        public string EventHierarchyButtonName = "";
    }

    // 検索モードごとの指定値
    [SerializeField] string mSelectedSwitchDisplayName = "";
    [SerializeField] string mSelectedValueDisplayName = "";
    [SerializeField] string mSelectedEventTypeDisplayName = "";
    [SerializeField] string mSelectedWords = "";

    // スイッチの選択中インデクス
    int SelectedSwitchIndex
    {
        get => mSelectedSwitchIndex;
        set
        {
            mSelectedSwitchIndex = value;
            var name = mFlagRepository.Load().switches[value].name;
            name = name == "" ? "(blank name)" : name;
            mSelectedSwitchDisplayName = string.Format("#{0:d4}:{1}", value + 1, name);
        }
    }
    // 変数の選択中インデクス
    int SelectedValueIndex
    {
        get => mSelectedValueIndex;
        set
        {
            mSelectedValueIndex = value;
            var name = mFlagRepository.Load().variables[value].name;
            name = name == "" ? "(blank name)" : name;
            mSelectedValueDisplayName = string.Format("#{0:d4}:{1}", value + 1, name);
        }
    }

    // イベントの表示テキストを構成するためのコンポーネント
    GetEventCommandLabelText mEventLabelStringBuilder = new GetEventCommandLabelText();
    // 選択した検索モード
    SearchMode mSearchMode = SearchMode.Switch;
    // 直接アクセス禁止
    int mSelectedSwitchIndex = 0;
    int mSelectedValueIndex = 0;
    // 選択中モード
    EventEnum mSelectedEnum = EventEnum.EVENT_CODE_MESSAGE_TEXT;

    // 各種リポジトリ：内部的にはStaticで変数を握っているのでLoad呼び出しは軽量、本来的にはNewの必要はない
    FlagsRepository mFlagRepository = new FlagsRepository();
    EventRepository mEventRepository = new EventRepository();
    EventCommonRepository mEventCommonRepository = new EventCommonRepository();
    EventMapRepository mEventMapRepository = new EventMapRepository();
    EventBattleRepository mEventBattleRepository = new EventBattleRepository();
    TroopRepository mTroopRepository = new TroopRepository();
    MapRepository mMapRepository = new MapRepository();
    EnemyRepository mEnemyRepository = new EnemyRepository();

    /// <summary>
    /// エレメントの翻訳実行処理
    /// TextElementをすべて処理するので重い場合はありそう
    /// またシステムのローカライズも通すので二度手間な面も…
    /// </summary>
    /// <param name="root">ルートになるVisualElement</param>
    static void LocalizeText(VisualElement root) {
        root.Query<TextElement>()
            .Where(element => element.text.StartsWith("[W"))
            .ForEach(element =>
                {
                    var code = element.text.Substring(2, 4);
                    element.text = EditorLocalize.LocalizeText($"WORD_{code}");
                });
    }

    /// <summary>
    /// 
    /// </summary>
    public void CreateGUI() {
        // メインUXML読込
        var mainUXMLTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/RPGMaker/Codebase/Editor/Additional/EventSearcher/EventSearcher_Main.uxml");
        // スタイルシート読込
        var styleSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>("Assets/RPGMaker/Codebase/Editor/Additional/EventSearcher/EventSearcher_Main.uss");
        // リザルトのカラム読込
        var resultColumnTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/RPGMaker/Codebase/Editor/Additional/EventSearcher/EventSearch_ResultColumn.uxml");

        // UIの構築
        rootVisualElement.Add(mainUXMLTree.Instantiate());
        LocalizeText(rootVisualElement);

        // 検索エリア取得
        var searchArea = rootVisualElement.Q<VisualElement>("SearchSettingArea");
        searchArea.Bind(new SerializedObject(this));

        // 検索結果オブジェクト
        var resultList = new List<SearchResult>();

        // 検索結果リストビューの初期化
        var listView = rootVisualElement.Q<ListView>();
        listView.makeItem = () =>
        {
            var item = resultColumnTree.Instantiate();
            // TODO:毎回ローカライズコンバートしているので今後処理コストの改善を
            LocalizeText(item);
            return item;
        };
        listView.bindItem = (item, Index) =>
        {
            var result = resultList[Index];
            item.Bind(new SerializedObject(result));
            var bt = item.Q<Button>();
            bt.SetEnabled(!string.IsNullOrEmpty(result.EventHierarchyButtonName));
            bt.clicked += () =>
            {
                // ヒエラルキー中の該当する項目を選択。
                RPGMaker.Codebase.Editor.Hierarchy.Hierarchy.SelectButton(result.EventHierarchyButtonName);
            };
        };

        // スイッチの検索ボタン初期化
        RadioButtonSetUP("RadioButton_SwitchSearch",
            () => mSearchMode = SearchMode.Switch,
            () => EventSearch_SelectValue.OpenModalWindow("SelectSwitch", mFlagRepository.Load().switches.Select(v => v.name).ToList(), select => SelectedSwitchIndex = select)
            , true);
        SelectedSwitchIndex = 0;

        // 値の検索ボタン初期化
        RadioButtonSetUP("RadioButton_VariableSearch",
            () => mSearchMode = SearchMode.Variable,
            () => EventSearch_SelectValue.OpenModalWindow("SelectVareable", mFlagRepository.Load().variables.Select(v => v.name).ToList(), select => SelectedValueIndex = select));
        SelectedValueIndex = 0;

        // タイプの検索ボタン初期化
        var typeStringList = new List<string>();
        mEventCodeToStringCode.ForEach(t =>
        {
            typeStringList.Add(EditorLocalize.LocalizeText(t.Item2));
        });

        RadioButtonSetUP("RadioButton_EventTypeSearch",
            () => mSearchMode = SearchMode.Type,
            () => EventSearch_SelectValue.OpenModalWindow("SelectType", typeStringList, select =>
                {
                    mSelectedEnum = mEventCodeToStringCode[select].Item1;
                    mSelectedEventTypeDisplayName = typeStringList[select];
                })
            );
        mSelectedEnum = mEventCodeToStringCode[0].Item1;
        mSelectedEventTypeDisplayName = EditorLocalize.LocalizeText(mEventCodeToStringCode[0].Item2);

        // テキストの検索ボタン初期化
        RadioButtonSetUP("RedioButton_TextSearch",
            () => mSearchMode = SearchMode.FreeWord,
            default
        );

        // 検索するものの種類は
        //・スイッチの参照/編集
        //・値の参照/編集
        //・イベントのタイプ
        //・自由文言：イベント名とテキスト？
        var bts = rootVisualElement.Q<Button>("Button_Search");
        bts.RegisterCallback<ClickEvent>(v =>
        {
            resultList.Clear();
            var eventDataModelList = mEventRepository.Load();
            switch (mSearchMode)
            {
                case SearchMode.Switch:
                    // 単体選択のイベントの場合はIDで特定することになる
                    var switchID = mFlagRepository.Load().switches[SelectedSwitchIndex].id;
                    // データ上の値はindex+1になる
                    var selectedCode = SelectedSwitchIndex + 1;
                    eventDataModelList.ForEach(ed =>
                    {
                        // スイッチの編集イベントの抽出
                        ed.eventCommands.ForEach(ev =>
                        {
                            switch (ev.code)
                            {
                                case (int) EventEnum.EVENT_CODE_GAME_SWITCH:
                                    // スイッチの判別
                                    if (ev.parameters[0] == "0")
                                    {
                                        //・パラメータの0番目が0の場合は個別指定
                                        //・その場合はパラメータの1番目がスイッチのユニークID
                                        //・2番目のパラメータは不明…
                                        //・3番目のパラメータが0の場合はON、1の場合はOFF
                                        if (ev.parameters[1] != switchID) return;
                                    }
                                    else
                                    {
                                        //・パラメータの0番目が1の場合は範囲指定
                                        //・その場合はパラメータの1番目の数字から2番目の数字までのスイッチを操作する
                                        //・3番目のパラメータが0の場合はON、1の場合はOFF
                                        // IDが文字列なのでパースする必要がある
                                        int.TryParse(ev.parameters[1], out var startCode);
                                        int.TryParse(ev.parameters[2], out var endCode);
                                        if (selectedCode < startCode || endCode < selectedCode) return;
                                    }
                                    break;
                                case (int) EventEnum.EVENT_CODE_FLOW_IF:
                                    if (ev.parameters[3] == "0") return;
                                    if (ev.parameters[4] != switchID) return;
                                    break;
                                default:
                                    return;
                            }
                            resultList.Add(CreateEventSearchResult(ed, ev));
                        });
                    });

                    // マップイベントでも使用している場合があるので検出する
                    // EventMapDataModel.pages.condition.switchOne.switchId
                    // EventMapDataModel.pages.condition.switchTwo.switchId
                    mEventMapRepository.Load().ForEach(mp =>
                        mp.pages.ForEach(page =>
                        {
                            if ((page.condition.switchOne.enabled != 0 && page.condition.switchOne.switchId == switchID) ||
                                (page.condition.switchTwo.enabled != 0 && page.condition.switchTwo.switchId == switchID))
                            {
                                resultList.Add(CreateMapEventSearchResult(mp, page, "condition switch"));
                            }
                        }));
                    // バトルイベントでも使用している場合があるので検出する
                    // EventBattleDataModel.pages.condition.switchData.switchId
                    mEventBattleRepository.Load().ForEach(bte =>
                        {
                            bte.pages.ForEach(page =>
                            {
                                if (page.condition.switchData.enabled != 0 &&
                                    page.condition.switchData.switchId == switchID)
                                {
                                    var pageIndex = bte.pages.IndexOf(page);
                                    var troop = mTroopRepository.Load().FirstOrDefault(tr => tr.battleEventId == bte.eventId);
                                    var r = CreateInstance<SearchResult>();
                                    r.EventID = bte.eventId;
                                    r.EventName = troop.name;
                                    r.EventType = "Enemy";
                                    r.EventInformaton = "battle event";
                                    r.EventParam = $"page{pageIndex + 1}";
                                    r.EventHierarchyButtonName = $"{bte.eventId}-{pageIndex}";
                                    resultList.Add(r);
                                }
                            });
                        });
                    // 敵の行動パターンでスイッチを使って要るものがあるので検出する
                    // EnemyDataModel.EnemyAction
                    mEnemyRepository.Load().ForEach(data =>
                        data.actions.ForEach(act =>
                        {
                            // conditionTypeが6の時param1がスイッチのインデックス
                            if (act.conditionType == 6 &&
                                act.conditionParam1 == SelectedSwitchIndex)
                            {
                                var r = CreateInstance<SearchResult>();
                                r.EventID = "";
                                r.EventName = data.name;
                                r.EventType = "Enemy";
                                r.EventInformaton = "action pattern";
                                r.EventParam = $"action pattern {data.actions.IndexOf(act) + 1}";
                                r.EventHierarchyButtonName = $"BattleHierarchyView{mEnemyRepository.Load().IndexOf(data)}";
                                resultList.Add(r);
                            }
                        }));
                    break;

                case SearchMode.Variable:
                    // 単体選択のイベントの場合はIDで特定することになる
                    var variableID = mFlagRepository.Load().variables[SelectedValueIndex].id;
                    // データ上の値はindex+1になる?
                    var variableCode = SelectedValueIndex + 1;
                    eventDataModelList.ForEach(ed =>
                        {
                            // 値の編集イベントの検索
                            ed.eventCommands.ForEach(ev =>
                                {
                                    switch (ev.code)
                                    {
                                        case (int) EventEnum.EVENT_CODE_GAME_VAL:
                                            if (ev.parameters[8] == "0")
                                            {
                                                // パラメータの８番目が0のときは個別指定
                                                //・その場合はパラメータの0番目が変数のユニークID
                                                //・まれにパラメータの5番目が変数のユニークID（パラメータの３番の値に拠る？？
                                                //入れる値はパラメータの４番目？あたりっぽい？？がここでは使わない
                                                if (ev.parameters[0] != variableID &&
                                                    ev.parameters[5] != variableID) return;
                                            }
                                            else
                                            {
                                                // パラメータの８番目が1のときは範囲指定
                                                //・その場合はパラメータの1番目の数字から2番目の数字までの値を操作する
                                                // IDが文字列なのでパースする必要がある
                                                int.TryParse(ev.parameters[0], out var startCode);
                                                int.TryParse(ev.parameters[1], out var endCode);
                                                if (variableCode < startCode || endCode < variableCode) return;
                                            }
                                            break;
                                        case (int) EventEnum.EVENT_CODE_FLOW_IF:
                                            // パラメータの６番目が1のときは変数との比較指定
                                            //・その場合はパラメータの7番目のIDか、11番のIDと比較している
                                            if (ev.parameters[6] == "0") return;
                                            if (ev.parameters[7] != variableID &&
                                                ev.parameters[11] != variableID) return;
                                            break;
                                        case (int) EventEnum.EVENT_CODE_SCENE_SET_BATTLE_CONFIG:
                                            // バトル開始イベントの敵セットを変数から指定している場合がある
                                            // パラメータ0番目が1、1番目が変数のID
                                            if (ev.parameters[0] != "1") return;
                                            if (ev.parameters[1] != variableID) return;
                                            break;

                                        default:
                                            return;
                                    }
                                    resultList.Add(CreateEventSearchResult(ed, ev));
                                });
                        });
                    // マップイベントでも使用している場合があるので検出する
                    // EventMapDataModel.pages.condition.variables.variableId
                    mEventMapRepository.Load().ForEach(mp =>
                            mp.pages.ForEach(page =>
                            {
                                if (page.condition.variables.enabled != 0 &&
                                    page.condition.variables.variableId == variableID)
                                {
                                    resultList.Add(CreateMapEventSearchResult(mp, page, "condition variable"));
                                }
                            }));
                    break;

                case SearchMode.Type:
                    eventDataModelList.ForEach(ed =>
                        {
                            // 値の編集イベントの検索
                            ed.eventCommands.ForEach(ev =>
                                {
                                    if (ev.code != (int) mSelectedEnum) return;
                                    resultList.Add(CreateEventSearchResult(ed, ev));
                                });
                        });
                    break;

                case SearchMode.FreeWord:
                    eventDataModelList.ForEach(ed =>
                        {
                            // フリーワードの検索
                            ed.eventCommands.ForEach(ev =>
                                {
                                    if (ev.code != (int) EventEnum.EVENT_CODE_MESSAGE_TEXT_ONE_LINE) return;
                                    if (ev.parameters[0].Contains(mSelectedWords) == false) return;
                                    resultList.Add(CreateEventSearchResult(ed, ev));
                                });
                        });
                    break;


            }
            listView.itemsSource = resultList;
            listView.Rebuild();
        });
    }

    /// <summary>
    /// ラジオボタンのセットアップ処理
    /// UIにbuttonが無いパターンが存在する
    /// </summary>
    /// <param name="rootButtonName">ルートにするラジオボタンの名称</param>
    /// <param name="OnFocused">選択されたときにする処理</param>
    /// <param name="OnButtonClicked">付属のボタンが押されたときにする処理</param>
    void RadioButtonSetUP(
        string rootButtonName,
        Action OnFocused,
        Action OnButtonClicked,
        bool defaultEnable = false
        ) {
        var radioButton = rootVisualElement.Q<RadioButton>(rootButtonName);
        var button = radioButton.Q<Button>();
        var textField = radioButton.Q<TextField>();
        radioButton.RegisterValueChangedCallback(v =>
        {
            button?.SetEnabled(v.newValue);
            textField.SetEnabled(v.newValue);
            if (v.newValue == true)
            {
                OnFocused.Invoke();
            }
        });
        if (OnButtonClicked != default)
        {
            button.clicked += OnButtonClicked;
        }
        button?.SetEnabled(defaultEnable);
        textField.SetEnabled(defaultEnable);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="eventDataModel"></param>
    /// <param name="eventCommand"></param>
    /// <returns></returns>
    SearchResult CreateEventSearchResult(EventDataModel eventDataModel, EventDataModel.EventCommand eventCommand) {

        var result = CreateInstance<SearchResult>();

        result.EventID = eventDataModel.id;
        result.EventType = "None";
        var tList = mEventLabelStringBuilder.Invoke(eventCommand);
        result.EventInformaton += tList.Q<Label>().text;
        tList.Query<ImTextField>().ForEach(t => result.EventInformaton += t.value);


        // マップから検索
        var mapEvents = mEventMapRepository.Load();
        var mpev = mapEvents.FirstOrDefault(mp => mp.eventId == result.EventID);
        if (mpev != default)
        {
            // MapDataModelにマップの表示情報等が入っているのでそれを取得します
            var mapDataModel = mMapRepository.LoadMapDataModels().FirstOrDefault(map => map.id == mpev.mapId);
            result.EventType = "Map";
            // イベント名
            result.EventName = mapDataModel?.name;
            // 表示パラメータを作成
            result.EventParam = $"{mpev?.name} position({mpev.x},{mpev.y}) {mpev.note}";
            // 飛び先設定
            result.EventHierarchyButtonName = CommonMapHierarchyView.GetEventPageButtonName(result.EventID, EventMapDataModel.EventMapPage.DefaultPage);
        }
        else
        {
            // バトルから検索
            var battleEvents = mEventBattleRepository.Load();
            var btev = battleEvents.FirstOrDefault(btev => btev.eventId == result.EventID || btev.pages.Any(page => page.eventId == result.EventID));
            if (btev != default)
            {
                var troop = mTroopRepository.Load().FirstOrDefault(tr => tr.battleEventId == btev.eventId);
                var page = btev.pages.FirstOrDefault(page => page.eventId == result.EventID);
                var index = btev.pages.IndexOf(page);

                result.EventType = "Battle";
                result.EventParam = "Battle Event " + index;
                result.EventName = "Enemy Group:" + troop.name;
                result.EventHierarchyButtonName = $"{troop.battleEventId}-{index}";
            }
            else
            {
                // コモンから検索
                var commonEvents = mEventCommonRepository.Load();
                var comev = commonEvents.FirstOrDefault(mp => mp.eventId == result.EventID);
                if (comev != default)
                {
                    result.EventType = "Common";
                    // イベント名
                    result.EventName = comev.name;
                    // 表示パラメータを作成
                    result.EventParam = $"-";
                    // 飛び先設定
                    result.EventHierarchyButtonName = $"CommonEventHierarchyView{commonEvents.IndexOf(comev)}";
                }
                else
                {
                    // 所属が無いイベント？
                }
            }
        }

        return result;
    }

    /// <summary>
    /// マップに設定されたイベントからの検索リザルト作成
    /// </summary>
    /// <param name="eventMapDataModel"></param>
    /// <param name="mapPage"></param>
    /// <param name="information"></param>
    /// <returns></returns>
    SearchResult CreateMapEventSearchResult(EventMapDataModel eventMapDataModel, EventMapDataModel.EventMapPage mapPage, string information) {
        var mapDataModel = mMapRepository.LoadMapDataModels().FirstOrDefault(map => map.id == eventMapDataModel.mapId);
        var r = CreateInstance<SearchResult>();
        var pageIndex = eventMapDataModel.pages.IndexOf(mapPage);
        r.EventID = eventMapDataModel.eventId;
        r.EventType = "Map";
        r.EventName = mapDataModel?.name;
        r.EventInformaton = information;
        r.EventHierarchyButtonName = $"{eventMapDataModel.eventId}_page_{pageIndex}";
        r.EventParam = $"page{pageIndex + 1} {eventMapDataModel?.name} position({eventMapDataModel.x},{eventMapDataModel.y}) {eventMapDataModel.note}";
        return r;
    }

    // イベントEnumとローカライズデータのコンバート用テーブル
    //TODO:共用可能なので別途使用する際は切り離す
    List<(EventEnum, string)> mEventCodeToStringCode = new List<(EventEnum, string)>{
        (EventEnum.EVENT_CODE_MESSAGE_TEXT,"WORD_1191"),
        (EventEnum.EVENT_CODE_MESSAGE_INPUT_SELECT,"WORD_0080"),
        (EventEnum.EVENT_CODE_MESSAGE_INPUT_NUMBER,"WORD_1209"),
        (EventEnum.EVENT_CODE_MESSAGE_INPUT_SELECT_ITEM,"WORD_1211"),
        (EventEnum.EVENT_CODE_MESSAGE_TEXT_SCROLL,"WORD_1213"),
        (EventEnum.EVENT_CODE_CHARACTER_SHOW_ANIMATION,"WORD_0951"),
        (EventEnum.EVENT_CODE_CHARACTER_SHOW_ICON,"WORD_0953"),
        (EventEnum.MOVEMENT_WALKING_ANIMATION_ON,"WORD_0967"),
        (EventEnum.MOVEMENT_CHANGE_IMAGE,"WORD_0972"),
        (EventEnum.EVENT_CODE_MOVE_PLACE,"WORD_0993"),
        (EventEnum.EVENT_CODE_MOVE_SET_MOVE_POINT,"WORD_1000"),
        (EventEnum.MOVEMENT_ONE_STEP_FORWARD,"WORD_1005"),
        (EventEnum.MOVEMENT_ONE_STEP_BACKWARD,"WORD_1006"),
        (EventEnum.MOVEMENT_JUMP,"WORD_1640"),
        (EventEnum.EVENT_CODE_FLOW_CUSTOM_MOVE,"WORD_2800"),
        (EventEnum.EVENT_CODE_STEP_MOVE,"WORD_2802"),
        (EventEnum.EVENT_CODE_CHANGE_MOVE_SPEED,"WORD_1547"),
        (EventEnum.EVENT_CODE_CHANGE_MOVE_FREQUENCY,"WORD_1548"),
        (EventEnum.EVENT_CODE_PASS_THROUGH,"WORD_0867"),
        (EventEnum.MOVEMENT_TURN_DOWN,"WORD_0858"),
        (EventEnum.EVENT_CODE_MOVE_PLACE_SHIP,"WORD_1008"),
        (EventEnum.EVENT_CODE_MOVE_RIDE_SHIP,"WORD_1010"),
        (EventEnum.EVENT_CODE_MOVE_RESPAWN_POINT,"WORD_1653"),
        (EventEnum.EVENT_CODE_GAME_SWITCH,"WORD_1012"),
        (EventEnum.EVENT_CODE_GAME_VAL,"WORD_1015"),
        (EventEnum.EVENT_CODE_GAME_SELF_SWITCH,"WORD_1048"),
        (EventEnum.EVENT_CODE_GAME_TIMER,"WORD_1049"),
        (EventEnum.EVENT_CODE_PICTURE_SHOW,"WORD_1116"),
        (EventEnum.EVENT_CODE_PICTURE_MOVE,"WORD_1121"),
        (EventEnum.EVENT_CODE_PICTURE_ROTATE,"WORD_1128"),
        (EventEnum.EVENT_CODE_PICTURE_CHANGE_COLOR,"WORD_1130"),
        (EventEnum.EVENT_CODE_PICTURE_ERASE,"WORD_1137"),
        (EventEnum.EVENT_CODE_FLOW_IF,"WORD_1139"),
        (EventEnum.EVENT_CODE_FLOW_LOOP,"WORD_1166"),
        (EventEnum.EVENT_CODE_FLOW_LOOP_BREAK,"WORD_1167"),
        (EventEnum.EVENT_CODE_FLOW_EVENT_BREAK,"WORD_1168"),
        (EventEnum.EVENT_CODE_FLOW_JUMP_COMMON,"WORD_0506"),
        (EventEnum.EVENT_CODE_FLOW_LABEL,"WORD_1169"),
        (EventEnum.EVENT_CODE_FLOW_JUMP_LABEL,"WORD_1171"),
        (EventEnum.EVENT_CODE_FLOW_ANNOTATION,"WORD_1172"),
        (EventEnum.EVENT_CODE_PARTY_CHANGE,"WORD_1089"),
        (EventEnum.EVENT_CODE_CHARACTER_CHANGE_ALPHA,"WORD_1094"),
        (EventEnum.EVENT_CODE_CHARACTER_CHANGE_WALK,"WORD_1096"),
        (EventEnum.EVENT_CODE_CHARACTER_CHANGE_PARTY,"WORD_1098"),
        (EventEnum.EVENT_CODE_PARTY_GOLD,"WORD_1099"),
        (EventEnum.EVENT_CODE_PARTY_ITEM,"WORD_1100"),
        (EventEnum.EVENT_CODE_PARTY_WEAPON,"WORD_1101"),
        (EventEnum.EVENT_CODE_PARTY_ARMS,"WORD_1102"),
        (EventEnum.EVENT_CODE_TIMING_WAIT,"WORD_1087"),
        (EventEnum.EVENT_CODE_SYSTEM_BATTLE_BGM,"WORD_1071"),
        (EventEnum.EVENT_CODE_SYSTEM_BATTLE_WIN,"WORD_1072"),
        (EventEnum.EVENT_CODE_SYSTEM_BATTLE_LOSE,"WORD_1073"),
        (EventEnum.EVENT_CODE_SYSTEM_SHIP_BGM,"WORD_1074"),
        (EventEnum.EVENT_CODE_SYSTEM_IS_SAVE,"WORD_1075"),
        (EventEnum.EVENT_CODE_SYSTEM_IS_MENU,"WORD_1077"),
        (EventEnum.EVENT_CODE_SYSTEM_IS_ENCOUNT,"WORD_1078"),
        (EventEnum.EVENT_CODE_SYSTEM_IS_SORT,"WORD_1079"),
        (EventEnum.EVENT_CODE_SYSTEM_WINDOW_COLOR,"WORD_1080"),
        (EventEnum.EVENT_CODE_SYSTEM_CHANGE_ACTOR_IMAGE,"WORD_1084"),
        (EventEnum.EVENT_CODE_SYSTEM_CHANGE_SHIP_IMAGE,"WORD_1085"),
        (EventEnum.EVENT_CODE_ACTOR_CHANGE_HP,"WORD_0891"),
        (EventEnum.EVENT_CODE_ACTOR_CHANGE_MP,"WORD_0898"),
        (EventEnum.EVENT_CODE_ACTOR_CHANGE_TP,"WORD_0899"),
        (EventEnum.EVENT_CODE_ACTOR_CHANGE_STATE,"WORD_0900"),
        (EventEnum.EVENT_CODE_ACTOR_HEAL,"WORD_0903"),
        (EventEnum.EVENT_CODE_ACTOR_CHANGE_EXP,"WORD_0904"),
        (EventEnum.EVENT_CODE_ACTOR_CHANGE_LEVEL,"WORD_0906"),
        (EventEnum.EVENT_CODE_ACTOR_CHANGE_PARAMETER,"WORD_0907"),
        (EventEnum.EVENT_CODE_ACTOR_CHANGE_SKILL,"WORD_0908"),
        (EventEnum.EVENT_CODE_ACTOR_CHANGE_EQUIPMENT,"WORD_0911"),
        (EventEnum.EVENT_CODE_ACTOR_CHANGE_CLASS,"WORD_0913"),
        (EventEnum.EVENT_CODE_ACTOR_CHANGE_NAME,"WORD_0916"),
        (EventEnum.EVENT_CODE_DISPLAY_FADEOUT,"WORD_1216"),
        (EventEnum.EVENT_CODE_DISPLAY_FADEIN,"WORD_1217"),
        (EventEnum.EVENT_CODE_DISPLAY_CHANGE_COLOR,"WORD_1218"),
        (EventEnum.EVENT_CODE_DISPLAY_FLASH,"WORD_1220"),
        (EventEnum.EVENT_CODE_DISPLAY_SHAKE,"WORD_1222"),
        (EventEnum.EVENT_CODE_DISPLAY_WEATHER,"WORD_1225"),
        (EventEnum.EVENT_CODE_AUDIO_BGM_PLAY,"WORD_0930"),
        (EventEnum.EVENT_CODE_AUDIO_BGM_FADEOUT,"WORD_0937"),
        (EventEnum.EVENT_CODE_AUDIO_BGM_SAVE,"WORD_0939"),
        (EventEnum.EVENT_CODE_AUDIO_BGM_CONTINUE,"WORD_0940"),
        (EventEnum.EVENT_CODE_AUDIO_BGS_PLAY,"WORD_0941"),
        (EventEnum.EVENT_CODE_AUDIO_BGS_FADEOUT,"WORD_0943"),
        (EventEnum.EVENT_CODE_AUDIO_ME_PLAY,"WORD_0944"),
        (EventEnum.EVENT_CODE_AUDIO_SE_PLAY,"WORD_0946"),
        (EventEnum.EVENT_CODE_AUDIO_SE_STOP,"WORD_0948"),
        (EventEnum.EVENT_CODE_AUDIO_MOVIE_PLAY,"WORD_0949"),
        (EventEnum.EVENT_CODE_BATTLE_CHANGE_STATUS,"WORD_1103"),
        (EventEnum.EVENT_CODE_BATTLE_CHANGE_STATE,"WORD_1106"),
        (EventEnum.EVENT_CODE_BATTLE_APPEAR,"WORD_1107"),
        (EventEnum.EVENT_CODE_BATTLE_TRANSFORM,"WORD_1108"),
        (EventEnum.EVENT_CODE_BATTLE_SHOW_ANIMATION,"WORD_1110"),
        (EventEnum.EVENT_CODE_BATTLE_EXEC_COMMAND,"WORD_1111"),
        (EventEnum.EVENT_CODE_BATTLE_STOP,"WORD_1115"),
        (EventEnum.EVENT_CODE_MOVE_MAP_SCROLL,"WORD_1173"),
        (EventEnum.EVENT_CODE_MAP_CHANGE_NAME,"WORD_1175"),
        (EventEnum.EVENT_CODE_MAP_CHANGE_BATTLE_BACKGROUND,"WORD_1179"),
        (EventEnum.EVENT_CODE_MAP_CHANGE_DISTANT_VIEW,"WORD_1181"),
        (EventEnum.EVENT_CODE_MAP_GET_POINT,"WORD_1184"),
        (EventEnum.EVENT_CODE_MAP_CHANGE_MINIMAP,"WORD_6106"),
        (EventEnum.EVENT_CODE_SCENE_SET_BATTLE_CONFIG,"WORD_1052"),
        (EventEnum.EVENT_CODE_SCENE_SET_SHOP_CONFIG,"WORD_1059"),
        (EventEnum.EVENT_CODE_SCENE_INPUT_NAME,"WORD_1064"),
        (EventEnum.EVENT_CODE_SCENE_MENU_OPEN,"WORD_1066"),
        (EventEnum.EVENT_CODE_SCENE_SAVE_OPEN,"WORD_1067"),
        (EventEnum.EVENT_CODE_SCENE_GAME_OVER,"WORD_1068"),
        (EventEnum.EVENT_CODE_SCENE_GOTO_TITLE,"WORD_1069"),
        (EventEnum.EVENT_CODE_CHARACTER_IS_EVENT,"WORD_0918"),
        (EventEnum.EVENT_CODE_MOVE_SET_EVENT_POINT,"WORD_0919"),
        (EventEnum.EVENT_CODE_ADDON_COMMAND,"WORD_2510"),
    };
}