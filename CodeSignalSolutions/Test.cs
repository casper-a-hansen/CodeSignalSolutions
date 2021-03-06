﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using System.Reflection;
using System.Collections;
using NUnit.Framework;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace CodeSignalSolutions
{
    public static class Test
    {
        public static bool AssertSuccess { get; set; } = true;
        /// <summary>
        /// Run tests and test results (no time limit)
        /// </summary>
        /// <param name="functionClass">typeof class containing private methodname returning result.</param>
        /// <param name="tests">strings containing all parameters for the execute method as "param1: [..]\n param2: [...]\n Expected Result: 12"</param>
        public static int Execute(Type functionClass, params string[] tests)
        {
            if (functionClass == null) throw new ArgumentNullException(nameof(functionClass));
            if (tests == null || tests.Length == 0) throw new ArgumentNullException(nameof(tests));

            var methodName = functionClass.Name;
            if (methodName.EndsWith("Class")) methodName = methodName.Substring(0, methodName.Length - 5);
            var method = functionClass.GetMethod(methodName, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (method == null) throw new ArgumentException(nameof(functionClass), $"The {functionClass.Name} does not contain a private method called {methodName}.");

            var parameters = method.GetParameters();
            var returnType = method.ReturnType;
            // Checking parameters.
            int testNo = 1;
            List<Exception> exceptions = new List<Exception>();
            foreach (var test in tests)
            {
                try
                {
                    var param = ParseParameters(parameters, test);
                    var expected = ParseExpectedOutput(returnType, test);
                }
                catch (Exception e)
                {
                    exceptions.Add(e);
                }
                testNo++;
            }
            if (exceptions.Count != 0)
            {
                throw new AggregateException("Parsing tests raised one or more exceptions", exceptions);
            }
            int success = 0;
            foreach (var test in tests)
            {
                var p = ParseParameters(parameters, test);
                var expected = ParseExpectedOutput(returnType, test);
                if (DoExecute(functionClass, method, expected, p))
                {
                    success++;
                }
            }
            Console.WriteLine($"You had {success} successes out of {tests.Length} tests");
            if (AssertSuccess)
            {
                Assert.AreEqual(tests.Length, success, "Not all tests was successfully.");
            }
            return success;
        }
        public static bool ExecuteOne(Type functionClass, object expected, params object[] parameters)
        {
            if (functionClass == null) throw new ArgumentNullException(nameof(functionClass));
            if (parameters == null || parameters.Length == 0) throw new ArgumentNullException(nameof(parameters));
            var methodName = functionClass.Name;
            if (methodName.EndsWith("Class")) methodName = methodName.Substring(0, methodName.Length - 5);
            var method = functionClass.GetMethod(methodName, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (method == null) throw new ArgumentException("The class {functionClass.Name} does not contain the private {methodName}() {{...}}", nameof(functionClass));

            return DoExecute(functionClass, method, expected, parameters);
        }
        static bool DoExecute(Type functionClass, MethodInfo method, object expected, object[] parameters)
        {
            var obj = Activator.CreateInstance(functionClass);
            Stopwatch watch = new Stopwatch();
            watch.Start();
            var result = method.Invoke(obj, parameters);
            watch.Stop();
            if (Same(expected, result))
            {
                Console.WriteLine($"Your result was as expected in {watch.ElapsedMilliseconds}ms.");
                return true;
            }
            else
            {
                Console.WriteLine($"Your result was different from expected after {watch.ElapsedMilliseconds}ms:");
                ShowDifference(expected, result);
                return false;
            }
        }
        static Regex regexParameters = new Regex(@"^\s*([_a-z0-9 ]+)\s*:", RegexOptions.IgnoreCase | RegexOptions.Compiled | RegexOptions.Multiline);
        static object[] ParseParameters(ParameterInfo[] parameters, string test) =>
            parameters.Select(p => ParseValue(p.Name, p.ParameterType, test)).ToArray();
        public static object ParseExpectedOutput(Type returnType, string test) => ParseValue("Expected Output", returnType, test);
        public static object ParseValue(string name, Type valueType, string test)
        {
            if (string.IsNullOrEmpty(name)) throw new ArgumentNullException(nameof(name), "Name cannot be null or empty");
            if (valueType == null) throw new ArgumentNullException(nameof(valueType), "The type of value cannot be null or empty.");
            if (string.IsNullOrEmpty(test)) throw new ArgumentNullException(nameof(test), "Test parameters cannot be null or empty");

            var matches = regexParameters.Matches(test);
            if (matches.Count == 0) throw new ArgumentException(nameof(test), "The test parameters does not contain any value");

            var match = matches.Select((match, index) => (match, index))
                .FirstOrDefault(t => t.match.Groups[1].Value.Equals(name, StringComparison.OrdinalIgnoreCase));
            if (match.match == null) throw new ArgumentException(nameof(name), $"The test parameters does not contain the value {name}");

            var end = match.index == matches.Count - 1 ? test.Length - 1 : matches[match.index + 1].Index - 1;
            var start = match.match.Index + match.match.Length;
            var value = test.Substring(start, end - start + 1);

            return JsonConvert.DeserializeObject(value, valueType);
        }
        public static bool Same(object expected, object result)
        {
            if (expected is int || expected is string || expected is char)
            {
                return expected.Equals(result);
            }
            var e = JsonConvert.SerializeObject(expected, Formatting.Indented);
            var r = JsonConvert.SerializeObject(result, Formatting.Indented);
            return e.Equals(r);
        }
        static void ShowDifference(object expected, object result)
        {
            var origColor = Console.ForegroundColor;
            try
            {
                var e = Transform(JsonConvert.SerializeObject(expected, Formatting.Indented)).Split("\n");
                var r = Transform(JsonConvert.SerializeObject(result, Formatting.Indented)).Split("\n");

                for (int i = 0; i < Math.Max(e.Length, r.Length); i++)
                {
                    var el = i < e.Length ? e[i].Trim() : "";
                    var rl = i < r.Length ? r[i].Trim() : "<missing>";
                    if (el == rl)
                    {
                        Console.WriteLine($"Identical: {el}");
                    }
                    else
                    {
                        Console.WriteLine($"Expected:  {el}");
                        Console.WriteLine($"Result:    {rl}");
                        Console.Write($"Difference:");
                        for(int j = 0; j < Math.Max(el.Length, rl.Length); j++)
                        {
                            if (j >= el.Length || j >= rl.Length) Console.Write('^');
                            else if (el[j] != rl[j]) Console.Write('^');
                            else Console.Write(' ');
                        }
                        Console.WriteLine();
                    }
                }
            }
            finally
            {
                Console.ForegroundColor = origColor;
            }
        }
        static string Transform(string json)
        {
            json = Regex.Replace(json, @"(?<=""[^""]*""|-?\w+)\s*,\s*(?=""[^""]*""|-?\w+)", ",");
            json = Regex.Replace(json, @"(?<=\[|-?\w+)\s*(?=""[^""]*""|-?\w+)", "");
            json = Regex.Replace(json, @"(?<=""[^""]*""|-?\w+)\s*(?=\])", "");
            json = Regex.Replace(json, @"(?<=\])\s*,\s*(?=\[)", "," + Environment.NewLine);
            json = Regex.Replace(json, @"(?<=\[)\s+(?=\[)", "");
            return Regex.Replace(json, @"(?<=\])\s+(?=\])", "");
        }
    }
}
