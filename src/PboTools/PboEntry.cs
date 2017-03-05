using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace PboTools
{
    public class PboEntry
    {
        List<string> extensions = new List<string>();

        public string Path { get; private set; }

        public uint PackingMethod { get; private set; }

        public uint OriginalSize { get; private set; }

        public uint Reserved { get; private set; }

        public uint Timestamp { get; private set; }

        public int DataSize { get; private set; }

        public byte[] FileContents { get; private set; }

        public IEnumerable<string> Extensions
        {
            get
            {
                return extensions;
            }
        }


        internal void ReadHeader(BinaryReader reader)
        {
            this.Path = reader.ReadStringZ();
            this.PackingMethod = reader.ReadUInt32();
            this.OriginalSize = reader.ReadUInt32();
            this.Reserved = reader.ReadUInt32();
            this.Timestamp = reader.ReadUInt32();
            this.DataSize = (int)reader.ReadUInt32();
        }

        internal void ReadExtensions(BinaryReader reader)
        {
            string s;
            while ((s = reader.ReadStringZ()).Length != 0)
            {
                extensions.Add(s);
            }
        }
        internal void ReadBodySeq(BinaryReader r)
        {
            if (PackingMethod == 0x43707273 && OriginalSize != DataSize)
            {
                FileContents = Unpack(r, OriginalSize);
            }
            else
            {
                FileContents = r.ReadBytes(DataSize);

            }

        }

        internal void WriteHeader(BinaryWriter writer)
        {
            writer.Write(Encoding.ASCII.GetBytes(this.Path)); writer.Write((byte)0);
            writer.Write(this.PackingMethod);
            writer.Write(this.OriginalSize);
            writer.Write(this.Reserved);
            writer.Write(this.Timestamp);
            writer.Write((uint)this.DataSize);
        }

        internal void WriteBody(Stream sr)
        {
            sr.Write(this.FileContents, 0, this.DataSize);
        }


        internal static int PathSorter(PboEntry e1, PboEntry e2)
        {
            string text = e1.Path;
            string text2 = e2.Path;
            text = text.ToUpperInvariant();
            text2 = text2.ToUpperInvariant();
            int num = Math.Min(text.Length, text2.Length);
            int i = 0;
            while (i < num)
            {
                char c = text[i];
                char c2 = text2[i];
                if (c != c2)
                {
                    if (c == '_')
                    {
                        return 1;
                    }
                    if (c2 == '_')
                    {
                        return -1;
                    }
                    return (int)(c - c2);
                }
                else
                {
                    i++;
                }
            }
            if (text.Length < text2.Length)
            {
                return -1;
            }
            if (text.Length > text2.Length)
            {
                return 1;
            }
            return 0;
        }

        public Stream ToStream()
        {
            return new MemoryStream(this.FileContents);
        }

        private static byte[] Unpack(BinaryReader reader, uint originalSize)
        {
            var fl = 0;
            int i = 0;
            var result = new byte[originalSize];
            while (fl < originalSize)
            {
                var b = reader.ReadByte();
                for (int k = 0; k < 8 && fl < originalSize; k++)
                {
                    var bit = b & 1;
                    b >>= 1;
                    if (bit == 1)
                    {
                        result[fl++] = reader.ReadByte();
                    }
                    else
                    {
                        var pointer = reader.ReadUInt16();
                        var rpos = fl - ((pointer & 0x00FF) + ((pointer & 0xF000) >> 4));
                        var rlen = ((pointer & 0x0F00) >> 8) + 3;
                        if (rpos < 0)
                        {
                            for (int j = 0; j < rlen; j++)
                            {
                                result[fl++] = (byte)' ';
                            }
                        }
                        else if (rpos + rlen <= fl)
                        {
                            Array.Copy(result, rpos, result, fl, rlen);
                            fl += rlen;
                        }
                        else
                        {
                            for (int j = 0; j < rlen; j++)
                            {
                                result[fl++] = result[rpos + j];
                            }
                        }
                        i += 2;

                    }

                }
            }
            //checksum
            reader.ReadUInt32();
            return result;
        }

    }
}
