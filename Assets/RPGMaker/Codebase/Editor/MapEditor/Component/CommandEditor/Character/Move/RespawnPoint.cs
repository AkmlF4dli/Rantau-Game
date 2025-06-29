using RPGMaker.Codebase.CoreSystem.Helper;
using RPGMaker.Codebase.CoreSystem.Knowledge.DataModel.Event;
using RPGMaker.Codebase.CoreSystem.Knowledge.DataModel.EventMap;
using RPGMaker.Codebase.CoreSystem.Knowledge.DataModel.Map;
using RPGMaker.Codebase.Editor.Common;
using RPGMaker.Codebase.Editor.Common.View;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine.UIElements;

namespace RPGMaker.Codebase.Editor.MapEditor.Component.CommandEditor.Character.Move
{
    /// <summary>
    ///  復活ポイントの設定
    /// </summary>
    public class RespawnPoint : AbstractCommandEditor
    {
        private const string SettingUxml =
            "Assets/RPGMaker/Codebase/Editor/MapEditor/Asset/Uxml/EventCommand/inspector_mapEvent_respawn_point.uxml";

        private EventMapDataModel _eventMapDataModel;

        // 移動先。
        private MapDataModel _mapDataModel;
        private Label _xPos;
        private Label _yPos;

        private int posX;
        private int posY;

        VisualElement mapSelectDirection;
        VisualElement mapSelectVariables;
        VisualElement direction;
        GenericPopupFieldBase<MapDataChoice> mapSelectPopupField;
        PopupFieldBase<string> directionPopupField;
        VisualElement map_select;
        RadioButton direct_toggle;
        RadioButton variable_toggle;
        RadioButton none_toggle;
        ImTextField mapName;
        Button directButton;
        Toggle _GameOverScreenToggle;

        public RespawnPoint(
            VisualElement rootElement,
            List<EventDataModel> eventDataModels,
            int eventIndex,
            int eventCommandIndex
        )
            : base(rootElement, eventDataModels, eventIndex, eventCommandIndex) {
        }

