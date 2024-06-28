using System.Collections.Generic;
using Barkar.BSRP.CameraRenderer;
using Barkar.BSRP.Passes;
using Barkar.BSRP.Passes.Bloom;
using Barkar.BSRP.Settings.Shadows;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;

public class BSRP : RenderPipeline
{
    private bool _hdr;
    private float _renderScale;

    private readonly RenderGraph RenderGraph = new RenderGraph("BSRP Render Graph");

    private static readonly ShaderTagId[] _commonShaderTags = { new ShaderTagId("BSRPLightMode"), new ShaderTagId("SRPDefaultUnlit") };

    private Vector2Int textureSize = default;
    private RenderGraphParameters renderGraphParameters;

    private ShadowSettings _shadowSettings;
    private BloomSettings _bloomSettings;
    
    private Material _finalPassMaterial;
    //private Material _customBloomMaterial;
    //private Material _dualFilterBlurMaterial;
    
    
    public BSRP(bool hdr, float renderScale, ShadowSettings shadowSettings, BloomSettings bloomSettings)
    {
        _hdr = hdr;
        _renderScale = renderScale;
      //  _finalPassMaterial = CoreUtils.CreateEngineMaterial("Hidden/FinalPass");
       // _customBloomMaterial = CoreUtils.CreateEngineMaterial("Hidden/CustomBloom");
        
        QualitySettings.shadows = ShadowQuality.All;
        GraphicsSettings.useScriptableRenderPipelineBatching = true;
        _shadowSettings = shadowSettings;
        _bloomSettings = bloomSettings;
    }
    
    
    protected override void Render(ScriptableRenderContext context, List<Camera> cameras)
    {
        BeginContextRendering(context, cameras);


        foreach (Camera camera in cameras)
        {
            BeginCameraRendering(context, camera);

            //destination buffer size
            textureSize.x = (int)(camera.pixelWidth * _renderScale);
            textureSize.y = (int)(camera.pixelHeight * _renderScale);
            
            if (!camera.TryGetCullingParameters(out ScriptableCullingParameters cullingParameters)) continue;
            cullingParameters.shadowDistance = Mathf.Min(camera.farClipPlane, _shadowSettings.ShadowMaxDistance);
            CullingResults cullingResults = context.Cull(ref cullingParameters);

#if UNITY_EDITOR
            if (camera.cameraType == CameraType.SceneView)
            {
                ScriptableRenderContext.EmitWorldGeometryForSceneView(camera);
                textureSize.x = camera.pixelWidth;
                textureSize.y = camera.pixelHeight;
            }
#endif
            renderGraphParameters = new RenderGraphParameters
            {
                commandBuffer = CommandBufferPool.Get(),
                currentFrameIndex = Time.frameCount,
                executionName = "Camera Render Graph",
                rendererListCulling = true,
                scriptableRenderContext = context
            };

            RenderGraph.BeginRecording(renderGraphParameters);
          
            LightingResources lightingResources =
                LightingPass.ExecuteLightngPass(RenderGraph, cullingResults, _shadowSettings);
           
            RenderDestinationTextures destinationTextures =
                SetupPass.SetupDestinationTextures(RenderGraph, textureSize, camera, _hdr);
            
            DrawGeometryPass.DrawGeometry(RenderGraph, _commonShaderTags, camera, cullingResults,
                destinationTextures, camera.cullingMask, true, lightingResources);
           
            if (camera.clearFlags == CameraClearFlags.Skybox)
                DrawSkyboxPass.DrawSkybox(RenderGraph, destinationTextures, camera);
           
            DrawGeometryPass.DrawGeometry(RenderGraph, _commonShaderTags, camera, cullingResults,
                destinationTextures, camera.cullingMask, false, lightingResources);
           
            //postprocess
            BloomPass.DrawBloom(camera, RenderGraph, _bloomSettings, destinationTextures, textureSize);
            
          //  FinalPass.DrawFinalPass(RenderGraph, destinationTextures, camera, _finalPassMaterial);

            RenderGraph.EndRecordingAndExecute();
        }

        context.ExecuteCommandBuffer(renderGraphParameters.commandBuffer);
        context.Submit();
        CommandBufferPool.Release(renderGraphParameters.commandBuffer);

        RenderGraph.EndFrame();
    }
    
    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        CoreUtils.Destroy(_finalPassMaterial);
        RenderGraph.Cleanup();
    }

    protected override void Render(ScriptableRenderContext context, Camera[] cameras)
    {
    }
}