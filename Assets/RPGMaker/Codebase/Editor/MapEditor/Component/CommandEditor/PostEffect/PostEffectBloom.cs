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
     * 発光エフェクトエディター
     *
     * パラメーター（固定）
     * @see PostEffectEditorBase
     *
     * パラメーター
     * 3: 強さ（0で処理なし）
     *
     */
    public class PostEffectBloom : PostEffectEditorBase
    {
        private static readonly int Strength = Shader.PropertyToID("_Strength");
        private static readonly int Blur = Shader.PropertyToID("_Blur");

        public PostEffectBloom(
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
            SettingUxml = UxmlDir + "/inspector_mapEvent_post_effect_bloom.uxml";
            ShaderName = "Hidden/Bloom";
        }

        public override void Invoke()
        {
            base.Invoke();

            ApplyParams();
            SetDirty(true, EffectRenderer.BlitType.Bloom);

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
                    SetDirty(true, EffectRenderer.BlitType.Bloom);
                    Save(EventDataModels[EventIndex]);
                });
        }

        // TODO: 名前変更（applyParams）
        protected override void AddParameter()
        {
            base.AddParameter();
            EventDataModels[EventIndex].eventCommands[EventCommandIndex].parameters.Add("50");
        }

        protected override void ApplyParams()
        {
            var intensity =
                int.Parse(EventDataModels[EventIndex].eventCommands[EventCommandIndex].parameters[3]) / 100.0f;
            var strength = intensity;
            var blur = intensity * 20.0f;

            strength = strength == 0 ? 0.01f : strength;
            blur = blur == 0 ? 1.0f : blur;

            if (!IsBattle)
            {
                if (EventEditCanvas == null) return;
                EventEditCanvas.EffectRenderer?.ApplyParams(Strength, strength);
                EventEditCanvas.EffectRenderer?.ApplyParams(Blur, blur);
            }
            else
            {
                if (PreviewSceneElement == null) return;
                PreviewSceneElement.EffectRenderer?.ApplyParams(Strength, strength);
                PreviewSceneElement.EffectRenderer?.ApplyParams(Blur, blur);
            }
        }
    }
}