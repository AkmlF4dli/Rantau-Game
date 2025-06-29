using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

/// <summary>
/// メニューブラーのシェーダーで用いるパラメータ。
/// </summary>
[Serializable]
public class MenuBlurParams
{
    [Range(0.5f, 3.0f)]
    [Tooltip("Sampling disatance around")]
    public float samplingDistance = 1.5f;
    public Shader shader;
}

public class MenuBlurRendererFeature : ScriptableRendererFeature
{
    [SerializeField] private MenuBlurParams _parameters;
    MenuBlurRenderPass _blurRenderPass;

    public override void Create()
    {
        _blurRenderPass = new MenuBlurRenderPass(_parameters);
        name = "MenuBlur";
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
#if UNITY_2022_1_OR_NEWER
        renderer.EnqueuePass(_blurRenderPass);
#else
        if (_blurRenderPass.Setup(renderer))
        {
            renderer.EnqueuePass(_blurRenderPass);
        }
#endif
    }

    protected override void Dispose(bool disposing) {
        base.Dispose(disposing);
        if (_blurRenderPass != null)
        {
            _blurRenderPass.Dispose();
            _blurRenderPass = null;
        }
    }

    public MenuBlurParams GetParameters() { return _parameters; }
}
