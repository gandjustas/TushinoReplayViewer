using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace PboTools
{
    public class Tokenizer
    {
        enum TokenizerState
        {
            Default,
            InString,
            InComment,
            InTerm,
            InNumber,

        }
        static readonly Dictionary<char, BisTokenType> SymbolTable1 = new Dictionary<char, BisTokenType>()
        {
            {'.', BisTokenType.Dot },
            {',', BisTokenType.Comma },
            {':', BisTokenType.Colon },
            {';', BisTokenType.Semicolon },
            {'(', BisTokenType.LParen },
            {')', BisTokenType.RParen },
            {'{', BisTokenType.LCurly },
            {'}', BisTokenType.RCurly },
            {'[', BisTokenType.LSquare },
            {']', BisTokenType.RSquare },
            {'=', BisTokenType.Operator },
            {'+', BisTokenType.Operator },
            {'-', BisTokenType.Operator },
            {'*', BisTokenType.Operator },
            {'/', BisTokenType.Operator },
            {'^', BisTokenType.Operator },
            {'>', BisTokenType.Operator },
            {'<', BisTokenType.Operator },
        };

        static readonly string[] Operators2 = new[] { "&&", "||", ">=", "<=" };

        IEnumerable<FileLine> lines;
        TokenizerState state;

        int line = 0;
        int pos = 0;
        FileLine l;
        string s;


        int elStartLine = 0;
        int elStartPos = 0;
        char currentStringDelimiter = '\0';
        StringBuilder buffer = new StringBuilder();

        private bool Is(string v)
        {
            return string.Compare(s, pos, v, 0, v.Length, StringComparison.Ordinal) == 0;
        }

        private Tokenizer(IEnumerable<FileLine> lines)
        {
            this.lines = lines;
        }


        private IEnumerable<BisToken> Parse()
        {
            state = TokenizerState.Default;

            foreach (var l in lines)
            {
                this.l = l;
                this.s = l.Line;
                line = l.LineNumber;
                pos = 0;

                while (pos <= s.Length)
                {
                    var c = pos < s.Length ? s[pos] : '\n';

                    switch (state)
                    {
                        case TokenizerState.Default:
                            if (c == '\n')
                            {
                                pos++;
                                break;
                            }

                            if (char.IsWhiteSpace(c))
                            {
                                pos++;
                                break;
                            }

                            if (Is("//"))
                            {
                                pos = s.Length + 1;
                                break;
                            }

                            if (Is("/*"))
                            {
                                elStartLine = line;
                                elStartPos = pos;
                                buffer.Clear();
                                state = TokenizerState.InComment;
                                pos++;
                            }
                            else if (Is("'") || Is("\""))
                            {
                                elStartLine = line;
                                elStartPos = pos;
                                buffer.Clear();
                                currentStringDelimiter = c;
                                state = TokenizerState.InString;
                            }
                            else if (char.IsDigit(c))
                            {
                                elStartLine = line;
                                elStartPos = pos;
                                buffer.Clear();
                                buffer.Append(c);
                                state = TokenizerState.InNumber;
                            }
                            else if (char.IsLetter(c) || c == '_')
                            {
                                elStartLine = line;
                                elStartPos = pos;
                                buffer.Clear();
                                buffer.Append(c);
                                state = TokenizerState.InTerm;
                            }
                            else
                            {
                                string op;
                                if (SymbolTable1.TryGetValue(c, out var tokenType))
                                {
                                    yield return new BisToken
                                    {
                                        Type = tokenType,
                                        BeginLine = line,
                                        BeginPos = pos,
                                        EndLine = line,
                                        EndPos = pos + 1,
                                        Value = c.ToString(),
                                        File = l.File
                                    };
                                }
                                else if ((op = Array.Find(Operators2, Is)) != null)
                                {
                                    yield return new BisToken
                                    {
                                        Type = BisTokenType.Operator,
                                        BeginLine = line,
                                        BeginPos = pos,
                                        EndLine = line,
                                        EndPos = pos + 2,
                                        Value = op,
                                        File = l.File
                                    };
                                    pos++;
                                }
                                else
                                {
                                    yield return new BisToken
                                    {
                                        Type = BisTokenType.Unknown,
                                        BeginLine = line,
                                        BeginPos = pos,
                                        EndLine = line,
                                        EndPos = pos + 1,
                                        Value = c.ToString(),
                                        File = l.File
                                    };
                                }

                            }

                            pos++;


                            break;
                        case TokenizerState.InString:
                            if (c == currentStringDelimiter)
                            {
                                if (Is(new string(currentStringDelimiter, 2)))
                                {
                                    buffer.Append(currentStringDelimiter);
                                    pos += 2;
                                }
                                else
                                {
                                    pos++;
                                    state = TokenizerState.Default;
                                    yield return new BisToken
                                    {
                                        Type = BisTokenType.String,
                                        Value = buffer.ToString(),
                                        BeginLine = elStartLine,
                                        BeginPos = elStartPos,
                                        EndLine = line,
                                        EndPos = pos,
                                        File = l.File
                                    };
                                }
                            }
                            else
                            {
                                buffer.Append(c);
                                pos++;
                            }
                            break;
                        case TokenizerState.InComment:
                            if (Is("*/"))
                            {
                                state = TokenizerState.Default;
                                pos++;
                            }
                            pos++;
                            break;
                        case TokenizerState.InTerm:
                            if (char.IsLetterOrDigit(c) || c == '_')
                            {
                                buffer.Append(c);
                                pos++;
                            }
                            else
                            {
                                var term = buffer.ToString();
                                yield return new BisToken
                                {
                                    Type = BisTokenType.Term,
                                    Value = term,
                                    BeginLine = elStartLine,
                                    BeginPos = elStartPos,
                                    EndLine = line,
                                    EndPos = pos,
                                    File = l.File
                                };
                                state = TokenizerState.Default;
                            }
                            break;
                        case TokenizerState.InNumber:
                            if (char.IsDigit(c))
                            {
                                buffer.Append(c);
                                pos++;
                            }
                            else
                            {
                                yield return new BisToken
                                {
                                    Type = BisTokenType.Number,
                                    Value = buffer.ToString(),
                                    BeginLine = elStartLine,
                                    BeginPos = elStartPos,
                                    EndLine = line,
                                    EndPos = pos,
                                    File = l.File
                                };
                                state = TokenizerState.Default;
                            }
                            break;
                    }
                }
            }
        }

        public static IEnumerable<BisToken> Tokenize(IEnumerable<FileLine> lines)
        {
            var t = new Tokenizer(lines);
            return t.Parse();
        }

    }
}
