using System.Collections.Generic;
using ProgComC.Statement;

namespace ProgComC.HighLevelContent
{
    class MethodEmitContext
    {
        private readonly CompilerContext _compilerContext;
        private readonly Method _method;
        private readonly List<string> _allocatedRegisters;
        private readonly Stack<ILine> _currentScope;

        public MethodEmitContext(CompilerContext compilerContext, Method method)
        {
            _compilerContext = compilerContext;
            _method = method;
            _allocatedRegisters = new List<string>();
            _currentScope = new Stack<ILine>();
        }

        public CompilerContext CompilerContext
        {
            get { return _compilerContext; }
        }

        public Method Method
        {
            get { return _method; }
        }

        public void EmitComment(string s)
        {
            _compilerContext.EmitLine(1, "; " + s);
        }

        public void EmitLine(string s)
        {
            _compilerContext.EmitLine(1, s);
        }

        public string AllocateRegister()
        {
            for (var i = 1; i < 13; i++)
            {
                var register = "r" + i;
                if (_allocatedRegisters.Contains(register))
                    continue;
                _allocatedRegisters.Add(register);
                return register;
            }
            for (var i = 0; i < 14; i++)
            {
                var register = "a" + i;
                if (_allocatedRegisters.Contains(register))
                    continue;
                _allocatedRegisters.Add(register);
                return register;
            }
            _compilerContext.AddError(_method.Mark, "Out of registers, expression was probably too big");
            return "invalidRegister";
        }

        public void FreeRegister(string i)
        {
            _allocatedRegisters.Remove(i);
        }

        public void FreeAllRegisters()
        {
            _allocatedRegisters.Clear();
        }

        public int AllocatedRegsiterCount()
        {
            return _allocatedRegisters.Count;
        }

        public StructDefinition Locals { get { return _method.MethodStruct; } }

        public Stack<ILine> CurrentScope
        {
            get { return _currentScope; }
        }
    }
}