        public override void Invoke() {
            var commandTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(SettingUxml);
            VisualElement commandFromUxml = commandTree.CloneTree();
            EditorLocalize.LocalizeElements(commandFromUxml);
            commandFromUxml.style.flexGrow = 1;
            RootElement.Add(commandFromUxml);

            var MapEntities = MapManagementService.LoadMaps();

            UnityEditorWrapper.AssetDatabaseWrapper.Refresh();
            //var num = 0;

            if (EventCommand.parameters.Count == 0)
            {
                EventCommand.parameters.Add("0");// 0 : 直接指定/プレイヤー
                EventCommand.parameters.Add(MapEntities.Count > 0 ? MapEntities[0].name : "");// 1 :マップ名
                EventCommand.parameters.Add(MapEntities.Count > 0 ? MapEntities[0].id : "-1");// 2 :マップID
                EventCommand.parameters.Add("0");// 3 : MapX
                EventCommand.parameters.Add("0");// 4 : MapY
                EventCommand.parameters.Add("0");// 5 : 状態
                EventCommand.parameters.Add("false");// 6 : ゲームオーバー表示
                EventCommand.parameters.Add(MapEntities.Count > 0 ? MapEntities[0].id : "-1");
                Save(EventDataModel);
                UnityEditorWrapper.AssetDatabaseWrapper.Refresh();
            }


            var flagDataModel = DatabaseManagementService.LoadFlags();
            var variableNameList = new List<string>();
            var variableIDList = new List<string>();
            for (var i = 0; i < flagDataModel.variables.Count; i++)
            {
                if (flagDataModel.variables[i].name == "")
                    variableNameList.Add(EditorLocalize.LocalizeText("WORD_1518"));
                else
                    variableNameList.Add(flagDataModel.variables[i].name);
                variableIDList.Add(flagDataModel.variables[i].id);
            }

            // 通常イベント。
            var currentEventMapIndex = -1;
            var eventMapDataModels = EventManagementService.LoadEventMap();
            for (var i = 0; i < eventMapDataModels.Count; i++)
                if (eventMapDataModels[i].eventId == EventDataModel.id)
                {
                    _eventMapDataModel = eventMapDataModels[i];
                    _mapDataModel = MapEntities.FirstOrDefault(c => c.id == EventCommand.parameters[7]);
                    currentEventMapIndex = i;
                    break;
                }

            // コモンイベント。
            if (currentEventMapIndex == -1)
            {
                Type = EventType.Common;
                var eventCommonDataModels = EventManagementService.LoadEventCommon();
                for (var i = 0; i < eventCommonDataModels.Count; i++)
                    if (eventCommonDataModels[i].eventId == EventDataModel.id)
                    {
                        _mapDataModel = MapEntities.FirstOrDefault(c => c.id == EventCommand.parameters[7]);
                        currentEventMapIndex = i;
                        break;
                    }
            }
            //--------------------------------------------------------------------------------------
            //マップ選択系
            map_select = RootElement.Query<VisualElement>("mapIDSelect");
            direct_toggle = RootElement.Query<RadioButton>("radioButton-eventCommand-display9");
            variable_toggle = RootElement.Query<RadioButton>("radioButton-eventCommand-display10");
            none_toggle = RootElement.Query<RadioButton>("radioButton-eventCommand-display11");
            mapName = RootElement.Query<ImTextField>("mapName");

            _xPos = RootElement.Query<Label>("xPos");
            _yPos = RootElement.Query<Label>("yPos");

            // 『設定開始』ボタン。
            directButton = RootElement.Query<Button>("direct_button");
            directButton.SetEnabled(false);
            var isEdit = false;
            directButton.text = EditorLocalize.LocalizeText("WORD_1583");
            directButton.clickable.clicked += () =>
            {
                if (isEdit)
                {
                    // 設定終了へ。
                    directButton.text = EditorLocalize.LocalizeText("WORD_1583");
                    EndMapPosition();
                }
                else
                {
                    // 設定開始へ。
                    directButton.text = EditorLocalize.LocalizeText("WORD_1584");
                    SetMapPosition();
                }

                isEdit = !isEdit;
            };

            // マップ指定。
            mapSelectPopupField = GenericPopupFieldBase<MapDataChoice>.Add(
                RootElement,
                "mapSelect",
                MapDataChoice.GenerateChoices(),
                EventCommand.parameters[7]);

            // mapSelectPopupFieldの選択項目変更時の処理。
            mapSelectPopupField.RegisterValueChangedCallback(
                (changeEvent) =>
                {
                    if (EventCommand.parameters[0] == "2")
                    {
                        var choice = changeEvent.newValue;
                        if (choice.Id == "-1")
                        {
                            EventCommand.parameters[1] = "";
                            EventCommand.parameters[2] = "";
                            EventCommand.parameters[7] = "";
                            EventCommand.parameters[3] = "";
                            EventCommand.parameters[4] = "";
                            _xPos.text = "[ ]";
                            _yPos.text = "[ ]";
                        }
                        else
                        {
                            EventCommand.parameters[2] = choice.Id;
                            EventCommand.parameters[1] = choice.Name;
                            EventCommand.parameters[7] = choice.Id;

                            EventCommand.parameters[0] = "2";

                            int.TryParse(EventCommand.parameters[3], out int parameter3IntValue);
                            EventCommand.parameters[3] = parameter3IntValue.ToString();

                            int.TryParse(EventCommand.parameters[4], out int parameter4IntValue);
                            EventCommand.parameters[4] = parameter4IntValue.ToString();

                            _xPos.text = $"[{parameter3IntValue}]";
                            _yPos.text = $"[{System.Math.Abs(parameter4IntValue)}]";
                        }

                        mapName.value = choice.Name;
                        Save(EventDataModel);
                        _mapDataModel = choice.MapDataModel;

                        directButton.SetEnabled(choice.MapDataModel != null);
                    }
                });

            directButton.SetEnabled(mapSelectPopupField.value.MapDataModel != null);
            
            //ゲームオーバー表示
            _GameOverScreenToggle = RootElement.Query<Toggle>("GameOverScreen");
            if (EventCommand.parameters[6] != "0")
            {
                _GameOverScreenToggle.value = bool.Parse(EventCommand.parameters[6]);
            }
            else
            {
                _GameOverScreenToggle.value = false;
                if (EventCommand.parameters[6] == "1")
                {
                    _GameOverScreenToggle.value = true;
                }
            }

            _GameOverScreenToggle.RegisterValueChangedCallback<bool>((evt) =>
            {
                EventCommand.parameters[6] =  evt.newValue.ToString().ToLower();
                Save(EventDataModel);
            });

            mapSelectDirection = RootElement.Query<VisualElement>("mapSelect");
            mapSelectVariables = RootElement.Query<VisualElement>("mapIDSelect");

            direction = RootElement.Query<VisualElement>("direction");
            //復活方
            var directionNameList = EditorLocalize.LocalizeTexts(new List<string> { "WORD_1658", "WORD_1654" });
            var selectID = int.Parse(EventCommand.parameters[5]);
            if (selectID == -1)
                selectID = 0;
            directionPopupField = new PopupFieldBase<string>(directionNameList, selectID);
            direction.Clear();
            direction.Add(directionPopupField);
            directionPopupField.RegisterValueChangedCallback(evt =>
            {
                EventCommand.parameters[5] = directionNameList.IndexOf(directionPopupField.value).ToString();
                Save(EventDataModel);
            });

            mapSelectDirection.SetEnabled(false);
            int RespawnPointType = int.Parse(EventCommand.parameters[0]);
            //----------------------------------------------------------------------------------
            //直接指定/プレイヤーの選択
            var defaultSelect = RespawnPointType;
            new CommonToggleSelector().SetRadioSelector(
                new List<RadioButton> { none_toggle , variable_toggle, direct_toggle },
                defaultSelect, new List<System.Action>
                {
                    //設定なし。
                    () =>
                    {
                        SetRespawnPointType(0);
                        none_toggle.value = false;
                        EventCommand.parameters[0] = "0";
                        MapEditor.WhenEventClosed();
                        mapName.value = "";
                        Save(EventDataModel);
                    },
                    //プレイヤー
                    () =>
                    {
                        SetRespawnPointType(1);
                        direct_toggle.value = false;
                        EventCommand.parameters[0] = "1";
                        MapEditor.WhenEventClosed();
                        mapName.value = "";
                        Save(EventDataModel);
                    },
                    //直接指定トグル。
                    () =>
                    {
                        SetRespawnPointType(2);
                        var num = 0;
                        variable_toggle.value = false;
                        EventCommand.parameters[0] = "2";
                        EventCommand.parameters[2] = EventCommand.parameters[7];

                        try
                        {
                            num = int.Parse(EventCommand.parameters[3]);
                        }
                        catch (Exception)
                        {
                            num = 0;
                        }

                        EventCommand.parameters[3] = num.ToString();

                        try
                        {
                            num = int.Parse(EventCommand.parameters[4]);
                        }
                        catch (Exception)
                        {
                            num = 0;
                        }

                        EventCommand.parameters[4] = num.ToString();

                        _xPos.text = "[" + EventCommand.parameters[3] + "]";
                        var y = int.Parse(EventCommand.parameters[4]) < 0
                            ? int.Parse(EventCommand.parameters[4]) * -1
                            : int.Parse(EventCommand.parameters[4]);
                        _yPos.text = "[" + y + "]";
                        for (int i = 0; i < MapEntities.Count; i++)
                            if (MapEntities[i].id == EventCommand.parameters[7])
                            {
                                _mapDataModel = MapEntities[i];
                                break;
                            }

                        mapName.value = mapSelectPopupField.value.Name;
                        directionPopupField.SetEnabled(true);

                        Save(EventDataModel);
                    },
                });

        }

