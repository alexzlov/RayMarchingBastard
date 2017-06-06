using System.Collections.Generic;
using UnityEngine;

namespace CodeEdit
{
    [System.Serializable]
    public struct ShaderVariables
    {
        public string Key;
        public string Value;
    }

    [System.Serializable]
    public struct ShaderCondition
    {
        public string Key;
        public bool   Value;
    }

    [System.Serializable]
    public struct ShaderBlock
    {
        public string Key;
        public string Value;
        public bool   IsFolded;
    }

    [CreateAssetMenu(menuName = "Shader/Raymarching Shader Generator", order = 110)]
    public class ShaderGenerator : ScriptableObject
    {
        public string ShaderName      = "";
        public Shader ShaderReference = null;
        public string ShaderTemplate  = "";

        public List<ShaderVariables> Variables  = new List<ShaderVariables>();
        public List<ShaderCondition> Conditions = new List<ShaderCondition>();
        public List<ShaderBlock>     Blocks     = new List<ShaderBlock>();

        public bool BasicFolded      = true;
        public bool ConditionsFolded = false;
        public bool VariablesFolded  = false;
        public bool MaterialsFolded  = false;

        public string DistanceFunction = "";
        public string PostEffect       = "";
    }
}