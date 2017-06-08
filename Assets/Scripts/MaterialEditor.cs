using UnityEditor;
using UnityEngine;

namespace CodeEdit
{
    public class MaterialEditor : ShaderGUI
    {
        private bool _folded = true;
        private Editor _cachedEditor;

        public void OnGUI(UnityEditor.MaterialEditor materialEditor, MaterialProperty[] properties)
        {
            if (!_cachedEditor)
            {
                var material = materialEditor.target as Material;
                var shader = material.shader;
                var generators = Utils.FindAllAssets<ShaderGenerator>();
            }
        }
    }

}