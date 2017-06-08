using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace CodeEdit
{
    public class ShaderTemplateConvertInfo
    {
        public Dictionary<string, bool>   Conditions = new Dictionary<string, bool>();
        public Dictionary<string, string> Blocks     = new Dictionary<string, string>(); 
        public Dictionary<string, string> Variables  = new Dictionary<string, string>();
    }

    public class ShaderTemplateParser
    {
        static readonly string ConditionPattern =
            @"@if\s*(?<Cond>[^:\s\n]+)(?:\s*:\s*)?(?<Init>[^\s\n]+)?\s*\n" +
            @"(?<TrueValue>[^@]*?)" +
            @"((\s*@else\s*)\n" +
            @"(?<FalseValue>[^@]*?))?" +
            @"\n\s*@endif";
        static readonly string BlockPattern =
            @"@block\s*(?<Block>[^\s\n]+)\s*\n" +
            @"(?<Value>[\s\S]*?)" +
            @"\n\s*(:?//\s*)*?@endblock";
        static readonly string VariablePattern =
            @"<(?<Name>[^=\s\n]+)(?:\s*=\s*(?<Value>[^\s\n|>]+)(\s*\|\s*(?<Value>[^\s\n|>]+))*)?\s*>";
        public string Code { get; set; }
        public Dictionary<string, bool> Conditions { get; private set; }
        public Dictionary<string, string> Blocks { get; private set; }
        public Dictionary<string, List<string>> Variables { get; private set; }
        public ShaderTemplateParser(string code)
        {
            Code = code;
            Conditions = new Dictionary<string, bool>();
            Blocks = new Dictionary<string, string>();
            Variables = new Dictionary<string, List<string>>();
            Parse();
        }
        void Parse()
        {
            ParseConditions();
            ParseBlocks();
            ParseVariables();
        }
        public string Convert(ShaderTemplateConvertInfo info)
        {
            var code = Code;
            code = WriteConditions(code, info);
            code = WriteBlocks(code, info);
            code = WriteVariables(code, info);
            code = code.Replace("\r\n", "\n");
            return code;
        }
        void ParseConditions()
        {
            Conditions.Clear();
            var regex = new Regex(ConditionPattern);
            var matches = regex.Matches(Code);
            foreach (Match match in matches)
            {
                var cond = match.Groups["Cond"].Value;
                if (Conditions.ContainsKey(cond)) continue;
                bool init = false;
                if (match.Groups["Init"].Success)
                {
                    init = bool.Parse(match.Groups["Init"].Value);
                }
                Conditions.Add(cond, init);
            }
        }
        string WriteConditions(string code, ShaderTemplateConvertInfo info)
        {
            var regex = new Regex(ConditionPattern);
            var evaluator = new MatchEvaluator(match => {
                var cond = match.Groups["Cond"].Value;
                var trueValue = match.Groups["TrueValue"].Value;
                var falseValue = match.Groups["FalseValue"].Value;
                if (!info.Conditions.ContainsKey(cond))
                {
                    throw new System.Exception(string.Format("The key \"{0}\" is not found in the given Conditions.", cond));
                }
                return (info.Conditions[cond]) ? trueValue : falseValue;
            });

            var preCode = code;
            code = regex.Replace(code, evaluator);
            while (code != preCode)
            {
                preCode = code;
                code = regex.Replace(code, evaluator);
            }

            return code;
        }

        void ParseBlocks()
        {
            Blocks.Clear();

            var regex = new Regex(BlockPattern);
            var matches = regex.Matches(Code);
            foreach (Match match in matches)
            {
                var block = match.Groups["Block"].Value;
                var value = match.Groups["Value"].Value;
                Blocks.Add(block, value);
            }
        }

        string WriteBlocks(string code, ShaderTemplateConvertInfo info)
        {
            var regex = new Regex(BlockPattern);
            var evaluator = new MatchEvaluator(match => {
                var block = match.Groups["Block"].Value;
                var value = info.Blocks[block];
                if (!info.Blocks.ContainsKey(block))
                {
                    throw new System.Exception(string.Format("The key \"{0}\" is not found in the given blocks.", block));
                }
                return string.Format("// @block {0}\n{1}\n// @endblock", block, value);
            });
            return regex.Replace(code, evaluator);
        }

        void ParseVariables()
        {
            Variables.Clear();

            var regex = new Regex(VariablePattern);
            var matches = regex.Matches(Code);
            foreach (Match match in matches)
            {
                var variable = match.Groups["Name"].Value;
                if (!Variables.ContainsKey(variable))
                {
                    var values = (from Capture capture in match.Groups["Value"].Captures select capture.Value).ToList();
                    Variables.Add(variable, values);
                }
            }
        }

        string WriteVariables(string code, ShaderTemplateConvertInfo info)
        {
            var regex = new Regex(VariablePattern);
            var evaluator = new MatchEvaluator(match => {
                var variable = match.Groups["Name"].Value;
                if (!info.Variables.ContainsKey(variable))
                {
                    throw new System.Exception(string.Format("The key \"{0}\" is not found in the given variables.", variable));
                }
                return info.Variables[variable];
            });
            return regex.Replace(code, evaluator);
        }
    }
}