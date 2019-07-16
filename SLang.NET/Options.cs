using System.IO;
using CommandLine;

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

        private FileInfo _output;
        
        [Option(
            'o',
            "output",
            Required = false,
            HelpText =
                "The path to the output *.dll assembly. " +
                "The default is input with `.json` extension replaced by `.dll`.")]
        public FileInfo Output
        {
            get => _output ?? new FileInfo(Path.ChangeExtension(Input.ToString(), ".dll"));
            set => _output = value;
        }

        [Option(
            "ast",
            Required = false,
            HelpText = "Dump internal AST in JSON format to the specified file.")]
        public FileInfo Ast { get; set; }
    }
}