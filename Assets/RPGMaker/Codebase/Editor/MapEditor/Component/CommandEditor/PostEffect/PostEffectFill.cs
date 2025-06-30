using System;
using System.Collections.Generic;
using System.Globalization;
using RPGMaker.Codebase.CoreSystem.Knowledge.DataModel.Event;
using RPGMaker.Codebase.Editor.Common;
using RPGMaker.Codebase.Editor.Common.View;
using RPGMaker.Codebase.Editor.DatabaseEditor.View;
using RPGMaker.Codebase.Editor.MapEditor.Component.Canvas;
using UnityEngine;
using UnityEngine.UIElements;

namespace RPGMaker.Codebase.Editor.MapEditor.Component.CommandEditor.PostEffect
{
    /**
     * 塗りつぶしエフェクトエディター
     *
     * パラメーター（固定）
     * @see PostEffectEditorBase
     *
     * パラメーター
     * 3: グラデーションありなし（0で単色）
     * 4: 単色.R
     * 5: 単色.G
     * 6: 単色.B
     * 7: 単色.A
     * 8: グラデーション方向[上から下 \ 左から→ \ 左上から右下 \ 右上から左下]
     * 9: 色1.R
     * 10: 色1.G
     * 11: 色1.B
     * 12: 色1.A
     * 13: 色2.R
     * 14: 色2.G
     * 15: 色2.B
     * 16: 色2.A
     * 17: ブレンド[通常 | 乗算 | 加算 | 除算 | スクリーン | オーバーレイ]
     *
     */
    public class PostEffectFill : PostEffectEditorBase
    {
        private static readonly int Color1 = Shader.PropertyToID("_Color1");
        private static readonly int Color2 = Shader.PropertyToID("_Color2");
        private static readonly int Horizontal = Shader.PropertyToID("_Horizontal");
        private static readonly int Vertical = Shader.PropertyToID("_Vertical");
        private static readonly int Inverse = Shader.PropertyToID("_Inverse");
        private static readonly int Blend = Shader.PropertyToID("_Blend");
        private static readonly int MixRatio = Shader.PropertyToID("_MixRatio");

        private readonly List<string> _dirs = new List<string>
        {
            EditorLocalize.LocalizeText("WORD_6036"),
            EditorLocalize.LocalizeText("WORD_6037"),
            EditorLocalize.LocalizeText("WORD_6038"),
            EditorLocalize.LocalizeText("WORD_6039"),
        };

        private readonly List<string> _blends = new List<string>
        {
            EditorLocalize.LocalizeText("WORD_6040"),
            EditorLocalize.LocalizeText("WORD_6041"),
            EditorLocalize.LocalizeText("WORD_6042"),
            EditorLocalize.LocalizeText("WORD_6043"),
            EditorLocalize.LocalizeText("WORD_6044"),
            EditorLocalize.LocalizeText("WORD_6045"),
        };

        public PostEffectFill(
            VisualElement rootElement,
            List<EventDataModel> eventDataModels,
            int eventIndex,
            int eventCommandIndex,
            EventEditCanvas eventEditCanvas,
            PreviewSceneElement previewSceneElement,
            bool isBattle = false
        )
            : base(
                rootElement,
                eventDataModels,
                eventIndex,
                eventCommandIndex,
                eventEditCanvas,
                previewSceneElement,
                isBattle
            )
        {
            SettingUxml = UxmlDir + "/inspector_mapEvent_post_effect_fill.uxml";
            ShaderName = "Hidden/Fill";
        }

