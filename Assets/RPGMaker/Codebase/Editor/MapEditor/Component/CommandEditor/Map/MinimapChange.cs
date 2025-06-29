using RPGMaker.Codebase.CoreSystem.Helper;
using RPGMaker.Codebase.CoreSystem.Knowledge.DataModel.Event;
using RPGMaker.Codebase.Editor.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using static RPGMaker.Codebase.Runtime.Event.Map.MapChangeMinimapProcessor;

namespace RPGMaker.Codebase.Editor.MapEditor.Component.CommandEditor.Map
{
    public class MinimapChange : AbstractCommandEditor
    {
        private const string SettingUxml =
            "Assets/RPGMaker/Codebase/Editor/MapEditor/Asset/Uxml/EventCommand/inspector_mapEvent_minimap_change.uxml";
        string InvalidFormatClaasName = "invalid_format";

        public MinimapChange(
            VisualElement rootElement,
            List<EventDataModel> eventDataModels,
            int eventIndex,
            int eventCommandIndex
        )
            : base(rootElement, eventDataModels, eventIndex, eventCommandIndex) {
        }

        private bool IsValidColorText(string colorText) {
            if (string.IsNullOrEmpty(colorText)) return true;
            var match = Regex.Match(colorText, @"^\d+$");
            if (match.Success) return true;
            match = Regex.Match(colorText, @"^#?[0-9a-fA-F]{6}$");
            if (match.Success) return true;
            return false;
        }

        private void ChangeTextColor(TextField textField, bool valid) {
            if (valid)
            {
                textField.RemoveFromClassList(InvalidFormatClaasName);
            }
            else
            {
                textField.AddToClassList(InvalidFormatClaasName);
            }
        }

