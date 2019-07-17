using System;
using System.Diagnostics;
using System.IO;
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

        public Report Run()
        {
            var report = new Report(this);

            // TODO: check if skipped

            report.Status = Status.Running;

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

            report.ResolveStatus();
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
                var error = e.Message;

                if (meta.Pass)
                {
                    report.ParserPass = false;
                    report.ParserError = error;
                }
                else if (!meta.Pass)
                {
                    if (meta.Error.IsMatch(error))
                    {
                        report.ParserPass = true;
                    }
                    else
                    {
                        report.ParserPass = false;
                        report.ParserError = $@"Error mismatch (expected: {meta.Error.Pattern}, actual: ""{error}"")";
                    }
                }

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
                var error = e.Message;

                if (meta.Pass)
                {
                    report.CompilerPass = false;
                    report.CompilerError = error;
                }
                else if (!meta.Pass)
                {
                    if (meta.Error.IsMatch(error))
                    {
                        report.CompilerPass = true;
                    }
                    else
                    {
                        report.CompilerPass = false;
                        report.CompilerError = $@"Error mismatch (expected: {meta.Error.Pattern}, actual: ""{error}"")";
                    }
                }
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

                if (meta.Pass && pass)
                {
                    report.PeVerifyPass = true;
                }
                else if (meta.Pass && !pass)
                {
                    report.PeVerifyPass = false;
                    report.PeVerifyError = error;
                }
                else if (!meta.Pass && pass)
                {
                    report.PeVerifyPass = false;
                    report.PeVerifyError = "Shouldn't have passed";
                }
                else if (!meta.Pass && !pass)
                {
                    if (meta.Error.IsMatch(error))
                    {
                        report.PeVerifyPass = true;
                    }
                    else
                    {
                        report.PeVerifyPass = false;
                        report.PeVerifyError = $@"Error mismatch (expected: {meta.Error.Pattern}, actual: ""{error}"")";
                    }
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

                var output = process.StandardOutput.ReadToEnd();
                var error = process.StandardError.ReadToEnd();

                if (meta.ExitCode != process.ExitCode)
                {
                    report.RunPass = false;
                    report.RunError = $"Exit code (expected: {meta.ExitCode}, actual: {process.ExitCode})";
                }
                else if (!meta.Output.IsMatch(output))
                {
                    report.RunPass = false;
                    report.RunError = $@"Output mismatch (expected: {meta.Output.Pattern}, actual: ""{output}"")";
                }
                else if (!meta.Error.IsMatch(error))
                {
                    report.RunPass = false;
                    report.RunError = $@"Error mismatch (expected: {meta.Error.Pattern}, actual: ""{error}"")";
                }
                else
                {
                    report.RunPass = true;
                }
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