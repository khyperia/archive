using System;
using System.Linq;
using ProgComC.Expressions;
using ProgComC.Expressions.BasicValue;
using ProgComC.Expressions.Operation;
using ProgComC.HighLevelContent;
using ProgComC.Statement;
using ProgComC.TypeIdentifier;

namespace ProgComC.ProgComBackend
{
    class ProgComValueVisitor : IValueVisitor<Register, MethodEmitContext>
    {
        private static ProgComValueVisitor _fetch;

        public static ProgComValueVisitor Fetch
        {
            get { return _fetch ?? (_fetch = new ProgComValueVisitor()); }
        }

        public Register Visit(BooleanLiteral node, MethodEmitContext context)
        {
            var register = new Register(context.AllocateRegister(), new IntTypeIdentifier());
            context.EmitLine(string.Format("movi {0}, {1}", register.RegisterName, node.Value ? 1 : 0));
            return register;
        }

        public Register Visit(Identifier node, MethodEmitContext context)
        {
            return node.Emit(context, context.Locals);
        }

        public Register Visit(IntegerLiteral node, MethodEmitContext context)
        {
            var register = new Register(context.AllocateRegister(), new IntTypeIdentifier());

            if (Math.Abs(node.Value) <= ushort.MaxValue)
            {
                context.EmitLine(node.Value < 0
                                     ? string.Format("subi {0}, r0, {1}", register.RegisterName, -node.Value)
                                     : string.Format("movi {0}, {1}", register.RegisterName, node.Value));
            }
            else
            {
                var value = (uint)node.Value;
                var hi = value >> 16 & ushort.MaxValue;
                var lo = value & ushort.MaxValue;
                context.EmitLine(string.Format("movhi {0}, {1}", register.RegisterName, hi));
                if (lo != 0)
                    context.EmitLine(string.Format("ori {0}, {0}, {1}", register.RegisterName, lo));
            }
            return register;
        }

        public Register Visit(NullLiteral node, MethodEmitContext context)
        {
            var register = new Register(context.AllocateRegister(), new PointerTypeIdentifier(new VoidTypeIdentifier()));
            context.EmitLine(string.Format("movi {0}, 0", register.RegisterName));
            return register;
        }

        public Register Visit(SizeofOperator node, MethodEmitContext context)
        {
            var register = new Register(context.AllocateRegister(), new IntTypeIdentifier());
            context.EmitLine(string.Format("movi {0}, {1}", register.RegisterName, node.Type.Size(context.CompilerContext)));
            return register;
        }

        public Register Visit(SpecialIdentifier node, MethodEmitContext context)
        {
            var register = new Register(context.AllocateRegister(), new IntTypeIdentifier());
            context.EmitLine(string.Format("rd {0}, r0, {1}", register.RegisterName, node.Name));
            return register;
        }

        public Register Visit(AddressOfOperation node, MethodEmitContext context)
        {
            var variable = node.Value as Identifier;
            if (variable == null)
            {
                var specialId = node.Value as SpecialIdentifier;
                if (specialId == null)
                {
                    context.CompilerContext.AddError(node.Mark, "Cannot take the address of anything other than a variable");
                    return Register.NullRegister;
                }
                var regi = new Register(context.AllocateRegister(), new PointerTypeIdentifier(new IntTypeIdentifier()));
                context.EmitLine(string.Format("movi {0}, {1}", regi.RegisterName, specialId.Name));
                return regi;
            }
            var localIdx = context.Locals.OffsetOf(context.CompilerContext, variable.Name);
            var type = context.Locals.TypeOf(context.CompilerContext, variable.Name);
            if (type == null)
            {
                var method = context.CompilerContext.AccessableHighLevelContents(context.Method.Filename).OfType<Method>().FirstOrDefault(m => m.MethodName.Name == variable.Name);
                if (method == null)
                {
                    context.CompilerContext.AddError(node.Mark, "Variable " + variable.Name + " not found");
                    return Register.NullRegister;
                }
                var regi = new Register(context.AllocateRegister(), new FunctionPointerType(node.Mark, method.ReturnType,
                    new Identifier(node.Mark, method.MethodName.Name.ToLower()), method.Parameters.Select(p => p.Key).ToArray()));
                context.EmitLine(string.Format("movi {0}, {1}", regi.RegisterName, method.MethodName.Name));
                return regi;
            }
            var ati = type as ArrayTypeIdentifier;
            var register = new Register(context.AllocateRegister(), new PointerTypeIdentifier(ati == null ? type : ati.ElementType));
            context.EmitLine(string.Format("addi {0}, r14, {1}", register.RegisterName, localIdx));
            return register;
        }

