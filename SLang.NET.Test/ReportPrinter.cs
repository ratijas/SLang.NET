using System;
using System.Collections.Generic;
using System.IO;
using MoreLinq.Extensions;

namespace SLang.NET.Test
{
    public class ReportPrinter
    {
        public TextWriter Out;

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
            Hr();
            Out.WriteLine("End of report");
        }

        public void Print(Report report)
        {
            var status = report.Pass ? "Passed" : "Failed";
            var separator = new string('-', 52 - report.TestCase.Name.Length);
            string template = $"{report.TestCase.Name} {separator} {status}";
            Out.WriteLine(template);
            if (!report.Pass)
                PrintDetails(report);
        }

        private void PrintDetails(Report report)
        {
            var stage = report.GetStage();
            var error = report.GetError();
            Tab(); Out.WriteLine($"Stage: {stage}");
            Tab(); Out.WriteLine($"Error: {error}");
        }

        private void Tab()
        {
            Out.Write("\t");
        }

        private void Hr()
        {
            Out.WriteLine(new string('=', 60));
        }
    }
}