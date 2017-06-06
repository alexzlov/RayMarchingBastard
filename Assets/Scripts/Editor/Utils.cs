using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace CodeEdit
{
    public static class Utils
    {
        public const string ShaderTemplateDirPath = "Raymarch/ShaderTemplates";

        public static string GetCgincDirPath()
        {
            var shader = Shader.Find("Hidden/Raymarching/GetPathFromScript");
            var path   = AssetDatabase.GetAssetPath(shader);
            return Path.GetDirectoryName(path);
        }

        public static string GetShaderTemplateDirPath()
        {
            var file = Resources.Load<TextAsset>(ShaderTemplateDirPath + "/_Get_Path_From_Script_");
            var path = AssetDatabase.GetAssetPath(file);
            return Path.GetDirectoryName(path);
        }

        public static bool Foldout(string title, bool display)
        {
            var style = new GUIStyle("ShurikenModuleTitle")
            {
                font          = new GUIStyle(EditorStyles.label).font,
                border        = new RectOffset(15, 7, 4, 4),
                fixedHeight   = 22,
                contentOffset = new Vector2(20f, -2f),
            };

            var rect = GUILayoutUtility.GetRect(16f, 22f, style);
            GUI.Box(rect, title, style);

            var e = Event.current;

            var toggleRect = new Rect(rect.x + 4f, rect.y + 2f, 13f, 13f);
            if (e.type == EventType.Repaint)
            {
                EditorStyles.foldout.Draw(toggleRect, false, false, display, false);
            }
            if (e.type == EventType.MouseDown && rect.Contains(e.mousePosition))
            {
                display = !display;
                e.Use();
            }

            return display;
        }

        public static string ToSpacedCamel(string str)
        {
            return System.Text.RegularExpressions.Regex.Replace(str, @"([A-Z][^A-Z]+)", @"$1 ");
        }

        public static void ReadOnlyTextField(string label, string text)
        {
            EditorGUILayout.BeginHorizontal();
            {
                EditorGUILayout.LabelField(label, GUILayout.Width(EditorGUIUtility.labelWidth - 4));
                EditorGUILayout.SelectableLabel(text, EditorStyles.textField, GUILayout.Height(
                    EditorGUIUtility.singleLineHeight    
                ));
            }
            EditorGUILayout.EndHorizontal();
        }

        public static List<T> FindAllAssets<T>(string query) where T : Object
        {
            var guids = AssetDatabase.FindAssets(query);
            var list = guids.Select(AssetDatabase.GUIDToAssetPath).Select(AssetDatabase.LoadAssetAtPath<T>).Where(obj => obj).ToList();
            return list;
        }

        public static List<T> FindAllAssets<T>() where T : Object
        {
            return FindAllAssets<T>("t:" + typeof (T));
        }

        public static List<Material> FindMaterialsUsingShader(Shader shader)
        {
            var allMaterials = FindAllAssets<Material>("t:Material");
            return allMaterials.Where(m => m.shader == shader).ToList();
        }
    }
}