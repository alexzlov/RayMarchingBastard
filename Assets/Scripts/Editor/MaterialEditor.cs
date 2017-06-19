using System.Linq;
using UnityEditor;
using UnityEngine;

namespace CodeEdit
{
    public class MaterialEditor : ShaderGUI
    {
        private bool _folded = true;
        private Editor _cachedEditor;

        public override void OnGUI(UnityEditor.MaterialEditor materialEditor, MaterialProperty[] properties)
        {
            if (!_cachedEditor)
            {
                var material = materialEditor.target as Material;
                if (material != null)
                {
                    var shader = material.shader;
                    var generators = Utils.FindAllAssets<ShaderGenerator>();
                    var targetGenerator = generators.FirstOrDefault(generator => generator.ShaderReference == shader);
                    if (targetGenerator)
                    {
                        _cachedEditor = Editor.CreateEditor(targetGenerator);
                    }
                }
            }

            if (_cachedEditor)
            {
                _cachedEditor.OnInspectorGUI();
                EditorGUILayout.Space();
            }

            _folded = Utils.Foldout("Material Properties", _folded);
            if (_folded)
            {
                ++EditorGUI.indentLevel;
                base.OnGUI(materialEditor, properties);
                --EditorGUI.indentLevel;
            }
        }
    }
}