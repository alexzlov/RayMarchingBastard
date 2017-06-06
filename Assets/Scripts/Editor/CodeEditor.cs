using UnityEngine;
using System;
using UnityEditor;

namespace CodeEdit
{
    public class CodeEditor
    {
        public string ControlName { get; set; }
        public Color BackgroundColor { get; set; }
        public Color TextColor { get; set; }
        public Func<string, string> Highlighter { get; set; }

        private string CachedCode { get; set; }
        private string CachedHighlightedCode { get; set; }

        public bool IsFocused
        {
            get { return GUI.GetNameOfFocusedControl() == ControlName; }
        }

        public CodeEditor(string controlName)
        {
            ControlName = controlName;
            BackgroundColor = Color.black;
            TextColor = Color.green;
            Highlighter = code => code;
        }

        public string Draw(string code, GUIStyle style, params GUILayoutOption[] options)
        {
            var preBackgroundColor = GUI.backgroundColor;
            var preColor = GUI.color;

            GUI.backgroundColor = BackgroundColor;
            GUI.color = TextColor;

            var backStyle = new GUIStyle(style)
            {
                normal = { textColor = Color.clear },
                hover = { textColor = Color.clear },
                active = { textColor = Color.clear },
                focused = { textColor = Color.clear },
            };
            GUI.SetNextControlName(ControlName);
            // TextArea doesn't support a lot of userful features (tab handling e.t.c.)
            var editedCode = EditorGUILayout.TextArea(code, backStyle, GUILayout.ExpandHeight(true));

            if (!Equals(editedCode, code))
            {
                code = editedCode;
            }
            if (string.IsNullOrEmpty(CachedHighlightedCode) || (CachedCode != code))
            {
                CachedCode = code;
                CachedHighlightedCode = Highlighter(code);
            }

            GUI.backgroundColor = Color.clear;

            var foreStyle = new GUIStyle(style)
            {
                richText = true,
                normal = { textColor = TextColor },
                hover = { textColor = TextColor },
                active = { textColor = TextColor },
                focused = { textColor = TextColor },
            };
            EditorGUI.TextArea(GUILayoutUtility.GetLastRect(), CachedHighlightedCode, foreStyle);

            GUI.backgroundColor = preBackgroundColor;
            GUI.color = preColor;
            return code;
        }
    }
}