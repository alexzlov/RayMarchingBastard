using CodeEdit.Syntaxes;
using UnityEditor;
using UnityEngine;

namespace CodeEdit
{
    public class ShaderCodeEditor
    {
        public string Name { get; private set; }
        public SerializedProperty Value { get; private set; }
        public SerializedProperty Folded { get; private set; }

        public string Code
        {
            get { return Value != null ? Value.stringValue : ""; }
            private set { Value.stringValue = value; }
        }

        private CodeEditor _editor;
        private Vector2 _scrollPosition;
        private Font _font;

        public ShaderCodeEditor(string name, SerializedProperty value, SerializedProperty folded)
        {
            Name   = name;
            Value  = value;
            Folded = folded;

            _font = Resources.Load<Font>(EditorSettings.Font);

            Color color, bgColor;
            ColorUtility.TryParseHtmlString(EditorColor.Background, out bgColor);
            ColorUtility.TryParseHtmlString(EditorColor.Color, out color);

            _editor = new CodeEditor(name)
            {
                BackgroundColor = bgColor,
                TextColor = color,
                Highlighter = ShaderSyntax.Highlight,
            };
        }

        public void Draw()
        {
            var preFolded = Folded.boolValue;
            Folded.boolValue = Utils.Foldout(Name, Folded.boolValue);
            if (!Folded.boolValue)
            {
                if (preFolded)
                {
                    GUI.FocusControl("");
                }
                return;
            }
            if (!preFolded)
            {
                GUI.FocusControl(Name);
            }
            var minHeight = GUILayout.MinHeight(EditorSettings.MinHeight);
            var maxHeight = GUILayout.MaxHeight(Screen.height);
            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition, minHeight, maxHeight);
            {
                var style = new GUIStyle(GUI.skin.textArea)
                {
                    padding = new RectOffset(6, 6, 6, 6),
                    font = _font,
                    fontSize = EditorSettings.FontSize,
                    wordWrap = EditorSettings.WordWrap,
                };
                var editedCode = _editor.Draw(Code, style, GUILayout.ExpandHeight(true));
                if (editedCode != null && editedCode != Code)
                {
                    Code = editedCode;
                }
            }
            EditorGUILayout.EndScrollView();

            EditorGUILayout.Space();
        }
    }
}