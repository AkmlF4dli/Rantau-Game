using System.Collections.Generic;
using RPGMaker.Codebase.CoreSystem.Knowledge.DataModel.Event;
using RPGMaker.Codebase.Editor.Common.View;
using RPGMaker.Codebase.Editor.DatabaseEditor.View;
using RPGMaker.Codebase.Editor.MapEditor.Component.Canvas;
using UnityEngine;
using UnityEngine.UIElements;

namespace RPGMaker.Codebase.Editor.MapEditor.Component.CommandEditor.PostEffect
{
    /**
     * ぼかしエフェクトエディター
     *
     * パラメーター（固定）
     * @see PostEffectEditorBase
     *
     * パラメーター
     * 3: 強さ（0で処理なし）
     * 4: 水平（1で適用）
     * 5: 垂直（1で適用）
     * 6: 放射（1で適用）
     *
     */
    public class PostEffectBlur : PostEffectEditorBase
    {
        private static readonly int Weights = Shader.PropertyToID("_Weights");
        private static readonly int OffsetX = Shader.PropertyToID("_OffsetX");
        private static readonly int OffsetY = Shader.PropertyToID("_OffsetY");
        private static readonly int RadialBlur = Shader.PropertyToID("_RadialBlur");

        private readonly float _offset = 1.5f;
        private float[] _weights = new float[10];
        private float _blur;

        public PostEffectBlur(
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
            SettingUxml = UxmlDir + "/inspector_mapEvent_post_effect_blur.uxml";
            ShaderName = "Hidden/Blur";
        }

        public override void Invoke()
        {
            base.Invoke();

            ApplyParams();
            SetDirty(true, EffectRenderer.BlitType.Blur);

            var intensitySliderArea =
                RootElement.Query<VisualElement>("intensity_sliderArea");
            SliderAndFieldBaseFix.IntegerSliderCallBack(
                intensitySliderArea,
                0,
                100,
                "",
                int.Parse(EventDataModels[EventIndex].eventCommands[EventCommandIndex].parameters[3]),
                evt =>
                {
                    EventDataModels[EventIndex].eventCommands[EventCommandIndex].parameters[3] = evt.ToString();
                    ApplyParams();
                    SetDirty(true, EffectRenderer.BlitType.Blur);
                    Save(EventDataModels[EventIndex]);
                });

            Toggle horizontalToggle =
                RootElement.Query<Toggle>("horizontal_toggle");
            if (EventDataModels[EventIndex].eventCommands[EventCommandIndex].parameters[4] == "1")
                horizontalToggle.value = true;
            horizontalToggle.RegisterValueChangedCallback(_ =>
            {
                EventDataModels[EventIndex].eventCommands[EventCommandIndex].parameters[4] =
                    horizontalToggle.value ? "1" : "0";
                ApplyParams();
                SetDirty(true, EffectRenderer.BlitType.Blur);
                Save(EventDataModels[EventIndex]);
            });

            Toggle verticalToggle =
                RootElement.Query<Toggle>("vertical_toggle");
            if (EventDataModels[EventIndex].eventCommands[EventCommandIndex].parameters[5] == "1")
                verticalToggle.value = true;
            verticalToggle.RegisterValueChangedCallback(_ =>
            {
                EventDataModels[EventIndex].eventCommands[EventCommandIndex].parameters[5] =
                    verticalToggle.value ? "1" : "0";
                ApplyParams();
                SetDirty(true, EffectRenderer.BlitType.Blur);
                Save(EventDataModels[EventIndex]);
            });

            Toggle radialToggle =
                RootElement.Query<Toggle>("radial_toggle");
            if (EventDataModels[EventIndex].eventCommands[EventCommandIndex].parameters[6] == "1")
                radialToggle.value = true;
            radialToggle.RegisterValueChangedCallback(_ =>
            {
                EventDataModels[EventIndex].eventCommands[EventCommandIndex].parameters[6] =
                    radialToggle.value ? "1" : "0";
                ApplyParams();
                SetDirty(true, EffectRenderer.BlitType.Blur);
                Save(EventDataModels[EventIndex]);
            });
        }

        protected override void AddParameter()
        {
            base.AddParameter();
            EventDataModels[EventIndex].eventCommands[EventCommandIndex].parameters.Add("50"); // Intensity
            EventDataModels[EventIndex].eventCommands[EventCommandIndex].parameters.Add("1"); // Horizontal
            EventDataModels[EventIndex].eventCommands[EventCommandIndex].parameters.Add("1"); // Vertical
            EventDataModels[EventIndex].eventCommands[EventCommandIndex].parameters.Add("0"); // Radial
        }

        protected override void ApplyParams()
        {
            var intensity = int.Parse(EventDataModels[EventIndex].eventCommands[EventCommandIndex].parameters[3]);
            _blur = 1000f * (intensity / 100f);
            var radialBlur = intensity / 100f;
            UpdateWeights();
            float x;
            float y;
            if (RenderTexture != null)
            {
                x = _offset / RenderTexture.width;
                y = _offset / RenderTexture.height;
            }
            else
            {
                x = 0f;
                y = 0f;
            }

            if (EventDataModels[EventIndex].eventCommands[EventCommandIndex].parameters[4] == "0") x = 0f;
            if (EventDataModels[EventIndex].eventCommands[EventCommandIndex].parameters[5] == "0") y = 0f;
            if (EventDataModels[EventIndex].eventCommands[EventCommandIndex].parameters[6] == "0") radialBlur = 0f;

            if (!IsBattle)
            {
                if (EventEditCanvas == null) return;
                EventEditCanvas.EffectRenderer?.ApplyParams(Weights, _weights);
                EventEditCanvas.EffectRenderer?.ApplyParams(OffsetX, new Vector4(x, 0, 0, 0));
                EventEditCanvas.EffectRenderer?.ApplyParams(OffsetY, new Vector4(0, y, 0, 0));
                EventEditCanvas.EffectRenderer?.ApplyParams(RadialBlur, radialBlur);
            }
            else
            {
                if (PreviewSceneElement == null) return;
                PreviewSceneElement.EffectRenderer?.ApplyParams(Weights, _weights);
                PreviewSceneElement.EffectRenderer?.ApplyParams(OffsetX, new Vector4(x, 0, 0, 0));
                PreviewSceneElement.EffectRenderer?.ApplyParams(OffsetY, new Vector4(0, y, 0, 0));
                PreviewSceneElement.EffectRenderer?.ApplyParams(RadialBlur, radialBlur);
            }
        }

        /**
         * ガウシアンウェイトを更新
         */
        private void UpdateWeights()
        {
            float total = 0;
            float d = _blur * _blur * 0.001f;
            d = d == 0 ? 0.001f : d;

            for (int i = 0; i < _weights.Length; i++)
            {
                // Offset position per x.
                float x = i * 2f;
                float w = Mathf.Exp(-0.5f * (x * x) / d);
                _weights[i] = w;

                if (i > 0)
                {
                    w *= 2.0f;
                }

                total += w;
            }

            for (int i = 0; i < _weights.Length; i++)
            {
                _weights[i] /= total;
            }
        }
    }
}