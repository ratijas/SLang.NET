using System;

namespace SLang.NET.Test
{
    public class Report
    {
        public TestCase TestCase;

        public bool ParserPass = true;
        public string ParserError = string.Empty;

        public bool CompilerPass = true;
        public string CompilerError = String.Empty;

        public bool Pass => ParserPass && CompilerPass;

        public Report(TestCase testCase)
        {
            TestCase = testCase;
        }

        public string GetStage()
        {
            if (!ParserPass)
                return "Parser";
            else if (!CompilerPass)
                return "Compiler";
            
            return "Unknown";
        }

        public string GetError()
        {
            if (!ParserPass)
                return ParserError;
            else if (!CompilerPass)
                return CompilerError;

            return "Unknown";
        }
    }
}