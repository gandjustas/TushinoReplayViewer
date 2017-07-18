using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace PboTools
{
    public class Preprocessor
    {
        static readonly Regex defineParser = new Regex(@"^\s*(?<name>\w+)(?:\s*\((?<params>\w+\s*(,\s*\w+\s*)*)\))?(?:\s+(?<body>.*))?", RegexOptions.Compiled);
        bool supress = false;
        bool wasIf = false;
        bool insideComment = false;
        int lineNumber = 0;

        private string fileName;
        private Func<string, string, Tuple<string, TextReader>> lookupFile;
        private Dictionary<string, MacroDef> macros;
        private TextReader reader;

        private Preprocessor(string fileName, TextReader reader, Func<string, string, Tuple<string, TextReader>> lookupFile, Dictionary<string, MacroDef> macros)
        {
            this.fileName = fileName;
            this.reader = reader;
            this.lookupFile = lookupFile;
            this.macros = macros;
        }

        public static IEnumerable<string> Prepocess(string fileName, TextReader reader, Func<string, string, Tuple<string, TextReader>> lookupFile)
        {
            var p = new Preprocessor(fileName, reader, lookupFile, new Dictionary<string, MacroDef>());
            return p.Prepocess();
        }

        public IEnumerable<string> Prepocess()
        {
            string s;
            newLine:
            while ((s = reader.ReadLine()) != null)
            {
                lineNumber++;
                s = s.Trim();
                if (!insideComment)
                {
                    var idx = s.IndexOf("/*");
                    if (idx > -1)
                    {
                        s = s.Substring(0, idx);
                        insideComment = true;
                    }
                }
                else
                {
                    var idx = s.IndexOf("*/");
                    if (idx > -1)
                    {
                        s = s.Substring(idx + 2);
                        insideComment = false;
                    }
                    else continue;
                }

                var commentIdx = s.IndexOf("//");
                if (commentIdx > -1)
                {
                    s = s.Substring(0, commentIdx);
                }

                if (s.StartsWith("#"))
                {
                    //Parse preprocessor
                    if (s.Trim() == "#endif")
                    {
                        supress = false;
                        wasIf = false;
                    }
                    if (s.Trim() == "#else")
                    {
                        if (wasIf)
                        {
                            supress = !supress;
                        }
                        else
                        {
                            throw new FormatException(string.Format("Unmatched #else in {0} line {1}", fileName, lineNumber));
                        }
                    }
                    if (supress) goto newLine;


                    if (s.StartsWith("#ifdef"))
                    {
                        var m = s.Substring("#ifdef".Length + 1);
                        supress = !macros.ContainsKey(m);
                        wasIf = true;
                    }

                    if (s.StartsWith("#ifndef"))
                    {
                        var m = s.Substring("#ifndef".Length + 1);
                        supress = macros.ContainsKey(m);
                        wasIf = true;

                    }

                    if (s.StartsWith("#undef"))
                    {
                        var m = s.Substring("#undef".Length + 1);
                        macros.Remove(m);
                    }

                    if (s.StartsWith("#define"))
                    {
                        var macro = new MacroDef
                        {
                            Line = lineNumber,
                            File = fileName
                        };

                        var block = s.Substring("#define".Length + 1);

                        while (block.EndsWith("\\"))
                        {
                            s = reader.ReadLine();
                            block = block.TrimEnd('\\') + s;
                            lineNumber++;
                        }
                        var m = defineParser.Match(block);
                        if (!m.Success)
                        {
                            throw new FormatException(string.Format("Error parsing define at {0} line {1}", fileName, lineNumber));
                        }

                        macro.Name = m.Groups["name"].Value;
                        macro.Body = m.Groups["body"].Value;
                        macro.Parameters = m.Groups["params"].Value.Split(',').Select(p => p.Trim()).Where(p => !string.IsNullOrEmpty(p)).ToArray();
                        macro.Prepare();
                        macros[macro.Name] = macro;

                    }

                    if (s.StartsWith("#include"))
                    {
                        var f = s.Substring("#include".Length + 1).Trim('"', ' ', '<', '>');
                        var l = lookupFile(fileName, f);
                        var p = new Preprocessor(l.Item1, l.Item2, lookupFile, macros);
                        foreach (var x in p.Prepocess())
                        {
                            yield return x;
                        }
                    }
                    continue;
                }
                if (supress) goto newLine;
                if (s.Length > 0)
                {
                    yield return PreprocessLine(s);
                }
            }
        }

        private string PreprocessLine(string s)
        {

            string s1;
            while ((s1 = PreprocessLineAux(s)) != s)
            {
                s = s1;
            }
            return s1;

        }

        private string PreprocessLineAux(string s)
        {
            foreach (var p in macros)
            {
                s = p.Value.Regex.Replace(s, m => ExpandMacro(m, p.Value));
            }
            return s;
        }

        private string ExpandMacro(Match m, MacroDef macro)
        {
            if (macro.Parameters.Length == 0)
            {
                return PreprocessLine(macro.Body);
            }
            else
            {
                var s = m.Groups[1].Value;
                var pos = 0;
                var level = 0;
                var ps = new List<string>(macro.Parameters.Length);
                var currentParam = new StringBuilder();
                while (pos < s.Length)
                {
                    if ("([{".Contains(s[pos])) level++;
                    if (")]}".Contains(s[pos])) level--;

                    if (s[pos] == ',' && level == 0)
                    {
                        //next param
                        ps.Add(currentParam.ToString());
                        currentParam.Clear();
                    }
                    else
                    {
                        currentParam.Append(s[pos]);
                    }
                    pos++;

                }
                ps.Add(currentParam.ToString());

                var pps = ps.Select(PreprocessLine).ToList();

                return PreprocessLine(InjectParams(macro, pps));
            }
        }

        private string InjectParams(MacroDef macro, List<string> values)
        {
            var ps = macro.Parameters.Zip(values, Tuple.Create).ToList(); ;
            var parts = macro.BodyParts;
            for (int i = 0; i < parts.Length; i++)
            {
                foreach (var p in ps)
                {
                    parts[i] = parts[i].Replace("#" + p.Item1, "\"" + p.Item2 + "\"");
                    parts[i] = parts[i].Replace(p.Item1, p.Item2);

                }
            }
            return string.Join("", parts);

        }

        //private string PreprocessParam(string pv)
        //{
        //    return string.Join("", (new Tokenizer(new StringReader(pv), FileName) { Macros = this.Macros }).Parse().Select(t => t.Value));
        //}

        //private IEnumerable<string> SplitParams(string value)
        //{
        //    var tokenizer = new Tokenizer(new StringReader(value), FileName);
        //    var r = new List<string>();
        //    var level = 0;
        //    var pos = 0;
        //    foreach (var t in tokenizer.Parse())
        //    {
        //        switch (t.Type)
        //        {
        //            case BisTokenType.LParen:
        //            case BisTokenType.LSquare:
        //            case BisTokenType.LCurly:
        //                level++;
        //                break;
        //            case BisTokenType.RParen:
        //            case BisTokenType.RSquare:
        //            case BisTokenType.RCurly:
        //                level--;
        //                break;
        //            case BisTokenType.Comma:
        //                if (level == 0)
        //                {
        //                    yield return value.Substring(pos, t.EndPos - pos - 1);
        //                    pos = t.EndPos;
        //                }
        //                break;
        //        }
        //    }
        //    yield return value.Substring(pos, value.Length - pos);

        //}
    }
}
