using UnityEngine;
using UnityEditor;

namespace Barkar.BSRP.Editor.ShaderEditor
{
public class BSRPStylizedLitShaderEditor : ShaderGUI
{
    private MaterialEditor materialEditor;
    private MaterialProperty[] properties;

    private MaterialProperty _BaseMap;
    private MaterialProperty _BaseColor;
    private MaterialProperty _AdditionalMap;
    private MaterialProperty _UsingNormalMap;
    private MaterialProperty _NormalMapScale;
    private MaterialProperty _Metallic;
    private MaterialProperty _Smoothness;
    private MaterialProperty _EmissionColor;
    private MaterialProperty _EmissionMap;
    private MaterialProperty _UseRim;
    private MaterialProperty _RimThreshold;
    private MaterialProperty _RimSmooth;
    private MaterialProperty _RimColor;
    private MaterialProperty _MediumThreshold;
    private MaterialProperty  _MediumSmooth;
    private MaterialProperty _MediumColor;
    private MaterialProperty _ShadowThreshold;
    private MaterialProperty _ShadowSmooth;
    private MaterialProperty  _ShadowColor;
    private MaterialProperty _ReflectThreshold;
    private MaterialProperty _ReflectSmooth;
    private MaterialProperty _ReflectColor;
    private MaterialProperty _UseBrush;
    private MaterialProperty _BrushTexture;
    private MaterialProperty _MedBrushStrength;
    private MaterialProperty _ShadowBrushStrength;
    private MaterialProperty _ReflectBrushStrength;
    private MaterialProperty _Brightness;
    private MaterialProperty _UseAlphaClip;
    private MaterialProperty _AlphaClip;
    private MaterialProperty _Cull;
    private MaterialProperty _Blend1;
    private MaterialProperty _Blend2;
    private MaterialProperty _ZWrite;

    // Enum declarations
    public enum CullEnum
    {
        Off = 0,
        Front = 1,
        Back = 2
    }

    public enum BlendModes
    {
        Opaque = 0,
        Transparent = 1,
        Fade = 2,
    }

    public enum BlendModeEnum
    {
        Zero = 0,
        One = 1,
        DstColor = 2,
        SrcColor = 3,
        OneMinusDstColor = 4,
        SrcAlpha = 5,
        OneMinusSrcColor = 6,
        DstAlpha = 7,
        OneMinusDstAlpha = 8,
        SrcAlphaSaturate = 9,
        OneMinusSrcAlpha = 10
    }

    public enum ZWriteEnum
    {
        On = 1,
        Off = 0
    }

    private BlendModes _blendMode;
    
