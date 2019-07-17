using System;
using System.Collections.Generic;

namespace SLang.NET.Test
{
    public class TestRunner
    {
        public List<TestCase> TestCases = new List<TestCase>();


        public TestRunner(TestsRepository repository)
        {
            TestCases.AddRange(repository.GetTestCases());
            TestCases.Sort(
                (lhs, rhs) => string.Compare(lhs.Name, rhs.Name, StringComparison.InvariantCultureIgnoreCase));
        }

        public ICollection<Report> RunAll()
        {
            var reports = new List<Report>(TestCases.Count);

            TestCases.ForEach(t =>
            {
                try
                {
                    var report = t.Run();
                    reports.Add(report);
                }
                catch (NotATestCaseException e)
                {
                    Console.WriteLine(e);
                }
            });

            return reports;
        }
    }
}