        public void SetMapPosition() {
            _mapDataModel ??= MapManagementService.LoadMaps().First();

            MapEditor.LaunchCommonEventEditMode(_mapDataModel, 0,
                v =>
                {
                    posX = v.x;
                    posY = v.y;
                    _xPos.text = "[" + v.x + "]";
                    var y = v.y < 0 ? v.y * -1 : v.y;
                    _yPos.text = "[" + y + "]";
                });
        }

        public void EndMapPosition() {
            _mapDataModel ??= MapManagementService.LoadMaps().First();
            MapEditor.LaunchCommonEventEditModeEnd(_mapDataModel);
            EventCommand.parameters[3] = posX.ToString();
            EventCommand.parameters[4] = posY.ToString();
            Save(EventDataModel);
        }

        /// <summary>
        /// 復活ポイントのタイプを設定する
        /// </summary>
        /// <param name="Type"></param>
        public void SetRespawnPointType(int Type) 
        {
            //U360 
            VisualElement mapSelectView = RootElement.Query<VisualElement>("mapSelectView");

            switch (Type)
            {
                case 0://ゲームオーバー
                    {
                        directButton.SetEnabled(false);
                        mapSelectPopupField.SetEnabled(false);
                        mapSelectDirection.SetEnabled(false);

                        mapSelectVariables.SetEnabled(false);
                        mapName.value = "";
                        //U360 移動先名無効
                        mapName.SetEnabled(false);
                        directionPopupField.SetEnabled(false);
                        _xPos.text = "[ ]";
                        _yPos.text = "[ ]";

                        _GameOverScreenToggle.SetEnabled(false);
                        mapSelectView.SetEnabled(false);
                    }
                    break;
                case 1://プレイヤー
                    {
                        directButton.SetEnabled(false);
                        mapSelectPopupField.SetEnabled(false);
                        mapSelectDirection.SetEnabled(false);
                        mapSelectVariables.SetEnabled(false);
                        mapName.value = "";
                        //U360 移動先名無効
                        mapName.SetEnabled(false);
                        directionPopupField.SetEnabled(true);
                        _xPos.text = "[ ]";
                        _yPos.text = "[ ]";

                        _GameOverScreenToggle.SetEnabled(true);
                        mapSelectView.SetEnabled(false);
                    }
                    break;
                case 2://直接指定
                    {
                        directButton.SetEnabled(true);
                        mapSelectPopupField.SetEnabled(true);
                        mapSelectDirection.SetEnabled(true);
                        mapSelectVariables.SetEnabled(true);
                        mapName.SetEnabled(true);

                        _GameOverScreenToggle.SetEnabled(true);
                        mapSelectView.SetEnabled(true);
                    }
                    break;
            }
        }
    }
}