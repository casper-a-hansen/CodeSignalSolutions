﻿using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;

namespace CodeSignalScraper
{
    public static class Source
    {
        public static readonly string SourcePath = @"..\..\..\..\CodeSignalSolutions";

        public static readonly string ClassFileName = @"<area>\<topic>\<task>Class.cs";
        public static readonly string ClassContent = @"/*
    Status:   <solved>
    Imported: <date>
    By:       <username>
    Url:      <taskUrl>

    Description:
        <description>
*/
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace CodeSignalSolutions.<area>.<topic>
{
    class <task>Class
    {
        <source>
    }
}
";
        public static readonly string TestFileName = @"<area>\<topic>\<topic>.cs";
        public static readonly string TestContent = @"/*
    Imported:   <date>
    Created By: <username>
    Url:        <areaUrl>
*/
using NUnit.Framework;
using CodeSignalSolutions;
using CodeSignalSolutions.<area>.<topic>;

namespace <area>
{
    public class <topic>
    {
    }
}
";
        public static readonly string TestFunction = @"    /*
        Imported: <date>
        By:       <username>
        Url:      <taskUrl>
    */
    [Test]
    public void <task>()
    {
        Test.Execute(typeof(<task>Class),
            <tests>
        );
    }
";

        private static Regex regexStatus = new Regex(@"^\s*Status\s*:\s*((?:Uns|S)olved)\s*$", RegexOptions.Compiled | RegexOptions.Multiline);
        public static IEnumerable<TaskInfo> FilterTasks(IEnumerable<TaskInfo> tasks)
        {
            foreach(var task in tasks)
            {
                var sourcePath = Path.Combine(SourcePath, ReplaceText(ClassFileName, task));
                if (File.Exists(sourcePath))
                {
                    // The task is not solved yet so no need to update source file.
                    if (!task.Solved) continue;

                    // Check if source contains the Solved source.
                    var source = File.ReadAllText(sourcePath);
                    var match = regexStatus.Match(source);
                    if (match.Success && match.Groups[1].Value == "Solved") continue;
                }
                yield return task;
            }
        }

        private static Regex regexParameter = new Regex(@"<(\w+)>", RegexOptions.Compiled);
        public static string ReplaceText(string source, TaskInfo task)
        {
            return regexParameter.Replace(source, match => match.Groups[1].Value.ToLower() switch { 
                "date" => DateTime.Now.ToString("yyyy-MM-dd HH:mm"),
                "username" => Environment.UserName,
                "taskurl" => task.TaskUrl,
                "task" => Fix(task.Task, true),
                "area" => Fix(task.Area),
                "areaurl" => task.AreaUrl,
                "topic" => Fix(task.Topic),
                "solved" => task.Solved ? "Solved" : "Unsolved",
                "description" => Indent(GetIndent(source, match.Index), task.Description.Trim()),
                "source" => Indent(GetIndent(source, match.Index), task.Source.Trim()),
                "tests" => Tests(GetIndent(source, match.Index), task.Tests),
                _ => $"{match.Groups[1].Value} was not found",
            } );
        }

        private static Regex regexCase = new Regex("\b[a-z]", RegexOptions.Compiled);
        private static Regex regexCamel = new Regex("^[A-Z]", RegexOptions.Compiled);
        private static Regex regexFix = new Regex("[^a-z0-9]", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        private static string Fix(string name, bool camelCase = false)
        {
            name = regexCase.Replace(name, match => match.Value.ToUpper());
            if (camelCase)
            {
                name = regexCase.Replace(name, match => match.Value.ToLower());
            }
            return regexFix.Replace(name, match => "");
        }
        public static int GetIndent(string source, int index)
        {
            for(var i = index - 1; i >= 0; i--)
            {
                if (source[i] == '\n') return index - i - 1;
            }
            return index;
        }
        public static string Indent(int indent, string text, bool firstLine = false)
        {
            if (!text.Contains("\r\n")) text = text.Replace("\n", "\r\n");

            StringBuilder result = new StringBuilder();
            var indentStr = firstLine ? new string(' ', indent) : "";
            int start = 0;
            while(start < text.Length)
            {
                var end = text.IndexOf('\n', start);
                if (end == -1) end = text.Length - 1;
                result.Append(indentStr);
                if (indentStr.Length == 0) indentStr = new string(' ', indent);
                result.Append(text, start, end - start + 1);
                start = end + 1;
            }
            return result.ToString();
        }
        public static string Tests(int indent, List<string> tests)
        {
            if (tests == null || tests.Count == 0) return "";
            bool comma = false;
            StringBuilder result = new StringBuilder();
            foreach (var test in tests)
            {
                if (comma)
                {
                    result.AppendLine(",");
                }
                result.Append(Indent(indent, "@\"", comma));
                result.Append(Indent(indent, test.Replace("\"", "\"\"").Trim(), true));
                result.Append("\"");
                comma = true;
            }
            result.AppendLine();
            return result.ToString();
        }

        public static void WriteTask(TaskInfo task)
        {
            Console.WriteLine($"Writing {task.Task} of {task.Topic}.");
            // Writing source file
            var sourcePath = Path.Combine(SourcePath, ReplaceText(ClassFileName, task));
            Directory.CreateDirectory(Path.GetDirectoryName(sourcePath));
            var content = ReplaceText(ClassContent, task);
            File.WriteAllText(sourcePath, content);

            // Writing test file.
            var testPath = Path.Combine(SourcePath, ReplaceText(TestFileName, task));
            string source = null;
            if (File.Exists(testPath))
            {
                source = File.ReadAllText(testPath);

                if (source.Contains(task.Task)) return;
            }
            else
            {
                // Create new file.
                source = ReplaceText(TestContent, task);
            }
            var function = ReplaceText(TestFunction, task);

            var index = source.LastIndexOf("    }");
            if (index == -1) return;

            source = source.Substring(0, index) + function + source.Substring(index);
            File.WriteAllText(testPath, source);
        }
    }
}