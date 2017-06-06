using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;

namespace CodeEdit.Syntaxes
{
    public static class ShaderSyntax
    {
        public static readonly string[] Type = {
            "void",
            "fixed",
            "fixed[1-4]",
            "fixed[1-4]x[1-4]",
            "half",
            "half[1-4]",
            "half[1-4]x[1-4]",
            "float",
            "float[1-4]",
            "float[1-4]x[1-4]",
            "RaymarchInfo",
            "PostEffectOutput"
        };

        public static readonly string[] Keyword = {
            "#include",
            "#define",
            "return",
            "out",
            "inout",
            "inline"
        };

        public static readonly string[] Symbol = {
            @"[{}()=;,+\-*/<>|]+"
        };

        public static readonly string[] Digit = {
            @"(?<![a-zz_Z_])[+-]?[0-9]+\.?[0-9]?(([eE][+-]?)?[0-9]+)?"
        };

        public static readonly string[] String = {
            "(\"[^\"\\n]*?\")"
        };

        public static readonly string[] Comment = {
            @"/\*[\s\S]*?\*/|//.*"
        };

        public static readonly string[] Entrypoint = {
            "DistanceFunction",
            "PostEffect"
        };

        public static readonly string[] CgProgram = {
            "abs",
            "acos",
            "all",
            "any",
            "asin",
            "atan",
            "atan2",
            "bitCount",
            "bitfieldExtract",
            "bitfieldInsert",
            "bitfieldReverse",
            "ceil",
            "clamp",
            "clip",
            "cos",
            "cosh",
            "cross",
            "ddx",
            "ddy",
            "degrees",
            "determinant",
            "distance",
            "dot",
            "exp",
            "exp2",
            "faceforward",
            "findLSB",
            "findMSB",
            "floatToIntBits",
            "floatToRawIntBits",
            "floor",
            "fmod",
            "frac",
            "frexp",
            "fwidth",
            "intBitsToFloat",
            "inverse",
            "isfinite",
            "isinf",
            "isnan",
            "ldexp",
            "length",
            "lerp",
            "lit",
            "log",
            "log10",
            "log2",
            "max",
            "min",
            "modf",
            "mul",
            "normalize",
            "pack",
            "pow",
            "radians",
            "reflect",
            "refract",
            "round",
            "rsqrt",
            "saturate",
            "sign",
            "sin",
            "sincos",
            "sinh",
            "smoothstep",
            "sqrt",
            "step",
            "tan",
            "tanh",
            "tex1D",
            "tex2D",
            "tex3D",
            "transpose",
            "trunc",
            "unpack"
        };

        public static readonly string[] Unity = {
            "UNITY_MATRIX_MVP",
            "UNITY_MATRIX_MV",
            "UNITY_MATRIX_V",
            "UNITY_MATRIX_P",
            "UNITY_MATRIX_VP",
            "UNITY_MATRIX_T_MV",
            "UNITY_MATRIX_IT_MV",
            "unity_ObjectToWorld",
            "unity_WorldToObject",
            "_WorldSpaceCameraPos",
            "_ProjectionParams",
            "_ScreenParams",
            "_ZBufferParams",
            "unity_OrthoParams",
            "unity_CameraProjection",
            "unity_CameraInvProjection",
            "unity_CameraWorldClipPlanes",
            "_Time",
            "_SinTime",
            "_CosTime",
            "unity_DeltaTime",
            "_LightColor0",
            "_WorldSpaceLightPos0",
            "_LightMatrix0",
            "unity_4LightPosX0",
            "unity_4LightAtten0",
            "unity_LightColor",
            "_LightColor",
            "_LightMatrix0",
            "unity_LightColor",
            "unity_LightPosition",
            "unity_LightAtten",
            "unity_SpotDirection",
            "unity_AmbientSky",
            "unity_AmbientEquator",
            "unity_AmbientGround",
            "UNITY_LIGHTMODEL_AMBIENT",
            "unity_FogColor",
            "unity_FogParams",
            "unity_LODFade"
        };

        public static readonly string[] Raymarching = {
            "Rand",
            "Mod",
            "SmoothMin",
            "Repeat",
            "Rotate",
            "TwistX",
            "TwistY",
            "TwistZ",
            "ToLocal",
            "ToWorld",
            "GetDepth",
            "Sphere",
            "RoundBox",
            "Box",
            "Torus",
            "Plane",
            "Cylinder",
            "HexagonalPrismX",
            "HexagonalPrismY",
            "HexagonalPrismZ",
            "PI",
            "_Scale"
        };

        static Regex _regex;
        static MatchEvaluator _evaluator;
        static Dictionary<string, string> ColorTable = new Dictionary<string, string> {
            { "symbol",      EditorColor.Symbol      },
            { "digit",       EditorColor.Digit       },
            { "str",         EditorColor.String      },
            { "comment",     EditorColor.Comment     },
            { "type",        EditorColor.Type        },
            { "keyword",     EditorColor.Keyword     },
            { "entrypoint",  EditorColor.Entrypoint  },
            { "cgprogram",   EditorColor.CgProgram   },
            { "raymarching", EditorColor.Raymarching },
            { "unity",       EditorColor.Unity       }
        };

        static string ToColoredCode(string code, string color)
        {
            return "<color=" + color + ">" + code + "</color>";
        }

        [InitializeOnLoadMethod]
        static void Init()
        {
            const string forwardSeparator = "(?<![0-9a-zA-Z_])";
            const string backwardSeparator = "(?![0-9a-zA-Z_])";
            const string pattern1 = "(?<{0}>({1}))";
            var pattern2 = string.Format("(?<{0}>{2}({1}){3})", "{0}", "{1}", forwardSeparator, backwardSeparator);

            var patterns = new[] {
                string.Format(pattern1, "comment",      string.Join("|", Comment)),
                string.Format(pattern2, "type",         string.Join("|", Type)),
                string.Format(pattern2, "keyword",      string.Join("|", Keyword)),
                string.Format(pattern2, "entrypoint",   string.Join("|", Entrypoint)),
                string.Format(pattern2, "cgprogram",    string.Join("|", CgProgram)),
                string.Format(pattern2, "raymarching",  string.Join("|", Raymarching)),
                string.Format(pattern2, "unity",        string.Join("|", Unity)),
                string.Format(pattern1, "str",          string.Join("|", String)),
                string.Format(pattern1, "digit",        string.Join("|", Digit)),
                string.Format(pattern1, "symbol",       string.Join("|", Symbol))
            };
            var combinedPattern = "(" + string.Join("|", patterns) + ")";

            _regex = new Regex(combinedPattern, RegexOptions.Compiled);

            _evaluator = match =>
            {
                foreach (var pair in ColorTable.Where(pair => match.Groups[pair.Key].Success))
                {
                    return ToColoredCode(match.Value, pair.Value);
                }
                return match.Value;
            };
        }

        public static string Highlight(string code)
        {
            return _regex.Replace(code, _evaluator);
        }
    }
}