using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Resources;

namespace BrainfuckNet
{
    /// <summary>
    /// Generate an assembly from Brainfuck source code.
    /// </summary>
    public sealed class AssemblyGenerator
    {
        #region Constants

        private const string DefaultName = "out";
        private const string Extension = ".exe";
        private const string DebugExtension = ".debug.exe";
        private const string ResourceName = "Source";
        private const string ResourceDescription = "Brainfuck source code.";
        private const string ResourceFilename = "Source.resources";
        private const string ClassName = "Program";

        #endregion Constants

        #region Fields

        TypeBuilder _typeBuilder;

        private FieldBuilder _cells;
        private FieldBuilder _activeCell;

        ConstructorBuilder _constructor;

        MethodBuilder _incrementCell;
        MethodBuilder _decrementCell;
        MethodBuilder _incrementActiveCell;
        MethodBuilder _decrementActiveCell;
        MethodBuilder _execute;
        MethodBuilder _main;
        MethodBuilder _printCell;

        #endregion Fields

        #region Constructors

        /// <summary>
        /// Creates a new instance of the Brainfuck assembly generator.
        /// </summary>
        public AssemblyGenerator()
        {
            Debug = false;
            Name = DefaultName;
            Errors = new CompilerErrorCollection();
            Cells = 1024;
        }

        #endregion Constructors

        #region Initialization

        private static MethodBuilder DefinePublicStaticMethod(TypeBuilder typeBuilder, string name)
        {
            return typeBuilder.DefineMethod(name, MethodAttributes.Public | MethodAttributes.Static, CallingConventions.Standard);
        }

        /// <summary>
        /// Initialize assembly, types and methods for the generated assembly.
        /// </summary>
        /// <param name="source">Brainfuck source code.</param>
        /// <returns>An instance of an AssemblyBuilder to use when compiling the source code.</returns>
        private AssemblyBuilder Initialize(string source)
        {
            AssemblyName assemblyName = new AssemblyName(Name + (Debug ? DebugExtension : Extension));

            AppDomain appDomain = AppDomain.CurrentDomain;
            AssemblyBuilder assemblyBuilder = appDomain.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.RunAndSave);

            IResourceWriter resources = assemblyBuilder.DefineResource(ResourceName, ResourceDescription, ResourceFilename);

            resources.AddResource(ResourceName, source);

            ModuleBuilder moduleBuilder = assemblyBuilder.DefineDynamicModule(assemblyBuilder.GetName().Name, true);
            
            _typeBuilder = moduleBuilder.DefineType(Name + "." + ClassName, TypeAttributes.Public);


            _cells = _typeBuilder.DefineField("_cells", typeof(byte[]), FieldAttributes.Private | FieldAttributes.Static);
            _activeCell = _typeBuilder.DefineField("_activeCell", typeof(int), FieldAttributes.Private | FieldAttributes.Static);


            _constructor = _typeBuilder.DefineConstructor(
                MethodAttributes.Public |
                MethodAttributes.Static |
                MethodAttributes.SpecialName |
                MethodAttributes.RTSpecialName,
                CallingConventions.Standard,
                Type.EmptyTypes);

            _incrementCell = DefinePublicStaticMethod(_typeBuilder, "IncrementCell");
            _decrementCell = DefinePublicStaticMethod(_typeBuilder, "DecrementCell");
            _printCell = DefinePublicStaticMethod(_typeBuilder, "PrintCell");
            _incrementActiveCell = DefinePublicStaticMethod(_typeBuilder, "IncrementActiveCell");
            _decrementActiveCell = DefinePublicStaticMethod(_typeBuilder, "DecrementActiveCell");
            _execute = DefinePublicStaticMethod(_typeBuilder, "Execute");
            _main = DefinePublicStaticMethod(_typeBuilder, "Main");

            assemblyBuilder.SetEntryPoint(_main);

            return assemblyBuilder;
        }

        #endregion Initialization

        #region Finalization

        /// <summary>
        /// Finalize the assembly by creating types compiled from source code.
        /// </summary>
        private void FinalizeAssembly()
        {
            _typeBuilder.CreateType();
        }

        #endregion Finalization

        #region Generate

        /// <summary>
        /// Generate an assembly from Brainfuck source code.
        /// </summary>
        /// <param name="sr">Source code.</param>
        /// <returns>Compiled assembly.</returns>
        public AssemblyBuilder Generate(StreamReader sr)
        {
            return Generate(sr.ReadToEnd());
        }

