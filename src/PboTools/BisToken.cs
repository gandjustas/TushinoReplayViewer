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
        public BisTokenType Type { get; set; }
        public int BeginLine { get; set; }
        public int BeginPos { get; set; }
        public int EndLine { get; set; }
        public int EndPos { get; set; }
        public string Value { get; set; }
        public string File { get; set; }

        public override string ToString()
        {
            return string.Format("Line {0:D3}, Column {1: D3}, Type {2}, Value {3}", BeginLine, BeginPos+1, Type, Value);
        }
    }
}