using UnityEngine;
using UnityEngine.Rendering;

namespace RPGMaker.Codebase.Runtime.PostEffectOnRender
{
    [ExecuteInEditMode, ImageEffectAllowedInSceneView]
    public class Bloom : PostEffectBase
    {
        private static readonly int Strength = Shader.PropertyToID("_Strength");
        private static readonly int Blur = Shader.PropertyToID("_Blur");
        private static readonly int Temp = Shader.PropertyToID("_Temp");
        
        private float _initialIntensity;
        private float _maxIntensity;
        private float _currentIntensity;
        
        private int _halfWidth;
        private int _halfHeight;

        protected override void Awake()
        {
            ShaderName = "Hidden/Bloom";
            base.Awake();
        }

        protected override void SetInitialParams()
        {
            _initialIntensity = _currentIntensity= 0.0f;
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

            var strength = _currentIntensity;
            var blur = _currentIntensity * 20.0f;

            strength = strength == 0 ? 0.01f : strength;
            blur = blur == 0 ? 1.0f : blur;

            Material.SetFloat(Strength, strength);
            Material.SetFloat(Blur, blur);
        }

        protected override void OnRenderImage(RenderTexture src, RenderTexture dest)
        {
            ApplyParams();

            if (Material != null)
            {
                RenderTexture rt1 = RenderTexture.GetTemporary(
                    src.width / 2,
                    src.height / 2,
                    src.depth,
                    src.format
                );
                Material.SetTexture(Temp, rt1);

                Graphics.Blit(src, rt1, Material, 0);
                Graphics.Blit(src, dest, Material, 1);

                RenderTexture.ReleaseTemporary(rt1);
            }
            else
            {
                Graphics.Blit(src, dest);
            }
        }

        public override RenderTextureDescriptor[] GetRenderTextureDescriptors(RenderTextureDescriptor cameraTextureDescriptor)
        {
            _halfWidth = cameraTextureDescriptor.width / 2;
            _halfHeight = cameraTextureDescriptor.height / 2;
            return base.GetRenderTextureDescriptors(cameraTextureDescriptor);
        }

#if UNITY_2022_1_OR_NEWER
        public override void AddBlit(CommandBuffer cmd, RTHandle src, RTHandle[] works)
#else
        public override void AddBlit(CommandBuffer cmd, RenderTargetIdentifier src, int[] works)
#endif
        {
            if (Material != null)
            {
                RenderTexture rt1 = RenderTexture.GetTemporary(
                    _halfWidth,
                    _halfHeight,
                    0
                );
                Material.SetTexture(Temp, rt1);
                cmd.Blit(src, rt1, Material, 0);
                cmd.Blit(src, works[0], Material, 1);
                cmd.Blit(works[0], src);
                RenderTexture.ReleaseTemporary(rt1);
            }
        }
    }
}