        public Register Visit(AssignmentOperation node, MethodEmitContext context)
        {
            var target = node.Target as IAssignable;
            if (target == null)
            {
                context.CompilerContext.AddError(node.Mark, "Left side of assignment target must be a variable");
                return Register.NullRegister;
            }
            var valueRegister = node.Value.Accept(this, context);
            target.Accept(ProgComAssignableVisitor.Fetch, valueRegister, context);
            return valueRegister;
        }

        public Register Visit(BinaryOperation node, MethodEmitContext context)
        {
            var leftConst = node.Left.ConstantFold();
            var rightConst = node.Right.ConstantFold();
            var immediateOpcode = node.ImmediateOpcode;
            if (leftConst != null && immediateOpcode != null && node.OrderMatters == false)
            {
                var rightReg = node.Right.Accept(this, context);
                context.EmitLine(string.Format("{0} {1}, {1}, {2}", immediateOpcode, rightReg.RegisterName, leftConst));
                return rightReg;
            }
            if (rightConst != null)
            {
                var leftReg = node.Left.Accept(this, context);
                context.EmitLine(string.Format("{0} {1}, {1}, {2}", immediateOpcode, leftReg.RegisterName, rightConst));
                return leftReg;
            }
            var leftRegi = node.Left.Accept(this, context);
            var rightRegi = node.Right.Accept(this, context);
            if (leftRegi.Type.IsRegisterLiteral() == false || rightRegi.Type.IsRegisterLiteral() == false)
                context.CompilerContext.AddError(node.Mark,
                                                 "Cannot apply operator " + node.Opcode + " to two values of type '" +
                                                 leftRegi.Type + "' and '" + rightRegi.Type + "'");
            context.EmitLine(string.Format("{0} {1}, {1}, {2}", node.Opcode, leftRegi.RegisterName, rightRegi.RegisterName));
            context.FreeRegister(rightRegi.RegisterName);
            return leftRegi;
        }

        public Register Visit(CastOperation node, MethodEmitContext context)
        {
            var register = node.Value.Accept(this, context);
            if (register.Type.Size(context.CompilerContext) != node.Type.Size(context.CompilerContext) || (register.Type.IsRegisterLiteral() && node.Type.IsRegisterLiteral() == false))
            {
                context.CompilerContext.AddError(node.Mark, "Cannot cast from '" + register.Type + "' to '" + node.Type + "'");
                return Register.NullRegister;
            }
            if (register.Type.IsRegisterLiteral() == false && node.Type.IsRegisterLiteral())
                context.EmitLine(string.Format("rd {0}, {0}, 0", register.RegisterName));
            return new Register(register.RegisterName, node.Type);
        }

        public Register Visit(DereferenceOperation node, MethodEmitContext context)
        {
            var value = node.Value.Accept(this, context);
            var pointer = value.Type as PointerTypeIdentifier;
            if (pointer == null)
            {
                context.CompilerContext.AddError(node.Mark, "Cannot dereference a non-pointer value");
                return Register.NullRegister;
            }
            var register = new Register(value.RegisterName, pointer.PointerTo);
            if (register.Type.IsRegisterLiteral())
                context.EmitLine(string.Format("rd {0}, {0}, 0", register.RegisterName));
            return register;
        }

