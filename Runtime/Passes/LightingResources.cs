using UnityEngine.Experimental.Rendering.RenderGraphModule;

namespace Barkar.BSRP.CameraRenderer
{
    public readonly ref struct LightingResources
    {
        public readonly BufferHandle DirectionalLightBuffer;
        public readonly BufferHandle DirectionalShadowMatricesBuffer;
        public readonly TextureHandle DirectionalShadowMap;


        public LightingResources(BufferHandle directionalLightBuffer, BufferHandle directionalShadowMatricesBuffer,
            TextureHandle directionalShadowMap)
        {
            DirectionalLightBuffer = directionalLightBuffer;
            DirectionalShadowMatricesBuffer = directionalShadowMatricesBuffer;
            DirectionalShadowMap = directionalShadowMap;
        }
    }
}
