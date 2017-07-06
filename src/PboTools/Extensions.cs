using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PboTools
{
    public static class Extensions
    {
        public static string ReadStringZ(this BinaryReader reader)
        {
            var stringBuilder = new StringBuilder();
            char value;
            while ((value = (char)reader.ReadByte()) != '\0')
            {
                stringBuilder.Append(value);
            }
            return stringBuilder.ToString();
        }

        public static Stream OpenFile(this PboFile p, string name)
        {
            var f = p.Entries.FirstOrDefault(e => e.Path == name);
            if (f == null) return null;
            else return f.ToStream();
        }

    }
}
