using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using MoreLinq;
using Newtonsoft.Json;
using SLang.IR;
using SLang.IR.JSON;
using SLang.NET.Gen;
using FormatException = SLang.IR.JSON.FormatException;
using RunProcessAsTask;

namespace SLang.NET.Test
{
    public class TestCase
    {
        public DirectoryInfo BaseDirectory;

        public string Name => BaseDirectory.Name;

        public FileInfo SourceJsonInfo => FileExists(GetFile("source.json"));

        public FileInfo MetaJsonInfo => FileExists(GetFile("meta.json"));

        public FileInfo DllInfo => GetFile($"{Name}.dll");

        public FileInfo DasmInfo => GetFile("assembly.il");

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

            if (Meta.Skip)
            {
                report.Status = Status.Skipped;
            }
            else
            {
                report.Status = Status.Running;
                report.Complete = Complete(report);
            }

            return report;
        }

        private async Task Complete(Report report)
        {
            var ast = await StageParser(report);

            if (report.ParserPass && Meta.Stages.Parser.Pass)
            {
                await StageCompile(report, ast);

                if (report.CompilerPass && Meta.Stages.Compiler.Pass)
                {
                    await StagePeVerify(report);
                    await StageIldasm();

                    if (report.PeVerifyPass && Meta.Stages.PeVerify.Pass)
                    {
                        await StageRun(report);
                    }
                }
            }

            report.ResolveStatus();
        }

        private async Task<Compilation> StageParser(Report report)
        {
            var meta = Meta.Stages.Parser;

            try
            {
                var ir = await StageParserDeserialize();
                var parser = new Parser();
                Compilation root = parser.ParseCompilation(ir);

                report.ParserPass = meta.Pass;
                if (!meta.Pass)
                {
                    report.ParserErrorShort = "Shouldn't have passed.";
                    report.ParserErrorFull = string.Empty;
                }

                return root;
            }
            catch (FormatException e)
            {
                var error = e.Message;
                var errorFull = e.ToString();

                if (meta.Pass)
                {
                    report.ParserPass = false;
                    report.ParserErrorShort = error;
                    report.ParserErrorFull = errorFull;
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
                        report.ParserErrorShort = $@"Error mismatch (expected: {meta.Error.Pattern}, actual: ""{error}"")";
                        report.ParserErrorFull = errorFull;
                    }
                }

                return null;
            }
        }

        private async Task<JsonEntity> StageParserDeserialize()
        {
            using (var inputStream = SourceJsonInfo.OpenText())
            {
                var content = await inputStream.ReadToEndAsync();
                return _serializer.Deserialize<JsonEntity>(new JsonTextReader(new StringReader(content)));
            }
        }

        private async Task StageCompile(Report report, Compilation compilation)
        {
            var meta = Meta.Stages.Compiler;

            try
            {
                var asm = Compiler.CompileToIL(compilation, DllInfo.Name);
                await Task.Run(() => asm.Write(DllInfo.FullName));

                report.CompilerPass = meta.Pass;
                if (!meta.Pass)
                {
                    report.CompilerErrorShort = "Shouldn't have compiled";
                    report.CompilerErrorFull = string.Empty;
                }
            }
            catch (Exception e)
            {
                var error = e.Message;
                var errorFull = e.ToString();

                if (meta.Pass)
                {
                    report.CompilerPass = false;
                    report.CompilerErrorShort = error;
                    report.CompilerErrorFull = errorFull;
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
                        report.CompilerErrorShort = $@"Error mismatch (expected: {meta.Error.Pattern}, actual: ""{error}"")";
                        report.CompilerErrorFull = errorFull;
                    }
                }
            }
        }

        private async Task StagePeVerify(Report report)
        {
            var meta = Meta.Stages.PeVerify;

            using (var process = await ProcessEx.RunAsync(new ProcessStartInfo
            {
                FileName = Options.Singleton.PeVerify,
                ArgumentList = {DllInfo.FullName},
            }))
            {
                var pass = process.ExitCode == 0;
                var error = string.Join("\n", process.StandardOutput);
                // NOT an Environment.NewLine because meta.json should contain only \n

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

        private async Task StageIldasm()
        {
            if (Options.Singleton.SkipIldasm) return;
            using (var process = await ProcessEx.RunAsync(new ProcessStartInfo
            {
                FileName = Options.Singleton.Ildasm,
                ArgumentList = {DllInfo.FullName},
            }))
            {
                using (var output = new StreamWriter(DasmInfo.Open(FileMode.Create, FileAccess.Write)))
                {
                    foreach (var line in process.StandardOutput)
                    {
                        await output.WriteLineAsync(line);
                    }
                }
            }
        }

        private async Task StageRun(Report report)
        {
            var meta = Meta.Stages.Run;
            if (!meta.Run) return;

            await GenerateRunTimeConfig();

            var info = new ProcessStartInfo {FileName = Options.Singleton.Runtime};

            info.ArgumentList.Add(DllInfo.FullName);
            if (Options.Singleton.Runtime.Equals("dotnet"))
                info.ArgumentList.Add("--");
            meta.Args.ForEach(info.ArgumentList.Add);

            var taskCts = new CancellationTokenSource();
            var timerCts = new CancellationTokenSource();

            var task = ProcessEx.RunAsync(info, taskCts.Token);
            var timer = Task.Delay(meta.TimeoutSeconds * 1000, timerCts.Token);

            if (task != await Task.WhenAny(task, timer))
            {
                taskCts.Cancel(); // timer completed, get rid of task

                report.RunPass = false;
                report.RunError = "Timeout";
            }
            else
            {
                timerCts.Cancel(); // task completed, get rid of timer

                using (var process = await task)
                {
                    var output = string.Join("\n", process.StandardOutput);
                    var error = string.Join("\n", process.StandardError);

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
        }

        private async Task GenerateRunTimeConfig()
        {
            if (Options.Singleton.Runtime.Equals("dotnet"))
                using (var file = new StreamWriter(RunTimeConfigJsonInfo.Open(FileMode.Create, FileAccess.Write)))
                {
                    await file.WriteAsync(RunTimeConfigJson);
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