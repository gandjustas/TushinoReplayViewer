using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace PboTools
{
    [SuppressMessage("Microsoft.Naming", "CA1710:IdentifiersShouldHaveCorrectSuffix")]
    public static class PboFile
    {
        public static IEnumerable<PboEntry> EnumerateEntries(Stream s)
        {
            var reader = new BinaryReader(s, System.Text.Encoding.ASCII, true);
            var entries = new List<PboEntry>();
            while (true)
            {
                var pboEntry = new PboEntry();
                pboEntry.ReadHeader(reader);
                if (pboEntry.Path.Length == 0)
                {
                    if (entries.Count != 0)
                    {
                        break;
                    }
                    pboEntry.ReadExtensions(reader);
                }
                else
                {
                    entries.Add(pboEntry);
                }

            }
            foreach (var current in entries)
            {
                current.ReadBodySeq(reader);
                yield return current;
            }
        }

    }
}