        public override void Invoke() {
            var commandTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(SettingUxml);
            VisualElement commandFromUxml = commandTree.CloneTree();
            EditorLocalize.LocalizeElements(commandFromUxml);
            commandFromUxml.style.flexGrow = 1;
            RootElement.Add(commandFromUxml);

            UnityEditorWrapper.AssetDatabaseWrapper.Refresh();

            var parameters = EventDataModels[EventIndex].eventCommands[EventCommandIndex].parameters;
            if (parameters.Count == 0)
            {
                parameters.Add("1");
                parameters.Add("0");
                parameters.Add("0");
                parameters.Add("0");
                parameters.Add("480");
                parameters.Add("270");
                parameters.Add("0.25");
                parameters.Add("255");
                parameters.Add($"{TileIndexPassable}");
                parameters.Add($"{TileIndexUnpassable}");
                parameters.Add($"{TileIndexEvent}");
                Save(EventDataModels[EventIndex]);
                UnityEditorWrapper.AssetDatabaseWrapper.Refresh();
            }

            //表示ON/OFF
            var displayOnOffRadioButtons = new List<string>() {
                "On",
                "Off",
            }.Select(name => RootElement.Q<RadioButton>(name)).ToList();
            var displayOnOffIndex = parameters[ParameterDisplayOnOff] == "1" ? 0 :1;
            displayOnOffRadioButtons[displayOnOffIndex].value = true;
            var displaySettingsVe = RootElement.Q<VisualElement>("DisplaySettings");
            displaySettingsVe.style.display = (displayOnOffIndex == 0) ? DisplayStyle.Flex : DisplayStyle.None;
            new CommonToggleSelector().SetRadioSelector(
                displayOnOffRadioButtons,
                displayOnOffIndex, Enumerable.Range(0, displayOnOffRadioButtons.Count).Select<int, System.Action>(index => () => {
                    parameters[ParameterDisplayOnOff] = (index == 0 ? "1" : "0");
                    displaySettingsVe.style.display = (index == 0) ? DisplayStyle.Flex : DisplayStyle.None;
                    Save(EventDataModels[EventIndex]);
                }).ToList());

            //表示位置
            var positionRadioButtons = new List<string>() {
                "FixeTopLeft",
                "FixedTopRight",
                "FixeBottomLeft",
                "FixedBottomRight",
                "FixedXy",
            }.Select(name => RootElement.Q<RadioButton>(name)).ToList();
            var positionIndex = int.Parse(parameters[ParameterPositionType]);
            positionRadioButtons[positionIndex].value = true;
            var positionXyVe = RootElement.Q<VisualElement>("PositionXy");
            positionXyVe.SetEnabled(positionIndex == PositionTypeXy);
            new CommonToggleSelector().SetRadioSelector(
                positionRadioButtons,
                positionIndex, Enumerable.Range(0, positionRadioButtons.Count).Select<int, System.Action>(index => () => {
                    parameters[ParameterPositionType] = $"{index}";
                    positionXyVe.SetEnabled(index == PositionTypeXy);
                    Save(EventDataModels[EventIndex]);
                }).ToList());

            //XY
            var xIntegerField = RootElement.Q<IntegerField>("PositionX");
            var yIntegerField = RootElement.Q<IntegerField>("PositionY");
            new List<(int, IntegerField, int, int)>() {
                (ParameterPositionX, xIntegerField, 0, ScreenWidth),
                (ParameterPositionY, yIntegerField, 0, ScreenHeight) }.ForEach((x) =>
                {
                    (var index, var integerField, var minValue, var maxValue) = x;
                    integerField.value = int.Parse(parameters[index]);
                    integerField.RegisterValueChangedCallback(evt =>
                    {
                        integerField.value = Mathf.Clamp(integerField.value, minValue, maxValue);
                        parameters[index] = $"{integerField.value}";
                        Save(EventDataModels[EventIndex]);
                    });
                });


            //表示サイズ
            var widthIntegerField = RootElement.Q<IntegerField>("DisplayWidth");
            var heightIntegerField = RootElement.Q<IntegerField>("DisplayHeight");
            new List<(int, IntegerField, int, int)>() {
                (ParameterWidth, widthIntegerField, 0, ScreenWidth),
                (ParameterHeight, heightIntegerField, 0, ScreenHeight) }.ForEach((x) =>
            {
                (var index, var integerField, var minValue, var maxValue) = x;
                integerField.value = int.Parse(parameters[index]);
                integerField.RegisterValueChangedCallback(evt =>
                {
                    integerField.value = Mathf.Clamp(integerField.value, minValue, maxValue);
                    parameters[index] = $"{integerField.value}";
                    Save(EventDataModels[EventIndex]);
                });
            });

            //縮小率
            var scaleFloatField = RootElement.Q<FloatField>("DisplayScale");
            scaleFloatField.value = float.Parse(parameters[ParameterScale]);
            scaleFloatField.RegisterValueChangedCallback(evt =>
            {
                scaleFloatField.value = Mathf.Clamp(scaleFloatField.value, 0.0f, 8.0f);
                parameters[ParameterScale] = $"{scaleFloatField.value}";
                Save(EventDataModels[EventIndex]);
            });

            //不透明度
            var opacityIntegerField = RootElement.Q<IntegerField>("Opacity");
            opacityIntegerField.value = int.Parse(parameters[ParameterOpacity]);
            opacityIntegerField.RegisterValueChangedCallback(evt =>
            {
                opacityIntegerField.value = Mathf.Clamp(opacityIntegerField.value, 0, 255);
                parameters[ParameterOpacity] = $"{opacityIntegerField.value}";
                Save(EventDataModels[EventIndex]);
            });

            //通行可・不可の色指定
            var passableColorTextField = RootElement.Q<TextField>("PassableColor");
            var unpassableColorTextField = RootElement.Q<TextField>("UnpassableColor");
            passableColorTextField.value = parameters[ParameterPassableColor];
            unpassableColorTextField.value = parameters[ParameterUnpassableColor];
            ChangeTextColor(passableColorTextField, IsValidColorText(passableColorTextField.value));
            ChangeTextColor(unpassableColorTextField, IsValidColorText(unpassableColorTextField.value));
            passableColorTextField.RegisterValueChangedCallback(evt =>
            {
                ChangeTextColor(passableColorTextField, IsValidColorText(passableColorTextField.value));
                parameters[ParameterPassableColor] = passableColorTextField.value;
                Save(EventDataModels[EventIndex]);
            });
            unpassableColorTextField.RegisterValueChangedCallback(evt =>
            {
                ChangeTextColor(unpassableColorTextField, IsValidColorText(unpassableColorTextField.value));
                parameters[ParameterUnpassableColor] = unpassableColorTextField.value;
                Save(EventDataModels[EventIndex]);
            });

            //イベントの色指定
            var eventColorTextField = RootElement.Q<TextField>("EventColor");
            eventColorTextField.value = parameters[ParameterEventColor];
            ChangeTextColor(eventColorTextField, IsValidColorText(eventColorTextField.value));
            eventColorTextField.RegisterValueChangedCallback(evt =>
            {
                ChangeTextColor(eventColorTextField, IsValidColorText(eventColorTextField.value));
                parameters[ParameterEventColor] = eventColorTextField.value;
                Save(EventDataModels[EventIndex]);
            });
        }
    }
}