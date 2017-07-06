using System;
using System.Collections.Generic;
using System.IO;

namespace PboTools
{
    internal class StreamPboFile : PboFile
    {
        protected Stream s;
        protected BinaryReader reader;
        private List<PboEntry> readSections;

        public StreamPboFile(Stream s)
        {
            this.s = s;
        }

        protected override void ReadHeader()
        {
            reader = new BinaryReader(s, System.Text.Encoding.ASCII, true);
            ReadPboHeader(reader);
        }

        protected override void PrepareEntries()
        {
            if (readSections == null)
            {
                readSections = new List<PboEntry>(entries.Count);
            }
            foreach (var current in entries)
            {
                if (!readSections.Contains(current))
                {
                    current.ReadBodySeq(reader);
                    readSections.Add(current);
                }
            }
            reader.Dispose();
        }

        protected override IEnumerable<PboEntry> EnumerateEntriesInternal()
        {
            if(readSections == null)
            {
                readSections = new List<PboEntry>(entries.Count);
            }

            foreach (var current in readSections)
            {
                yield return current;
            }
            foreach (var current in entries)
            {
                if (!readSections.Contains(current))
                {
                    current.ReadBodySeq(reader);
                    readSections.Add(current);
                    yield return current;
                }
            }
            reader.Dispose();
        }

        protected void ReadPboHeader(BinaryReader reader)
        {
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
                    string s;
                    while ((s = reader.ReadStringZ()).Length != 0)
                    {
                        extensions.Add(s);
                    }
                }
                else
                {
                    entries.Add(pboEntry);
                }
            }
        }

    }
}