using System;
using System.Collections.Generic;
using System.Linq;

namespace SLang.NET.Test
{
    public class TestRunner
    {
        public List<TestCase> TestCases = new List<TestCase>();


        public TestRunner(TestsRepository repository, ISet<string> selected)
        {
            if (selected.Any())
            {
                // add selected
                TestCases.AddRange(repository.GetTestCases().Where(testCase => selected.Contains(testCase.Name)));

                ISet<string> found = new HashSet<string>(TestCases.Select(test => test.Name));
                ISet<string> notFound = new SortedSet<string>(selected.Where(test => !found.Contains(test)));
                if (notFound.Count > 0)
                    throw new TestCasesNotFoundException(notFound);
            }
            else
                TestCases.AddRange(repository.GetTestCases());

            TestCases.Sort(
                (lhs, rhs) => string.Compare(lhs.Name, rhs.Name, StringComparison.InvariantCultureIgnoreCase));
        }

        public IEnumerable<Report> RunAll()
        {
            foreach (var t in TestCases)
            {
                Report report;
                try
                {
                    report = t.Run();
                }
                catch (NotATestCaseException e)
                {
                    Console.WriteLine(e);
                    continue;
                }

                yield return report;
            }
        }
    }
}