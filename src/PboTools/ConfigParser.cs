using System;
using System.Collections.Generic;
using System.Text;

namespace PboTools
{
    public class ConfigParser
    {
        const string @class = "class";
        const string @enum = "enum";
        const string @delete = "delete";
        public static ConfigParseResult Parse(IEnumerable<BisToken> tokens)
        {
            var parser = new ConfigParser(tokens);
            return parser.Parse();
        }

        private IEnumerable<BisToken> tokens;
        private BisToken t;

        private ConfigParser(IEnumerable<BisToken> tokens)
        {
            this.tokens = tokens;
        }

        private IEnumerator<BisToken> e;
        private List<ConfigClass> classes = new List<ConfigClass>();
        private List<string> externs = new List<string>();
        private Dictionary<string, int> enums = new Dictionary<string, int>();
        private List<string> errors = new List<string>();
        private List<BisToken> recordBuffer = new List<BisToken>();
        private bool recordTokens = false;

        private ConfigParseResult Parse()
        {
            var root = new ConfigClass("", null);
            e = tokens.GetEnumerator();

            while (e.MoveNext())
            {
                t = e.Current;
                if (t.Type == BisTokenType.Term)
                {
                    switch (t.Value.ToLower())
                    {
                        case @class:
                            var c = ParseClass();
                            if (c != null) root.AddSubclass(c);
                            break;
                        case @enum:
                            ParseEnum();
                            break;

                    }
                    continue;
                }
                errors.Add(string.Format("Expected 'class' or 'enum' at {0}, Line={1}, Column={2}", t.File, t.BeginLine, t.BeginPos + 1));
            }
            return new ConfigParseResult(new ConfigFile(root, enums), errors);
        }

        private ConfigClass ParseClass()
        {
            if (!Expect(BisTokenType.Term, @class)) return null;

            var name = ParseName();
            if (name == null) return null;
            string baseName = null;

            if (!Next()) return null;
            if (t.Type == BisTokenType.Semicolon) //External
            {
                return new ConfigClass(name);
            }

            if (t.Type == BisTokenType.Colon) //inheritance
            {
                baseName = ParseName();
                if (!Next()) return null;
            }

            var c = new ConfigClass(name, baseName);


            if (!Expect(BisTokenType.LCurly, "{")) return null;
            while (true)
            {
                if (!Next()) return null;
                switch (t.Value.ToLower())
                {
                    case "}": break;
                    case @class:
                        var sc = ParseClass();
                        if (sc != null) c.AddSubclass(sc);
                        break;
                    case @delete:
                        var del = ParseName();
                        if (del == null) continue;
                        c.AddDelete(del);
                        break;
                    default:
                        var propName = ReadIdentifier();
                        if (propName == null) continue;
                        if (!Next()) return null;
                        if (t.Type == BisTokenType.LSquare)
                        {
                            if (!ExpectNext(BisTokenType.RSquare, "]")) continue;
                            if (!Next()) return null;
                            propName += "[]";
                        }
                        Expect(BisTokenType.Operator, "=");

                        var propValue = ParseConfigPropertyValue(propName);
                        if (propValue == null) continue;
                        c.AddProperty(propValue);
                        break;

                }
                if (t.Type == BisTokenType.RCurly) break;
            }
            if (!Expect(BisTokenType.RCurly, "}")) return null;
            ExpectNext(BisTokenType.Semicolon, ";");
            return c;
        }

        private void ParseEnum()
        {
            if (!ExpectNext(BisTokenType.Term, @enum)) return;
            if (!ExpectNext(BisTokenType.LCurly, "{")) return;
            while (true)
            {
                var r = ParseEnumValue();
                if (r == null) continue;
                if (r.Type != ConfigPropertyType.Int)
                {
                    errors.Add(string.Format("Expected number value for {2} at {0}, Line={1}", t.File, t.BeginLine, r.Name));
                }
                else
                {
                    enums.Add(r.Name, (int)r.Value);
                }
                if (t.Type == BisTokenType.RCurly) break;
            }
            if (!Expect(BisTokenType.RCurly, "}")) return;
            ExpectNext(BisTokenType.Semicolon, ";");
        }

        private ConfigProperty ParseEnumValue()
        {
            var name = ParseName();
            if (name == null) return null;
            ExpectNext(BisTokenType.Operator, "=");

            return ParseConfigPropertyValue(name);

        }

        private ConfigProperty ParseConfigPropertyValue(string name)
        {
            recordBuffer.Clear();
            recordTokens = true;
            try
            {
                if (!Next()) return null;
                var v = ReadValue();
                if (v == null) return null;

                if (t.Type == BisTokenType.Operator && (v.Item1 == ConfigPropertyType.Float || v.Item1 == ConfigPropertyType.Int))
                {
                    //Might be expression, convert to string
                    while (Next() && t.Type != BisTokenType.Semicolon);
                    recordTokens = false;
                    if (!Expect(BisTokenType.Semicolon, ";")) return null;
                        
                    return new ConfigProperty(name, ConvertToString(recordBuffer), ConfigPropertyType.String);
                }
                else
                {
                    if (!Expect(BisTokenType.Semicolon, ";")) return null;
                    return new ConfigProperty(name, v.Item2, v.Item1);
                }
            }
            finally
            {
                recordTokens = false;
            }
        }

