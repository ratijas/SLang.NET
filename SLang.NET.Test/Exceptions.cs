using System;
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

        public override string ToString()
        {
            return $"Directory does not contain a test case: {Directory}";
        }
    }
}