        public Register Visit(DotOperation node, MethodEmitContext context)
        {
            var register = node.Accept(this, context);
            var bti = register.Type as CustomTypeIdentifier;
            if (bti == null)
            {
                context.CompilerContext.AddError(node.Mark, "Cannot access a field of an object of type '" + register.Type + "'");
                return Register.NullRegister;
            }
            var structType = bti.ResolveStruct(context.CompilerContext);
            var fieldType = structType.TypeOf(context.CompilerContext, node.Fieldname.Name);
            var fieldOffset = structType.OffsetOf(context.CompilerContext, node.Fieldname.Name);
            if (fieldType == null)
            {
                context.CompilerContext.AddError(node.Mark, "Field '" + node.Fieldname.Name + "' does not exist in type '" + bti + "'");
                return Register.NullRegister;
            }
            context.EmitLine(fieldType.IsRegisterLiteral()
                                 ? string.Format("rd {0}, {0}, {1}", register.RegisterName, fieldOffset)
                                 : string.Format("add {0}, {0}, {1}", register.RegisterName, fieldOffset));
            register = new Register(register.RegisterName, fieldType);
            return register;
        }

        public Register Visit(EqualityOperation node, MethodEmitContext context)
        {
            var left = node.Left.Accept(this, context);
            var right = node.Right.Accept(this, context);
            if (left.Type.IsRegisterLiteral() == false || right.Type.IsRegisterLiteral() == false)
                context.CompilerContext.AddError(node.Mark,
                                                 "Cannot compare two items of type '" + left.Type + "' and '" +
                                                 right.Type + "'");
            context.EmitLine(string.Format("cmp {0}, {0}, {1}", left.RegisterName, right.RegisterName));
            context.EmitLine(string.Format("andi {0}, {0}, 1", left.RegisterName));
            context.EmitLine(string.Format("xori {0}, {0}, 1", left.RegisterName));
            context.FreeRegister(right.RegisterName);
            return new Register(left.RegisterName, new IntTypeIdentifier());
        }

        public Register Visit(GreaterThanOperation node, MethodEmitContext context)
        {
            var left = node.Left.Accept(this, context);
            var right = node.Right.Accept(this, context);
            if (left.Type.IsRegisterLiteral() == false || right.Type.IsRegisterLiteral() == false)
                context.CompilerContext.AddError(node.Mark,
                                                 "Cannot compare two items of type '" + left.Type + "' and '" +
                                                 right.Type + "'");
            context.EmitLine(string.Format("cmp {0}, {0}, {1}", left.RegisterName, right.RegisterName));
            context.EmitLine(string.Format("movi {0}, 1", right.RegisterName));
            context.EmitLine(string.Format("cmp {0}, {1}, {0}", left.RegisterName, right.RegisterName));
            context.EmitLine(string.Format("xori {0}, {0}, 1", left.RegisterName));
            context.FreeRegister(right.RegisterName);
            return new Register(left.RegisterName, new IntTypeIdentifier());
        }

        public Register Visit(GreaterThanOrEqualOperation node, MethodEmitContext context)
        {
            var left = node.Left.Accept(this, context);
            var right = node.Right.Accept(this, context);
            if (left.Type.IsRegisterLiteral() == false || right.Type.IsRegisterLiteral() == false)
                context.CompilerContext.AddError(node.Mark,
                                                 "Cannot compare two items of type '" + left.Type + "' and '" +
                                                 right.Type + "'");
            context.EmitLine(string.Format("cmp {0}, {1}, {0}", left.RegisterName, right.RegisterName));
            context.EmitLine(string.Format("movi {0}, 1", right.RegisterName));
            context.EmitLine(string.Format("cmp {0}, {1}, {0}", left.RegisterName, right.RegisterName));
            context.FreeRegister(right.RegisterName);
            return new Register(left.RegisterName, new IntTypeIdentifier());
        }

        public Register Visit(IndexerOperation node, MethodEmitContext context)
        {
            var returnRegister = node.GetPointerToElement(this, context);
            if (returnRegister.Type.IsRegisterLiteral())
                context.EmitLine(string.Format("rd {0}, {0}, 0", returnRegister.RegisterName));
            return returnRegister;
        }

        public Register Visit(InequalityOperation node, MethodEmitContext context)
        {
            var left = node.Left.Accept(this, context);
            var right = node.Right.Accept(this, context);
            if (left.Type.IsRegisterLiteral() == false || right.Type.IsRegisterLiteral() == false)
                context.CompilerContext.AddError(node.Mark,
                                                 "Cannot compare two items of type '" + left.Type + "' and '" +
                                                 right.Type + "'");
            context.EmitLine(string.Format("cmp {0}, {0}, {1}", left.RegisterName, right.RegisterName));
            context.EmitLine(string.Format("andi {0}, {0}, 1", left.RegisterName));
            context.FreeRegister(right.RegisterName);
            return new Register(left.RegisterName, new IntTypeIdentifier());
        }

