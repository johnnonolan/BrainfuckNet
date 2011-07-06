using System;
using System.CodeDom.Compiler;

namespace BrainfuckNet
{
    public class BrainfuckCodeProvider : CodeDomProvider
    {
        private BrainfuckCodeGenerator _codeGenerator = new BrainfuckCodeGenerator();

        /// <summary>
        /// When overridden in a derived class, creates a new code generator.
        /// </summary>
        /// <returns>
        /// An <see cref="T:System.CodeDom.Compiler.ICodeGenerator"/> that can be used to generate <see cref="N:System.CodeDom"/> based source code representations.
        /// </returns>
        [Obsolete("Callers should not use the ICodeGenerator interface and should instead use the methods directly on the CodeDomProvider class. Those inheriting from CodeDomProvider must still implement this interface, and should exclude this warning or also obsolete this method.")]
        public override ICodeGenerator CreateGenerator()
        {
            return null;
        }

        /// <summary>
        /// When overridden in a derived class, creates a new code compiler. 
        /// </summary>
        /// <returns>
        /// An <see cref="T:System.CodeDom.Compiler.ICodeCompiler"/> that can be used for compilation of <see cref="N:System.CodeDom"/> based source code representations. 
        /// </returns>
        //[Obsolete("Callers should not use the ICodeCompiler interface and should instead use the methods directly on the CodeDomProvider class. Those inheriting from CodeDomProvider must still implement this interface, and should exclude this warning or also obsolete this method.")]
        public override ICodeCompiler CreateCompiler()
        {
            return _codeGenerator;
        }
    }
}
