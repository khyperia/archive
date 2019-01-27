using System.Linq;
using ProgComC.Expressions.BasicValue;
using ProgComC.Expressions.Operation;
using ProgComC.HighLevelContent;
using ProgComC.TypeIdentifier;

namespace ProgComC.ProgComBackend
{
    static class ProgComBackendHelper
    {
        public static string MethodUid(this Method method)
        {
            return method.MethodName.Name == "main" ? "main" :
                string.Format("{0}{1}", method.MethodName.Name, method.Parameters.Select(n => (uint)n.Key.ToString().GetHashCode()).Aggregate(0U, (total, part) => total + part));
        }

        public static Register EmitMethod(this MethodCall node, ProgComValueVisitor visitor, MethodEmitContext context)
        {
            var paramRegs = node.Parameters.Select(p => p.Accept(visitor, context)).ToArray();
            var methodNameId = node.MethodName as Identifier;
            var method = methodNameId == null
                             ? null
                             : context.CompilerContext
                                   .AccessableHighLevelContents(context.Method.Filename)
                                   .OfType<Method>()
                                   .LastOrDefault(m => m.MethodName.Name == methodNameId.Name &&
                                                       m.ParametersEqual(paramRegs));
            var isFuncPtr = false;
            var functionPointer = Register.NullRegister;
            if (method == null)
            {
                isFuncPtr = true;
                functionPointer = node.MethodName.Accept(visitor, context);
                var fpt = functionPointer.Type as FunctionPointerType;
                if (fpt == null)
                {
                    context.CompilerContext.AddError(node.Mark, "Method " + node.MethodName.Source() + " not found");
                    return Register.NullRegister;
                }
                method = fpt.Method;
            }
            var localsSize = context.Locals.Size(context.CompilerContext);
            var callingMethodStruct = method.MethodStruct;
            context.EmitLine(string.Format("addi r14, r14, {0}", localsSize));
            for (var i = 0; i < paramRegs.Length; i++)
                new Identifier(node.Mark, method.Parameters[i].Value.Name).EmitAssign(context, paramRegs[i], callingMethodStruct);
            foreach (var register in paramRegs)
                context.FreeRegister(register.RegisterName);
            if (isFuncPtr)
            {
                context.EmitLine(string.Format("callr {0}", functionPointer.RegisterName));
                context.FreeRegister(functionPointer.RegisterName);
            }
            else
                context.EmitLine(string.Format("call {0}", method.MethodUid()));
            var returnRegister = method.ReturnType is VoidTypeIdentifier ? Register.NullRegister : new Identifier(node.Mark, "%retval").Emit(context, callingMethodStruct);
            context.EmitLine(string.Format("subi r14, r14, {0}", localsSize));
            return returnRegister;
        }

        public static Register GetPointerToElement(this IndexerOperation node, ProgComValueVisitor visitor, MethodEmitContext context)
        {
            var array = node.Array.Accept(visitor, context);
            var arrtype = array.Type as PointerTypeIdentifier;
            var stackArrType = array.Type as ArrayTypeIdentifier;
            int typesize;
            if (arrtype == null)
            {
                if (stackArrType == null)
                {
                    context.CompilerContext.AddError(node.Mark, "Cannot apply an indexer to a non-pointer type '" + array.Type + "'");
                    return Register.NullRegister;
                }
                typesize = stackArrType.ElementType.Size(context.CompilerContext);
            }
            else
            {
                typesize = arrtype.PointerTo.Size(context.CompilerContext);
            }
            if (typesize == 0)
            {
                context.CompilerContext.AddError(node.Mark, "Cannot index a type with element size of zero, '" + array.Type + "'");
                return Register.NullRegister;
            }
            var constantIndex = node.Index.ConstantFold();
            if (constantIndex is int)
            {
                var constIndex = (int)constantIndex;
                constIndex *= typesize;
                context.EmitLine(string.Format("addi {0}, {0}, {1}", array.RegisterName, constIndex));
            }
            else
            {
                var index = node.Index.Accept(visitor, context);
                if (typesize != 1)
                    context.EmitLine(string.Format("muli {0}, {0}, {1}", index.RegisterName, typesize));
                context.EmitLine(string.Format("add {0}, {0}, {1}", array.RegisterName, index.RegisterName));
                context.FreeRegister(index.RegisterName);
            }
            return new Register(array.RegisterName, arrtype == null ? stackArrType.ElementType : arrtype.PointerTo);
        }

        private static bool ParametersEqual(this Method method, Register[] registers)
        {
            var mp = method.Parameters;
            if (mp.Count != registers.Length)
                return false;
            for (var i = 0; i < mp.Count; i++)
            {
                var left = mp[i].Key;
                var right = registers[i].Type;
                if (left.Equals(right) == false)
                    return false;
            }
            return true;
        }

