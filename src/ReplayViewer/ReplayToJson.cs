using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Tushino;

namespace ReplayViewer
{
    public static class ReplayToJson
    {
        public static void Normalize(TextReader input, TextWriter output)
        {
            using (var writer = new JsonTextWriter(output) { CloseOutput = false })
            {                
                var parser = new ReplayParser(input);

                while (!parser.IsEof)
                {
                    if(parser.IsArray())
                    {
                        writer.WriteStartArray();
                        parser.Down();
                    }
                    else if(!parser.HasMoreElements)
                    {
                        writer.WriteEndArray();
                        parser.Up();
                    }
                    else if (parser.IsNumber())
                    {
                        writer.WriteValue(parser.ReadDouble());
                    }
                    else if (parser.IsString())
                    {
                        writer.WriteValue(parser.ReadString());
                    }
                    else
                    {
                        throw new InvalidOperationException();
                    }
                }
                writer.Flush();
            }
        }
    }
}
