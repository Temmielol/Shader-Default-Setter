using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

public class SPD : EditorWindow
{
    private Material mat;

    [MenuItem("Temmie/Shader Default Setter")]
    private static void Open() => GetWindow<SPD>("Shader Default Setter");

    [MenuItem("CONTEXT/Material/Apply Values To Shader Defaults")]
    private static void ApplyFromContext(MenuCommand command) => Apply((Material)command.context);

    [MenuItem("CONTEXT/Material/Apply Values To Shader Defaults", true)]
    private static bool ValidateContext(MenuCommand command)
    {
        Material m = command.context as Material;
        return m != null && m.shader != null;
    }

    private void OnGUI()
    {
        mat = (Material)EditorGUILayout.ObjectField("Material", mat, typeof(Material), false);

        using (new EditorGUI.DisabledScope(mat == null || mat.shader == null))
            if (GUILayout.Button("Apply Material Values To Shader Defaults", GUILayout.Height(35)))
                Apply(mat);
    }

    private static void Apply(Material mat)
    {
        Shader shader = mat.shader;
        string path = AssetDatabase.GetAssetPath(shader);
        if (string.IsNullOrEmpty(path) || !File.Exists(path)) return;

        string source = File.ReadAllText(path);

        for (int i = 0; i < shader.GetPropertyCount(); i++)
        {
            string name = shader.GetPropertyName(i);
            if (!mat.HasProperty(name)) continue;

            string value;
            switch (shader.GetPropertyType(i))
            {
                case ShaderPropertyType.Color: value = Vec(mat.GetColor(name)); break;
                case ShaderPropertyType.Vector: value = Vec(mat.GetVector(name)); break;
                case ShaderPropertyType.Float:
                case ShaderPropertyType.Range: value = Num(mat.GetFloat(name)); break;
                case ShaderPropertyType.Int: value = mat.GetInteger(name).ToString(); break;
                default: continue;
            }

            source = Regex.Replace(
                source,
                @"((?<!\w)" + Regex.Escape(name) + @"\s*\((?:[^()]|\([^()]*\))*\)\s*=\s*)(\([^)]*\)|[^\s,\r\n}]+)",
                m => m.Groups[1].Value + value,
                RegexOptions.IgnoreCase);
        }

        File.WriteAllText(path, source);
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
    }

    private static string Num(float v) => v.ToString("0.##########", CultureInfo.InvariantCulture);

    private static string Vec(Vector4 v) => $"({Num(v.x)},{Num(v.y)},{Num(v.z)},{Num(v.w)})";
}