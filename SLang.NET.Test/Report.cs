namespace SLang.NET.Test
{
    public class Report
    {
        public TestCase TestCase;

        public bool ParserPass = true;
        public string ParserError = string.Empty;

        public bool CompilerPass = true;
        public string CompilerError = string.Empty;

        public bool PeVerifyPass = true;
        public string PeVerifyError = string.Empty;

        public bool Pass => ParserPass && CompilerPass && PeVerifyPass;

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
            else if (!PeVerifyPass)
                return "PeVerify";

            return "Unknown";
        }

        public string GetError()
        {
            if (!ParserPass)
                return ParserError;
            else if (!CompilerPass)
                return CompilerError;
            else if (!PeVerifyPass)
                return PeVerifyError;

            return "Unknown";
        }
    }
}