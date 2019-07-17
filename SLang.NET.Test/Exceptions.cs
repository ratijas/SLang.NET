using System;
using System.Collections.Generic;
using System.IO;

namespace SLang.NET.Test
{
    public class Exceptions : Exception
    {
    }

    public class NotATestCaseException : Exceptions
    {
        public DirectoryInfo Directory;

        public NotATestCaseException(DirectoryInfo directory)
        {
            Directory = directory;
        }

        public override string Message =>
            $"Directory does not contain a test case: {Directory}";
    }

    public class TestCasesNotFoundException : Exception
    {
        public ISet<string> TestCases;

        public TestCasesNotFoundException(ISet<string> testCases)
        {
            if (testCases.Count == 0)
                throw new ArgumentNullException(nameof(testCases));
            TestCases = testCases;
        }

        public override string Message =>
            $@"Test cases not found: [{string.Join(", ", TestCases)}].";
    }
}