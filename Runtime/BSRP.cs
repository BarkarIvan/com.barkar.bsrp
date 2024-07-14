using System.Collections.Generic;
using Barkar.BSRP;
using Barkar.BSRP.CameraRenderer;
using Barkar.BSRP.Passes;
using Barkar.BSRP.Passes.Bloom;
using Barkar.BSRP.Passes.Setup;
using Barkar.BSRP.Settings.Shadows;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;

public class BSRP : RenderPipeline
{
    private float _renderScale;

    private RenderGraph RenderGraph = new RenderGraph("BSRP Render Graph");

    private readonly ShaderTagId[] _commonShaderTags =
        { new ShaderTagId("BSRPGBuffer"), new ShaderTagId("SRPDefaultUnlit") };

    private Vector2Int textureSize = default;
    private RenderGraphParameters renderGraphParameters;

    private ShadowSettings _shadowSettings;
    private BloomSettings _bloomSettings;

    // private Material _finalPassMaterial;
    //
    private Material _defferedLightingMaterial;

    private Material _deferredFinalPassMaterial;

    private Material _screenSpaceShadowMaterial;

    private ComputeShader _tiledDeferredShadingComputeShader;
    //
    // private Material _postEffectsMaterial;
    //private Material _dualFilterBlurMaterial;

    private LocalKeyword _useLensDirtKeyword;
    private LocalKeyword _useBloomKeyword;

    private LightingPass _lightingPass = new LightingPass();
    private SetupPass _setupPass = new SetupPass();

    private DrawGeometryPass _drawGeometryPass = new DrawGeometryPass();

    //private PostEffectsPass _postEffectsPass = new PostEffectsPass();
    private DirectionalLightPass _directionalLight = new DirectionalLightPass();
    private DeferredFinalPass _deferredFinalPass = new DeferredFinalPass();
    private ScreenSpaceShadowPass _screenSpaceShadowPass = new ScreenSpaceShadowPass();
    
    //!
    private TiledShadingPass _tileShadingPass = new TiledShadingPass();
    //
    
    Matrix4x4 _matrixVP;
    Matrix4x4 _matrixVPI;
    Matrix4x4 _matrixVPprev;
    Matrix4x4 _matrixVPIprev;

    public BSRP(bool hdr, float renderScale, ShadowSettings shadowSettings, BloomSettings bloomSettings, ComputeShader tiledDeferredShadingComputeShader)
    {
        _renderScale = renderScale;
        _shadowSettings = shadowSettings;
        _bloomSettings = bloomSettings;

        _tiledDeferredShadingComputeShader = tiledDeferredShadingComputeShader;
        _deferredFinalPassMaterial = CoreUtils.CreateEngineMaterial("Hidden/DeferredFinalPass");
        _defferedLightingMaterial = CoreUtils.CreateEngineMaterial("Hidden/DeferredLights");
        _screenSpaceShadowMaterial = CoreUtils.CreateEngineMaterial("Hidden/ScreenSpaceShadow");
        QualitySettings.shadows = ShadowQuality.All;
        GraphicsSettings.useScriptableRenderPipelineBatching = true;
    }


    protected override void Render(ScriptableRenderContext context, List<Camera> cameras)
    {
        BeginContextRendering(context, cameras);


        foreach (Camera camera in cameras)
        {
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


            BeginCameraRendering(context, camera);

            Matrix4x4 viewMatrix = camera.worldToCameraMatrix;
            Matrix4x4 projectionMatrix = GL.GetGPUProjectionMatrix(camera.projectionMatrix, false);
            _matrixVP = projectionMatrix * viewMatrix;
            _matrixVPI = _matrixVP.inverse;
            

            Shader.SetGlobalMatrix("unity_MatrixIVP", _matrixVPI);

            LightingResources lightingResources =
                _lightingPass.ExecuteLightngPass(RenderGraph, cullingResults, _shadowSettings);

            RenderDestinationTextures destinationTextures =
                _setupPass.SetupDestinationTextures(RenderGraph, textureSize, camera);
           
            _drawGeometryPass.DrawGeometry(RenderGraph, _commonShaderTags, camera, cullingResults,
                destinationTextures, camera.cullingMask, true, lightingResources);

            if (camera.clearFlags == CameraClearFlags.Skybox)
                DrawSkyboxPass.DrawSkybox(RenderGraph, destinationTextures, camera);

            _screenSpaceShadowPass.DrawScreenSpaceShadow(RenderGraph, destinationTextures, lightingResources,
                _shadowSettings, _screenSpaceShadowMaterial, camera);
            
            _directionalLight.DrawDirectinalLight(RenderGraph, destinationTextures, camera, _defferedLightingMaterial);
            
            _tileShadingPass.ExecuteTileShadingPass(RenderGraph, destinationTextures, cullingResults, camera, _tiledDeferredShadingComputeShader);
            
            _deferredFinalPass.DrawDeferredFinalPass(RenderGraph, destinationTextures, camera, _deferredFinalPassMaterial);
            
           

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
        CoreUtils.Destroy(_screenSpaceShadowMaterial);
        CoreUtils.Destroy(_defferedLightingMaterial);
        CoreUtils.Destroy(_deferredFinalPassMaterial);
        RenderGraph.Cleanup();
    }

    protected override void Render(ScriptableRenderContext context, Camera[] cameras)
    {
    }
}