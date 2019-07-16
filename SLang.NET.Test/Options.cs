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
    }
}