        public override void Invoke()
        {
            base.Invoke();

            ApplyParams();
            SetDirty();

            RadioButton postEffectColorToggle1 =
                RootElement.Q<RadioButton>("radioButton-post-effect-color1");
            RadioButton postEffectColorToggle2 =
                RootElement.Q<RadioButton>("radioButton-post-effect-color2");
            VisualElement postEffectMono =
                RootElement.Q<VisualElement>("post-effect-mono");
            VisualElement postEffectGradient =
                RootElement.Q<VisualElement>("post-effect-gradient");
            InspectorItemUnit postEffectColorToggles =
                RootElement.Q<InspectorItemUnit>("post_effect_color_type_toggles");
            postEffectColorToggles.Clear();

            int defaultColorSelect =
                int.Parse(EventDataModels[EventIndex].eventCommands[EventCommandIndex].parameters[3]);

            new CommonToggleSelector().SetRadioInVisualElementSelector(
                new List<RadioButton> { postEffectColorToggle1, postEffectColorToggle2 },
                new List<VisualElement> { postEffectMono, postEffectGradient },
                defaultColorSelect,
                new List<Action>
                {
                    () =>
                    {
                        EventDataModels[EventIndex].eventCommands[EventCommandIndex].parameters[3] = "0";
                        ApplyParams();
                        SetDirty();
                        Save(EventDataModels[EventIndex]);
                    },
                    () =>
                    {
                        EventDataModels[EventIndex].eventCommands[EventCommandIndex].parameters[3] = "1";
                        ApplyParams();
                        SetDirty();
                        Save(EventDataModels[EventIndex]);
                    }
                });

            ColorFieldBase colorPicker1 =
                RootElement.Q<ColorFieldBase>("colorPicker1");
            var r1 = int.Parse(EventDataModels[EventIndex].eventCommands[EventCommandIndex].parameters[4]);
            var g1 = int.Parse(EventDataModels[EventIndex].eventCommands[EventCommandIndex].parameters[5]);
            var b1 = int.Parse(EventDataModels[EventIndex].eventCommands[EventCommandIndex].parameters[6]);
            var a1 = int.Parse(EventDataModels[EventIndex].eventCommands[EventCommandIndex].parameters[7]);
            colorPicker1.value = new Color32((byte)r1, (byte)g1, (byte)b1, (byte)a1);
            colorPicker1.RegisterValueChangedCallback(_ =>
            {
                Color32 color = colorPicker1.value;
                var r = color.r;
                var g = color.g;
                var b = color.b;
                var a = color.a;
                ApplyParams();
                SetDirty();
                EventDataModels[EventIndex].eventCommands[EventCommandIndex].parameters[4] =
                    r.ToString(CultureInfo.InvariantCulture);
                EventDataModels[EventIndex].eventCommands[EventCommandIndex].parameters[5] =
                    g.ToString(CultureInfo.InvariantCulture);
                EventDataModels[EventIndex].eventCommands[EventCommandIndex].parameters[6] =
                    b.ToString(CultureInfo.InvariantCulture);
                EventDataModels[EventIndex].eventCommands[EventCommandIndex].parameters[7] =
                    a.ToString(CultureInfo.InvariantCulture);

                Save(EventDataModels[EventIndex]);
            });

            //U356 カラー設定を行なえる様に修正
            colorPicker1.style.flexGrow = 1.0f;

            VisualElement dirSelector =
                RootElement.Q<VisualElement>("choices_dir_select");
            int dirIndex = int.Parse(EventDataModels[EventIndex].eventCommands[EventCommandIndex].parameters[8]);
            PopupFieldBase<string> dirSelectPopupField = new PopupFieldBase<string>(_dirs, dirIndex);
            dirSelector.Clear();
            dirSelector.Add(dirSelectPopupField);
            dirSelectPopupField.RegisterValueChangedCallback((_ =>
            {
                var index = _dirs.IndexOf(dirSelectPopupField.value);
                if (index < 0 || index >= _dirs.Count) index = 0;
                EventDataModels[EventIndex].eventCommands[EventCommandIndex].parameters[8] = index.ToString();
                ApplyParams();
                SetDirty();
                Save(EventDataModels[EventIndex]);
            }));

            ColorFieldBase colorPicker2 =
                RootElement.Q<ColorFieldBase>("colorPicker2");
            var r2 = int.Parse(EventDataModels[EventIndex].eventCommands[EventCommandIndex].parameters[9]);
            var g2 = int.Parse(EventDataModels[EventIndex].eventCommands[EventCommandIndex].parameters[10]);
            var b2 = int.Parse(EventDataModels[EventIndex].eventCommands[EventCommandIndex].parameters[11]);
            var a2 = int.Parse(EventDataModels[EventIndex].eventCommands[EventCommandIndex].parameters[12]);
            colorPicker2.value = new Color32((byte)r2, (byte)g2, (byte)b2, (byte)a2);
            colorPicker2.RegisterValueChangedCallback(_ =>
            {
                Color32 color = colorPicker2.value;
                var r = color.r;
                var g = color.g;
                var b = color.b;
                var a = color.a;

                EventDataModels[EventIndex].eventCommands[EventCommandIndex].parameters[9] =
                    r.ToString(CultureInfo.InvariantCulture);
                EventDataModels[EventIndex].eventCommands[EventCommandIndex].parameters[10] =
                    g.ToString(CultureInfo.InvariantCulture);
                EventDataModels[EventIndex].eventCommands[EventCommandIndex].parameters[11] =
                    b.ToString(CultureInfo.InvariantCulture);
                EventDataModels[EventIndex].eventCommands[EventCommandIndex].parameters[12] =
                    a.ToString(CultureInfo.InvariantCulture);
                ApplyParams();
                SetDirty();
                Save(EventDataModels[EventIndex]);
            });

            //U356 カラー設定を行なえる様に修正
            colorPicker2.style.flexGrow = 1.0f;

            ColorFieldBase colorPicker3 =
                RootElement.Q<ColorFieldBase>("colorPicker3");
            var r3 = int.Parse(EventDataModels[EventIndex].eventCommands[EventCommandIndex].parameters[13]);
            var g3 = int.Parse(EventDataModels[EventIndex].eventCommands[EventCommandIndex].parameters[14]);
            var b3 = int.Parse(EventDataModels[EventIndex].eventCommands[EventCommandIndex].parameters[15]);
            var a3 = int.Parse(EventDataModels[EventIndex].eventCommands[EventCommandIndex].parameters[16]);
            colorPicker3.value = new Color32((byte)r3, (byte)g3, (byte)b3, (byte)a3);
            colorPicker3.RegisterValueChangedCallback(_ =>
            {
                Color32 color = colorPicker3.value;
                var r = color.r;
                var g = color.g;
                var b = color.b;
                var a = color.a;

                EventDataModels[EventIndex].eventCommands[EventCommandIndex].parameters[13] =
                    r.ToString(CultureInfo.InvariantCulture);
                EventDataModels[EventIndex].eventCommands[EventCommandIndex].parameters[14] =
                    g.ToString(CultureInfo.InvariantCulture);
                EventDataModels[EventIndex].eventCommands[EventCommandIndex].parameters[15] =
                    b.ToString(CultureInfo.InvariantCulture);
                EventDataModels[EventIndex].eventCommands[EventCommandIndex].parameters[16] =
                    a.ToString(CultureInfo.InvariantCulture);
                ApplyParams();
                SetDirty();
                Save(EventDataModels[EventIndex]);
            });
            //U356 カラー設定を行なえる様に修正
            colorPicker3.style.flexGrow = 1.0f;

            VisualElement blendSelector =
                RootElement.Q<VisualElement>("choices_blend_select");
            int blendIndex = int.Parse(EventDataModels[EventIndex].eventCommands[EventCommandIndex].parameters[17]);
            PopupFieldBase<string> blendSelectPopupField = new PopupFieldBase<string>(_blends, blendIndex);
            blendSelector.Clear();
            blendSelector.Add(blendSelectPopupField);
            blendSelectPopupField.RegisterValueChangedCallback((_ =>
            {
                var index = _blends.IndexOf(blendSelectPopupField.value);
                if (index < 0 || index >= _blends.Count) index = 0;
                EventDataModels[EventIndex].eventCommands[EventCommandIndex].parameters[17] = index.ToString();
                ApplyParams();
                SetDirty();
                Save(EventDataModels[EventIndex]);
            }));
        }

