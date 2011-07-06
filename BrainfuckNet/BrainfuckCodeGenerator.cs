using System;
using System.CodeDom;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Threading;

namespace BrainfuckNet
{
    public class BrainfuckCodeGenerator : ICodeCompiler
    {
        /// <summary>
        /// Compiles an assembly from the <see cref="N:System.CodeDom"/> tree contained in the specified <see cref="T:System.CodeDom.CodeCompileUnit"/>, using the specified compiler settings.
        /// </summary>
        /// <returns>
        /// A <see cref="T:System.CodeDom.Compiler.CompilerResults"/> object that indicates the results of compilation.
        /// </returns>
        /// <param name="options">A <see cref="T:System.CodeDom.Compiler.CompilerParameters"/> object that indicates the settings for compilation. </param><param name="compilationUnit">A <see cref="T:System.CodeDom.CodeCompileUnit"/> that indicates the code to compile. </param>
        public CompilerResults CompileAssemblyFromDom(CompilerParameters options, CodeCompileUnit compilationUnit)
        {
            return CompileAssemblyFromDomBatch(options, new [] {compilationUnit});
        }

        /// <summary>
        /// Compiles an assembly from the source code contained within the specified file, using the specified compiler settings.
        /// </summary>
        /// <returns>
        /// A <see cref="T:System.CodeDom.Compiler.CompilerResults"/> object that indicates the results of compilation.
        /// </returns>
        /// <param name="options">A <see cref="T:System.CodeDom.Compiler.CompilerParameters"/> object that indicates the settings for compilation. </param><param name="fileName">The file name of the file that contains the source code to compile. </param>
        public CompilerResults CompileAssemblyFromFile(CompilerParameters options, string fileName)
        {
            return CompileAssemblyFromFileBatch(options, new [] {fileName});
        }

        /// <summary>
        /// Compiles an assembly from the specified string containing source code, using the specified compiler settings.
        /// </summary>
        /// <returns>
        /// A <see cref="T:System.CodeDom.Compiler.CompilerResults"/> object that indicates the results of compilation.
        /// </returns>
        /// <param name="options">A <see cref="T:System.CodeDom.Compiler.CompilerParameters"/> object that indicates the settings for compilation. </param><param name="source">The source code to compile. </param>
        public CompilerResults CompileAssemblyFromSource(CompilerParameters options, string source)
        {
            return CompileAssemblyFromSourceBatch(options, new [] {source});
        }

        /// <summary>
        /// Compiles an assembly based on the <see cref="N:System.CodeDom"/> trees contained in the specified array of <see cref="T:System.CodeDom.CodeCompileUnit"/> objects, using the specified compiler settings.
        /// </summary>
        /// <returns>
        /// A <see cref="T:System.CodeDom.Compiler.CompilerResults"/> object that indicates the results of compilation.
        /// </returns>
        /// <param name="options">A <see cref="T:System.CodeDom.Compiler.CompilerParameters"/> object that indicates the settings for compilation. </param><param name="compilationUnits">An array of type <see cref="T:System.CodeDom.CodeCompileUnit"/> that indicates the code to compile. </param>
        public CompilerResults CompileAssemblyFromDomBatch(CompilerParameters options, CodeCompileUnit[] compilationUnits)
        {
            throw new NotSupportedException("BrainfuckCodeGenerator does not support compiling from dom.");
        }

        /// <summary>
        /// Compiles an assembly from the source code contained within the specified files, using the specified compiler settings.
        /// </summary>
        /// <returns>
        /// A <see cref="T:System.CodeDom.Compiler.CompilerResults"/> object that indicates the results of compilation.
        /// </returns>
        /// <param name="options">A <see cref="T:System.CodeDom.Compiler.CompilerParameters"/> object that indicates the settings for compilation. </param><param name="fileNames">The file names of the files to compile. </param>
        public CompilerResults CompileAssemblyFromFileBatch(CompilerParameters options, string[] fileNames)
        {
            string[] sources = new string[fileNames.Length];

            for (int i = 0; i < fileNames.Length; i++)
            {
                sources[i] = File.ReadAllText(fileNames[i]);
            }

            return CompileAssemblyFromSourceBatch(options, sources);
        }

        /// <summary>
        /// Compiles an assembly from the specified array of strings containing source code, using the specified compiler settings.
        /// </summary>
        /// <returns>
        /// A <see cref="T:System.CodeDom.Compiler.CompilerResults"/> object that indicates the results of compilation.
        /// </returns>
        /// <param name="options">A <see cref="T:System.CodeDom.Compiler.CompilerParameters"/> object that indicates the settings for compilation. </param><param name="sources">The source code strings to compile. </param>
        public CompilerResults CompileAssemblyFromSourceBatch(CompilerParameters options, string[] sources)
        {
            string name = options.OutputAssembly;

            CompilerResults results = new CompilerResults(options.TempFiles);

            AssemblyName assemblyName = new AssemblyName(name + ".exe");
            
            AppDomain thisDomain = Thread.GetDomain();
            AssemblyBuilder assemblyBuilder = thisDomain.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.Save);

