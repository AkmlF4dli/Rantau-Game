using RPGMaker.Codebase.Runtime.PostEffectOnRender;
using System;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class PostEffectRendererFeature : ScriptableRendererFeature
{
    private PostEffectBase _parameters = null;
    PostEffectRenderPass _postEffectRenderPass;

    static int _counter = 0;
    public override void Create() {
        //Debug.Log($"PostEffectRendererFeature.Create called");
        name = "PostEffect";
        if (_parameters != null)
        {
            if (_postEffectRenderPass == null)
            {
                _postEffectRenderPass = new PostEffectRenderPass(_parameters);
            }
            name = _parameters.ShaderName;
        }
    }

    public void SetParameters(PostEffectBase parameters) {
        _parameters = parameters;
        //Debug.Log($"SetParameters: {_parameters}, {_parameters?.ShaderName}");
        _postEffectRenderPass = new PostEffectRenderPass(_parameters);
        name = _parameters.ShaderName;
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        //Debug.Log($"_postEffectRenderPass: {_postEffectRenderPass}");
        if (_postEffectRenderPass == null) return;
#if UNITY_2022_1_OR_NEWER
        renderer.EnqueuePass(_postEffectRenderPass);
#else
        if (_postEffectRenderPass.Setup(renderer))
        {
            renderer.EnqueuePass(_postEffectRenderPass);
        }
#endif
    }

    protected override void Dispose(bool disposing) {
        //Debug.Log($"PostEffectRendererFeature.Dispose called: disposing: {disposing}, name: {name}, _parameters.ShaderName: {_parameters?.ShaderName}");
        base.Dispose(disposing);
        if (_postEffectRenderPass != null)
        {
            _postEffectRenderPass.Dispose();
            _postEffectRenderPass = null;
        }
    }
}
