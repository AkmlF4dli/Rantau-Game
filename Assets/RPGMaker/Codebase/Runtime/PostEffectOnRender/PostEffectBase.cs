using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace RPGMaker.Codebase.Runtime.PostEffectOnRender
{
    [ExecuteInEditMode, ImageEffectAllowedInSceneView]
    public class PostEffectBase : MonoBehaviour, IDisposable
    {
        private Material _material;

        private int[] _param;

        public Material Material
        {
            get => _material;
            set => _material = value;
        }

        public int[] Param
        {
            get => _param;
            set
            {
                _param = value;
                SetInitialParams();
            }
        }

        public string ShaderName = "";

        protected virtual void Awake()
        {
            if (string.IsNullOrEmpty(ShaderName)) _material = null;
            else _material = new Material(Shader.Find(ShaderName));

            enabled = false;
        }

        public virtual void ApplyParams()
        {
        }

        protected virtual void SetInitialParams()
        {
        }

        /**
         * パラメータを更新
         * @param ratio パラメータ初期値と最大値の比率
         */
        public virtual void UpdateParams(float ratio)
        {
        }

        protected virtual void OnRenderImage(RenderTexture src, RenderTexture dest)
        {
            ApplyParams();

            if (_material != null)
            {
                Graphics.Blit(src, dest, _material);
            }
            else
            {
                Graphics.Blit(src, dest);
            }
        }

        public virtual RenderTextureDescriptor[] GetRenderTextureDescriptors(RenderTextureDescriptor cameraTextureDescriptor) {
            var descriptors = new RenderTextureDescriptor[] { cameraTextureDescriptor };
            descriptors[0].depthBufferBits = 0;
            return descriptors;
        }

#if UNITY_2022_1_OR_NEWER
        public virtual void AddBlit(CommandBuffer cmd, RTHandle src, RTHandle[] works)
#else
        public virtual void AddBlit(CommandBuffer cmd, RenderTargetIdentifier src, int[] works)
#endif
        {
            if (_material != null)
            {
                cmd.Blit(src, works[0], _material);
                cmd.Blit(works[0], src);
            }
            else
            {
                //cmd.Blit(src, src);
            }
        }

        public void Dispose() {
            if (_material != null)
            {
                Destroy(_material);
                _material = null;
            }
        }
    }
}