using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Resources;
using System.Threading;

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
        private FieldBuilder _breakEvent;
        private FieldBuilder _breakWait;

        ConstructorBuilder _constructor;

        MethodBuilder _incrementCell;
        MethodBuilder _decrementCell;
        MethodBuilder _incrementActiveCell;
        MethodBuilder _decrementActiveCell;
        MethodBuilder _execute;
        MethodBuilder _main;
        MethodBuilder _printCell;
        MethodBuilder _break;
        MethodBuilder _continue;

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

            _cells = _typeBuilder.DefineField("_cells", typeof(byte[]), FieldAttributes.Private);
            _activeCell = _typeBuilder.DefineField("_activeCell", typeof(int), FieldAttributes.Private);

            if (Debug)
            {
                _breakWait = _typeBuilder.DefineField("_breakWait", typeof (ManualResetEvent), FieldAttributes.Private);
                //_breakEvent = _typeBuilder.DefineField("BreakEvent", typeof (EventHandler<EventArgs>), FieldAttributes.Public | FieldAttributes.Static);
                _breakEvent = AddEvent(_typeBuilder, "BreakEvent", typeof(EventHandler<EventArgs>));
            }

            _constructor = _typeBuilder.DefineConstructor(
                MethodAttributes.Public |
                MethodAttributes.SpecialName |
                MethodAttributes.RTSpecialName,
                CallingConventions.Standard,
                Type.EmptyTypes);

            _incrementCell = AddPublicMethod(_typeBuilder, "IncrementCell", new[] { typeof(int) });
            _decrementCell = AddPublicMethod(_typeBuilder, "DecrementCell", new[] { typeof(int) });
            _printCell = AddPublicMethod(_typeBuilder, "PrintCell", new[] { typeof(int) });
            _incrementActiveCell = AddPublicMethod(_typeBuilder, "IncrementActiveCell", new[] { typeof(int) });
            _decrementActiveCell = AddPublicMethod(_typeBuilder, "DecrementActiveCell", new[] { typeof(int) });
            _execute = AddPublicMethod(_typeBuilder, "Execute");
            _main = AddPublicStaticMethod(_typeBuilder, "Main");

            if (Debug)
            {
                _break = AddPublicMethod(_typeBuilder, "Break");
                _continue = AddPublicMethod(_typeBuilder, "Continue");
            }

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

            if (Debug)
            {
                Break();
                Continue();
            }

            FinalizeAssembly();

            return assemblyBuilder;
        }
        
        #endregion Generate

        #region Method Bodies

        private void Constructor()
        {
            ILGenerator generator = _constructor.GetILGenerator();

            generator.Emit(OpCodes.Ldarg_0);
            generator.Emit(OpCodes.Ldc_I4_0);
            generator.Emit(OpCodes.Stfld, _activeCell);

            generator.Emit(OpCodes.Ldarg_0);
            generator.Emit(OpCodes.Ldc_I4, 1000000);
            generator.Emit(OpCodes.Newarr, typeof(byte));
            generator.Emit(OpCodes.Stfld, _cells);

            if (Debug)
            {
                generator.Emit(OpCodes.Ldarg_0);
                generator.Emit(OpCodes.Ldc_I4_0);
                generator.Emit(OpCodes.Newobj, typeof (ManualResetEvent).GetConstructor(new []{typeof(bool)}));
                generator.Emit(OpCodes.Stfld, _breakWait);
            }

            generator.Emit(OpCodes.Ret);
        }

        private void IncrementCell()
        {
            ILGenerator generator = _incrementCell.GetILGenerator();

            generator.Emit(OpCodes.Ldarg_0);
            generator.Emit(OpCodes.Ldfld, _cells);
            generator.Emit(OpCodes.Ldarg_0);
            generator.Emit(OpCodes.Ldfld, _activeCell);
            generator.Emit(OpCodes.Ldelema, typeof(byte));
            generator.Emit(OpCodes.Dup);
            generator.Emit(OpCodes.Ldobj, typeof(byte));
            generator.Emit(OpCodes.Ldarg_1);
            generator.Emit(OpCodes.Add);
            generator.Emit(OpCodes.Conv_U1);
            generator.Emit(OpCodes.Stobj, typeof(byte));
            generator.Emit(OpCodes.Ret);
        }

        private void DecrementCell()
        {
            ILGenerator generator = _decrementCell.GetILGenerator();

            generator.Emit(OpCodes.Ldarg_0);
            generator.Emit(OpCodes.Ldfld, _cells);
            generator.Emit(OpCodes.Ldarg_0);
            generator.Emit(OpCodes.Ldfld, _activeCell);
            generator.Emit(OpCodes.Ldelema, typeof(byte));
            generator.Emit(OpCodes.Dup);
            generator.Emit(OpCodes.Ldobj, typeof(byte));
            generator.Emit(OpCodes.Ldarg_1);
            generator.Emit(OpCodes.Sub);
            generator.Emit(OpCodes.Conv_U1);
            generator.Emit(OpCodes.Stobj, typeof(byte));
            generator.Emit(OpCodes.Ret);
        }

        private void PrintCell()
        {
            ILGenerator generator = _printCell.GetILGenerator();

            LocalBuilder c = generator.DeclareLocal(typeof (char));
            LocalBuilder left = generator.DeclareLocal(typeof(int));
            LocalBuilder countZero = generator.DeclareLocal(typeof(bool));

            Label loopStart = generator.DefineLabel();
            Label loop = generator.DefineLabel();

            generator.Emit(OpCodes.Ldarg_1);
            generator.Emit(OpCodes.Stloc, left.LocalIndex);
            generator.Emit(OpCodes.Ldc_I4_0);
            generator.Emit(OpCodes.Stloc, countZero.LocalIndex);
            
            generator.Emit(OpCodes.Ldarg_0);
            generator.Emit(OpCodes.Ldfld, _cells);
            generator.Emit(OpCodes.Ldarg_0);
            generator.Emit(OpCodes.Ldfld, _activeCell);
            generator.Emit(OpCodes.Ldelem, typeof(byte));
            generator.Emit(OpCodes.Stloc, c.LocalIndex);
            
            generator.Emit(OpCodes.Br, loopStart);
            generator.MarkLabel(loop);

            generator.Emit(OpCodes.Ldloc, c.LocalIndex);
            EmitConsoleWriteChar(generator);

            generator.Emit(OpCodes.Ldloc, left.LocalIndex);
            generator.Emit(OpCodes.Ldc_I4_1);
            generator.Emit(OpCodes.Sub);
            generator.Emit(OpCodes.Stloc, left.LocalIndex);

            generator.MarkLabel(loopStart);

            generator.Emit(OpCodes.Ldloc, left.LocalIndex);
            generator.Emit(OpCodes.Ldc_I4_0);
            generator.Emit(OpCodes.Cgt);
            generator.Emit(OpCodes.Stloc, countZero.LocalIndex);
            generator.Emit(OpCodes.Ldloc, countZero.LocalIndex);
            generator.Emit(OpCodes.Brtrue, loop);
            
            EmitConsoleOutFlush(generator);
            generator.Emit(OpCodes.Ret);
        }

        private void IncrementActiveCell()
        {
            ILGenerator generator = _incrementActiveCell.GetILGenerator();

            generator.Emit(OpCodes.Ldarg_0);
            generator.Emit(OpCodes.Ldarg_0);
            generator.Emit(OpCodes.Ldfld, _activeCell);
            generator.Emit(OpCodes.Ldarg_1);
            generator.Emit(OpCodes.Add);
            generator.Emit(OpCodes.Stfld, _activeCell);
            generator.Emit(OpCodes.Ret);
        }

        private void DecrementActiveCell()
        {
            ILGenerator generator = _decrementActiveCell.GetILGenerator();

            generator.Emit(OpCodes.Ldarg_0);
            generator.Emit(OpCodes.Ldarg_0);
            generator.Emit(OpCodes.Ldfld, _activeCell);
            generator.Emit(OpCodes.Ldarg_1);
            generator.Emit(OpCodes.Sub);
            generator.Emit(OpCodes.Stfld, _activeCell);
            generator.Emit(OpCodes.Ret);
        }

        private void Execute(string source)
        {
            ILGenerator generator = _execute.GetILGenerator();

            Stack<Label> loopStarts = new Stack<Label>();
            Stack<Label> loopEnds = new Stack<Label>();
            Stack<int> cellIndex = new Stack<int>();
            Stack<int> boolIndex = new Stack<int>();

            for (int i = 0; i < source.Length; i++)
            {
                char c = source[i];
                int count = 1;

                if (c == '>' || c == '<' || c == '+' || c == '-' || c == '.')
                {
                    while (i++ < source.Length - 1 && source[i] == c)
                    {
                        count++;
                    }

                    i--;
                }

                switch (c)
                {
                    case'*':
                        if (Debug)
                        {
                            generator.Emit(OpCodes.Ldarg_0);
                            generator.Emit(OpCodes.Call, _break);
                        }
                        break;

                    case '>':
                        generator.Emit(OpCodes.Ldarg_0);
                        generator.Emit(OpCodes.Ldc_I4, count);
                        generator.Emit(OpCodes.Call, _incrementActiveCell);
                        break;

                    case '<':
                        generator.Emit(OpCodes.Ldarg_0);
                        generator.Emit(OpCodes.Ldc_I4, count);
                        generator.Emit(OpCodes.Call, _decrementActiveCell);
                        break;

                    case '+':
                        generator.Emit(OpCodes.Ldarg_0);
                        generator.Emit(OpCodes.Ldc_I4, count);
                        generator.Emit(OpCodes.Call, _incrementCell);
                        break;

                    case '-':
                        generator.Emit(OpCodes.Ldarg_0);
                        generator.Emit(OpCodes.Ldc_I4, count);
                        generator.Emit(OpCodes.Call, _decrementCell);
                        break;

                    case '.':
                        generator.Emit(OpCodes.Ldarg_0);
                        generator.Emit(OpCodes.Ldc_I4, count);
                        generator.Emit(OpCodes.Call, _printCell);
                        break;

                    case '[':
                        LocalBuilder loopBool = generator.DeclareLocal(typeof(bool));
                        Label loopStart = generator.DefineLabel();
                        Label loopEnd = generator.DefineLabel();
                        boolIndex.Push(loopBool.LocalIndex);
                        loopStarts.Push(loopStart);
                        loopEnds.Push(loopEnd);
                        LocalBuilder local = generator.DeclareLocal(typeof(int));
                        cellIndex.Push(local.LocalIndex);

                        generator.Emit(OpCodes.Br, loopEnd);
                        generator.MarkLabel(loopStart);
                        generator.Emit(OpCodes.Nop);

                        break;

                    case ']':
                        generator.MarkLabel(loopEnds.Pop());
                        
                        generator.Emit(OpCodes.Ldarg_0);
                        generator.Emit(OpCodes.Ldfld, _cells);
                        generator.Emit(OpCodes.Ldarg_0);
                        generator.Emit(OpCodes.Ldfld, _activeCell);
                        generator.Emit(OpCodes.Ldelem, typeof(byte));

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

            generator.Emit(OpCodes.Ret);
        }

        private void Main()
        {
            ILGenerator generator = _main.GetILGenerator();

            LocalBuilder e = generator.DeclareLocal(typeof (Exception));
            LocalBuilder program = generator.DeclareLocal(_typeBuilder);
            program.SetLocalSymInfo("program");

            generator.BeginExceptionBlock();

            generator.Emit(OpCodes.Newobj, _constructor);
            generator.Emit(OpCodes.Stloc, program.LocalIndex);
            generator.Emit(OpCodes.Ldloc, program.LocalIndex);
            generator.Emit(OpCodes.Call, _execute);

            generator.BeginCatchBlock(typeof(Exception));

            generator.Emit(OpCodes.Stloc, e.LocalIndex);
            generator.Emit(OpCodes.Ldloc, e.LocalIndex);

            generator.Emit(OpCodes.Callvirt, typeof(Exception).GetMethod("get_Message", Type.EmptyTypes));
            EmitConsoleWriteLine(generator);

            generator.Emit(OpCodes.Ldloc, e.LocalIndex);

            generator.Emit(OpCodes.Callvirt, typeof(Exception).GetMethod("get_StackTrace", Type.EmptyTypes));
            EmitConsoleWriteLine(generator);

            generator.Emit(OpCodes.Ldstr, "");
            EmitConsoleWriteLine(generator);

            generator.Emit(OpCodes.Ldstr, "Active Cell: ");
            EmitConsoleWriteLine(generator);

            generator.Emit(OpCodes.Ldloc, program.LocalIndex);
            generator.Emit(OpCodes.Ldflda, _activeCell);
            EmitToString(generator, typeof(int));
            EmitConsoleWriteLine(generator);

            generator.EndExceptionBlock();
            generator.Emit(OpCodes.Nop);

            EmitConsoleReadLine(generator);
            generator.Emit(OpCodes.Ret);
        }

        private void Break()
        {
            ILGenerator generator = _break.GetILGenerator();

            LocalBuilder breakEventIsNull = generator.DeclareLocal(typeof (bool));
            Label returnLabel = generator.DefineLabel();

            generator.Emit(OpCodes.Ldarg_0);
            generator.Emit(OpCodes.Ldfld, _breakEvent);
            generator.Emit(OpCodes.Ldnull);
            generator.Emit(OpCodes.Ceq);
            generator.Emit(OpCodes.Stloc, breakEventIsNull);
            generator.Emit(OpCodes.Ldloc, breakEventIsNull);
            generator.Emit(OpCodes.Brtrue, returnLabel);

            generator.Emit(OpCodes.Ldarg_0);
            generator.Emit(OpCodes.Ldfld, _breakEvent);
            generator.Emit(OpCodes.Ldarg_0);
            generator.Emit(OpCodes.Newobj, typeof(EventArgs).GetConstructor(Type.EmptyTypes));
            generator.Emit(OpCodes.Callvirt, typeof(EventHandler<EventArgs>).GetMethod("Invoke", new[] { typeof(object), typeof(EventArgs) }));

            generator.MarkLabel(returnLabel);

            generator.Emit(OpCodes.Ldarg_0);
            generator.Emit(OpCodes.Ldfld, _breakWait);
            generator.Emit(OpCodes.Callvirt, typeof(EventWaitHandle).GetMethod("WaitOne", Type.EmptyTypes));
            generator.Emit(OpCodes.Pop);

            generator.Emit(OpCodes.Ret);
        }

        private void Continue()
        {
            ILGenerator generator = _continue.GetILGenerator();

            generator.Emit(OpCodes.Ldarg_0);
            generator.Emit(OpCodes.Ldfld, _breakWait);
            generator.Emit(OpCodes.Callvirt, typeof(EventWaitHandle).GetMethod("Set", Type.EmptyTypes));

            generator.Emit(OpCodes.Ret);
        }

        #endregion Method Bodies

        #region Helpers

        private static MethodBuilder AddPublicStaticMethod(TypeBuilder typeBuilder, string name)
        {
            return typeBuilder.DefineMethod(name, MethodAttributes.Public | MethodAttributes.Static, CallingConventions.Standard);
        }

        private static MethodBuilder AddPublicMethod(TypeBuilder typeBuilder, string name, Type[] methodArguments = null)
        {
            if (methodArguments == null)
                methodArguments = new Type[0];

            return typeBuilder.DefineMethod(name, MethodAttributes.Public, CallingConventions.Standard, typeof(void), methodArguments);
        }

        private static MethodBuilder AddPrivateMethod(TypeBuilder typeBuilder, string name)
        {
            return typeBuilder.DefineMethod(name, MethodAttributes.Private, CallingConventions.Standard);
        }

        private static FieldBuilder AddEvent(TypeBuilder typeBuilder, string eventName, Type eventHandlerType)
        {
            EventBuilder eventBuilder = typeBuilder.DefineEvent(eventName, EventAttributes.None, eventHandlerType);

            FieldBuilder backingField = typeBuilder.DefineField(eventName, eventHandlerType, FieldAttributes.Private);

            MethodBuilder addMethod = typeBuilder.DefineMethod("add_" + eventName,
                                                                MethodAttributes.Public | MethodAttributes.HideBySig |
                                                                MethodAttributes.SpecialName,
                                                                CallingConventions.Standard, typeof(void), new [] { eventHandlerType });

            ILGenerator generator = addMethod.GetILGenerator();

            generator.DeclareLocal(eventHandlerType);
            generator.DeclareLocal(eventHandlerType);
            generator.DeclareLocal(eventHandlerType);
            generator.DeclareLocal(typeof(bool));

            generator.Emit(OpCodes.Ldarg_0);
            generator.Emit(OpCodes.Ldfld, backingField);
            generator.Emit(OpCodes.Stloc_0);
            Label loop = generator.DefineLabel();
            generator.MarkLabel(loop);
            generator.Emit(OpCodes.Ldloc_0);
            generator.Emit(OpCodes.Stloc_1);
            generator.Emit(OpCodes.Ldloc_1);
            generator.Emit(OpCodes.Ldarg_1);
            generator.Emit(OpCodes.Call, typeof(Delegate).GetMethod("Combine", new []{typeof(Delegate), typeof(Delegate)}));
            generator.Emit(OpCodes.Castclass, eventHandlerType);
            generator.Emit(OpCodes.Stloc_2);
            generator.Emit(OpCodes.Ldarg_0);
            generator.Emit(OpCodes.Ldflda, backingField);
            generator.Emit(OpCodes.Ldloc_2);
            generator.Emit(OpCodes.Ldloc_1);

            MethodInfo m = GetGenericMethod(typeof(Interlocked), "CompareExchange", new [] { eventHandlerType }, new [] { eventHandlerType, eventHandlerType, eventHandlerType });

            generator.Emit(OpCodes.Call, m);
            generator.Emit(OpCodes.Stloc_0);
            generator.Emit(OpCodes.Ldloc_0);
            generator.Emit(OpCodes.Ldloc_1);
            generator.Emit(OpCodes.Ceq);
            generator.Emit(OpCodes.Ldc_I4_0);
            generator.Emit(OpCodes.Ceq);
            generator.Emit(OpCodes.Stloc_3);
            generator.Emit(OpCodes.Ldloc_3);
            generator.Emit(OpCodes.Brtrue_S, loop);

            generator.Emit(OpCodes.Ldstr, "ADD...");
            EmitConsoleWriteLine(generator);

            generator.Emit(OpCodes.Ret);

            MethodBuilder removeMethod = typeBuilder.DefineMethod("remove_" + eventName,
                                                                MethodAttributes.Public | MethodAttributes.HideBySig |
                                                                MethodAttributes.SpecialName,
                                                                CallingConventions.Standard, typeof(void), new[] { eventHandlerType });

            generator = removeMethod.GetILGenerator();

            generator.Emit(OpCodes.Ldstr, "REMOVE...");

            generator.Emit(OpCodes.Ret);

            eventBuilder.SetAddOnMethod(addMethod);
            eventBuilder.SetRemoveOnMethod(removeMethod);

            return backingField;
        }

        public static MethodInfo GetGenericMethod(Type type, string name, Type[] genericTypeArgs, Type[] paramTypes)
        {
            MethodInfo[] methodInfos = type.GetMethods();

            foreach (MethodInfo m in methodInfos)
                if (m.Name == name && m.IsGenericMethod)
                {
                    ParameterInfo[] parameterInfos = m.GetParameters();

                    if (parameterInfos.Length != paramTypes.Length)
                        continue;

                    for (int i = 0; i < parameterInfos.Length; i++)
                    {
                        if (parameterInfos[i].ParameterType != paramTypes[i])
                            continue;
                    }

                    Type[] genericArguments = m.GetGenericArguments();

                    if (genericArguments.Length != genericTypeArgs.Length)
                        continue;

                    for (int i = 0; i < genericArguments.Length; i++)
                    {
                        if (genericArguments[i] != genericTypeArgs[i])
                            continue;
                    }

                    return m.MakeGenericMethod(genericTypeArgs);
                    
                }
            return null;
        }


        private static void EmitConsoleReadLine(ILGenerator generator)
        {
            generator.Emit(OpCodes.Call, typeof(Console).GetMember("ReadLine").OfType<MethodInfo>().First());
        }

        private static void EmitConsoleWriteChar(ILGenerator generator)
        {
            MethodInfo writeMethod = typeof(Console).GetMethod("Write", new[] { typeof(char) });
            generator.Emit(OpCodes.Call, writeMethod);
        }

        private static void EmitConsoleOutFlush(ILGenerator generator)
        {
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
