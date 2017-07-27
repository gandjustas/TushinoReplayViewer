namespace PboTools
{
    public class FileLine
    {

        public FileLine(string file, int lineNumber, string line)
        {
            File = file;
            LineNumber = lineNumber;
            Line = line;
        }

        public string File { get; private set; }
        public int LineNumber { get; private set; }
        public string Line { get; private set; }
    }
}