        public Register Visit(LessThanOperation node, MethodEmitContext context)
        {
            var left = node.Left.Accept(this, context);
            var right = node.Right.Accept(this, context);
            if (left.Type.IsRegisterLiteral() == false || right.Type.IsRegisterLiteral() == false)
                context.CompilerContext.AddError(node.Mark,
                                                 "Cannot compare two items of type '" + left.Type + "' and '" +
                                                 right.Type + "'");
            context.EmitLine(string.Format("cmp {0}, {1}, {0}", left.RegisterName, right.RegisterName));
            context.EmitLine(string.Format("movi {0}, 1", right.RegisterName));
            context.EmitLine(string.Format("cmp {0}, {1}, {0}", left.RegisterName, right.RegisterName));
            context.EmitLine(string.Format("xori {0}, {0}, 1", left.RegisterName));
            context.FreeRegister(right.RegisterName);
            return new Register(left.RegisterName, new IntTypeIdentifier());
        }

        public Register Visit(LessThanOrEqualOperation node, MethodEmitContext context)
        {
            var left = node.Left.Accept(this, context);
            var right = node.Right.Accept(this, context);
            if (left.Type.IsRegisterLiteral() == false || right.Type.IsRegisterLiteral() == false)
                context.CompilerContext.AddError(node.Mark,
                                                 "Cannot compare two items of type '" + left.Type + "' and '" +
                                                 right.Type + "'");
            context.EmitLine(string.Format("cmp {0}, {0}, {1}", left.RegisterName, right.RegisterName));
            context.EmitLine(string.Format("movi {0}, 1", right.RegisterName));
            context.EmitLine(string.Format("cmp {0}, {1}, {0}", left.RegisterName, right.RegisterName));
            context.FreeRegister(right.RegisterName);
            return new Register(left.RegisterName, new IntTypeIdentifier());
        }

        public Register Visit(MethodCall node, MethodEmitContext context)
        {
            var register = node.EmitMethod(this, context);
            if (register.Type == null)
                context.CompilerContext.AddError(node.Mark, "Method " + node.MethodName.Source() + " returns void, cannot use it as a value");
            return register;
        }

        public Register Visit(NegationOperation node, MethodEmitContext context)
        {
            var register = node.Value.Accept(this, context);
            if (register.Type.IsRegisterLiteral() == false)
            {
                context.CompilerContext.AddError(node.Mark, "Cannot negate a value of type '" + register.Type + "'");
                return Register.NullRegister;
            }
            context.EmitLine(string.Format("sub {0}, r0, {0}", register.RegisterName));
            return register;
        }

        public Register Visit(NotOperation node, MethodEmitContext context)
        {
            var register = node.Value.Accept(this, context);
            if (register.Type.IsRegisterLiteral() == false)
                context.CompilerContext.AddError(node.Mark, "Cannot apply not operation to type '" + register.Type + "'");
            context.EmitLine(string.Format("cmp {0}, {0}, r0", register.RegisterName));
            context.EmitLine(string.Format("andi {0}, {0}, 1", register.RegisterName));
            return register;
        }

        public Register Visit(StringLiteral node, MethodEmitContext context)
        {
            var register = new Register(context.AllocateRegister(), new PointerTypeIdentifier(new IntTypeIdentifier()));
            context.EmitLine(string.Format("movi {0}, __string{1}", register.RegisterName, (uint)node.Value.GetHashCode()));
            return register;
        }

        public Register Visit(PreIncOperation node, MethodEmitContext context)
        {
            var assignable = node.Variable as IAssignable;
            if (assignable == null)
            {
                context.CompilerContext.AddError(node.Mark, "Value not able to be assigned a value");
                return Register.NullRegister;
            }
            var register = new AddOperation(node.Mark, node.Variable, new IntegerLiteral(node.Mark, 1)).Accept(this, context);
            assignable.Accept(ProgComAssignableVisitor.Fetch, register, context);
            return register;
        }

