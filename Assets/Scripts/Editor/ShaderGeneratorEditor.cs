using System.Collections.Generic;
using UnityEditor;

namespace CodeEdit
{
    [CustomEditor(typeof (ShaderGenerator))]
    public class ShaderGeneratorEditor : Editor
    {
        private SerializedProperty _name;
        private SerializedProperty _shader;

        private SerializedProperty _basicFolded;
        private SerializedProperty _materialsFolded;

        private SerializedProperty _conditions;
        private SerializedProperty _conditionsFolded;

        private SerializedProperty _variables;
        private SerializedProperty _variablesFolded;

        private SerializedProperty _blocks;
        Dictionary<string, ShaderCodeEditor> _editors = new Dictionary<string, ShaderCodeEditor>();

        private ShaderTemplateSelector _template;
        private ShaderTemplateParser _templateParser;
    }
}