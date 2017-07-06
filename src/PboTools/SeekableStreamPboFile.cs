using System;
using System.Collections.Generic;
using System.IO;

namespace PboTools
{
    internal class SeekableStreamPboFile : StreamPboFile
    {
        public SeekableStreamPboFile(Stream s) : base(s)
        {
        }

        protected override void ReadHeader()
        {
            base.ReadHeader();
            var offset = s.Position;

            foreach (var current in entries)
            {
                current.SetSource(new PatrialStream(s, offset, current.DataSize));
                offset += current.DataSize;
            }
            reader.Dispose();
        }

        protected override void PrepareEntries()
        {
            //do nothing
        }

        protected override IEnumerable<PboEntry> EnumerateEntriesInternal()
        {
            foreach (var current in entries)
            {
                yield return current;
            }
        }
    }
}