using RPGMaker.Codebase.CoreSystem.Helper;
using RPGMaker.Codebase.CoreSystem.Knowledge.DataModel.Event;
using RPGMaker.Codebase.Editor.Common;
using RPGMaker.Codebase.Editor.Common.View;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine.UIElements;

namespace RPGMaker.Codebase.Editor.MapEditor.Component.CommandEditor.FlowControl
{
    public class CallUnityScene : AbstractCommandEditor
    {
        private const string SettingUxml =
            "Assets/RPGMaker/Codebase/Editor/MapEditor/Asset/Uxml/EventCommand/inspector_mapEvent_call_unity_scene.uxml";

        public CallUnityScene(
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

            UnityEditorWrapper.AssetDatabaseWrapper.Refresh();

            var targetCommand = EventDataModels[EventIndex].eventCommands[EventCommandIndex];
            if (targetCommand.parameters.Count == 0)
            {
                targetCommand.parameters.Add("");
                targetCommand.parameters.Add("");
                targetCommand.parameters.Add("");
                EventManagementService.SaveEvent(EventDataModels[EventIndex]);
                UnityEditorWrapper.AssetDatabaseWrapper.Refresh();
            }

            //Unityシーン選択
            VisualElement dropdown = RootElement.Query<VisualElement>("menu");
            dropdown.Clear();

            //UnityEngine.Debug.Log($"{string.Join(", ", AssetDatabase.FindAssets("t:Scene").Select(guid => AssetDatabase.GUIDToAssetPath(guid)))}");
#if false
            var sceneList = AssetDatabase.FindAssets("t:Scene")
                .Select(guid => AssetDatabase.GUIDToAssetPath(guid)).Where(x => x.StartsWith("Assets/") && !x.StartsWith("Assets/RPGMaker/Codebase/Runtime/")).Select(x => Path.GetFileNameWithoutExtension(x))
                .ToList();
#endif
#if false
            var sceneList = new List<string>();
            foreach (EditorBuildSettingsScene scene in EditorBuildSettings.scenes)
            {
                if (scene.enabled) // ビルドに含まれるシーンのみをリストアップ
                {
                    UnityEngine.Debug.Log($"Scene path: {scene.path}");
                    sceneList.Add(Path.GetFileNameWithoutExtension(scene.path));
                }
            }
#endif
            var sceneList = EditorBuildSettings.scenes
                .Where(scene => scene.enabled && !scene.path.StartsWith("Assets/RPGMaker/Codebase/Runtime/"))
                .Select(scene => Path.GetFileNameWithoutExtension(scene.path))
                .ToList();
            sceneList.Insert(0, EditorLocalize.LocalizeText("WORD_2505"));
            var defaultIndex = sceneList.IndexOf(targetCommand.parameters[0]);
            var defaultId = targetCommand.parameters[0];
            if (defaultIndex < 0)
            {
                defaultIndex = 0;
            }
            var menu = RootElement.Q<VisualElement>("menu");
            menu.Clear();
            var dropdownPopupField = new PopupFieldBase<string>(sceneList, defaultIndex);
            menu.Add(dropdownPopupField);
            dropdownPopupField.RegisterValueChangedCallback(evt =>
            {
                //0番目は未設定
                if (dropdownPopupField.index == 0)
                    targetCommand.parameters[0] = "";
                else
                    targetCommand.parameters[0] = dropdownPopupField.value;

                Save(EventDataModels[EventIndex]);
            });

            //マップシーンをアンロード
            Toggle toggle = RootElement.Query<Toggle>("unload_toggle");
            toggle.value = targetCommand.parameters[1] == "1";
            toggle.RegisterValueChangedCallback(evt =>
            {
                targetCommand.parameters[1] = toggle.value ? "1" : "0";
                Save(EventDataModels[EventIndex]);
            });

            //変数
            var variables = DatabaseManagementService.LoadFlags().variables;
            var variableNameList = new List<string>() { EditorLocalize.LocalizeText("WORD_2505") };
            var variableIdList = new List<string>() { "" };
            for (var i = 0; i < variables.Count; i++)
            {
                var name = variables[i].name;
                if (name == "")
                    name = EditorLocalize.LocalizeText("WORD_1518");
                variableNameList.Add("#" + (i + 1).ToString("0000") + " " + name);

                variableIdList.Add(variables[i].id);
            }
            VisualElement variable = RootElement.Query<VisualElement>("variable");
            // [変数]プルダウンメニュー
            var selectID = variableIdList.IndexOf(EventCommand.parameters[2]);
            if (selectID == -1)
                selectID = 0;
            var variablePopupField = new PopupFieldBase<string>(variableNameList, selectID);
            variable.Clear();
            variable.Add(variablePopupField);
            variablePopupField.RegisterValueChangedCallback(_ =>
            {
                EventCommand.parameters[2] = variableIdList[variablePopupField.index];
                Save(EventDataModel);
            });

        }
    }
}