            ModuleBuilder moduleBuilder = assemblyBuilder.DefineDynamicModule(assemblyBuilder.GetName().Name, true);

            TypeBuilder typeBuilder = moduleBuilder.DefineType(name + ".Program", TypeAttributes.Public);

            FieldBuilder cellsField = typeBuilder.DefineField("_cells", typeof (byte[]), FieldAttributes.Private | FieldAttributes.Static);
            FieldBuilder activeCellsField = typeBuilder.DefineField("_activeCell", typeof (int), FieldAttributes.Private | FieldAttributes.Static);
            FieldBuilder lastInstruction = typeBuilder.DefineField("_lastInstruction", typeof(int), FieldAttributes.Private | FieldAttributes.Static);
            FieldBuilder lastInstructionChar = typeBuilder.DefineField("_lastInstructionChar", typeof(char), FieldAttributes.Private | FieldAttributes.Static);

            ConstructorBuilder constructor = typeBuilder.DefineConstructor(
            MethodAttributes.Public |
            MethodAttributes.Static |
            MethodAttributes.SpecialName |
            MethodAttributes.RTSpecialName,
            CallingConventions.Standard,
            Type.EmptyTypes);

            ILGenerator generator = constructor.GetILGenerator();

            generator.Emit(OpCodes.Ldc_I4_0);
            generator.Emit(OpCodes.Stsfld, activeCellsField);

            generator.Emit(OpCodes.Ldc_I4_0);
            generator.Emit(OpCodes.Stsfld, lastInstruction);

            generator.Emit(OpCodes.Ldc_I4_0);
            generator.Emit(OpCodes.Stsfld, lastInstructionChar);

            generator.Emit(OpCodes.Ldc_I4, 1000000);
            generator.Emit(OpCodes.Newarr, typeof(byte));
            generator.Emit(OpCodes.Stsfld, cellsField);
            
            generator.Emit(OpCodes.Ret);

            MethodBuilder incrementCellMethodBuilder = typeBuilder.DefineMethod("IncrementCell", MethodAttributes.Public | MethodAttributes.Static, CallingConventions.Standard);
            generator = incrementCellMethodBuilder.GetILGenerator();

            generator.Emit(OpCodes.Ldsfld, cellsField);
            generator.Emit(OpCodes.Ldsfld, activeCellsField);
            generator.Emit(OpCodes.Ldelema, typeof(byte));
            generator.Emit(OpCodes.Dup);
            generator.Emit(OpCodes.Ldobj, typeof(byte));
            generator.Emit(OpCodes.Ldc_I4_1);
            generator.Emit(OpCodes.Add);
            generator.Emit(OpCodes.Conv_U1);
            generator.Emit(OpCodes.Stobj, typeof(byte));

            //generator.Emit(OpCodes.Ret);

            MethodBuilder decrementCellMethodBuilder = typeBuilder.DefineMethod("DecrementCell", MethodAttributes.Public | MethodAttributes.Static, CallingConventions.Standard);
            generator = decrementCellMethodBuilder.GetILGenerator();

            generator.Emit(OpCodes.Ldsfld, cellsField);
            generator.Emit(OpCodes.Ldsfld, activeCellsField);
            generator.Emit(OpCodes.Ldelema, typeof(byte));
            generator.Emit(OpCodes.Dup);
            generator.Emit(OpCodes.Ldobj, typeof(byte));
            generator.Emit(OpCodes.Ldc_I4_1);
            generator.Emit(OpCodes.Sub);
            generator.Emit(OpCodes.Conv_U1);
            generator.Emit(OpCodes.Stobj, typeof(byte));

            MethodBuilder printCellMethodBuilder = typeBuilder.DefineMethod("PrintCell", MethodAttributes.Public | MethodAttributes.Static, CallingConventions.Standard);
            generator = printCellMethodBuilder.GetILGenerator();

            generator.Emit(OpCodes.Ldsfld, cellsField);
            generator.Emit(OpCodes.Ldsfld, activeCellsField);
            generator.Emit(OpCodes.Ldelem, typeof(byte));
            EmitConsoleWriteChar(generator);

            MethodBuilder incrementActiveCellMethodBuilder = typeBuilder.DefineMethod("IncrementActiveCell", MethodAttributes.Public | MethodAttributes.Static, CallingConventions.Standard);
            generator = incrementActiveCellMethodBuilder.GetILGenerator();