    public override void OnGUI(MaterialEditor materialEditor, MaterialProperty[] properties)
    {
        this.materialEditor = materialEditor;
        this.properties = properties;

        FindProperties();

        EditorGUI.BeginChangeCheck();
        {
            EditorGUILayout.HelpBox("MAIN TEXTURE", MessageType.None);

            materialEditor.TextureProperty(_BaseMap, "Albedo");
            EditorGUILayout.Space(30);

            EditorGUILayout.HelpBox("AdditionalMap (Normals, Smoothness, metallic)", MessageType.None);
            materialEditor.TextureProperty(_AdditionalMap, "Additional Map");
            if (_AdditionalMap.textureValue != null)
            {
                materialEditor.ShaderProperty(_UsingNormalMap, "Use Normal Map");
                materialEditor.RangeProperty(_NormalMapScale, "Normal Map Scale");
            }
            EditorGUILayout.Space(30);

            EditorGUILayout.HelpBox("BRDF", MessageType.None);
            materialEditor.RangeProperty(_Metallic, "Metallic");
            materialEditor.RangeProperty(_Smoothness, "Smoothness");
            EditorGUILayout.Space(30);
           
            EditorGUILayout.HelpBox("DIFFUSE BRUSH TEXTURE", MessageType.None);
            materialEditor.ShaderProperty(_UseBrush, "Use Diffuse Brush texture");
            
            if (_UseBrush.floatValue == 1)
            {
                materialEditor.TextureProperty(_BrushTexture, "Brush Texture");
                materialEditor.RangeProperty(_MedBrushStrength, "Medium brush strength");
                materialEditor.RangeProperty(_ShadowBrushStrength, "Shadow brush strength");
                materialEditor.RangeProperty(_ReflectBrushStrength, "Reflection brush strength");
            }
            EditorGUILayout.Space(30);
           
            EditorGUILayout.HelpBox("STYLIZED DIFFUSE", MessageType.None);
            EditorGUILayout.Space();
            materialEditor.ColorProperty(_BaseColor, "Object Color");
            materialEditor.ColorProperty(_MediumColor, "Medium Color");
            materialEditor.RangeProperty(_MediumThreshold, "Medium Threshold");
            materialEditor.RangeProperty(_MediumSmooth, "Medium Smoothness");
            materialEditor.ColorProperty(_ShadowColor, "Shadow Color");
            materialEditor.RangeProperty(_ShadowThreshold, "Shadow Threshold");
            materialEditor.RangeProperty(_ShadowSmooth, "Shadow Smoothness");
            materialEditor.ColorProperty(_ReflectColor, "Reflect Color");
            materialEditor.RangeProperty(_ReflectThreshold, "Reflect Threshold");
            materialEditor.RangeProperty(_ReflectSmooth, "Reflect Smoothness");
            EditorGUILayout.Space(30);
            
            EditorGUILayout.HelpBox("RIM LIGHT", MessageType.None);
            materialEditor.ShaderProperty(_UseRim, "Use Rim");
            if (_UseRim.floatValue == 1)
            {
                materialEditor.RangeProperty(_RimThreshold, "Rim Power");
                materialEditor.RangeProperty(_RimSmooth, "Rim Smooth");
                materialEditor.ColorProperty(_RimColor, "Rim Color");
            }
            EditorGUILayout.Separator();
            EditorGUILayout.Space(30);
            
            EditorGUILayout.HelpBox("EMISSION", MessageType.None);
            materialEditor.TextureProperty(_EmissionMap, "EmissionMap");
            materialEditor.ColorProperty(_EmissionColor, "EmissionColor");
            EditorGUILayout.Space(30);

            materialEditor.RangeProperty(_Brightness, "Brightness");
            EditorGUILayout.Space(30);
            materialEditor.ShaderProperty(_UseAlphaClip, "Use Alpha Clip");
            if (_UseAlphaClip.floatValue == 1)
            {
                materialEditor.RangeProperty(_AlphaClip, "Alpha Clip Threshold");
            }
            
            _Cull.floatValue = (float)(CullEnum)EditorGUILayout.EnumPopup("Cull", (CullEnum)_Cull.floatValue);
            EditorGUILayout.Space();
            _blendMode = (BlendModes)EditorGUILayout.EnumPopup("Blend Mode", _blendMode);
            // 
            switch (_blendMode)
            {
                case BlendModes.Opaque:
                    _Blend1.floatValue = (int)BlendModeEnum.One;
                    _Blend2.floatValue = (int)BlendModeEnum.Zero;
                    _ZWrite.floatValue = (int)ZWriteEnum.On;

                    break;
                case BlendModes.Transparent:
                    _Blend1.floatValue = (int)BlendModeEnum.SrcAlpha;
                    _Blend2.floatValue = (int)BlendModeEnum.OneMinusSrcAlpha;
                    _ZWrite.floatValue = (int)ZWriteEnum.Off;

                    break;
                case BlendModes.Fade:
                    _Blend1.floatValue = (int)BlendModeEnum.SrcAlpha;
                    _Blend2.floatValue = (int)BlendModeEnum.OneMinusSrcAlpha;
                    _ZWrite.floatValue = (int)ZWriteEnum.On;

                    break;
            }

            materialEditor.RenderQueueField();
            
        }
        if (EditorGUI.EndChangeCheck())
        {
            foreach (var obj in materialEditor.targets)
            {
                if (obj is Material material)
                {
                    SetKeyword(material, "_USEALPHACLIP", _UseAlphaClip.floatValue == 1);
                    SetKeyword(material, "_NORMALMAP", _UsingNormalMap.floatValue == 1 && _AdditionalMap.textureValue != null);
                    SetKeyword(material, "_ADDITIONALMAP", _AdditionalMap.textureValue != null);
                    SetKeyword(material, "_EMISSION", _EmissionMap.textureValue != null);
                }
            }
        }
    }

