using System;
using System.Linq;
using System.Collections.Generic;
using System.IO;

namespace PboTools
{
    internal class FolderPboFile : PboFile
    {
        private DirectoryInfo dir;
        private string prefix;

        public FolderPboFile(DirectoryInfo dir, string prefix = "")
        {
            this.dir = dir;
            this.prefix = prefix;
        }

        protected override IEnumerable<PboEntry> EnumerateEntriesInternal()
        {
            return entries.AsReadOnly();
        }

        protected override void PrepareEntries()
        {
            
        }

        protected override void ReadHeader()
        {
            var d = dir;
            ReadPboPrefix();
            if(prefix == "")
            {
                var q = from x in extensions
                        let p = x.Split('=')
                        where p.Length == 2
                        where p[0] == "prefix"
                        select p[1];
                prefix = q.FirstOrDefault() ?? prefix;
            }
            ReadDirectoryStructure(d, prefix);
        }

        private void ReadPboPrefix()
        {
            foreach (var f in dir.EnumerateFiles("$PBOPREFIX$*").Take(1))
            {
                using (var r = f.OpenText())
                {
                    while (!r.EndOfStream)
                    {
                        var l = r.ReadLine();
                        if (!l.StartsWith("//"))
                        {
                            extensions.Add(l);
                        }
                    }
                }
            }
        }

        private void ReadDirectoryStructure(DirectoryInfo d, string path)
        {
            foreach (var f in d.EnumerateFiles())
            {
                if (f.Name == "$PBOPREFIX$.txt" || f.Name == "$PBOPREFIX$")
                {
                    continue;
                }
                else
                {
                    var e = new PboEntry();
                    e.FromFile(f, path);
                    entries.Add(e);
                }
            }
            foreach (var sd in d.EnumerateDirectories())
            {
                ReadDirectoryStructure(sd, path + sd.Name + "\\");
            }
        }
    }
}