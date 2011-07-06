using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace BrainfuckNet.Compiler
{
    class Program
    {
        #region Arguments

        private const string OutputArgument = "-o";
        private const string OutputArgumentLong = "--output";
        private const string HelpArgument = "-h";
        private const string HelpArgumentLong = "--help";
        private const string VerboseArgument = "-v";

        #endregion Arguments

        #region ExitCodes

        private const int ErrorExitCode = -1;
        private const int OkExitCode = 0;

        #endregion ExitCodes

        #region Fields

        private static string _output = "bf";
        private static readonly List<string> Files = new List<string>();
        private static bool _verbose;

        #endregion Fields

        #region Private Methods
        
        private static void Main(string[] args)
        {
            if (args.Length == 0)
            {
               Log(LogLevel.Error, "No source files specified.");
               PrintUsage();
               Environment.Exit(ErrorExitCode);
            }

            for (int i = 0; i < args.Length; i++)
            {
                if (args[i] == OutputArgument || args[i] == OutputArgumentLong)
                {
                    i++;

                    if (i + 1 > args.Length)
                    {
                        Log(LogLevel.Error, "No output file specified.");
                        PrintUsage();
                        Environment.Exit(ErrorExitCode);
                    }

                    _output = args[i];

                    if (_output.Contains(".exe"))
                        _output = _output.Substring(0, _output.Length - 4);

                    continue;   
                }

                if (args[i] == VerboseArgument)
                {
                    _verbose = true;
                    continue;
                }

                if (args[i] == HelpArgument || args[i] == HelpArgumentLong)
                {
                    PrintUsage();
                    Environment.Exit(OkExitCode);
                }

                Files.Add(args[i]);
            }
            try
            {
                if (_verbose)
                {
                    Log(LogLevel.Information, "Output file: " + _output + ".exe");

                    foreach (string file in Files)
                    {
                        Log(LogLevel.Information, "Source File: " + file);
                    }
                }

                BrainfuckCodeProvider codeProvider = new BrainfuckCodeProvider();

                ICodeCompiler codeCompiler = codeProvider.CreateCompiler();

                CompilerParameters parameters = new CompilerParameters();
                parameters.OutputAssembly = _output;

                CompilerResults results = codeCompiler.CompileAssemblyFromFileBatch(parameters, Files.ToArray());

                if (results.Errors.HasErrors)
                {
                    StringBuilder sb = new StringBuilder();

                    sb.Append(results.Errors.Count);
                    sb.Append(" Errors:");
                    sb.Append(Environment.NewLine);

                    foreach (CompilerError error in results.Errors)
                    {
                        sb.Append(error.ErrorText);
                    }

                    Console.WriteLine(sb.ToString());
                }
            }
            catch (Exception e)
            {
                Log(LogLevel.Error, e.Message);
            }
        }

        private static void PrintUsage()
        {
            AssemblyName assemblyName = Assembly.GetEntryAssembly().GetName();

            string name = assemblyName.Name + ".exe";

            Console.WriteLine("Usage: " + name + " [-o|--output outputFile] [-h|--help] source.bf source2.bf ...");
        }

        private static void Log(LogLevel logLevel, string message)
        {
            Console.WriteLine(logLevel + ": " + message);
        }

        #endregion Private Methods
    }
}