        /// <summary>
        /// Generate an assembly from Brainfuck source code.
        /// </summary>
        /// <param name="source">Source code.</param>
        /// <returns>Compiled assembly.</returns>
        public AssemblyBuilder Generate(string source)
        {
            AssemblyBuilder assemblyBuilder = Initialize(source);
            
            Constructor();
            Main();
            Execute(source);
            IncrementCell();
            DecrementCell();
            IncrementActiveCell();
            DecrementActiveCell();
            PrintCell();

            FinalizeAssembly();

            return assemblyBuilder;
        }
        
        #endregion Generate

        #region Method Bodies

        private void Constructor()
        {
            ILGenerator generator = _constructor.GetILGenerator();

            generator.Emit(OpCodes.Ldc_I4_0);
            generator.Emit(OpCodes.Stsfld, _activeCell);

            generator.Emit(OpCodes.Ldc_I4, 1000000);
            generator.Emit(OpCodes.Newarr, typeof(byte));
            generator.Emit(OpCodes.Stsfld, _cells);

            generator.Emit(OpCodes.Ret);
        }

        private void IncrementCell()
        {
            ILGenerator generator = _incrementCell.GetILGenerator();

            generator.Emit(OpCodes.Ldsfld, _cells);
            generator.Emit(OpCodes.Ldsfld, _activeCell);
            generator.Emit(OpCodes.Ldelema, typeof(byte));
            generator.Emit(OpCodes.Dup);
            generator.Emit(OpCodes.Ldobj, typeof(byte));
            generator.Emit(OpCodes.Ldc_I4_1);
            generator.Emit(OpCodes.Add);
            generator.Emit(OpCodes.Conv_U1);
            generator.Emit(OpCodes.Stobj, typeof(byte));
        }

        private void DecrementCell()
        {
            ILGenerator generator = _decrementCell.GetILGenerator();

            generator.Emit(OpCodes.Ldsfld, _cells);
            generator.Emit(OpCodes.Ldsfld, _activeCell);
            generator.Emit(OpCodes.Ldelema, typeof(byte));
            generator.Emit(OpCodes.Dup);
            generator.Emit(OpCodes.Ldobj, typeof(byte));
            generator.Emit(OpCodes.Ldc_I4_1);
            generator.Emit(OpCodes.Sub);
            generator.Emit(OpCodes.Conv_U1);
            generator.Emit(OpCodes.Stobj, typeof(byte));
        }

        private void PrintCell()
        {
            ILGenerator generator = _printCell.GetILGenerator();

            generator.Emit(OpCodes.Ldsfld, _cells);
            generator.Emit(OpCodes.Ldsfld, _activeCell);
            generator.Emit(OpCodes.Ldelem, typeof(byte));
            EmitConsoleWriteChar(generator);
        }

        private void IncrementActiveCell()
        {
            ILGenerator generator = _incrementActiveCell.GetILGenerator();

            generator.Emit(OpCodes.Ldsfld, _activeCell);
            generator.Emit(OpCodes.Ldc_I4_1);
            generator.Emit(OpCodes.Add);
            generator.Emit(OpCodes.Stsfld, _activeCell);
        }

        private void DecrementActiveCell()
        {
            ILGenerator generator = _decrementActiveCell.GetILGenerator();

            generator.DeclareLocal(typeof(bool));
            generator.DeclareLocal(typeof(int));

            generator.Emit(OpCodes.Ldsfld, _activeCell);
            generator.Emit(OpCodes.Ldc_I4_1);
            generator.Emit(OpCodes.Sub);
            generator.Emit(OpCodes.Stsfld, _activeCell);
        }