        private string ConvertToString(List<BisToken> recordBuffer)
        {
            var sb = new StringBuilder();
            var start = true;
            foreach (var t in recordBuffer)
            {
                switch (t.Type)
                {
                    case BisTokenType.LParen:
                        start = true;
                        sb.Append(t.Value);
                        break;
                    case BisTokenType.Comma:
                    case BisTokenType.Colon:
                    case BisTokenType.Semicolon:
                        sb.Append(t.Value);
                        sb.Append(' ');
                        break;
                    case BisTokenType.Operator:
                        if(!start)
                        {
                            sb.Append(' ');
                            sb.Append(t.Value);
                            sb.Append(' ');

                        }
                        else
                        {
                            sb.Append(t.Value);

                        }
                        break;
                    default:
                        sb.Append(t.Value);
                        break;
                }
                start = false;
            }
            return sb.ToString();
        }

        private string ParseName()
        {
            if (!Next()) return null;
            return ReadIdentifier();
        }

        private string ReadIdentifier()
        {
            if (t.Type != BisTokenType.Term)
            {
                errors.Add(string.Format("Expected identifier at {0}, Line={1}, Column={2}", t.File, t.BeginLine, t.BeginPos + 1));
                return null;
            }
            return t.Value;
        }

        private Tuple<ConfigPropertyType, object> ReadValue()
        {
            var num = 1;
            string locToken = "";
            switch (t.Type)
            {
                case BisTokenType.Unknown:
                    if(t.Value == "$")
                    {
                        locToken = t.Value;
                        if (!Next()) return null;
                        goto case BisTokenType.String;
                    }
                    break;
                case BisTokenType.Operator:
                    if (t.Value == "-")
                    {
                        num = -1;
                        if (!Next()) return null;
                        goto case BisTokenType.Number;
                    }
                    break;
                case BisTokenType.Term:
                case BisTokenType.String:
                    var s = t.Value;
                    if (!Next()) return null;
                    return Tuple.Create(ConfigPropertyType.String, (object)(locToken+s));
                case BisTokenType.Number:
                    var intPart = ReadInt();
                    if (!intPart.HasValue) return null;

                    if (t.Type != BisTokenType.Semicolon)
                    {
                        var r = ReadFloatPart(intPart.Value);
                        if (!r.HasValue) return null;
                        return Tuple.Create(ConfigPropertyType.Float, (object)(r.Value * num));
                    }
                    else
                    {
                        return Tuple.Create(ConfigPropertyType.Int, (object)(intPart * num));
                    }
                case BisTokenType.LCurly:
                    var array = ReadArray();
                    if (array == null) return null;
                    if (!Next()) return null;
                    return Tuple.Create(ConfigPropertyType.Array, (object)array);
            }
            errors.Add(string.Format("Expected string, number or array at {0}, Line={1}, Column={2}", t.File, t.BeginLine, t.BeginPos + 1));
            return null;
        }

        private int? ReadInt()
        {
            if (t.Type != BisTokenType.Number)
            {
                errors.Add(string.Format("Expected number at {0}, Line={1}, Column={2}", t.File, t.BeginLine, t.BeginPos + 1));
            }
            if (!int.TryParse(t.Value, out var intPart))
            {
                errors.Add(string.Format("Can't convert {3} to number at {0}, Line={1}, Column={2}", t.File, t.BeginLine, t.BeginPos + 1, t.Value));
                return null;
            }
            if (!Next()) return null;
            return intPart;
        }
        private double? ReadFloatPart(int intPart)
        {
            int decimalPart = 0;
            int exponentPart = 1;
            int exponentSign = 1;
            if (t.Type == BisTokenType.Dot)
            {
                if (!Next()) return null;
                var r = ReadInt();
                if (!r.HasValue) return null;
                decimalPart = r.Value;
            }
            if (t.Type == BisTokenType.Term && (t.Value == "e" || t.Value == "E"))
            {
                if (!Next()) return null;
                if (t.Type == BisTokenType.Operator && (t.Value == "+" || t.Value == "-"))
                {
                    if (t.Value == "-") exponentSign = -1;
                    if (!Next()) return null;
                }
                var r = ReadInt();
                if (!r.HasValue) return null;
                exponentPart = r.Value;
            }
            return (intPart + (decimalPart / (decimalPart.ToString().Length * 1.0))) * Math.Pow(10, exponentPart * exponentSign);

        }

        private object[] ReadArray()
        {
            if (!Expect(BisTokenType.LCurly, "{")) return null;
            var result = new List<object>();
            while (true)
            {
                if (!Next()) return null;
                if (t.Type == BisTokenType.RCurly) break;
                var v = ReadValue();
                if (v == null) break;
                result.Add(v.Item2);
                if (t.Type == BisTokenType.RCurly) break;
                Expect(BisTokenType.Comma, ",");
            }

            Expect(BisTokenType.RCurly, "}");
            return result.ToArray();

        }

        private bool ExpectNext(BisTokenType type, string expected)
        {
            return Next() && Expect(type, expected);

        }
        private bool Expect(BisTokenType type, string expected)
        {
            if (t.Type != type || string.CompareOrdinal(t.Value, expected) != 0)
            {
                errors.Add(string.Format("Expected '{3}' at {0}, Line={1}, Column={2}", t.File, t.BeginLine, t.BeginPos + 1, expected));
                return false;
            }
            return true;
        }

        private bool Next()
        {
            if (!e.MoveNext())
            {
                errors.Add("Unexpected end of file");
                return false;
            }
            t = e.Current;
            recordBuffer.Add(t);
            return true;
        }
    }
}
