using System.Runtime.InteropServices;
using Barkar.BSRP.CameraRenderer;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;
using UnityEngine.Experimental.Rendering.RenderGraphModule;
using UnityEngine.Rendering;

namespace Barkar.BSRP.Passes
{
    public class LightingPassData
    {
        public TextureHandle ShadowMap;
        public BufferHandle DirectionalLightMatricesBuffer;
        public BufferHandle DirectionalLightDataBuffer;

        public RendererListHandle DirectionalRendererListHandle;
        //buffers
    }


    struct DirectionalLightShadowData
    {
        public int visibleLightIndex;
        public float shadowBias;
        public float shadowNearPlane;
    }
[StructLayout(LayoutKind.Sequential)]
    struct DirectionalLightData
    {
        public const int stride = 4 * 4 * 3; //3 float4
        public Vector4 Color;
        public Vector4 DirectionAndMask;
        public Vector4 ShadowData; //light.strenght, normalBias,

        public DirectionalLightData(ref VisibleLight visibleLight, int layerMask, Vector4 shadowData)
        {
            Color = visibleLight.finalColor;
            DirectionAndMask = -visibleLight.localToWorldMatrix.GetColumn(2);
            DirectionAndMask.w = (float)layerMask;
            ShadowData = shadowData;
        }
    }


    public class LightingPass
    {
        private const int maxDirectionalLightsCount = 1;
        private static readonly ProfilingSampler _profilingSampler = new ProfilingSampler("LightingPass");

        private static int _directionalLightsCount;
        private static DirectionalLightShadowData _directionalLightShadowData;
        private static DirectionalLightData[] _directionalLightData = new DirectionalLightData[maxDirectionalLightsCount];
        private static Matrix4x4[] _directionalLightMatrices = new Matrix4x4[maxDirectionalLightsCount];

        private static CullingResults _cullingResults;
        public static LightingResources ExecuteLightngPass(RenderGraph renderGraph, CullingResults cullingResults)
        {

            _cullingResults = cullingResults;
            using RenderGraphBuilder builder = renderGraph.AddRenderPass<LightingPassData>(_profilingSampler.name,
                out var lightingPassData, _profilingSampler);

            var textureDescriptor = new TextureDesc(2048,2048);
            textureDescriptor.depthBufferBits = DepthBits.Depth32;
            textureDescriptor.isShadowMap = true;
            textureDescriptor.name = "Directional Light ShadowMap";

            //TODO: default res
            lightingPassData.ShadowMap = builder.ReadWriteTexture(renderGraph.CreateTexture(textureDescriptor));

            var directionalLightDataBufferDescriptor = new BufferDesc();
            directionalLightDataBufferDescriptor.name = "Directional Light Data Buffer";
            directionalLightDataBufferDescriptor.count = maxDirectionalLightsCount;
            directionalLightDataBufferDescriptor.stride = DirectionalLightData.stride;

            lightingPassData.DirectionalLightDataBuffer =
                builder.WriteBuffer(renderGraph.CreateBuffer(directionalLightDataBufferDescriptor));

            var directionalLightMatricesBufferDescriptor = new BufferDesc();
            directionalLightMatricesBufferDescriptor.name = "Directional Shadow Matrices";
            directionalLightMatricesBufferDescriptor.stride = 4 * 16;
            directionalLightMatricesBufferDescriptor.count = maxDirectionalLightsCount; //*castadeCount
            lightingPassData.DirectionalLightMatricesBuffer =
                builder.WriteBuffer(renderGraph.CreateBuffer(directionalLightMatricesBufferDescriptor));
            
            NativeArray<VisibleLight> visibleLights = _cullingResults.visibleLights;
            int i;
            _directionalLightsCount = 0;
            for (i = 0; i < visibleLights.Length; i++)
            {
                VisibleLight visibleLight = visibleLights[i];
                Light light = visibleLight.light;
                
                switch (visibleLight.lightType)
                {
                    case LightType.Directional:
                        if (_directionalLightsCount < maxDirectionalLightsCount && light.shadows != LightShadows.None && light.shadowStrength > 0f)
                        {

                            _directionalLightShadowData = new DirectionalLightShadowData()
                            {
                                visibleLightIndex = i,
                                shadowNearPlane = light.shadowNearPlane,
                                shadowBias = light.shadowBias
                            };

                            
                            _directionalLightData[i] =
                                new DirectionalLightData(ref visibleLight, -1, new Vector4(light.shadowStrength,light.shadowBias,0f,0f ) );
                            

                            _directionalLightsCount++;
                        }

                        break;
                }

            }
           
            builder.SetRenderFunc((LightingPassData lightingPassData, RenderGraphContext context) =>
            {
                if (_directionalLightsCount > 0)
                {
                    var cmd = context.cmd;
                   
                    cmd.SetRenderTarget(lightingPassData.ShadowMap, RenderBufferLoadAction.DontCare,
                        RenderBufferStoreAction.Store);
                    cmd.ClearRenderTarget(true, false, Color.clear);
                    cmd.BeginSample("Directional Shadow");
                    context.renderContext.ExecuteCommandBuffer(cmd);
                    cmd.Clear();

                    //buffers


                    
                    cullingResults.ComputeDirectionalShadowMatricesAndCullingPrimitives(
                        _directionalLightShadowData.visibleLightIndex,
                        0, 1, Vector3.one, 
                        2048, _directionalLightShadowData.shadowNearPlane, out Matrix4x4 shadowViewMatrix,
                        out Matrix4x4 shadowProjectionMatrix, out ShadowSplitData splitData);
                   
                    _directionalLightMatrices[0] = shadowProjectionMatrix * shadowViewMatrix;
                    cmd.SetViewProjectionMatrices(shadowViewMatrix, shadowProjectionMatrix);
                    cmd.SetGlobalDepthBias(0f, _directionalLightShadowData.shadowBias);
                    context.renderContext.ExecuteCommandBuffer(cmd);
                    cmd.Clear();
                    
                    var shadowSeettings =
                        new ShadowDrawingSettings(_cullingResults, _directionalLightShadowData.visibleLightIndex);
                    shadowSeettings.useRenderingLayerMaskTest = false;
                    shadowSeettings.objectsFilter = ShadowObjectsFilter.AllObjects;
                    var directionalRendererListHandle = context.renderContext.CreateShadowRendererList(ref shadowSeettings);

                   cmd.DrawRendererList(directionalRendererListHandle);
                 
                    cmd.SetBufferData(lightingPassData.DirectionalLightMatricesBuffer,_directionalLightMatrices);
                   
                    cmd.SetBufferData(lightingPassData.DirectionalLightDataBuffer, _directionalLightData );
                    cmd.SetGlobalBuffer("_DirectionalLightDataBuffer", lightingPassData.DirectionalLightDataBuffer);
                    cmd.SetGlobalTexture("_ShadowMap", lightingPassData.ShadowMap);
                    
                    cmd.EndSample("Directional Shadow");
                    context.renderContext.ExecuteCommandBuffer(cmd);
                    cmd.Clear();
                    cmd.SetGlobalDepthBias(0f, 0f);
                    context.renderContext.ExecuteCommandBuffer(cmd);
                    cmd.Clear();
                }
            });

            return new LightingResources(lightingPassData.DirectionalLightDataBuffer, lightingPassData.DirectionalLightMatricesBuffer,lightingPassData.ShadowMap);
        }
    }
}