using UnityEngine;
using UnityEngine.Rendering;

namespace RPGMaker.Codebase.Runtime.PostEffectOnRender
{
    [ExecuteInEditMode, ImageEffectAllowedInSceneView]
    public class Blur : PostEffectBase
    {
        private static readonly int Weights = Shader.PropertyToID("_Weights");
        private static readonly int OffsetX = Shader.PropertyToID("_OffsetX");
        private static readonly int OffsetY = Shader.PropertyToID("_OffsetY");
        private static readonly int RadialBlur = Shader.PropertyToID("_RadialBlur");

        private readonly float _offset = 1.5f;
        private float[] _weights = new float[10];
        private float _initialBlur;
        private float _maxBlur;
        private float _currentBlur;
        private float _initialRadialBlur;
        private float _maxRadialBlur;
        private float _currentRadialBlur;

        protected override void Awake()
        {
            ShaderName = "Hidden/Blur";
            base.Awake();
        }

        protected override void SetInitialParams()
        {
            _initialBlur = _currentBlur = 0.0f;
            _initialRadialBlur = _currentRadialBlur = 0.0f;

            var intensity = Param[0];
            _maxBlur = 1000f * (intensity / 100f);
            _maxRadialBlur = intensity / 100f;
        }

        public override void UpdateParams(float ratio)
        {
            _currentBlur = Mathf.Lerp(_initialBlur, _maxBlur, ratio);
            _currentRadialBlur = Mathf.Lerp(_initialRadialBlur, _maxRadialBlur, ratio);
        }

        public override void ApplyParams()
        {
            if (Material == null) return;
            if (Param == null) return;

            UpdateWeights();

            float x = _offset / Screen.width;
            float y = _offset / Screen.height;
            float r = _currentRadialBlur;
            if (Param[1] == 0) x = 0f;
            if (Param[2] == 0) y = 0f;
            if (Param[3] == 0) r = 0f;

            Material.SetFloatArray(Weights, _weights);
            Material.SetVector(OffsetX, new Vector4(x, 0, 0, 0));
            Material.SetVector(OffsetY, new Vector4(0, y, 0, 0));
            Material.SetFloat(RadialBlur, r);
        }

        protected override void OnRenderImage(RenderTexture src, RenderTexture dest)
        {
            ApplyParams();

            if (Material != null)
            {
                RenderTexture rt1 = RenderTexture.GetTemporary(
                    src.width,
                    src.height,
                    src.depth,
                    src.format
                );
                RenderTexture rt2 = RenderTexture.GetTemporary(
                    src.width,
                    src.height,
                    src.depth,
                    src.format
                );

                Graphics.Blit(src, rt1);
                Graphics.Blit(rt1, rt2, Material, 0);
                Graphics.Blit(rt2, rt1, Material, 0);
                Graphics.Blit(rt1, rt2, Material, 1);
                Graphics.Blit(rt2, dest);

                RenderTexture.ReleaseTemporary(rt1);
                RenderTexture.ReleaseTemporary(rt2);
            }
            else
            {
                Graphics.Blit(src, dest);
            }
        }

        /**
         * ガウシアンウェイトを更新
         */
        private void UpdateWeights()
        {
            float total = 0;
            float d = _currentBlur * _currentBlur * 0.001f;
            d = d == 0 ? 0.001f : d;

            for (int i = 0; i < _weights.Length; i++)
            {
                float x = i * 2f;
                float w = Mathf.Exp(-0.5f * (x * x) / d);
                _weights[i] = w;
                if (i > 0) w *= 2.0f;
                total += w;
            }

            for (int i = 0; i < _weights.Length; i++)
            {
                _weights[i] /= total;
            }
        }

        public override RenderTextureDescriptor[] GetRenderTextureDescriptors(RenderTextureDescriptor cameraTextureDescriptor) {
            var descriptors = new RenderTextureDescriptor[2];
            for (int i = 0; i < descriptors.Length; i++)
            {
                descriptors[i] = cameraTextureDescriptor;
                descriptors[i].depthBufferBits = 0;
            }
            return descriptors;
        }

#if UNITY_2022_1_OR_NEWER
        public override void AddBlit(CommandBuffer cmd, RTHandle src, RTHandle[] works)
#else
        public override void AddBlit(CommandBuffer cmd, RenderTargetIdentifier src, int[] works)
#endif
        {
            if (Material != null)
            {
                cmd.Blit(src, works[0]);
                cmd.Blit(works[0], works[1], Material, 0);
                cmd.Blit(works[1], works[0], Material, 0);
                cmd.Blit(works[0], works[1], Material, 1);
                cmd.Blit(works[1], src);
            }

        }

    }
}