using PboTools;
using System;
using System.IO;
using System.Linq;

namespace PboToolsTest
{
    class Program
    {
        static void Main(string[] args)
        {
            if(args.Length > 0)
            {
                foreach (var d in Directory.EnumerateDirectories(args[0]))
                {
                    var file = Path.Combine(d, "config.cpp");
                    if (File.Exists(file))
                    {
                        Console.WriteLine(file);
                        using (var f = File.OpenText(file))
                        {
                            var lines = Preprocessor.Prepocess(file, f,
                                        (x, path) =>
                                        {
                                            var p = path.StartsWith(@"\")
                                                   ? path
                                                   : Path.Combine(Path.GetDirectoryName(x), path);
                                            return Tuple.Create(p, (TextReader)File.OpenText(p));
                                        });//.ToList();
                            var t = new Tokenizer(lines);
                            t.Parse().ToList();
                            //foreach (var token in t.Parse())
                            //{
                            //    Console.WriteLine(token);
                            //}
                        }
                    }
                }
            }
        }
    }
}