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
     * グリッチエフェクトエディター
     *
     * パラメーター（固定）
     * @see PostEffectEditorBase
     *
     * パラメーター
     * 3: 強さ（0で処理なし）
     * 4: ブロックサイズ
     * 5: ノイズスピード
     * 6: 波
     * 7: 色収差
     *
     */
    public class PostEffectGlitch : PostEffectEditorBase
    {
        private static readonly int GlitchIntensity = Shader.PropertyToID("_GlitchIntensity");
        private static readonly int BlockScale = Shader.PropertyToID("_BlockScale");
        private static readonly int NoiseSpeed = Shader.PropertyToID("_NoiseSpeed");
        private static readonly int WaveWidth = Shader.PropertyToID("_WaveWidth");
        private static readonly int ChromaticAberration = Shader.PropertyToID("_ChromaticAberration");

        public PostEffectGlitch(
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
            SettingUxml = UxmlDir + "/inspector_mapEvent_post_effect_glitch.uxml";
            ShaderName = "Hidden/Glitch";
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
                    EventDataModels[EventIndex].eventCommands[EventCommandIndex].parameters[3] = evt.ToString();
                    ApplyParams();
                    SetDirty();
                    Save(EventDataModels[EventIndex]);
                });

            var sizeSliderArea =
                RootElement.Query<VisualElement>("size_sliderArea");
            SliderAndFieldBaseFix.IntegerSliderCallBack(
                sizeSliderArea,
                0,
                100,
                "",
                int.Parse(EventDataModels[EventIndex].eventCommands[EventCommandIndex].parameters[4]),
                evt =>
                {
                    EventDataModels[EventIndex].eventCommands[EventCommandIndex].parameters[4] = evt.ToString();
                    ApplyParams();
                    SetDirty();
                    Save(EventDataModels[EventIndex]);
                });

            var speedSliderArea =
                RootElement.Query<VisualElement>("speed_sliderArea");
            SliderAndFieldBaseFix.IntegerSliderCallBack(
                speedSliderArea,
                0,
                100,
                "",
                int.Parse(EventDataModels[EventIndex].eventCommands[EventCommandIndex].parameters[5]),
                evt =>
                {
                    EventDataModels[EventIndex].eventCommands[EventCommandIndex].parameters[5] = evt.ToString();
                    ApplyParams();
                    SetDirty();
                    Save(EventDataModels[EventIndex]);
                });
            
            var waveArea =
                RootElement.Query<VisualElement>("wave_sliderArea");
            SliderAndFieldBaseFix.IntegerSliderCallBack(
                waveArea,
                0,
                100,
                "",
                int.Parse(EventDataModels[EventIndex].eventCommands[EventCommandIndex].parameters[6]),
                evt =>
                {
                    EventDataModels[EventIndex].eventCommands[EventCommandIndex].parameters[6] = evt.ToString();
                    ApplyParams();
                    SetDirty();
                    Save(EventDataModels[EventIndex]);
                });

            var chromaticAberrationArea =
                RootElement.Query<VisualElement>("chroma_sliderArea");
            SliderAndFieldBaseFix.IntegerSliderCallBack(
                chromaticAberrationArea,
                0,
                100,
                "",
                int.Parse(EventDataModels[EventIndex].eventCommands[EventCommandIndex].parameters[7]),
                evt =>
                {
                    EventDataModels[EventIndex].eventCommands[EventCommandIndex].parameters[7] = evt.ToString();
                    ApplyParams();
                    SetDirty();
                    Save(EventDataModels[EventIndex]);
                });
        }

        protected override void AddParameter()
        {
            base.AddParameter();
            EventDataModels[EventIndex].eventCommands[EventCommandIndex].parameters.Add("50");
            EventDataModels[EventIndex].eventCommands[EventCommandIndex].parameters.Add("50");
            EventDataModels[EventIndex].eventCommands[EventCommandIndex].parameters.Add("50");
            EventDataModels[EventIndex].eventCommands[EventCommandIndex].parameters.Add("50");
            EventDataModels[EventIndex].eventCommands[EventCommandIndex].parameters.Add("50");
        }

        protected override void ApplyParams()
        {
            var intensity =
                int.Parse(EventDataModels[EventIndex].eventCommands[EventCommandIndex].parameters[3]);
            var blockSize =
                int.Parse(EventDataModels[EventIndex].eventCommands[EventCommandIndex].parameters[4]);
            var noiseSpeed =
                int.Parse(EventDataModels[EventIndex].eventCommands[EventCommandIndex].parameters[5]);
            var waveWidth =
                int.Parse(EventDataModels[EventIndex].eventCommands[EventCommandIndex].parameters[6]);
            var chromaticAberration =
                int.Parse(EventDataModels[EventIndex].eventCommands[EventCommandIndex].parameters[7]);

            if (!IsBattle)
            {
                if (EventEditCanvas == null) return;
                EventEditCanvas.EffectRenderer?.ApplyParams(GlitchIntensity, intensity / 100.0f);
                EventEditCanvas.EffectRenderer?.ApplyParams(BlockScale, blockSize);
                EventEditCanvas.EffectRenderer?.ApplyParams(NoiseSpeed, noiseSpeed);
                EventEditCanvas.EffectRenderer?.ApplyParams(WaveWidth, waveWidth / 100.0f);
                EventEditCanvas.EffectRenderer?.ApplyParams(ChromaticAberration, chromaticAberration / 100.0f);
            }
            else
            {
                if (PreviewSceneElement == null) return;
                PreviewSceneElement.EffectRenderer?.ApplyParams(GlitchIntensity, intensity / 100.0f);
                PreviewSceneElement.EffectRenderer?.ApplyParams(BlockScale, blockSize);
                PreviewSceneElement.EffectRenderer?.ApplyParams(NoiseSpeed, noiseSpeed);
                PreviewSceneElement.EffectRenderer?.ApplyParams(WaveWidth, waveWidth / 100.0f);
                PreviewSceneElement.EffectRenderer?.ApplyParams(ChromaticAberration, chromaticAberration / 100.0f);
            }
        }
    }
}