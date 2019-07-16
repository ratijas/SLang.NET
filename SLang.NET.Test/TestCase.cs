using System;
using System.IO;
using Newtonsoft.Json;
using SLang.IR;
using SLang.IR.JSON;
using SLang.NET.Gen;
using FormatException = SLang.IR.JSON.FormatException;

namespace SLang.NET.Test
{
    public class TestCase
    {
        public DirectoryInfo BaseDirectory;

        public string Name => BaseDirectory.Name;

        public FileInfo SourceJsonInfo => FileExists(GetFile("source.json"));

        public FileInfo MetaJsonInfo => FileExists(GetFile("meta.json"));

        public FileInfo DllInfo => GetFile($"{Name}.dll");

        private Meta _meta;
        public Meta Meta => _meta ?? (_meta = Meta.FromFile(MetaJsonInfo));

        private JsonSerializer Serializer = new JsonSerializer();
        
        public virtual Report Run()
        {
            var report = new Report(this);
            var ast = StageParser(report);
            if (report.ParserPass && Meta.Stages.Parser.Pass)
            {
                StageCompile(report, ast);
            }

            return report;
        }

        private Compilation StageParser(Report report)
        {
            var meta = Meta.Stages.Parser;

            try
            {
                var ir = StageParserDeserialize();
                var parser = new Parser();
                Compilation root = parser.ParseCompilation(ir);

                report.ParserPass = meta.Pass;
                if (!meta.Pass)
                    report.ParserError = "Shouldn't have passed.";

                return root;
            }
            catch (FormatException e)
            {
                report.ParserError = e.Message;
                // TODO: more flexible error matching
                report.ParserPass = !meta.Pass && report.ParserError.Equals(meta.Error);

                return null;
            }
        }

        private JsonEntity StageParserDeserialize()
        {
            using (var inputStream = SourceJsonInfo.OpenText())
            {
                return Serializer.Deserialize<JsonEntity>(new JsonTextReader(inputStream));
            }
        }

        private void StageCompile(Report report, Compilation compilation)
        {
            var meta = Meta.Stages.Compiler;
            
            try
            {
                var asm = Compiler.CompileToIL(compilation, DllInfo.Name);
                asm.Write(DllInfo.FullName);

                report.CompilerPass = meta.Pass;
                if (!meta.Pass)
                    report.CompilerError = "Shouldn't have compiled";
            }
            catch (Exception e)
            {
                report.CompilerError = e.Message;
                // TODO: more flexible error matching
                report.CompilerPass = !meta.Pass && report.CompilerError.Equals(meta.Error);
            }
        }

        private FileInfo GetFile(string name)
        {
            return new FileInfo(Path.Join(BaseDirectory.FullName, name));
        }

        private FileInfo FileExists(FileInfo file)
        {
            if (!File.Exists(file.FullName))
                throw new NotATestCaseException(BaseDirectory);
            return file;
        }
    }
}