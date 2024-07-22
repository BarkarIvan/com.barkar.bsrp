

using UnityEngine.Rendering.RenderGraphModule;

namespace Barkar.BSRP.CameraRenderer
{
    public readonly ref struct LightingResources
    {
        public readonly BufferHandle DirectionalLightBuffer;
        public readonly BufferHandle PointLightBuffer;
        public readonly TextureHandle DirectionalShadowMap;


        public LightingResources(BufferHandle directionalLightBuffer, BufferHandle pointLightBuffer,
            TextureHandle directionalShadowMap)
        {
            DirectionalLightBuffer = directionalLightBuffer;
            PointLightBuffer = pointLightBuffer;
            DirectionalShadowMap = directionalShadowMap;
        }
    }
}
