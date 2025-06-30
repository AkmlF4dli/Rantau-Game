using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class MenuBlurRenderPass : ScriptableRenderPass, IDisposable
{
    private Material _material;
    private readonly MenuBlurParams _parameters;

#if UNITY_2022_1_OR_NEWER
#else
    private RenderTargetIdentifier source;
    private RenderTargetHandle blurTex;
    private int blurTexID;
#endif

    public static class ShaderID
    {
        public static readonly string BlurCaptureRTName = "_BlurTex";
    }

    private RTHandle _blurCaptureRT;

    public MenuBlurRenderPass(MenuBlurParams parameters) {
        _parameters = parameters;
        _material = CoreUtils.CreateEngineMaterial(_parameters.shader);
#if UNITY_2022_1_OR_NEWER
        renderPassEvent = RenderPassEvent.AfterRenderingTransparents;
#endif
    }

#if UNITY_2022_1_OR_NEWER
#else
    public bool Setup(ScriptableRenderer renderer)
    {
        source = renderer.cameraColorTarget;
        renderPassEvent = RenderPassEvent.AfterRenderingTransparents;

        return true;
    }
#endif

#if UNITY_2022_1_OR_NEWER
    public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData) {
        base.OnCameraSetup(cmd, ref renderingData);

        // ブラー適用のための一時バッファ確保
        var rtDescriptor = renderingData.cameraData.cameraTargetDescriptor;
        rtDescriptor.depthBufferBits = 0;
        RenderingUtils.ReAllocateIfNeeded(ref _blurCaptureRT, rtDescriptor, FilterMode.Bilinear, TextureWrapMode.Clamp, name: ShaderID.BlurCaptureRTName);
    }
#endif

#if UNITY_2022_1_OR_NEWER
#else
    public override void Configure(CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescriptor)
    {
        blurTexID = Shader.PropertyToID("_BlurTex");
        blurTex = new RenderTargetHandle();
        blurTex.id = blurTexID;
        cmd.GetTemporaryRT(blurTex.id, cameraTextureDescriptor);

        base.Configure(cmd, cameraTextureDescriptor);
    }
#endif

    public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
    {
        CommandBuffer cmd = CommandBufferPool.Get("Blur Post Process");

        _material.SetFloat("_SamplingDistance", _parameters.samplingDistance);

        // Execute effect using effect material with two passes.
#if UNITY_2022_1_OR_NEWER
        var cameraColorRT = renderingData.cameraData.renderer.cameraColorTargetHandle;
        if (cameraColorRT == null || cameraColorRT.rt == null)
        {
            return;
        }
        cmd.Blit(cameraColorRT, _blurCaptureRT, _material, 0);
        cmd.Blit(_blurCaptureRT, cameraColorRT, _material, 1);
#else
        cmd.Blit(source, blurTex.id, _material, 0);
        cmd.Blit(blurTex.id, source, _material, 1);
#endif

        context.ExecuteCommandBuffer(cmd);
        cmd.Clear();
        CommandBufferPool.Release(cmd);
    }

#if UNITY_2022_1_OR_NEWER
    public void Dispose() {
        if (_material != null)
        {
            CoreUtils.Destroy(_material);
            _material = null;
        }
        _blurCaptureRT?.Release();
        _blurCaptureRT = null;
    }
#else
    public void Dispose() {
    }
    public override void FrameCleanup(CommandBuffer cmd)
    {
        cmd.ReleaseTemporaryRT(blurTexID);
        base.FrameCleanup(cmd);
    }
#endif

}
