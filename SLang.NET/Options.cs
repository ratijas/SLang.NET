using System;
using System.IO;
using CommandLine;
using CommandLine.Text;

namespace SLang.NET
{
    public class Options
    {
        public string GetHelp()
        {
            return "Compile SLang JSON IR into .NET Core assembly.";
        }

        [Option(
            'v',
            "verbose",
            Required = false,
            HelpText = "Set output to verbose messages.")]
        public bool Verbose { get; set; }

        [Option(
            'i',
            "input",
            Required = false,
            HelpText = "The path to the SLang JSON IR file that is to be compiled. The default in standard input."
        )]
        public FileInfo Input { get; set; }

        [Option(
            'o',
            "output",
            Required = false,
            HelpText =
                "The path to the output *.dll assembly. The default is out.dll in the current working directory.")]
        public FileInfo Output { get; set; } = new FileInfo("out.dll");
    }
}