        private void Execute(IEnumerable<char> source)
        {
            ILGenerator generator = _execute.GetILGenerator();

            Stack<Label> loopStarts = new Stack<Label>();
            Stack<Label> loopEnds = new Stack<Label>();
            Stack<int> cellIndex = new Stack<int>();
            Stack<int> boolIndex = new Stack<int>();

            foreach (char c in source)
            {
                switch (c)
                {
                    case '>':
                        generator.Emit(OpCodes.Call, _incrementActiveCell);
                        break;
                    case '<':
                        generator.Emit(OpCodes.Call, _decrementActiveCell);
                        break;
                    case '+':
                        generator.Emit(OpCodes.Call, _incrementCell);
                        break;

                    case '-':
                        generator.Emit(OpCodes.Call, _decrementCell);
                        break;

                    case '.':
                        generator.Emit(OpCodes.Call, _printCell);
                        break;
                    case '[':
                        LocalBuilder loopBool = generator.DeclareLocal(typeof (bool));
                        Label loopStart = generator.DefineLabel();
                        Label loopEnd = generator.DefineLabel();
                        boolIndex.Push(loopBool.LocalIndex);
                        loopStarts.Push(loopStart);
                        loopEnds.Push(loopEnd);
                        LocalBuilder local = generator.DeclareLocal(typeof (int));
                        cellIndex.Push(local.LocalIndex);
                        generator.Emit(OpCodes.Ldsfld, _activeCell);
                        generator.Emit(OpCodes.Stloc, local.LocalIndex);

                        generator.Emit(OpCodes.Br, loopEnd);
                        generator.MarkLabel(loopStart);
                        generator.Emit(OpCodes.Nop);

                        break;
                    case ']':
                        generator.MarkLabel(loopEnds.Pop());

                        generator.Emit(OpCodes.Ldsfld, _cells);
                        generator.Emit(OpCodes.Ldsfld, _activeCell);
                        generator.Emit(OpCodes.Ldelem, typeof (byte));

                        generator.Emit(OpCodes.Ldc_I4_0);
                        generator.Emit(OpCodes.Cgt);

                        int boolLocalIndex = boolIndex.Pop();

                        generator.Emit(OpCodes.Stloc, boolLocalIndex);
                        generator.Emit(OpCodes.Ldloc, boolLocalIndex);
                        generator.Emit(OpCodes.Brtrue, loopStarts.Pop());

                        break;

                    default:
                        break;
                }
            }
        }

        private void Main()
        {
            ILGenerator generator = _main.GetILGenerator();

            generator.DeclareLocal(typeof (Exception));

            generator.BeginExceptionBlock();
            generator.Emit(OpCodes.Call, _execute);
            generator.BeginCatchBlock(typeof(Exception));
            
            generator.Emit(OpCodes.Stloc_0);
            generator.Emit(OpCodes.Ldloc_0);

            generator.Emit(OpCodes.Call, typeof(Exception).GetMethod("get_Message", new Type[0]));
            EmitConsoleWriteLine(generator);

            generator.Emit(OpCodes.Ldloc_0);

            generator.Emit(OpCodes.Call, typeof(Exception).GetMethod("get_StackTrace", new Type[0]));
            EmitConsoleWriteLine(generator);

            generator.Emit(OpCodes.Ldstr, "");
            EmitConsoleWriteLine(generator);

            generator.Emit(OpCodes.Ldstr, "Active Cell: ");
            EmitConsoleWriteLine(generator);

            generator.Emit(OpCodes.Ldsflda, _activeCell);
            EmitToString(generator, typeof(int));
            EmitConsoleWriteLine(generator);

            generator.Emit(OpCodes.Ret);
            generator.EndExceptionBlock();
            generator.Emit(OpCodes.Nop);

            EmitConsoleReadLine(generator);
        }

        #endregion Method Bodies

        #region Helpers

        private static void EmitConsoleReadLine(ILGenerator generator)
        {
            generator.Emit(OpCodes.Call, typeof(Console).GetMember("ReadLine").OfType<MethodInfo>().First());
        }

        private static void EmitConsoleWriteChar(ILGenerator generator)
        {
            MethodInfo writeMethod = typeof(Console).GetMethod("Write", new[] { typeof(char) });
            generator.Emit(OpCodes.Call, writeMethod);
            generator.Emit(OpCodes.Call, typeof(Console).GetMethod("get_Out", new Type[0]));
            generator.Emit(OpCodes.Callvirt, typeof(TextWriter).GetMethod("Flush", new Type[0]));
        }

        private static void EmitConsoleWriteLine(ILGenerator generator)
        {
            MethodInfo writeLineMethod = typeof(Console).GetMethod("WriteLine", new[] { typeof(string) });
            generator.Emit(OpCodes.Call, writeLineMethod);
        }

        private static void EmitToString(ILGenerator generator, Type type = null)
        {
            Type t = typeof(object);

            if (type != null)
                t = type;

            MethodInfo toStringMethod = t.GetMethod("ToString", new Type[0]);
            generator.Emit(type == null ? OpCodes.Callvirt : OpCodes.Call, toStringMethod);
        }

        #endregion Helpers

        #region Properties

        /// <summary>
        /// True to inject debug methods.
        /// </summary>
        public bool Debug { get; set; }

        /// <summary>
        /// Output assembly Name.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Errors and warnings from compilation.
        /// </summary>
        public CompilerErrorCollection Errors { get; private set; }

        /// <summary>
        /// Cell count to use in the compiled Brainfuck program.
        /// </summary>
        public int Cells { get; set; }

        #endregion Properties
    }
}
