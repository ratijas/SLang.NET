using System.Collections.Generic;
using System.IO;
using CommandLine;

namespace SLang.NET.Test
{
    public class Options
    {
        public static Options Singleton;

        [Option(
            "testDirRoot",
            Required = false,
            HelpText = "Path to directory with test cases.")]
        public DirectoryInfo TestDirRoot { get; set; }

        [Option(
            "peverify",
            Required = false,
            Default = "peverify",
            HelpText = "Path to peverify utility.")]
        public string PeVerify { get; set; }

        [Option(
            "runtime",
            Required = false,
            Default = "dotnet",
            HelpText = "Run generated *.dll with given runtime, like dotnet, mono or wine.")]
        public string Runtime { get; set; }

        [Option("skip-ildasm",
            Required = false,
            Default = false,
            HelpText = "Don't run ildasm step.")]
        public bool SkipIldasm { get; set; }

        [Option(
            "ildasm",
            Required = false,
            Default = "ildasm",
            HelpText = "Path to ildasm utility, the IL DisAsSeMbler.")]
        public string Ildasm { get; set; }

        [Value(0,
            Required = false,
            HelpText = "Run only selected test cases.")]
        public IEnumerable<string> TestCases { get; set; }
    }
}