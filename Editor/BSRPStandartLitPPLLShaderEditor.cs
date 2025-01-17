using UnityEngine;
using UnityEditor;

namespace Barkar.BSRP.Editor.ShaderEditor
{
public class BSRPStandartLitPPLLShaderEditor : ShaderGUI
{
    private MaterialEditor materialEditor;
    private MaterialProperty[] properties;

    private MaterialProperty _BaseMap;
    private MaterialProperty _BaseColor;
    private MaterialProperty _AdditionalMap;
    private MaterialProperty _UsingNormalMap;
    private MaterialProperty _NormalMapScale;
    private MaterialProperty _Metallic;
    private MaterialProperty _Roughness;
    private MaterialProperty _EmissionColor;
    private MaterialProperty _EmissionMap;
    private MaterialProperty _Brightness;
    private MaterialProperty _UseAlphaClip;
    private MaterialProperty _AlphaClip;
    private MaterialProperty _Cull;

    // Enum declarations
    public enum CullEnum
    {
        Off = 0,
        Front = 1,
        Back = 2
    }

    
    public override void OnGUI(MaterialEditor materialEditor, MaterialProperty[] properties)
    {
        this.materialEditor = materialEditor;
        this.properties = properties;

        FindProperties();

        EditorGUI.BeginChangeCheck();
        {
            EditorGUILayout.HelpBox("MAIN TEXTURE", MessageType.None);

            materialEditor.TextureProperty(_BaseMap, "Albedo");
            materialEditor.ColorProperty(_BaseColor, "Object Color");
            EditorGUILayout.Space(30);
            
            EditorGUILayout.HelpBox("AdditionalMap (Normals, Smoothness, metallic)", MessageType.None);
            materialEditor.TextureProperty(_AdditionalMap, "Additional Map");
            if (_AdditionalMap.textureValue != null)
            {
                materialEditor.ShaderProperty(_UsingNormalMap, "Use Normal Map");
                materialEditor.RangeProperty(_NormalMapScale, "Normal Map Scale");
            }
            else
            {
                materialEditor.RangeProperty(_Metallic, "Metallic");
                materialEditor.RangeProperty(_Roughness, "Roughness");
            }
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
        _Roughness = FindProperty("_Roughness");
        _Brightness = FindProperty("_Brightness");
        _EmissionMap = FindProperty("_EmissionMap");
        _EmissionColor = FindProperty("_EmissionColor");
        _Cull = FindProperty("_Cull");
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
