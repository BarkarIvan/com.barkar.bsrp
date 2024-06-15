using System.Collections.Generic;
using Barkar.BSRP.CameraRenderer;
using Barkar.BSRP.Passes;
using Barkar.BSRP.Settings;
using Barkar.BSRP.Settings.Shadows;
using UnityEngine;
using UnityEngine.Experimental.Rendering.RenderGraphModule;
using UnityEngine.Rendering;

public class BSRP : RenderPipeline
{
    //TODO: rreefactor
    private bool _hdr;
    private float _renderScale;

    private readonly RenderGraph RenderGraph = new RenderGraph("BSRP Render Graph");

    private static readonly ShaderTagId[] _commonShaderTags = { new ShaderTagId("BSRPLightMode") };

    private Vector2Int textureSize = default;
    private RenderGraphParameters renderGraphParameters;
    
    private ShadowSettings _shadowSettings;
    private Material _finalPassMaterial;

    public BSRP(bool hdr, float renderScale, ShadowSettings shadowSettings)
    {
        _hdr = hdr;
        _renderScale = renderScale;
        _finalPassMaterial = CoreUtils.CreateEngineMaterial("Hidden/BSRP/Camera Renderer");
        QualitySettings.shadows = ShadowQuality.All;
        GraphicsSettings.useScriptableRenderPipelineBatching = true;
        _shadowSettings = shadowSettings;
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
            //TODO SETTINGS
            cullingParameters.shadowDistance = Mathf.Min(camera.farClipPlane, _shadowSettings.ShadowDistance); 
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

            using (RenderGraph.RecordAndExecute(renderGraphParameters))
            {
                using var _ = new RenderGraphProfilingScope(RenderGraph, new ProfilingSampler("Camera_" + camera.name));
                
                LightingResources lightingResources = LightingPass.ExecuteLightngPass(RenderGraph, cullingResults, _shadowSettings);

                //setup destinations
                RenderDestinationTextures destinationTextures =
                    SetupPass.SetupDestinationTextures(RenderGraph, textureSize, camera, _hdr);
                //draw opaque
                DrawGeometryPass.DrawGeometry(RenderGraph, _commonShaderTags, camera, cullingResults,
                    destinationTextures, camera.cullingMask, true, lightingResources);

                if (camera.clearFlags == CameraClearFlags.Skybox)
                {
                    DrawSkyboxPass.DrawSkybox(RenderGraph, destinationTextures, camera);
                }

                DrawGeometryPass.DrawGeometry(RenderGraph, _commonShaderTags, camera, cullingResults,
                    destinationTextures, camera.cullingMask, false, lightingResources);

                //final pass
                FinalPass.DrawFinalPass(RenderGraph, destinationTextures, camera, _finalPassMaterial);
                

            }

            context.ExecuteCommandBuffer(renderGraphParameters.commandBuffer);
            context.Submit();
            CommandBufferPool.Release(renderGraphParameters.commandBuffer);
        }

        RenderGraph.EndFrame();
    }


    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        CoreUtils.Destroy(_finalPassMaterial);
        RenderGraph.Cleanup();
    }

    //old version
    protected override void Render(ScriptableRenderContext context, Camera[] cameras)
    {
    }
}