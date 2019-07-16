using System;
using System.IO;
using CommandLine;
using Newtonsoft.Json;

namespace SLang.NET.Test
{
    class Program
    {
        static void Main(string[] args)
        {
            Parser.Default.ParseArguments<Options>(args).WithParsed(options => { 
                var repo = new TestsRepository(options.TestDirRoot);
                Console.WriteLine($"Test repository root: {repo.BaseDirectory.FullName}");

                var runner = new TestRunner(repo);
                var reports = runner.RunAll();

                var printer = new ReportPrinter();
                printer.Print(reports);
            });
        }
    }
}