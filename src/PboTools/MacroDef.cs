using System;
using System.Text.RegularExpressions;

namespace PboTools
{
    internal class MacroDef
    {
        public string Name { get; set; }
        public string File { get; set; }
        public int Line { get; set; }
        public string Body { get; set; }
        public string[] Parameters { get; set; }

        public Regex Regex { get; private set; }

        public string[] BodyParts
        {
            get
            {
                return (string[])bodyParts.Clone();
            }
        }

        private string[] bodyParts;

        internal void Prepare()
        {
            bodyParts = Body.Split(separator, StringSplitOptions.None);
            Regex = new Regex(@"\b" + Name + (Parameters.Length > 0 ? string.Format(@"\s*
  {0}                       # Match first opeing delimiter
  (?<inner>
    (?>
        {0} (?<LEVEL>)      # On opening delimiter push level
      | 
        {1} (?<-LEVEL>)     # On closing delimiter pop level
      |
        (?! {0} | {1} ) .   # Match any char unless the opening   
    )+                      # or closing delimiters are in the lookahead string
    (?(LEVEL)(?!))          # If level exists then fail
  )
  {1}                       # Match last closing delimiter
  ", @"\(", @"\)") : @"\b"), RegexOptions.IgnorePatternWhitespace | RegexOptions.Compiled);
        }

        private static readonly string[] separator = new[] { "##" };

    }
}