        public Register Visit(PostIncOperation node, MethodEmitContext context)
        {
            var assignable = node.Variable as IAssignable;
            if (assignable == null)
            {
                context.CompilerContext.AddError(node.Mark, "Value not able to be assigned a value");
                return Register.NullRegister;
            }
            var register = node.Variable.Accept(this, context);
            var returnRegister = new Register(context.AllocateRegister(), register.Type);
            if (register.Type.IsRegisterLiteral() == false)
            {
                context.CompilerContext.AddError(node.Mark, "Cannot apply a post-increment operation to a value of type " + register.Type);
                context.FreeRegister(register.RegisterName);
                return returnRegister;
            }
            context.EmitLine(string.Format("mov {0}, {1}", returnRegister.RegisterName, register.RegisterName));
            context.EmitLine(string.Format("addi {0}, {0}, 1", register.RegisterName));
            assignable.Accept(ProgComAssignableVisitor.Fetch, register, context);
            context.FreeRegister(register.RegisterName);
            return returnRegister;
        }

        public Register Visit(PreDecOperation node, MethodEmitContext context)
        {
            var assignable = node.Variable as IAssignable;
            if (assignable == null)
            {
                context.CompilerContext.AddError(node.Mark, "Value not able to be assigned a value");
                return Register.NullRegister;
            }
            var register = new SubOperation(node.Mark, node.Variable, new IntegerLiteral(node.Mark, 1)).Accept(this, context);
            assignable.Accept(ProgComAssignableVisitor.Fetch, register, context);
            return register;
        }

        public Register Visit(PostDecOperation node, MethodEmitContext context)
        {
            var assignable = node.Variable as IAssignable;
            if (assignable == null)
            {
                context.CompilerContext.AddError(node.Mark, "Value not able to be assigned a value");
                return Register.NullRegister;
            }
            var register = node.Variable.Accept(this, context);
            var returnRegister = new Register(context.AllocateRegister(), register.Type);
            if (register.Type.IsRegisterLiteral() == false)
            {
                context.CompilerContext.AddError(node.Mark, "Cannot apply a post-increment operation to a value of type " + register.Type);
                context.FreeRegister(register.RegisterName);
                return returnRegister;
            }
            context.EmitLine(string.Format("mov {0}, {1}", returnRegister.RegisterName, register.RegisterName));
            context.EmitLine(string.Format("subi {0}, {0}, 1", register.RegisterName));
            assignable.Accept(ProgComAssignableVisitor.Fetch, register, context);
            context.FreeRegister(register.RegisterName);
            return returnRegister;
        }

        public Register Visit(FloatLiteral node, MethodEmitContext context)
        {
            context.CompilerContext.AddError(node.Mark, "Floating point numbers are not valid in ProgCom (yet)");
            return Register.NullRegister;
        }

        public Register Visit(BitwiseInversionOperation node, MethodEmitContext context)
        {
            var register = node.Value.Accept(this, context);
            if (register.Type.IsRegisterLiteral() == false)
                context.CompilerContext.AddError(node.Mark, "Cannot apply not operation to type '" + register.Type + "'");
            context.EmitLine(string.Format("not {0}", register.RegisterName));
            return register;
        }

        public Register Visit(ModOperation node, MethodEmitContext context)
        {
            var leftRegi = node.Left.Accept(this, context);
            var rightRegi = node.Right.Accept(this, context);
            if (leftRegi.Type.IsRegisterLiteral() == false || rightRegi.Type.IsRegisterLiteral() == false)
                context.CompilerContext.AddError(node.Mark,
                                                 "Cannot apply operator modulus to two values of type '" +
                                                 leftRegi.Type + "' and '" + rightRegi.Type + "'");
            context.EmitLine(string.Format("div r0, {0}, {1}", leftRegi.RegisterName, rightRegi.RegisterName));
            context.EmitLine(string.Format("ax {0}, r0", leftRegi.RegisterName));
            context.FreeRegister(rightRegi.RegisterName);
            return leftRegi;
        }
    }
}
