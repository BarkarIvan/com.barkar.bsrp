Shader "Hidden/DeferredFinalPass"
{
   
    SubShader
    {
        Tags
        {
            "RenderPipeline"="BSRP" 
        }
        ZTest Always ZWrite Off Cull Off

        
        //Stencil
           //  {
              //  Ref 8
              //  Comp Equal
            // } 

        Pass
        {
            HLSLPROGRAM
            #pragma vertex DefaultPassVertex
            #pragma fragment LightPassFragment

            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ _SOFT_SHADOWS_LOW _SOFT_SHADOWS_MEDIUM _SOFT_SHADOWS_HIGH

            #pragma multi_compile_fog

            #pragma prefer_hlslcc gles
            #pragma exclude_renderers d3d11_9x

            #include "Packages/com.barkar.bsrp/ShaderLibrary/Common.hlsl"
           // #include "Packages/com.barkar.bsrp/ShaderLibrary/Surface.hlsl"
          //  #include "Packages/com.barkar.bsrp/ShaderLibrary/Lighting.hlsl"
           // #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/ImageBasedLighting.hlsl"
          //  #include "Packages/com.barkar.bsrp/ShaderLibrary/CustomBRDF.hlsl"
           // #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/AmbientProbe.hlsl"
    #include "Packages/com.barkar.bsrp/ShaderLibrary/CameraRendererPasses.hlsl"

            //to inputs include

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);
            TEXTURE2D(_AdditionalMap);
            SAMPLER(sampler_AdditionalMap);
            TEXTURE2D(_EmissionMap);
            SAMPLER(sampler_EmissionMap);
            TEXTURE2D(_BrushTexture);
            SAMPLER(sampler_BrushTexture);

            CBUFFER_START(UnityPerMaterial)
                half4 _BaseColor;
                half4 _BaseMap_ST;
                half4 _AdditionalMap_ST;
                half3 _EmissionColor;
                half _Brightness;

                half _Metallic;
                half _Smoothness;
                half _AlphaClip;
                half _NormalMapScale;

                half3 _RimColor;
                half _RimSmooth;

                half3 _MediumColor;
                half _MediumThreshold;

                half3 _ShadowColor;
                half _ShadowThreshold;

                half3 _ReflectColor;
                half _ReflectThreshold;

                half _MediumSmooth;
                half _ShadowSmooth;
                half _ReflectSmooth;
                half _RimThreshold;

                half4 _BrushTexture_ST;
                half _MedBrushStrength;
                half _ShadowBrushStrength;
                half _ReflectBrushStrength;
            CBUFFER_END

            half4 _Filter;

            struct Attributes
            {
                float3 positionOS : POSITION;
                half3 normalOS : NORMAL;
                half4 tangentOS : TANGENT;
                float2 uv : TEXCOORD0;
                half4 color : COLOR;
            };
            
TEXTURE2D_HALF(_GBuffer0); //albedo, smoothness
TEXTURE2D_HALF(_GBuffer1); //radiance, metallic
TEXTURE2D_HALF(_GBuffer2); //normal
TEXTURE2D_HALF(_GBuffer3); //emission
            TEXTURE2D_HALF(_CameraDepth);

             half3 ACESFilmTonemapping(half3 col)
            {
                half a = 2.51;
                half b = 0.03;
                half c = 2.43;
                half d = 0.59;
                half e = 0.14;
                return saturate((col * (a * col + b)) / (col * (c * col + d) + e));
            }

            half3 Prefilter(half3 c)
    {
        half brightness = Max3(c.r, c.g, c.b);
        half soft = brightness - _Filter.y;
        soft = clamp(soft, 0, _Filter.z);
        soft = soft * soft * _Filter.w;
        half contribution = max(soft, brightness - _Filter.x);
        contribution /= max(brightness, 1e-4);
        return c * contribution;
    }

           

            half4 LightPassFragment(Varyings IN): SV_Target
            {

                 half3 g3 = SAMPLE_TEXTURE2D(_GBuffer3,sampler_linear_clamp, IN.uv);

               half4 result = half4(g3,1.0);
                                //result = pow(result, 1.0 / 2.2);

               result.rgb = ACESFilmTonemapping(result.rgb);
                return half4(result.rgb, 1);
            }
            ENDHLSL
        }
    }
}