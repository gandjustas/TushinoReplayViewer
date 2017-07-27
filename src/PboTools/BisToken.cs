using System;

namespace PboTools
{
    public enum BisTokenType
    {
        Unknown = 0,
        LParen, RParen, // ()
        LSquare, RSquare, // []
        LCurly, RCurly, // {}
        String,
        Number,
        Term,
        Dot,
        Comma,
        Colon,
        Semicolon,
        Operator,
    }
    public struct BisToken
    {
        public BisTokenType Type { get; internal set; }
        public int BeginLine { get; internal set; }
        public int BeginPos { get; internal set; }
        public int EndLine { get; internal set; }
        public int EndPos { get; internal set; }
        public string Value { get; internal set; }
        public string File { get; internal set; }

        public override string ToString()
        {
            return Value;
        }
    }
}