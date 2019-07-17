using System;
using System.Collections.Generic;
using System.Linq;
using CommandLine;

namespace SLang.NET.Test
{
    public static class Program
    {
        public static int Main(string[] args)
        {
            return Parser.Default.ParseArguments<Options>(args).MapResult(
                RunAndReturnExitCode,
                _ => 2);
        }

        public static int RunAndReturnExitCode(Options options)
        {
            Options.Singleton = options;

            var repo = new TestsRepository(Options.Singleton.TestDirRoot);
            Console.WriteLine($"Test repository root: {repo.BaseDirectory.FullName}");

            var runner = new TestRunner(repo, new HashSet<string>(Options.Singleton.TestCases));
            var reports = runner.RunAll();

            var printer = new ReportPrinter();
            printer.Print(reports);

            var exit_code = reports.Any(r => r.Status == Status.Failed) ? 1 : 0;
            return exit_code;
        }
    }
}