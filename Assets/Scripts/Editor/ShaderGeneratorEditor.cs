using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

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

        private FileWatcher _fileWatcher = new FileWatcher();

        private string _errorMessage;
        
        private bool _constVarsFolded;

        private bool HasShaderReference
        {
            get { return _shader.objectReferenceValue != null; }
        }

        void OnEnable()
        {
            _name             = serializedObject.FindProperty("ShaderName");
            _shader           = serializedObject.FindProperty("ShaderReference");
            _variables        = serializedObject.FindProperty("Variables");
            _variablesFolded  = serializedObject.FindProperty("VariablesFolded");
            _conditions       = serializedObject.FindProperty("Conditions");
            _conditionsFolded = serializedObject.FindProperty("ConditionsFolded");
            _blocks           = serializedObject.FindProperty("Blocks");
            _basicFolded      = serializedObject.FindProperty("BasicFolded");
            _materialsFolded  = serializedObject.FindProperty("MaterialsFolded");

            _template = new ShaderTemplateSelector(serializedObject.FindProperty("ShaderTemplate"));
            _template.OnChange += OnTemplateChanged;
            _fileWatcher.OnChange += CheckShaderUpdate;

            if (HasShaderReference)
            {
                _fileWatcher.Start(GetShaderPath());
            }

            CheckShaderUpdate();
        }

        void OnDisable()
        {
            if (_template != null)
            {
                _template.OnChange -= OnTemplateChanged;
            }
            if (_fileWatcher != null)
            {
                _fileWatcher.Stop();
            }
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            _fileWatcher.Update();
            HandleKeyEvents();
            if (_templateParser == null)
            {
                OnTemplateChanged();
            }
            DrawBasics();
            DrawConditions();
            DrawVariables();
            DrawBlocks();
            DrawMaterialReferences();
            DrawButtons();
            DrawMessages();

            serializedObject.ApplyModifiedProperties();
        }

        SerializedProperty FindProperty(SerializedProperty array, string key)
        {
            for (var i = 0; i < array.arraySize; ++i)
            {
                var prop = array.GetArrayElementAtIndex(i);
                var keyProp = prop.FindPropertyRelative("Key");
                if (keyProp.stringValue == key)
                {
                    return prop;
                }
            }
            return null;
        }

        private SerializedProperty AddProperty(SerializedProperty array, string key)
        {
            var prop = FindProperty(array, key);
            if (prop != null)
            {
                return prop;
            }
            var index = array.arraySize;
            array.InsertArrayElementAtIndex(index);
            return array.GetArrayElementAtIndex(index);
        }

        void DrawBasics()
        {
            _basicFolded.boolValue = Utils.Foldout("Basic", _basicFolded.boolValue);
            if (_basicFolded.boolValue)
            {
                ++EditorGUI.indentLevel;
                EditorGUILayout.PropertyField(_name);
                EditorGUILayout.PropertyField(_shader);
                _template.Draw();
                --EditorGUI.indentLevel;
            }
        }

        void DrawConditions()
        {
            _conditionsFolded.boolValue = Utils.Foldout("Conditions", _conditionsFolded.boolValue);
            if (!_conditionsFolded.boolValue)
            {
                return;
            }
            ++EditorGUI.indentLevel;
            foreach (var kv in _templateParser.Conditions)
            {
                var prop  = FindProperty(_conditions, kv.Key);
                var value = prop.FindPropertyRelative("Value");
                var kvName  = Utils.ToSpacedCamel(kv.Key);
                var isSelected = EditorGUILayout.Toggle(kvName, value.boolValue);
                if (value.boolValue != isSelected)
                {
                    value.boolValue = isSelected;
                }
            }
            --EditorGUI.indentLevel;
        }

        void DrawBlocks()
        {
            foreach (var kv in _templateParser.Blocks)
            {
                var prop   = FindProperty(_blocks, kv.Key);
                var value  = prop.FindPropertyRelative("Value");
                var folded = prop.FindPropertyRelative("Folded");
                var kvName = Utils.ToSpacedCamel(kv.Key);
                ShaderCodeEditor editor;

                if (_editors.ContainsKey(kvName))
                {
                    editor = _editors[kvName];
                }
                else
                {
                    editor = new ShaderCodeEditor(kvName, value, folded);
                    _editors.Add(kvName, editor);
                }
                editor.Draw();
            }
        }

        void DrawVariables()
        {
            _variablesFolded.boolValue = Utils.Foldout("Variables", _variablesFolded.boolValue);
            if (!_variablesFolded.boolValue)
            {
                return;
            }

            ++EditorGUI.indentLevel;
            var constVars = new Dictionary<string, string>();
            foreach (var kv in _templateParser.Variables)
            {
                var prop = FindProperty(_variables, kv.Key);
                if (prop == null)
                {
                    continue;
                }
                var value = prop.FindPropertyRelative("Value");
                var kvName = Utils.ToSpacedCamel(kv.Key);
                var constValue = ToConstVariable(kv.Key);
                string changedValue;

                if (constValue != null)
                {
                    changedValue = constValue;
                    constVars.Add(kvName, constValue);
                }
                else
                {
                    if (kv.Value.Count <= 1)
                    {
                        changedValue = EditorGUILayout.TextField(kvName, value.stringValue);
                    }
                    else
                    {
                        var index = kv.Value.IndexOf(value.stringValue);
                        if (index == -1)
                        {
                            index = 0;
                        }
                        index = EditorGUILayout.Popup(kvName, index, kv.Value.ToArray());
                        changedValue = kv.Value[index];
                    }
                }
                if (value.stringValue != changedValue)
                {
                    value.stringValue = changedValue;
                }
            }

            _constVarsFolded = EditorGUILayout.Foldout(_constVarsFolded, "(Constants controlled by raymarching");
            if (_constVarsFolded)
            {
                ++EditorGUI.indentLevel;
                foreach (var kv in constVars)
                {
                    Utils.ReadOnlyTextField(kv.Key, kv.Value);
                }
                --EditorGUI.indentLevel;
            }
            --EditorGUI.indentLevel;
        }

        void DrawMaterialReferences()
        {
            _materialsFolded.boolValue = Utils.Foldout("Material References", _materialsFolded.boolValue);
            if (_materialsFolded.boolValue)
            {
                ++EditorGUI.indentLevel;
                var materials = Utils.FindMaterialsUsingShader(_shader.objectReferenceValue as Shader);
                foreach (var material in materials)
                {
                    EditorGUILayout.ObjectField(material, typeof (Material), false);
                }
                -- EditorGUI.indentLevel;
            }
        }

        void DrawButtons()
        {
            EditorGUILayout.BeginHorizontal();
            {
                var buttonFontSize = GUI.skin.label.fontSize;
                var buttonPadding  = new RectOffset(24, 24, 6, 6);
                GUILayout.FlexibleSpace();
                var style = new GUIStyle(EditorStyles.miniButtonLeft)
                {
                    fontSize = buttonFontSize,
                    padding  = buttonPadding,
                };
                if (GUILayout.Button("Export (Ctrl+R)", style))
                {
                    ClearError();
                    try
                    {
                        ExportShader();
                    }
                    catch (System.Exception e)
                    {
                        AddError(e.Message);
                    }
                }

                style = new GUIStyle(EditorStyles.miniButtonMid)
                {
                    fontSize = buttonFontSize,
                    padding  = buttonPadding,
                };
                if (GUILayout.Button("Create Material", style))
                {
                    CreateMaterial();
                }
                if (GUILayout.Button("Update Template", style))
                {
                    OnTemplateChanged();
                }

                style = new GUIStyle(EditorStyles.miniButtonRight)
                {
                    fontSize = buttonFontSize,
                    padding  = buttonPadding,
                };
                if (GUILayout.Button("Reconvert All", style))
                {
                    ReconvertAll();
                }
            }
            EditorGUILayout.EndHorizontal();
        }

        void DrawMessages()
        {
            if (!string.IsNullOrEmpty(_errorMessage))
            {
                EditorGUILayout.HelpBox(_errorMessage, MessageType.Error, true);

            }
        }

        string ToConstVariable(string vName)
        {
            switch (vName) 
            {
                case "Name":
                    return _name.stringValue;
                case "RaymarchingShaderDirectory":
                    return Utils.GetCgincDirPath();
                default:
                    return null;
            }
        }

        void OnTemplateChanged()
        {
            _templateParser = new ShaderTemplateParser(_template.Text);
            foreach (var kv in _templateParser.Conditions)
            {
                if (FindProperty(_conditions, kv.Key) != null) continue;
                var prop = AddProperty(_conditions, kv.Key);
                prop.FindPropertyRelative("Key").stringValue = kv.Key;
                prop.FindPropertyRelative("Value").boolValue = kv.Value;
            }

            foreach (var kv in _templateParser.Blocks)
            {
                if (FindProperty(_blocks, kv.Key) != null) continue;
                var prop = AddProperty(_blocks, kv.Key);
                prop.FindPropertyRelative("key").stringValue = kv.Key;
                prop.FindPropertyRelative("value").stringValue = kv.Value;
                prop.FindPropertyRelative("folded").boolValue = false;
            }

            foreach (var kv in _templateParser.Variables)
            {
                if (FindProperty(_variables, kv.Key) != null) continue;
                var prop = AddProperty(_variables, kv.Key);
                var hasDefaultValue = (kv.Value.Count >= 1);
                prop.FindPropertyRelative("key").stringValue = kv.Key;
                prop.FindPropertyRelative("value").stringValue = hasDefaultValue ? kv.Value[0] : "";
            }
        }

        string GetShaderName()
        {
            var shName = _name.stringValue;
            if (string.IsNullOrEmpty(shName))
            {
                throw new System.Exception("Shader name is empty");
            }
            return _name.stringValue;
        }

        string GetOutputDirPath()
        {
            if (HasShaderReference)
            {
                return Path.GetDirectoryName(AssetDatabase.GetAssetPath(_shader.objectReferenceValue));
            }
            return Path.GetDirectoryName(AssetDatabase.GetAssetPath(target));
        }

        string GetShaderPath()
        {
            return string.Format("{0}/{1}.shader", GetOutputDirPath(), GetShaderName());
        }

        void Reimport()
        {
            var outputPath = GetShaderPath();
            AssetDatabase.ImportAsset(outputPath);
            _shader.objectReferenceValue = AssetDatabase.LoadAssetAtPath<Shader>(outputPath);
        }

        void ExportShader()
        {
            ShaderTemplateConvertInfo info = new ShaderTemplateConvertInfo();
            foreach (var kv in _templateParser.Conditions)
            {
                var prop = FindProperty(_conditions, kv.Key);
                var value = prop.FindPropertyRelative("Value");
                info.Conditions.Add(kv.Key, value.boolValue);
            }
            foreach (var kv in _templateParser.Blocks)
            {
                var prop = FindProperty(_blocks, kv.Key);
                var value = prop.FindPropertyRelative("Value");
                info.Blocks.Add(kv.Key, value.stringValue);
            }
            foreach (var kv in _templateParser.Variables)
            {
                var prop = FindProperty(_variables, kv.Key);
                var value = prop.FindPropertyRelative("Value");
                var constValue = ToConstVariable(kv.Key);
                if (constValue != null)
                {
                    value.stringValue = constValue;
                }
                info.Variables.Add(kv.Key, value.stringValue);
            }

            var code = _templateParser.Convert(info);
            // Rename if generator has a shader reference
            if (HasShaderReference)
            {
                var shaderFilePath = AssetDatabase.GetAssetPath(_shader.objectReferenceValue);
                var shaderFileName = Path.GetFileNameWithoutExtension(shaderFilePath);
                var newFilePath = GetShaderPath();
                if (GetShaderName() != shaderFileName)
                {
                    if (File.Exists(newFilePath))
                    {
                        throw new System.Exception(
                            string.Format("Attempted to rename {0} to {1}, but target file is exists.",
                            shaderFilePath, newFilePath)
                        );
                    }
                    AssetDatabase.RenameAsset(shaderFilePath, GetShaderName());
                }
            }

            using (var writer = new StreamWriter(GetShaderPath()))
            {
                writer.Write(code);
            }

            Reimport();

            if (HasShaderReference)
            {
                _fileWatcher.Start(GetShaderPath());
            }
        }

        void ReconvertAll()
        {
            Debug.LogFormat("<color=blue>Reconvert started.\n------------------------------</color>");
            var generators = Utils.FindAllAssets<ShaderGenerator>();
            foreach (var generator in generators)
            {
                if (target == generator)
                {
                    Debug.LogFormat("<color=green>{0}</color>", GetShaderPath());
                    OnTemplateChanged();
                    ExportShader();
                }
                else
                {
                    var editor = CreateEditor(generator) as ShaderGeneratorEditor;
                    if (editor)
                    {
                        Debug.LogFormat("<color=green>{0}</color>", editor.GetShaderPath());
                        editor.CheckShaderUpdate();
                        editor.OnTemplateChanged();
                        editor.ExportShader();
                    }                    
                }
                Debug.LogFormat("<color=blue>------------------------------\nReconvert finished.</color>");
            }
        }

        void CheckShaderUpdate()
        {
            if (!HasShaderReference)
            {
                return;
            }

            ClearError();
            try
            {
                var shaderPath = GetShaderPath();
                using (var reader = new StreamReader(shaderPath))
                {
                    var code = reader.ReadToEnd();
                    var parser = new ShaderTemplateParser(code);
                    foreach (var kv in parser.Blocks)
                    {
                        var prop = FindProperty(_blocks, kv.Key);
                        if (prop != null)
                        {
                            var value = prop.FindPropertyRelative("Value");
                            value.stringValue = kv.Value;
                        }
                    }
                }
            }
            catch (System.Exception e)
            {
                AddError(e.Message);
            }
        }

        void CreateMaterial()
        {
            var material = new Material(_shader.objectReferenceValue as Shader);
            var path = string.Format("{0}/{1}.mat", GetOutputDirPath(), GetShaderName());
            ProjectWindowUtil.CreateAsset(material, path);
        }

        void HandleKeyEvents()
        {
            var e = Event.current;
            var iskeyPressing = e.type == EventType.Layout;
            if (iskeyPressing && e.control && e.keyCode == KeyCode.R)
            {
                ExportShader();
            }
        }

        void ClearError()
        {
            _errorMessage = "";
        }

        void AddError(string error)
        {
            if (!string.IsNullOrEmpty(_errorMessage))
            {
                _errorMessage += "\n";
            }
            _errorMessage += error;
        }
    }
}