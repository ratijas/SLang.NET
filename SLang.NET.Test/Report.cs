using System.Threading.Tasks;

namespace SLang.NET.Test
{
    public class Report
    {
        //                |   should pass   |    should NOT pass
        // +--------------+-----------------+------------------------+
        //  did     pass  |      OK         |  Shouldn't have passed
        //  did NOT pass  |     error       |      match error

        //                       | should NOT pass && did NOT pass
        // +---------------------+---------------------------------+
        //  error matches        |                OK
        //  error does NOT match |          Error mismatch

        public TestCase TestCase;
        public Status Status = Status.Invalid;

        public bool ParserPass = true;
        public string ParserErrorShort = string.Empty;
        public string ParserErrorFull = string.Empty;

        public bool CompilerPass = true;
        public string CompilerErrorShort = string.Empty;
        public string CompilerErrorFull = string.Empty;

        public bool PeVerifyPass = true;
        public string PeVerifyError = string.Empty;
        // no such thing as PeVerifyError{Short,Full}

        public bool RunPass = true;
        public string RunError = string.Empty;
        // no such thing as RunError{Short,Full}

        public Task Complete = Task.CompletedTask;

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
            else if (!RunPass)
                return "Run";

            return "Unknown";
        }

        public string GetErrorShort()
        {
            if (!ParserPass)
                return ParserErrorShort;
            else if (!CompilerPass)
                return CompilerErrorShort;
            else if (!PeVerifyPass)
                return PeVerifyError;
            else if (!RunPass)
                return RunError;

            return "Unknown";
        }

        public string GetErrorFull()
        {
            if (!ParserPass)
                return ParserErrorFull;
            else if (!CompilerPass)
                return CompilerErrorFull;
            else if (!PeVerifyPass)
                return string.Empty;
            else if (!RunPass)
                return string.Empty;

            return "Unknown";
        }

        /// <summary>
        /// Indicate that report is finished and status shall be set to either 'passed' or 'failed'.
        /// </summary>
        public void ResolveStatus()
        {
            if (ParserPass && CompilerPass && PeVerifyPass && RunPass)
                Status = Status.Passed;
            else
                Status = Status.Failed;
        }
    }
}