    private BlendModes GetBlendModeFromMaterialProperties()
    {
        if (_Blend1.floatValue == (int)BlendModeEnum.SrcAlpha && _Blend2.floatValue == (int)BlendModeEnum.OneMinusSrcAlpha)
        {
            if (_ZWrite.floatValue == (int)ZWriteEnum.On) return BlendModes.Fade;
            
            return BlendModes.Transparent;
        }
        return BlendModes.Opaque;
    }
    
    /// <summary>
    /// Sets the keyword of a material.
    /// </summary>
    /// <param name="material">The material to set the keyword for.</param>
    /// <param name="keyword">The keyword to set.</param>
    /// <param name="enabled">Specifies whether the keyword should be enabled or disabled.</param>
    private void SetKeyword(Material material, string keyword, bool enabled)
    {
        if (enabled)
        {
            material.EnableKeyword(keyword);
        }
        else
        {
            material.DisableKeyword(keyword);
        }
    }

    /// <summary>
    /// Finds the properties of the material.
    /// </summary>
    private void FindProperties()
    {
        _BaseMap = FindProperty("_BaseMap");
        _BaseColor = FindProperty("_BaseColor");
        _AdditionalMap = FindProperty("_AdditionalMap");
        _UsingNormalMap = FindProperty("_UsingNormalMap");
        _NormalMapScale = FindProperty("_NormalMapScale");
        _UseAlphaClip = FindProperty("_UseAlphaClip");
        _AlphaClip = FindProperty("_AlphaClip");
        _Metallic = FindProperty("_Metallic");
        _Smoothness = FindProperty("_Smoothness");
        _Brightness = FindProperty("_Brightness");
        _EmissionMap = FindProperty("_EmissionMap");
        _EmissionColor = FindProperty("_EmissionColor");
        _UseRim = FindProperty("_UsingRim");
        _RimThreshold = FindProperty("_RimThreshold");
        _RimSmooth = FindProperty("_RimSmooth");
        _RimColor = FindProperty("_RimColor");
        _UseBrush = FindProperty("_UseBrush");
        _BrushTexture = FindProperty("_BrushTexture");
        _MedBrushStrength = FindProperty("_MedBrushStrength");
        _ShadowBrushStrength = FindProperty("_ShadowBrushStrength");
        _ReflectBrushStrength = FindProperty("_ReflectBrushStrength");
        
        _MediumThreshold = FindProperty("_MediumThreshold");
        _MediumSmooth = FindProperty("_MediumSmooth");
        _MediumColor = FindProperty("_MediumColor");
        _ShadowThreshold = FindProperty("_ShadowThreshold");
        _ShadowSmooth = FindProperty("_ShadowSmooth");
        _ShadowColor = FindProperty("_ShadowColor");
        _ReflectThreshold = FindProperty("_ReflectThreshold");
        _ReflectSmooth = FindProperty("_ReflectSmooth");
        _ReflectColor = FindProperty("_ReflectColor");
        
        _Cull = FindProperty("_Cull");
        _Blend1 = FindProperty("_Blend1");
        _Blend2 = FindProperty("_Blend2");
        _ZWrite = FindProperty("_ZWrite");
        _blendMode = _blendMode = GetBlendModeFromMaterialProperties();
    }

    /// <summary>
    /// Finds the properties of the material.
    /// </summary>
    /// <param name="propertyName">The name of the property to find.</param>
    /// <returns>The MaterialProperty object for the given property name.</returns>
    private MaterialProperty FindProperty(string propertyName)
    {
        MaterialProperty prop = FindProperty(propertyName, properties);

        if (prop == null)
        {
            Debug.LogError("Property " + propertyName + " not found");
            return null;
        }

        return prop;
    }
}
}
