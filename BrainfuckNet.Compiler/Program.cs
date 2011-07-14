using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Reflection.Emit;
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
        private const string DebugArgument = "-d";

        #endregion Arguments

        #region Constants

        private const string NoSourceFilesSpecified = "No source files specified.";
        private const string NoOutputFileSpecified = "No output file specified.";
        private const string Extension = ".exe";
        private const string BuildDirectory = "Build";

        #endregion Constants

        #region ExitCodes

        private const int ErrorExitCode = -1;
        private const int OkExitCode = 0;

        #endregion ExitCodes

        #region Fields

        private static string _output = "bf";
        private static readonly List<string> Files = new List<string>();
        private static bool _verbose;
        private static bool _debug;

        #endregion Fields

        #region Private Methods
        
        private static void Main(string[] args)
        {
            if (args.Length == 0)
            {
               Log(LogLevel.Error, NoSourceFilesSpecified);
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
                        Log(LogLevel.Error, NoOutputFileSpecified);
                        PrintUsage();
                        Environment.Exit(ErrorExitCode);
                    }

                    _output = args[i];

                    if (_output.Contains(Extension))
                        _output = _output.Substring(0, _output.Length - 4);

                    continue;   
                }

                if (args[i] == VerboseArgument)
                {
                    _verbose = true;
                    continue;
                }

                if (args[i] == DebugArgument)
                {
                    _debug = true;
                    continue;
                }

                if (args[i] == HelpArgument || args[i] == HelpArgumentLong)
                {
                    PrintUsage();
                    Environment.Exit(OkExitCode);
                }

                //MONGOHACK because im to lazy to refactor

                if (Files.Count > 0)
                {
                    Log(LogLevel.Error, "Can only have one source file.");
                    Environment.Exit(ErrorExitCode);
                }

                Files.Add(args[i]);
            }
            try
            {
                StreamReader inputFile = File.OpenText(Files[0]);

                if (!Directory.Exists(BuildDirectory))
                    Directory.CreateDirectory(BuildDirectory);

                Environment.CurrentDirectory = Path.GetFullPath(BuildDirectory);

                foreach (string file in Directory.EnumerateFiles(Environment.CurrentDirectory))
                {
                    File.Delete(file);
                }

                if (_verbose)
                {
                    Log(LogLevel.Information, "Output file: " + _output + Extension);

                    foreach (string file in Files)
                    {
                        Log(LogLevel.Information, "Source File: " + file);
                    }
                }

                GenerateAssembly(false, inputFile);

                if (_debug)
                {
                    inputFile.BaseStream.Seek(0, SeekOrigin.Begin);
                    GenerateAssembly(true, inputFile);
                }

                foreach (string file in Directory.EnumerateFiles(Environment.CurrentDirectory, "*.resources"))
                {
                    File.Delete(file);
                }
                
            }
            catch (Exception e)
            {
                Log(LogLevel.Error, e.Message);
            }
        }

        private static void GenerateAssembly(bool debug, StreamReader inputFile)
        {
            AssemblyGenerator assemblyGenerator = new AssemblyGenerator();
            assemblyGenerator.Debug = debug;
            assemblyGenerator.Name = _output;

            AssemblyBuilder assemblyBuilder = assemblyGenerator.Generate(inputFile);

            assemblyBuilder.Save(assemblyBuilder.GetName().Name);

            if (assemblyGenerator.Errors.HasErrors)
            {
                StringBuilder sb = new StringBuilder();

                sb.Append(assemblyGenerator.Errors.Count);
                sb.Append(" Errors:");
                sb.Append(Environment.NewLine);

                foreach (CompilerError error in assemblyGenerator.Errors)
                {
                    sb.Append(error.ErrorText);
                }

                Console.WriteLine(sb.ToString());
            }

            if (debug)
            {
                Assembly assembly = Assembly.LoadFrom("test.debug.exe");
                
                Type t = assembly.GetType("test.Program");

                object o = Activator.CreateInstance(t);

                MemberInfo[] memberInfos =
                    t.GetMembers(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic |
                                 BindingFlags.Static);

                EventInfo eventInfo = t.GetEvent("BreakEvent", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic |
                                 BindingFlags.Static);

                //eventInfo.AddEventHandler(o, new EventHandler<EventArgs>(Test));

                //MethodInfo m = t.GetMethod("Break");

                //m.Invoke(o, new object[0]);
            }

            //Console.ReadLine();
        }

        private static void Test(object sender, EventArgs eventArgs)
        {
            Console.WriteLine("ASDJFKASDFKADFJS");
        }

        private static void PrintUsage()
        {
            AssemblyName assemblyName = Assembly.GetEntryAssembly().GetName();

            string name = assemblyName.Name + Extension;

            Console.WriteLine("Usage: " + name + " [-o|--output outputFile] [-h|--help] source.bf");
        }

        private static void Log(LogLevel logLevel, string message)
        {
            Console.WriteLine(logLevel + ": " + message);
        }

        #endregion Private Methods
    }
}
