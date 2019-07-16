using System.IO;
using CommandLine;

namespace SLang.NET.Test
{
    public class Options
    {
        [Option(
            "testDirRoot",
            Required = false,
            HelpText = "Path to directory with test cases")]
        public DirectoryInfo TestDirRoot { get; set; }
    }
}