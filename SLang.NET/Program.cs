using System;
using System.IO;
using CommandLine;
using Newtonsoft.Json;
using SLang.IR;
using SLang.IR.JSON;
using SLang.NET.Gen;
using Parser = CommandLine.Parser;

namespace SLang.NET
{
    class Program
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

            JsonEntity ir;

            using (inputStream)
            {
                using (var reader = new JsonTextReader(inputStream))
                {
                    JsonSerializer serializer = new JsonSerializer();
                    ir = (JsonEntity) serializer.Deserialize(reader, typeof(JsonEntity));
                }
            }

            var parser = new IR.JSON.Parser();
            Entity root = parser.Parse(ir);

            if (o.Ast != null)
                using (var outputStream =
                    new StreamWriter(
                        new BufferedStream(
                            new FileStream(o.Ast.ToString(), FileMode.Create, FileAccess.Write,
                                FileShare.None))))
                {
                    JsonSerializer serializer = new JsonSerializer {Formatting = Formatting.Indented};
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