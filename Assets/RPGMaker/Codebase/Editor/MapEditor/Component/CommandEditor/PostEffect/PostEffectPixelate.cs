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
     * モザイクエフェクトエディター
     *
     * パラメーター（固定）
     * @see PostEffectEditorBase
     *
     * パラメーター
     * 3: ピクセル数（0でテクスチャ解像度）
     *
     */
    public class PostEffectPixelate : PostEffectEditorBase
    {
        private static readonly int NumPixelX = Shader.PropertyToID("_NumPixelX");
        private static readonly int NumPixelY = Shader.PropertyToID("_NumPixelY");

        public PostEffectPixelate(
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
            SettingUxml = UxmlDir + "/inspector_mapEvent_post_effect_pixelate.uxml";
            ShaderName = "Hidden/Pixelate";
        }

        public override void Invoke()
        {
            base.Invoke();

            ApplyParams();
            SetDirty();

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
                    EventDataModels[EventIndex].eventCommands[EventCommandIndex].parameters[3] =
                        evt.ToString();
                    ApplyParams();
                    SetDirty();
                    Save(EventDataModels[EventIndex]);
                });
        }

        protected override void AddParameter()
        {
            base.AddParameter();
            EventDataModels[EventIndex].eventCommands[EventCommandIndex].parameters.Add("50");
        }

        protected override void ApplyParams()
        {
            var intensity =
                int.Parse(EventDataModels[EventIndex].eventCommands[EventCommandIndex].parameters[3]);

            intensity = intensity == 0 ? 1 : intensity + 1;

            Vector2 resolution = RenderTexture == null
                ? Vector2.zero
                : new Vector2(RenderTexture.width, RenderTexture.height);
            var numPixelX = resolution.x / intensity;
            var numPixelY = resolution.y / intensity;

            if (!IsBattle)
            {
                if (EventEditCanvas == null) return;
                EventEditCanvas.EffectRenderer?.ApplyParams(NumPixelX, numPixelX);
                EventEditCanvas.EffectRenderer?.ApplyParams(NumPixelY, numPixelY);
            }
            else
            {
                if (PreviewSceneElement == null) return;
                PreviewSceneElement.EffectRenderer?.ApplyParams(NumPixelX, numPixelX);
                PreviewSceneElement.EffectRenderer?.ApplyParams(NumPixelY, numPixelY);
            }
        }
    }
}