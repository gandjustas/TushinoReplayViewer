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
    public abstract class PboFile
    {
        private bool isInitialized = false;
        protected List<string> extensions;
        protected List<PboEntry> entries;

        public ICollection<string> Extensions
        {
            get
            {
                EnsureHeader();
                return extensions.AsReadOnly();
            }
        }
        public ICollection<PboEntry> Entries
        {
            get
            {
                EnsureHeader();
                PrepareEntries();
                return entries;
            }
        }

        protected abstract void PrepareEntries();

        public IEnumerable<PboEntry> EnumerateEntries()
        {
            EnsureHeader();
            return EnumerateEntriesInternal();
        }

        private void EnsureHeader()
        {
            if (!isInitialized)
            {
                entries = new List<PboEntry>();
                extensions = new List<string>();

                ReadHeader();
                isInitialized = true;
            }
        }

        protected abstract void ReadHeader();
        protected abstract IEnumerable<PboEntry> EnumerateEntriesInternal();


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
                    while (reader.ReadStringZ().Length != 0)
                    {
                    }
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

        public static PboFile FromStream(Stream s)
        {
            return s.CanSeek ? new SeekableStreamPboFile(s) : new StreamPboFile(s);
        }
        public static PboFile FromFolder(DirectoryInfo dir)
        {
            return new FolderPboFile(dir);
        }

    }
}
