using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace SLang.NET.Test
{
    public class TestsRepository
    {
        public const string DefaultTestsDirectoryName = "tests";

        public TestsRepository(DirectoryInfo baseDirectory = null)
        {
            if (baseDirectory == null)
                baseDirectory = LookupInParents();
            // if STILL null...
            if (baseDirectory == null)
                throw new Exception("Tests repository not found. Try using --testRootDir option.");
            BaseDirectory = baseDirectory;
        }

        public DirectoryInfo BaseDirectory;

        public IEnumerable<TestCase> GetTestCases()
        {
            return BaseDirectory.GetDirectories().Select(info => new TestCase {BaseDirectory = info});
        }

        public static DirectoryInfo LookupInParents(string name = DefaultTestsDirectoryName)
        {
            var dir = Directory.GetCurrentDirectory();

            while (true)
            {
                var target = Path.Join(dir, name);

                if (Directory.Exists(target))
                    return new DirectoryInfo(target);

                var parent = Directory.GetParent(dir);
                if (parent == null)
                    return null;
                dir = parent.FullName;
            }
        }
    }
}