        protected override void AddParameter()
        {
            base.AddParameter();
            EventDataModels[EventIndex].eventCommands[EventCommandIndex].parameters.Add("0"); // [3] Mono | Gradient
            EventDataModels[EventIndex].eventCommands[EventCommandIndex].parameters.Add("0"); // [4] Color1.R
            EventDataModels[EventIndex].eventCommands[EventCommandIndex].parameters.Add("0"); // [5] Color1.G
            EventDataModels[EventIndex].eventCommands[EventCommandIndex].parameters.Add("0"); // [6] Color1.B
            EventDataModels[EventIndex].eventCommands[EventCommandIndex].parameters.Add("255"); // [7] Color1.A
            EventDataModels[EventIndex].eventCommands[EventCommandIndex].parameters.Add("0"); // [8] GradientDirection
            EventDataModels[EventIndex].eventCommands[EventCommandIndex].parameters.Add("0"); // [9] Color2.R
            EventDataModels[EventIndex].eventCommands[EventCommandIndex].parameters.Add("0"); // [10] Color2.G
            EventDataModels[EventIndex].eventCommands[EventCommandIndex].parameters.Add("0"); // [11] Color2.B
            EventDataModels[EventIndex].eventCommands[EventCommandIndex].parameters.Add("255"); // [12] Color2.A
            EventDataModels[EventIndex].eventCommands[EventCommandIndex].parameters.Add("0"); // [13] Color3.R
            EventDataModels[EventIndex].eventCommands[EventCommandIndex].parameters.Add("0"); // [14] Color3.G
            EventDataModels[EventIndex].eventCommands[EventCommandIndex].parameters.Add("0"); // [15] Color3.B
            EventDataModels[EventIndex].eventCommands[EventCommandIndex].parameters.Add("255"); // [16] Color3.A
            EventDataModels[EventIndex].eventCommands[EventCommandIndex].parameters.Add("0"); // [17] Blend
        }

