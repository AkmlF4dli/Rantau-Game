using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using RPGMaker.Codebase.Runtime.PostEffectOnRender;
using System;

public class PostEffectRenderPass : ScriptableRenderPass, IDisposable
{
    private readonly PostEffectBase _parameters;

#if UNITY_2022_1_OR_NEWER
#else
    private RenderTargetIdentifier source;
    private RenderTargetHandle[] workTexList;
    private int[] workTexIDList;
#endif

    public static class ShaderID
    {
        public static readonly string WorkCaptureRTName = "_WorkTex{0}";
    }

    private RTHandle[] _workCaptureRTList;

    public PostEffectRenderPass(PostEffectBase parameters) {
        //Debug.Log($"parameters: {parameters}");
        _parameters = parameters;
        //_material = CoreUtils.CreateEngineMaterial(_parameters.shader);
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
        //Debug.Log($"OnCameraSetup() called");
        base.OnCameraSetup(cmd, ref renderingData);

        // ポストエフェクト適用のための一時バッファ確保
        //var rtDescriptor = renderingData.cameraData.cameraTargetDescriptor;
        //rtDescriptor.depthBufferBits = 0;
        //RenderingUtils.ReAllocateIfNeeded(ref _workCaptureRT, rtDescriptor, FilterMode.Bilinear, TextureWrapMode.Clamp, name: ShaderID.WorkCaptureRTName);
        var descriptors = _parameters.GetRenderTextureDescriptors(renderingData.cameraData.cameraTargetDescriptor);
        if (descriptors != null && _workCaptureRTList == null)
        {
            _workCaptureRTList = new RTHandle[descriptors.Length];
            for (int i = 0; i < descriptors.Length; i++)
            {
                var descriptor = descriptors[i];
                descriptor.depthBufferBits = 0;
                RenderingUtils.ReAllocateIfNeeded(ref _workCaptureRTList[i], descriptor, FilterMode.Bilinear, TextureWrapMode.Clamp, name: string.Format(ShaderID.WorkCaptureRTName, i));
            }
        }
    }
#endif

#if UNITY_2022_1_OR_NEWER
#else
    public override void Configure(CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescriptor)
    {
        var descriptors = _parameters.GetRenderTextureDescriptors(cameraTextureDescriptor);
        //Debug.Log($"descriptors: {descriptors}, {descriptors?.Length}");
        if (descriptors != null)
        {
            workTexList = new RenderTargetHandle[descriptors.Length];
            workTexIDList = new int[descriptors.Length];
            for (int i = 0; i < descriptors.Length; i++)
            {
                workTexIDList[i] = Shader.PropertyToID(string.Format(ShaderID.WorkCaptureRTName, i));
                workTexList[i] = new RenderTargetHandle();
                workTexList[i].id = workTexIDList[i];
                cmd.GetTemporaryRT(workTexList[i].id, descriptors[i]);
            }
        }

        base.Configure(cmd, cameraTextureDescriptor);
    }
#endif

    public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
    {
        /*Debug.Log($"_parameters: {_parameters}");
        if (_parameters == null)
        {
            return;
        }*/
        var cmd = CommandBufferPool.Get($"{_parameters.ShaderName}/Post Process");

        //_material.SetFloat("_SamplingDistance", _parameters.samplingDistance);
        _parameters.ApplyParams();
        var material = _parameters.Material;

        // Execute effect using effect material with two passes.
#if UNITY_2022_1_OR_NEWER
        var cameraColorRT = renderingData.cameraData.renderer.cameraColorTargetHandle;
        if (cameraColorRT == null || cameraColorRT.rt == null)
        {
            return;
        }
        //cmd.Blit(cameraColorRT, _workCaptureRT, material, 0);
        //cmd.Blit(_workCaptureRT, cameraColorRT, material, 1);
        _parameters.AddBlit(cmd, cameraColorRT, _workCaptureRTList);
#else
        //cmd.Blit(source, workTex.id, material, 0);
        //cmd.Blit(workTex.id, source, material, 1);
        _parameters.AddBlit(cmd, source, workTexIDList);
#endif

        context.ExecuteCommandBuffer(cmd);
        cmd.Clear();
        CommandBufferPool.Release(cmd);
    }

#if UNITY_2022_1_OR_NEWER
    public void Dispose() {
        //Debug.Log($"PostEffectRenderPass.Dispose() called");
        if (_workCaptureRTList != null)
        {
            foreach (var handle in _workCaptureRTList)
            {
                handle?.Release();
            }
            _workCaptureRTList = null;
        }
    }
#else
    public void Dispose() {
    }
    public override void FrameCleanup(CommandBuffer cmd)
    {
        foreach (var texId in workTexIDList)
        {
            cmd.ReleaseTemporaryRT(texId);
        }
        base.FrameCleanup(cmd);
    }
#endif

}
