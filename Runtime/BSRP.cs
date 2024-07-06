using System.Collections.Generic;
using Barkar.BSRP.CameraRenderer;
using Barkar.BSRP.Data;
using Barkar.BSRP.Passes;
using Barkar.BSRP.Passes.Bloom;
using Barkar.BSRP.Passes.Setup;
using Barkar.BSRP.Settings.Shadows;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;

public class BSRP : RenderPipeline
{
    private bool _hdr;
    private float _renderScale;

    private  RenderGraph RenderGraph = new RenderGraph("BSRP Render Graph");

    private readonly ShaderTagId[] _commonShaderTags = { new ShaderTagId("BSRPGBuffer"), new ShaderTagId("SRPDefaultUnlit") };

    private Vector2Int textureSize = default;
    private RenderGraphParameters renderGraphParameters;

    private ShadowSettings _shadowSettings;
    private BloomSettings _bloomSettings;
    
    private Material _finalPassMaterial;
    //
    private Material _testFinalPassMaterial;
    //
    private Material _postEffectsMaterial;
    //private Material _dualFilterBlurMaterial;
    private LocalKeyword _useLensDirtKeyword;
    private LocalKeyword _useBloomKeyword;

    private LightingPass _lightingPass = new LightingPass();
    private SetupPass _setupPass = new SetupPass();
    private DrawGeometryPass _drawGeometryPass = new DrawGeometryPass();
    private PostEffectsPass _postEffectsPass = new PostEffectsPass();
    private TestFinalPass _testFinal = new TestFinalPass();
   
    Matrix4x4 _matrixVP;
    Matrix4x4 _matrixVPI;
    Matrix4x4 _matrixVPprev;    
    Matrix4x4 _matrixVPIprev;
    
    public BSRP(bool hdr, float renderScale, ShadowSettings shadowSettings, BloomSettings bloomSettings)
    {
        _hdr = hdr;
        _renderScale = renderScale;
        _shadowSettings = shadowSettings;
        _bloomSettings = bloomSettings;
        
        //materials and keywords
        _finalPassMaterial = CoreUtils.CreateEngineMaterial("Hidden/FinalPass");
        _postEffectsMaterial = CoreUtils.CreateEngineMaterial("Hidden/PostEffectPasses");

        _testFinalPassMaterial = CoreUtils.CreateEngineMaterial("Hidden/TestFinalPass");
        
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
            Matrix4x4 toScreen = new Matrix4x4(
                new Vector4(0.5f * textureSize.x , 0.0f, 0.0f, 0.0f),
                new Vector4(0.0f, 0.5f * textureSize.y , 0.0f, 0.0f),
                new Vector4(0.0f, 0.0f, 1.0f, 0.0f),
                new Vector4(0.5f * textureSize.x, 0.5f * textureSize.y, 0.0f, 1.0f)
            );

            Matrix4x4 zScaleBias = Matrix4x4.identity;
          //  if (DeferredConfig.IsOpenGL)
           // {
          ///      // We need to manunally adjust z in NDC space from [-1; 1] to [0; 1] (storage in depth texture).
            //    zScaleBias = new Matrix4x4(
            //        new Vector4(1.0f, 0.0f, 0.0f, 0.0f),
            //        new Vector4(0.0f, 1.0f, 0.0f, 0.0f),
            //        new Vector4(0.0f, 0.0f, 0.5f, 0.0f),
             //       new Vector4(0.0f, 0.0f, 0.5f, 1.0f)
             //   );
           // }

            //_matrixVPI= Matrix4x4.Inverse(toScreen * zScaleBias * projectionMatrix * viewMatrix);
        
          
            Shader.SetGlobalMatrix("unity_MatrixIVP", _matrixVPI);
            
            LightingResources lightingResources =
                _lightingPass.ExecuteLightngPass(RenderGraph, cullingResults, _shadowSettings);
           
            RenderDestinationTextures destinationTextures =
                _setupPass.SetupDestinationTextures(RenderGraph, textureSize, camera, _hdr);
            
            _drawGeometryPass.DrawGeometry(RenderGraph, _commonShaderTags, camera, cullingResults,
                destinationTextures, camera.cullingMask, true, lightingResources);
           
            if (camera.clearFlags == CameraClearFlags.Skybox)
                DrawSkyboxPass.DrawSkybox(RenderGraph, destinationTextures, camera);
           
            _drawGeometryPass.DrawGeometry(RenderGraph, _commonShaderTags, camera, cullingResults,
                destinationTextures, camera.cullingMask, false, lightingResources);
           
           
         //  _postEffectsPass.DrawBloom(RenderGraph, _bloomSettings, destinationTextures,
                 //  camera, _postEffectsMaterial, _finalPassMaterial);
            
            _testFinal.DrawTestFinal(RenderGraph, destinationTextures, camera, _testFinalPassMaterial);
          //  FinalPass.DrawFinalPass(RenderGraph, destinationTextures, camera, _finalPassMaterial, bloomData);

            RenderGraph.EndRecordingAndExecute();
        }

        context.ExecuteCommandBuffer(renderGraphParameters.commandBuffer);
        context.Submit();
        CommandBufferPool.Release(renderGraphParameters.commandBuffer);

        //RenderGraph.EndFrame();
    }
    
    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        CoreUtils.Destroy(_finalPassMaterial);
        CoreUtils.Destroy(_postEffectsMaterial);
        RenderGraph.Cleanup();
    }

    protected override void Render(ScriptableRenderContext context, Camera[] cameras)
    {
    }
}