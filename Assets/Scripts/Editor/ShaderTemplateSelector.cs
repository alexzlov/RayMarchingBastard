using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace CodeEdit
{
    public class ShaderTemplateSelector
    {
        public SerializedProperty Property { get; private set; }
        public delegate void OnChangeEventHandler();
        public OnChangeEventHandler OnChange = () => { };

        public string Selected
        {
            get { return Property.stringValue; }
        }

        public string Text
        {
            get
            {
                if (string.IsNullOrEmpty(Property.stringValue))
                {
                    Property.stringValue = _list[0];
                }
                var asset = Resources.Load<TextAsset>(Utils.ShaderTemplateDirPath + "/" + Property.stringValue);
                return asset ? asset.text : "";
            }
        }

        private readonly List<string> _list = new List<string>();

        public ShaderTemplateSelector(SerializedProperty property)
        {
            Property = property;
            var paths = Directory.GetFiles(Utils.ShaderTemplateDirPath);
            foreach (var path in paths)
            {
                if (Path.GetExtension(path) == ".txt")
                {
                    var name = Path.GetFileNameWithoutExtension(path);
                    if (name != null && name[0] != '_')
                    {
                        _list.Add(Path.GetFileNameWithoutExtension(path));
                    }
                }
            }
        }

        public void Draw()
        {
            var currentIndex = _list.IndexOf(Property.stringValue);
            if (currentIndex == -1)
            {
                currentIndex = 0;
            }
            var selectedIndex = EditorGUILayout.Popup("Shader Template", currentIndex, _list.ToArray());
            var previous = Property.stringValue;
            var currrent = _list[selectedIndex];
            if (previous != currrent)
            {
                Property.stringValue = currrent;
                OnChange();
            }
        }
    }

}