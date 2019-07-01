using System;
using System.IO;
using CommandLine;
using Newtonsoft.Json;
using System.Collections.Generic;

namespace SLang.NET
{
    partial class Program
    {
        static void Main(params string[] args)
        {
            new Parser(settings =>
                {
                    settings.IgnoreUnknownArguments = false;
                    settings.AutoHelp = false;
                })
                .ParseArguments<Options>(args)
                .WithParsed(new Program().EntryPoint);
        }

        public void EntryPoint(Options o)
        {
            TextReader inputStream = o.Input == null
                ? Console.In
                : new StreamReader(
                    new BufferedStream(
                        new FileStream(o.Input.ToString(), FileMode.Open, FileAccess.Read, FileShare.Read)));

            JsonIr ir;

            using (inputStream)
            {
                using (var reader = new JsonTextReader(inputStream))
                {
                    JsonSerializer serializer = new JsonSerializer();
                    ir = (JsonIr) serializer.Deserialize(reader, typeof(JsonIr));
                }
            }

            Console.WriteLine(ir);

            using (var outputStream =
                new StreamWriter(
                    new BufferedStream(
                        new FileStream(o.Output.ToString(), FileMode.Create, FileAccess.Write, FileShare.None))))
            {
                JsonSerializer serializer = new JsonSerializer();
                serializer.Serialize(outputStream, ir);
            }
        }
    }

    public class JsonIr
    {
        public string type { get; set; }
        public List<JsonIr> children { get; set; }
        public string value { get; set; }
    }
}