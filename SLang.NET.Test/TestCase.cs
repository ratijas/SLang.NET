using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using MoreLinq;
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

        public FileInfo RunTimeConfigJsonInfo => GetFile($"{Name}.runtimeconfig.json");

        // TODO: let user specify framework
        public string RunTimeConfigJson = @"{
""runtimeOptions"": {
        ""framework"": {
            ""name"": ""Microsoft.NETCore.App"",
            ""version"": ""2.2.0""
        }
    }
}";

        private Meta _meta;
        public Meta Meta => _meta ?? (_meta = Meta.FromFile(MetaJsonInfo));

        private JsonSerializer _serializer = new JsonSerializer();

        public virtual Report Run()
        {
            var report = new Report(this);
            var ast = StageParser(report);

            if (report.ParserPass && Meta.Stages.Parser.Pass)
            {
                StageCompile(report, ast);

                if (report.CompilerPass && Meta.Stages.Compiler.Pass)
                {
                    StagePeVerify(report);

                    if (report.PeVerifyPass && Meta.Stages.PeVerify.Pass)
                    {
                        StageRun(report);
                    }
                }
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
                return _serializer.Deserialize<JsonEntity>(new JsonTextReader(inputStream));
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

        private void StagePeVerify(Report report)
        {
            var meta = Meta.Stages.PeVerify;

            using (var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = Options.Singleton.PeVerify,
                    ArgumentList = {DllInfo.FullName},
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                }
            })
            {
                process.Start();
                process.WaitForExit();

                var pass = process.ExitCode == 0;
                var error = process.StandardOutput.ReadToEnd();

                if (pass)
                {
                    report.PeVerifyPass = meta.Pass;
                    if (!meta.Pass)
                        report.PeVerifyError = "Shouldn't have passed";
                }
                else
                {
                    report.PeVerifyError = error;
                    // TODO: more flexible error matching
                    report.PeVerifyPass = !meta.Pass && report.PeVerifyError.Equals(meta.Error);
                }
            }
        }

        private void StageRun(Report report)
        {
            var meta = Meta.Stages.Run;
            if (!meta.Run) return;

            GenerateRunTimeConfig();

            var info = new ProcessStartInfo
            {
                FileName = Options.Singleton.Runtime,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };
            info.ArgumentList.Add(DllInfo.FullName);
            if (Options.Singleton.Runtime.Equals("dotnet"))
                info.ArgumentList.Add("--");
            meta.Args.ForEach(info.ArgumentList.Add);

            using (var process = new Process {StartInfo = info})
            {
                process.Start();
                var exited = process.WaitForExit(meta.TimeoutSeconds * 1000);

                if (!exited)
                {
                    try
                    {
                        process.Kill();
                    }
                    catch (InvalidOperationException)
                    {
                        // don't care
                    }

                    report.RunPass = false;
                    report.RunError = "Timeout";
                    return;
                }

                var stdout = process.StandardOutput.ReadToEnd();
                var stderr = process.StandardError.ReadToEnd();

                if (meta.ExitCode != process.ExitCode)
                {
                    report.RunPass = false;
                    report.RunError = $"Exit code (expected: {meta.ExitCode}, actual: {process.ExitCode})";
                    return;
                }

                if (meta.Output != stdout)
                {
                    report.RunPass = false;
                    report.RunError = $"Standard output mismatch (expected: \"{meta.Output}\", actual: \"{stdout}\")";
                    return;
                }

                if (meta.Error != stderr)
                {
                    report.RunPass = false;
                    report.RunError = $"Standard error mismatch (expected: \"{meta.Error}\", actual: \"{stderr}\")";
                    return;
                }

                report.RunPass = true;
            }
        }

        private void GenerateRunTimeConfig()
        {
            if (Options.Singleton.Runtime.Equals("dotnet"))
                using (var file = new StreamWriter(RunTimeConfigJsonInfo.Open(FileMode.Create, FileAccess.Write)))
                {
                    file.Write(RunTimeConfigJson);
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