using System;
using System.IO;
using CommandLine;
using Newtonsoft.Json;
using System.Collections.Generic;
using SLang.IR;
using SLang.IR.JSON;
using Parser = SLang.IR.JSON.Parser;

namespace SLang.NET
{
    partial class Program
    {
        static void Main(params string[] args)
        {
            new CommandLine.Parser(settings =>
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

            JsonEntity ir;

            using (inputStream)
            {
                using (var reader = new JsonTextReader(inputStream))
                {
                    JsonSerializer serializer = new JsonSerializer();
                    ir = (JsonEntity) serializer.Deserialize(reader, typeof(JsonEntity));
                }
            }

            var parser = new Parser();
            Entity root = parser.Parse(ir);

            using (var outputStream =
                new StreamWriter(
                    new BufferedStream(
                        new FileStream(o.Output.ToString(), FileMode.Create, FileAccess.Write, FileShare.None))))
            {
                JsonSerializer serializer = new JsonSerializer { Formatting = Formatting.Indented };
                serializer.Serialize(outputStream, root);
            }
        }
    }
}