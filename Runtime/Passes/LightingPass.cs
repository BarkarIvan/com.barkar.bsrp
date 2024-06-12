using System.Runtime.InteropServices;
using Barkar.BSRP.CameraRenderer;
using Unity.Collections;
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
            textureDescriptor.name = "Direcctional Light ShadowMap";

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
            
            // NativeArray<int> lightsIndexMap = cullingResults.GetLightIndexMap(Allocator.Temp);
            NativeArray<VisibleLight> visibleLights = _cullingResults.visibleLights;
            
            _directionalLightsCount = 0;
            for (int i = 0; i < visibleLights.Length; i++)
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

                            _directionalLightsCount++;
                        }

                        break;
                }
            }
            
            var shadowSeettings =
                new ShadowDrawingSettings(_cullingResults, _directionalLightShadowData.visibleLightIndex);
           
           lightingPassData.DirectionalRendererListHandle =
                builder.UseRendererList(renderGraph.CreateShadowRendererList(ref shadowSeettings));

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
                
                   cmd.DrawRendererList(lightingPassData.DirectionalRendererListHandle);
                 
                   Mesh mesh = new Mesh();
        
                   // Задаем вершины треугольника
                   Vector3[] vertices = new Vector3[3];
                   vertices[0] = new Vector3(0, 1, 0);
                   vertices[1] = new Vector3(1, -1, 0);
                   vertices[2] = new Vector3(-1, -1, 0);
        
                   // Задаем треугольники
                   int[] triangles = new int[3];
                   triangles[0] = 0;
                   triangles[1] = 1;
                   triangles[2] = 2;
                   Material mat = CoreUtils.CreateEngineMaterial("BSRP/TestShader");
                   // Устанавливаем вершины и треугольники
                   mesh.vertices = vertices;
                   mesh.triangles = triangles;
                  // cmd.DrawMesh(mesh, Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3( 100f, 100f,100f)), mat);
                   cmd.SetBufferData(lightingPassData.DirectionalLightMatricesBuffer,_directionalLightMatrices);
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