        private static void Resolve(this Identifier identifier, MethodEmitContext context, StructDefinition locals, bool ignoreGlobalAccessModifiers, out bool onStack, out int index, out ITypeIdentifier type, out object constantValue)
        {
            onStack = true;
            index = locals.OffsetOf(context.CompilerContext, identifier.Name);
            if (index == -1)
            {
                onStack = false;
                var globalField = context.CompilerContext.AccessableHighLevelContents(ignoreGlobalAccessModifiers ? null : context.Method.Filename)
                    .OfType<GlobalField>().FirstOrDefault(g => g.Name.Name == identifier.Name);
                if (globalField == null)
                {
                    context.CompilerContext.AddError(identifier.Mark, "Unknown variable name " + identifier.Name);
                    onStack = true;
                    index = -1;
                    type = Register.NullRegister.Type;
                    constantValue = null;
                    return;
                }
                index = globalField.Offset;
                type = globalField.Type;
                constantValue = globalField.ConstValue;
            }
            else
            {
                constantValue = null;
                type = locals.TypeOf(context.CompilerContext, identifier.Name);
            }
        }

        public static Register Emit(this Identifier identifier, MethodEmitContext context, StructDefinition locals)
        {
            bool onStack;
            int index;
            ITypeIdentifier type;
            object constantValue;
            identifier.Resolve(context, locals, false, out onStack, out index, out type, out constantValue);
            Register register;
            if (constantValue is int)
            {
                register = new IntegerLiteral(identifier.Mark, (int)constantValue).Accept(ProgComValueVisitor.Fetch, context);
            }
            else if (type.IsRegisterLiteral())
            {
                register = new Register(context.AllocateRegister(), type);
                if (onStack)
                    context.EmitLine(string.Format("rd {0}, r14, {1}", register.RegisterName, index));
                else
                {
                    context.EmitLine(string.Format("movi {0}, __GLOBALS", register.RegisterName));
                    context.EmitLine(string.Format("rd {0}, {0}, {1}", register.RegisterName, index));
                }
            }
            else
            {
                register = new Register(context.AllocateRegister(), type);
                if (onStack)
                    context.EmitLine(string.Format("addi {0}, r14, {1}", register.RegisterName, index));
                else
                {
                    context.EmitLine(string.Format("movi {0}, __GLOBALS", register.RegisterName));
                    context.EmitLine(string.Format("addi {0}, {0}, {1}", register.RegisterName, index));
                }
            }
            return register;
        }

        public static void EmitAssign(this Identifier identifier, MethodEmitContext context, Register assignedValue, StructDefinition locals)
        {
            identifier.EmitAssign(context, assignedValue, locals, false);
        }

        public static void EmitAssign(this Identifier identifier, MethodEmitContext context, Register assignedValue, StructDefinition locals, bool ignoreGlobalAccessModifiers)
        {
            bool onStack;
            int index;
            ITypeIdentifier type;
            object constantValue;
            identifier.Resolve(context, locals, ignoreGlobalAccessModifiers, out onStack, out index, out type, out constantValue);
            if (type.Size(context.CompilerContext) != assignedValue.Type.Size(context.CompilerContext))
            {
                context.CompilerContext.AddError(identifier.Mark, "Cannot assign a value of type " + assignedValue.Type + " to " + type);
                return;
            }
            if (constantValue != null)
            {
                context.CompilerContext.AddError(identifier.Mark, "Cannot assign a constant field");
                return;
            }
            if (assignedValue.Type.IsRegisterLiteral())
            {
                if (onStack)
                    context.EmitLine(string.Format("wr {0}, r14, {1}", assignedValue.RegisterName, index));
                else
                {
                    var tempRegister = context.AllocateRegister();
                    context.EmitLine(string.Format("movi {0}, __GLOBALS", tempRegister));
                    context.EmitLine(string.Format("wr {0}, {1}, {2}", assignedValue.RegisterName, tempRegister, index));
                    context.FreeRegister(tempRegister);
                }
            }
            else
            {
                var tempLocal = context.AllocateRegister();
                var size = type.Size(context.CompilerContext);
                var tempGlobalRegister = onStack ? Register.NullRegister.RegisterName : context.AllocateRegister();
                if (onStack)
                    context.EmitLine(string.Format("movi {0}, __GLOBALS", tempGlobalRegister));
                for (var i = 0; i < size; i++)
                {
                    context.EmitLine(string.Format("rd {0}, {1}, {2}", tempLocal, assignedValue.RegisterName, i));
                    context.EmitLine(string.Format("wr {0}, {1}, {2}", tempLocal, onStack ? "r14" : tempGlobalRegister, index + i));
                }
                context.FreeRegister(tempLocal);
                if (onStack)
                    context.FreeRegister(tempGlobalRegister);
            }
        }
    }
}