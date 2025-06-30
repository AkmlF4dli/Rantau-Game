using System;
using System.Collections.Generic;
using RPGMaker.Codebase.CoreSystem.Helper;
using RPGMaker.Codebase.CoreSystem.Knowledge.DataModel.Event;
using RPGMaker.Codebase.Editor.Common;
using RPGMaker.Codebase.Editor.Common.View;
using RPGMaker.Codebase.Editor.DatabaseEditor.View;
using RPGMaker.Codebase.Editor.MapEditor.Component.Canvas;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace RPGMaker.Codebase.Editor.MapEditor.Component.CommandEditor.PostEffect
{
    /**
     * ポストエフェクトエディターの基底クラス
     *
     * パラメーター（固定）
     * 0: フレーム数 | マップ変更まで
     * 1: フレーム数
     * 2: 終了まで待機
     *
     */
    public class PostEffectEditorBase : AbstractCommandEditor
    {
        protected const string UxmlDir = "Assets/RPGMaker/Codebase/Editor/MapEditor/Asset/Uxml/EventCommand";
        protected string SettingUxml;
        protected string ShaderName;

        protected EventEditCanvas EventEditCanvas;
        protected PreviewSceneElement PreviewSceneElement;
        protected bool IsBattle;

        protected RenderTexture RenderTexture
        {
            get
            {
                return IsBattle
                    ? PreviewSceneElement.EffectRenderer?.RenderTexture
                    : EventEditCanvas?.EffectRenderer?.RenderTexture;
            }
        }

        public PostEffectEditorBase(
            VisualElement rootElement,
            List<EventDataModel> eventDataModels,
            int eventIndex,
            int eventCommandIndex,
            EventEditCanvas eventEditCanvas,
            PreviewSceneElement previewSceneElement,
            bool isBattle = false
        )
            : base(rootElement, eventDataModels, eventIndex, eventCommandIndex)
        {
            EventEditCanvas = eventEditCanvas;
            PreviewSceneElement = previewSceneElement;
            IsBattle = isBattle;
        }

        public override void Invoke()
        {
            if (EventEditCanvas != null)
            {
                EventEditCanvas.EffectRenderer?.SetEffectMaterial(ShaderName);
            }

            if (PreviewSceneElement != null)
            {
                PreviewSceneElement.EffectRenderer?.SetEffectMaterial(ShaderName);
            }

            if (string.IsNullOrEmpty(SettingUxml))
            {
                throw new Exception("SettingUxml is not set.");
            }

            var commandTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(SettingUxml);
            VisualElement commandFromUxml = commandTree.CloneTree();
            EditorLocalize.LocalizeElements(commandFromUxml);
            commandFromUxml.style.flexGrow = 1;
            RootElement.Add(commandFromUxml);

            UnityEditorWrapper.AssetDatabaseWrapper.Refresh();

            if (EventDataModels[EventIndex].eventCommands[EventCommandIndex].parameters.Count == 0)
            {
                AddParameter();
                Save(EventDataModels[EventIndex]);
                UnityEditorWrapper.AssetDatabaseWrapper.Refresh();
            }

            RadioButton _postEffectTimeToggle1 = RootElement.Q<RadioButton>("radioButton-post-effect-time1");
            RadioButton _postEffectTimeToggle2 = RootElement.Q<RadioButton>("radioButton-post-effect-time2");
            VisualElement _postEffectFrame = RootElement.Q<VisualElement>("post-effect-frame");
            VisualElement _postEffectMap = RootElement.Q<VisualElement>("post-effect-map");
            InspectorItemUnit _postEffectTimeToggles =
                RootElement.Q<InspectorItemUnit>("post_effect_frame_type_toggles");
            _postEffectTimeToggles.Clear();
            
            if (IsBattle) RootElement.Q<Foldout>("post-effect-map").text = EditorLocalize.LocalizeText("WORD_6046");

            int defaultSelect = int.Parse(EventDataModels[EventIndex].eventCommands[EventCommandIndex].parameters[0]);

            new CommonToggleSelector().SetRadioInVisualElementSelector(
                new List<RadioButton> { _postEffectTimeToggle1, _postEffectTimeToggle2 },
                new List<VisualElement> { _postEffectFrame, _postEffectMap },
                defaultSelect, new List<Action>
                {
                    () =>
                    {
                        EventDataModels[EventIndex].eventCommands[EventCommandIndex].parameters[0] = "0";
                        Save(EventDataModels[EventIndex]);
                    },
                    () =>
                    {
                        EventDataModels[EventIndex].eventCommands[EventCommandIndex].parameters[0] = "1";
                        Save(EventDataModels[EventIndex]);
                    }
                });

            IntegerField flame = RootElement.Q<VisualElement>("command_changePostEffectSettings")
                .Query<IntegerField>("flame");
            if (EventDataModels[EventIndex].eventCommands[EventCommandIndex].parameters[1] != null)
                flame.value =
                    int.Parse(EventDataModels[EventIndex].eventCommands[EventCommandIndex].parameters[1]);
            flame.RegisterCallback<FocusOutEvent>(evt =>
            {
                if (flame.value < 0)
                    flame.value = 0;
                else if (flame.value > 999)
                    flame.value = 999;
                EventDataModels[EventIndex].eventCommands[EventCommandIndex].parameters[1] =
                    flame.value.ToString();
                Save(EventDataModels[EventIndex]);
            });

            Toggle wait_toggle = RootElement.Q<VisualElement>("command_changePostEffectSettings")
                .Query<Toggle>("wait_toggle");
            if (EventDataModels[EventIndex].eventCommands[EventCommandIndex].parameters[2] == "1")
                wait_toggle.value = true;

            wait_toggle.RegisterValueChangedCallback(o =>
            {
                EventDataModels[EventIndex].eventCommands[EventCommandIndex].parameters[2] =
                    wait_toggle.value ? "1" : "0";
                Save(EventDataModels[EventIndex]);
            });
        }

        protected virtual void AddParameter()
        {
            EventDataModels[EventIndex].eventCommands[EventCommandIndex].parameters.Add("0");
            EventDataModels[EventIndex].eventCommands[EventCommandIndex].parameters.Add("255");
            EventDataModels[EventIndex].eventCommands[EventCommandIndex].parameters.Add("0");
        }

        protected virtual void ApplyParams()
        {
        }

        protected void SetDirty(bool dirty = true, EffectRenderer.BlitType blitType = EffectRenderer.BlitType.Normal)
        {
            if (!IsBattle)
            {
                if (EventEditCanvas == null) return;
                EventEditCanvas.EffectRenderer?.SetDirty(dirty, blitType);
            }
            else
            {
                if (PreviewSceneElement == null) return;
                PreviewSceneElement.EffectRenderer?.SetDirty(dirty, blitType);
            }
        }
    }
}