        protected override void ApplyParams()
        {
            int colorType = int.Parse(EventDataModels[EventIndex].eventCommands[EventCommandIndex].parameters[3]);
            bool isGradient = colorType == 1;

            Vector4 color1;
            Vector4 color2;
            if (!isGradient)
            {
                color1 = new Vector4(
                    int.Parse(EventDataModels[EventIndex].eventCommands[EventCommandIndex].parameters[4]) / 255.0f,
                    int.Parse(EventDataModels[EventIndex].eventCommands[EventCommandIndex].parameters[5]) / 255.0f,
                    int.Parse(EventDataModels[EventIndex].eventCommands[EventCommandIndex].parameters[6]) / 255.0f,
                    int.Parse(EventDataModels[EventIndex].eventCommands[EventCommandIndex].parameters[7]) / 255.0f
                );

                color2 = new Vector4(
                    color1.x,
                    color1.y,
                    color1.z,
                    color1.w
                );
            }
            else
            {
                color1 = new Vector4(
                    int.Parse(EventDataModels[EventIndex].eventCommands[EventCommandIndex].parameters[9]) / 255.0f,
                    int.Parse(EventDataModels[EventIndex].eventCommands[EventCommandIndex].parameters[10]) / 255.0f,
                    int.Parse(EventDataModels[EventIndex].eventCommands[EventCommandIndex].parameters[11]) / 255.0f,
                    int.Parse(EventDataModels[EventIndex].eventCommands[EventCommandIndex].parameters[12]) / 255.0f
                );

                color2 = new Vector4(
                    int.Parse(EventDataModels[EventIndex].eventCommands[EventCommandIndex].parameters[13]) / 255.0f,
                    int.Parse(EventDataModels[EventIndex].eventCommands[EventCommandIndex].parameters[14]) / 255.0f,
                    int.Parse(EventDataModels[EventIndex].eventCommands[EventCommandIndex].parameters[15]) / 255.0f,
                    int.Parse(EventDataModels[EventIndex].eventCommands[EventCommandIndex].parameters[16]) / 255.0f
                );
            }

            float horizontal;
            float vertical;
            float inverse;
            int dir = int.Parse(EventDataModels[EventIndex].eventCommands[EventCommandIndex].parameters[8]);
            switch (dir)
            {
                case 0:
                    horizontal = 0f;
                    vertical = 1f;
                    inverse = 0f;
                    break;
                case 1:
                    horizontal = 1f;
                    vertical = 0f;
                    inverse = 0f;
                    break;
                case 2:
                    horizontal = 1f;
                    vertical = 1f;
                    inverse = 0f;
                    break;
                case 3:
                    horizontal = 1f;
                    vertical = 1f;
                    inverse = 1f;
                    break;
                default:
                    horizontal = 0f;
                    vertical = 1f;
                    inverse = 0f;
                    break;
            }

            float blend = int.Parse(EventDataModels[EventIndex].eventCommands[EventCommandIndex].parameters[17]);

            if (!IsBattle)
            {
                if (EventEditCanvas == null) return;
                EventEditCanvas.EffectRenderer?.ApplyParams(Color1, color1);
                EventEditCanvas.EffectRenderer?.ApplyParams(Color2, color2);
                EventEditCanvas.EffectRenderer?.ApplyParams(Horizontal, horizontal);
                EventEditCanvas.EffectRenderer?.ApplyParams(Vertical, vertical);
                EventEditCanvas.EffectRenderer?.ApplyParams(Inverse, inverse);
                EventEditCanvas.EffectRenderer?.ApplyParams(Blend, blend);
                EventEditCanvas.EffectRenderer?.ApplyParams(MixRatio, 1.0f);
            }
            else
            {
                if (PreviewSceneElement == null) return;
                PreviewSceneElement.EffectRenderer?.ApplyParams(Color1, color1);
                PreviewSceneElement.EffectRenderer?.ApplyParams(Color2, color2);
                PreviewSceneElement.EffectRenderer?.ApplyParams(Horizontal, horizontal);
                PreviewSceneElement.EffectRenderer?.ApplyParams(Vertical, vertical);
                PreviewSceneElement.EffectRenderer?.ApplyParams(Inverse, inverse);
                PreviewSceneElement.EffectRenderer?.ApplyParams(Blend, blend);
                PreviewSceneElement.EffectRenderer?.ApplyParams(MixRatio, 1.0f);
            }

        }
    }
}