using System;
using System.IO;
using CommandLine;
using Mono.Cecil;
using Newtonsoft.Json;
using SLang.IR;
using SLang.IR.JSON;
using SLang.NET.Gen;
using Parser = SLang.IR.JSON.Parser;

namespace SLang.NET
{
    class Program
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

            var jsonOutputPath = o.Output.ToString() + ".ast.json";
            using (var outputStream =
                new StreamWriter(
                    new BufferedStream(
                        new FileStream(jsonOutputPath, FileMode.Create, FileAccess.Write, FileShare.None))))
            {
                JsonSerializer serializer = new JsonSerializer { Formatting = Formatting.Indented };
                serializer.Serialize(outputStream, root);
            }


            var dllPath = o.Output;
            if (root is Compilation compilation)
            {
                var asm = Compiler.CompileToIL(compilation, dllPath.Name);
                asm.Write(dllPath.ToString());
            }
            else
                throw new JsonFormatException(ir, "Root entity is not COMPILATION type");
        }
    }
}