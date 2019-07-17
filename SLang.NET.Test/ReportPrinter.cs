using System;
using System.Collections.Generic;
using System.IO;
using MoreLinq.Extensions;

namespace SLang.NET.Test
{
    public class ReportPrinter
    {
        public TextWriter Out;
        private Summary summary = new Summary();

        public ReportPrinter(TextWriter writer)
        {
            Out = writer;
        }

        public ReportPrinter()
        {
            Out = Console.Out;
        }

        public void Print(ICollection<Report> reports)
        {
            Out.WriteLine($"Running {reports.Count} test cases");
            Hr();
            reports.ForEach(Print);
            reports.ForEach(summary.Add);
            Hr();
            PrintSummary();
            Out.WriteLine("End of report");
        }

        public void Print(Report report)
        {
            var status = report.Status.ToString();
            var separator = new string('-', 58 - report.TestCase.Name.Length - status.Length);
            string template = $"{report.TestCase.Name} {separator} {status}";
            Out.WriteLine(template);
            if (report.Status == Status.Failed)
                PrintDetails(report);
        }

        private void PrintDetails(Report report)
        {
            var stage = report.GetStage();
            var error = report.GetError();
            Tab(); Out.WriteLine($"Stage: {stage}");
            Tab(); Out.WriteLine($"Error: {error}");
        }

        private void PrintSummary()
        {
            Out.WriteLine("Summary");
            Tab(); Out.WriteLine($"Passed:     {summary.Passed}");
            Tab(); Out.WriteLine($"Failed:     {summary.Failed}");
            Tab(); Out.WriteLine($"Skipped:    {summary.Skipped}");
            Tab(); Out.WriteLine($"===============");
            Tab(); Out.WriteLine($"Total:      {summary.Total}");
        }

        private void Tab()
        {
            Out.Write("\t");
        }

        private void Hr()
        {
            Out.WriteLine(new string('=', 60));
        }

        private class Summary
        {
            public int Passed;
            public int Failed;
            public int Skipped;
            public int Total;

            public void Add(Report report)
            {
                Total += 1;
                switch (report.Status)
                {
                    case Status.Passed:
                        Passed += 1;
                        break;
                    case Status.Failed:
                        Failed += 1;
                        break;
                    case Status.Skipped:
                        Skipped += 1;
                        break;
                }
            }
        }
    }
}