            generator.Emit(OpCodes.Ldsfld, activeCellsField);
            generator.Emit(OpCodes.Ldc_I4_1);
            generator.Emit(OpCodes.Add);
            generator.Emit(OpCodes.Stsfld, activeCellsField);

            MethodBuilder decrementActiveCellMethodBuilder = typeBuilder.DefineMethod("DecrementActiveCell", MethodAttributes.Public | MethodAttributes.Static, CallingConventions.Standard);
            generator = decrementActiveCellMethodBuilder.GetILGenerator();
            generator.DeclareLocal(typeof (bool));
            generator.DeclareLocal(typeof (int));

            generator.Emit(OpCodes.Ldsfld, activeCellsField);
            generator.Emit(OpCodes.Ldc_I4_1);
            generator.Emit(OpCodes.Sub);
            generator.Emit(OpCodes.Stsfld, activeCellsField);
            
            MethodBuilder executeMethodBuilder = typeBuilder.DefineMethod("Execute", MethodAttributes.Public | MethodAttributes.Static, CallingConventions.Standard);
            generator = executeMethodBuilder.GetILGenerator();
            
            Stack<Label> loopStarts = new Stack<Label>();
            Stack<Label> loopEnds = new Stack<Label>();
            Stack<int> cellIndex = new Stack<int>();
            Stack<int> boolIndex = new Stack<int>();

            foreach (string source in sources)
            {
                foreach (char c in source)
                {
                    switch (c)
                    {
                        case '>':
                            generator.Emit(OpCodes.Call, incrementActiveCellMethodBuilder);
                            break;
                        case '<':
                            generator.Emit(OpCodes.Call, decrementActiveCellMethodBuilder);
                            break;
                        case '+':
                            generator.Emit(OpCodes.Call, incrementCellMethodBuilder);
                            break;

                        case '-':
                            generator.Emit(OpCodes.Call, decrementCellMethodBuilder);
                            break;

                        case '.':
                            generator.Emit(OpCodes.Call, printCellMethodBuilder);
                            break;
                        case '[':
                            LocalBuilder loopBool = generator.DeclareLocal(typeof(bool));
                            Label loopStart = generator.DefineLabel();
                            Label loopEnd = generator.DefineLabel();
                            boolIndex.Push(loopBool.LocalIndex);
                            loopStarts.Push(loopStart);
                            loopEnds.Push(loopEnd);
                            LocalBuilder local = generator.DeclareLocal(typeof (int));
                            cellIndex.Push(local.LocalIndex);
                            generator.Emit(OpCodes.Ldsfld, activeCellsField);
                            generator.Emit(OpCodes.Stloc, local.LocalIndex);

                            generator.Emit(OpCodes.Br, loopEnd);
                            generator.MarkLabel(loopStart);
                            generator.Emit(OpCodes.Nop);

                            break;
                        case ']':
                            generator.MarkLabel(loopEnds.Pop());
                            
                            generator.Emit(OpCodes.Ldsfld, cellsField);
                            generator.Emit(OpCodes.Ldsfld, activeCellsField);
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
            }
            
            generator.Emit(OpCodes.Ret);

            MethodBuilder mainMethodBuilder = typeBuilder.DefineMethod("Main", MethodAttributes.Public | MethodAttributes.Static, CallingConventions.Standard);
            generator = mainMethodBuilder.GetILGenerator();
            generator.DeclareLocal(typeof (Exception));

            generator.BeginExceptionBlock();
            generator.Emit(OpCodes.Call, executeMethodBuilder);
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

            generator.Emit(OpCodes.Ldsflda, activeCellsField);
            EmitToString(generator, typeof(int));
            EmitConsoleWriteLine(generator);

            generator.Emit(OpCodes.Ldstr, "Last Instruction: ");
            EmitConsoleWriteLine(generator);

            generator.Emit(OpCodes.Ldsflda, lastInstruction);
            EmitToString(generator, typeof(int));
            EmitConsoleWriteLine(generator);

            generator.Emit(OpCodes.Ldsflda, lastInstructionChar);
            EmitToString(generator, typeof(char));
            EmitConsoleWriteLine(generator);

            generator.Emit(OpCodes.Ret);
            generator.EndExceptionBlock();
            generator.Emit(OpCodes.Nop);

            EmitConsoleReadLine(generator);

            typeBuilder.CreateType();

            assemblyBuilder.SetEntryPoint(mainMethodBuilder);
            assemblyBuilder.Save(assemblyName.Name);

            return results;
        }

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
            MethodInfo writeLineMethod = typeof(Console).GetMethod("WriteLine", new []{typeof(string)});
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
    }
}
