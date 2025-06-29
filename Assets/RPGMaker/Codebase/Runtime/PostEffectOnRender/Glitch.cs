using UnityEngine;

namespace RPGMaker.Codebase.Runtime.PostEffectOnRender
{
    [ExecuteInEditMode, ImageEffectAllowedInSceneView]
    public class Glitch : PostEffectBase
    {
        private static readonly int GlitchIntensity = Shader.PropertyToID("_GlitchIntensity");
        private static readonly int BlockScale = Shader.PropertyToID("_BlockScale");
        private static readonly int NoiseSpeed = Shader.PropertyToID("_NoiseSpeed");
        private static readonly int WaveWidth = Shader.PropertyToID("_WaveWidth");
        private static readonly int ChromaticAberration = Shader.PropertyToID("_ChromaticAberration");

        private float _initialIntensity;
        private float _maxIntensity;
        private float _currentIntensity;

        protected override void Awake()
        {
            ShaderName = "Hidden/Glitch";
            base.Awake();
        }

        protected override void SetInitialParams()
        {
            _initialIntensity = _currentIntensity = 0.0f;
            _maxIntensity = Param[0] / 100.0f;
        }

        public override void UpdateParams(float ratio)
        {
            _currentIntensity = Mathf.Lerp(_initialIntensity, _maxIntensity, ratio);
        }

        public override void ApplyParams()
        {
            if (Material == null) return;
            if (Param == null) return;

            Material.SetFloat(GlitchIntensity, _currentIntensity);
            Material.SetFloat(BlockScale, Param[1]);
            Material.SetFloat(NoiseSpeed, Param[2]);
            Material.SetFloat(WaveWidth, Param[3] / 100.0f);
            Material.SetFloat(ChromaticAberration, Param[4] / 100.0f);
        }
    }
}