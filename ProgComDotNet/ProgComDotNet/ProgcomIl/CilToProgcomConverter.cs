using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

namespace ProgComDotNet.ProgcomIl
{
    static class CilToProgcomConverter
    {
        class Context
        {
            private readonly IInstructionItem _returnLocationRegister;
            private readonly AllocatedRegister _returnValueRegister;
            private readonly IList<IInstructionItem> _parameterRegisters;
            private readonly IList<IInstructionItem> _localRegisters;
            private readonly Stack<IInstructionItem> _evalStack;
            private readonly InfiniteRegister.Generator _registerGenerator;

            public Context(MethodInfo method)
            {
                _evalStack = new Stack<IInstructionItem>();
                _registerGenerator = new InfiniteRegister.Generator();
                _returnValueRegister = method.ReturnType == typeof(void) ? null : new AllocatedRegister(method.ReturnType, 0);
                _returnLocationRegister = _registerGenerator.CreateNew(typeof(int));
                _parameterRegisters = method.GetParameters().Select((p, i) => new AllocatedRegister(p.ParameterType, i)).Cast<IInstructionItem>().ToList();
                var methodBody = method.GetMethodBody();
                if (methodBody != null)
                    _localRegisters = methodBody.LocalVariables.Select(l => _registerGenerator.CreateNew(l.LocalType)).Cast<IInstructionItem>().ToList();
            }

            public Stack<IInstructionItem> EvalStack
            {
                get { return _evalStack; }
            }

            public IInstructionItem ReturnLocationRegister
            {
                get { return _returnLocationRegister; }
            }

            public IInstructionItem ReturnValueRegister
            {
                get { return _returnValueRegister; }
            }

            public IList<IInstructionItem> ParameterRegisters
            {
                get { return _parameterRegisters; }
            }

            public IList<IInstructionItem> LocalRegisters
            {
                get { return _localRegisters; }
            }

            public IInstructionItem Pop()
            {
                return _evalStack.Pop();
            }

            public void Push(IInstructionItem item)
            {
                _evalStack.Push(item);
            }

            public IInstructionItem GenerateRegister(Type type)
            {
                return _registerGenerator.CreateNew(type);
            }
        }

        public static IEnumerable<Instruction> ConvertMethod(MethodInfo method)
        {
            var context = new Context(method);
            var opcodes = IlReader.Read(method.GetMethodBody(), method.Module).ToList();
            Resolver.Labels(opcodes, method);
            var returnHeader = new[] { new Instruction(PcOpcode.Mov, context.ReturnLocationRegister, new LabelLiteral("ra")) };
            var translated = opcodes.SelectMany(op =>
                                                {
                                                    if (Translators.ContainsKey(op.OpCode.Value) == false)
                                                        throw new CompilerException("Opcode " + op.OpCode + " not supported!");
                                                    return Translators[op.OpCode.Value](op.Argument, context);
                                                });
            return returnHeader.Concat(translated);
        }

        private static readonly Dictionary<short, Func<object, Context, IEnumerable<Instruction>>> Translators =
            new Dictionary<short, Func<object, Context, IEnumerable<Instruction>>>
            {
                {OpCodes.Nop.Value, Nop},
                {OpCodes.Ldc_I4.Value, LdcI4},
                {OpCodes.Ldloc.Value, Ldloc},
                {OpCodes.Stloc.Value, Stloc},
                {OpCodes.Ldarg.Value, Ldarg},
                {OpCodes.Starg.Value, Starg},
                {OpCodes.Stsfld.Value, Stsfld},
                {OpCodes.Ldsfld.Value, Ldsfld},
                {OpCodes.Add.Value, Add},
                {OpCodes.Sub.Value, Sub},
                {OpCodes.Mul.Value, Mul},
                {OpCodes.Div.Value, Div},
                {OpCodes.Rem.Value, Rem},
                {OpCodes.Shl.Value, Shl},
                {OpCodes.Shr.Value, Shr},
                {OpCodes.And.Value, And},
                {OpCodes.Or.Value, Or},
                {OpCodes.Xor.Value, Xor},
                {OpCodes.Ret.Value, Ret},
                
                {OpCodes.Ldloc_0.Value, (o, c) => Ldloc(0, c)},
                {OpCodes.Ldloc_1.Value, (o, c) => Ldloc(1, c)},
                {OpCodes.Ldloc_2.Value, (o, c) => Ldloc(2, c)},
                {OpCodes.Ldloc_3.Value, (o, c) => Ldloc(3, c)},
                {OpCodes.Ldloc_S.Value, Ldloc},
                {OpCodes.Stloc_0.Value, (o, c) => Stloc(0, c)},
                {OpCodes.Stloc_1.Value, (o, c) => Stloc(1, c)},
                {OpCodes.Stloc_2.Value, (o, c) => Stloc(2, c)},
                {OpCodes.Stloc_3.Value, (o, c) => Stloc(3, c)},
                {OpCodes.Stloc_S.Value, Stloc},
                
                {OpCodes.Ldarg_0.Value, (o, c) => Ldarg(0, c)},
                {OpCodes.Ldarg_1.Value, (o, c) => Ldarg(1, c)},
                {OpCodes.Ldarg_2.Value, (o, c) => Ldarg(2, c)},
                {OpCodes.Ldarg_3.Value, (o, c) => Ldarg(3, c)},
                {OpCodes.Ldarg_S.Value, Ldarg},
                {OpCodes.Starg_S.Value, Starg},

                {OpCodes.Ldc_I4_S.Value, LdcI4},
                {OpCodes.Ldc_I4_0.Value, (o, c) => LdcI4(0, c)},
                {OpCodes.Ldc_I4_1.Value, (o, c) => LdcI4(1, c)},
                {OpCodes.Ldc_I4_2.Value, (o, c) => LdcI4(2, c)},
                {OpCodes.Ldc_I4_3.Value, (o, c) => LdcI4(3, c)},
                {OpCodes.Ldc_I4_4.Value, (o, c) => LdcI4(4, c)},
                {OpCodes.Ldc_I4_5.Value, (o, c) => LdcI4(5, c)},
                {OpCodes.Ldc_I4_6.Value, (o, c) => LdcI4(6, c)},
                {OpCodes.Ldc_I4_7.Value, (o, c) => LdcI4(7, c)},
                {OpCodes.Ldc_I4_8.Value, (o, c) => LdcI4(8, c)},
                {OpCodes.Ldc_I4_M1.Value, (o, c) => LdcI4(-1, c)},
            };

        private static IEnumerable<Instruction> Nop(object argument, Context context)
        {
            if (argument != null)
            {
                yield return new Instruction(PcOpcode.Label, (LabelLiteral)argument);
            }
        }

        private static IEnumerable<Instruction> Ldloc(object argument, Context context)
        {
            context.Push(context.LocalRegisters[Convert.ToInt32(argument)]);
            yield break;
        }

        private static IEnumerable<Instruction> Stloc(object argument, Context context)
        {
            yield return new Instruction(PcOpcode.Mov, context.LocalRegisters[Convert.ToInt32(argument)], context.Pop());
        }

        private static IEnumerable<Instruction> Ldarg(object argument, Context context)
        {
            context.Push(context.ParameterRegisters[Convert.ToInt32(argument)]);
            yield break;
        }

        private static IEnumerable<Instruction> Starg(object argument, Context context)
        {
            yield return new Instruction(PcOpcode.Mov, context.ParameterRegisters[Convert.ToInt32(argument)], context.Pop());
        }

        private static IEnumerable<Instruction> Stsfld(object argument, Context context)
        {
            throw new CompilerException("Static fields are not supported");
        }

        private static IEnumerable<Instruction> Ldsfld(object argument, Context context)
        {
            throw new CompilerException("Static fields are not supported");
        }

        private static IEnumerable<Instruction> Ret(object argument, Context context)
        {
            if (context.ReturnValueRegister != null)
                yield return new Instruction(PcOpcode.Mov, context.ReturnValueRegister, context.Pop());
            yield return new Instruction(PcOpcode.Jmpr, context.ReturnLocationRegister);
        }

        private static IEnumerable<Instruction> LdcI4(object argument, Context context)
        {
            var register = context.GenerateRegister(typeof(int));
            var value = Convert.ToInt32(argument);
            if (Math.Abs(value) <= ushort.MaxValue)
            {
                if (value < 0)
                    yield return new Instruction(PcOpcode.Subi, register, new ZeroRegister(), new NumberLiteral(-value));
                else
                    yield return new Instruction(PcOpcode.Movi, register, new NumberLiteral(value));
            }
            else
            {
                var uintvalue = (uint)value;
                var hi = (int)(uintvalue >> 16 & ushort.MaxValue);
                var lo = (int)(uintvalue & ushort.MaxValue);
                yield return new Instruction(PcOpcode.Movhi, register, new NumberLiteral(hi));
                if (lo != 0)
                    yield return new Instruction(PcOpcode.Ori, register, register, new NumberLiteral(lo));
            }
            context.Push(register);
        }

        private static IEnumerable<Instruction> BinOp(Context context, PcOpcode normal, PcOpcode immediate, bool orderMatters)
        {
            var right = context.Pop();
            var left = context.Pop();
            if (left is IRegister == false && right is IRegister == false)
            {
                var register = context.GenerateRegister(typeof(int));
                yield return new Instruction(PcOpcode.Movi, register, left);
                context.Push(register);
                context.Push(right);
                foreach (var instruction in BinOp(context, normal, immediate, orderMatters))
                    yield return instruction;
            }
            else if (left is IRegister == false)
            {
                if (orderMatters)
                {
                    var register = context.GenerateRegister(typeof(int));
                    yield return new Instruction(PcOpcode.Movi, register, left);
                    context.Push(register);
                    context.Push(right);
                    foreach (var instruction in BinOp(context, normal, immediate, true))
                        yield return instruction;
                }
                else
                    yield return new Instruction(immediate, right, left);
                context.Push(right);
            }
            else if (right is IRegister == false)
            {
                yield return new Instruction(immediate, left, right);
                context.Push(left);
            }
            else
            {
                yield return new Instruction(normal, left, right);
                context.Push(left);
            }
        }

        private static IEnumerable<Instruction> Add(object argument, Context context)
        {
            return BinOp(context, PcOpcode.Add, PcOpcode.Addi, false);
        }

        private static IEnumerable<Instruction> Sub(object argument, Context context)
        {
            return BinOp(context, PcOpcode.Sub, PcOpcode.Subi, true);
        }

        private static IEnumerable<Instruction> Mul(object argument, Context context)
        {
            return BinOp(context, PcOpcode.Mul, PcOpcode.Muli, false);
        }

        private static IEnumerable<Instruction> Div(object argument, Context context)
        {
            return BinOp(context, PcOpcode.Div, PcOpcode.Divi, true);
        }

        private static IEnumerable<Instruction> Rem(object argument, Context context)
        {
            var right = context.Pop();
            var left = context.Pop();
            yield return new Instruction(PcOpcode.Div, new ZeroRegister(), left, right);
            yield return new Instruction(PcOpcode.Ax, left, new ZeroRegister());
            context.Push(left);
        }

        private static IEnumerable<Instruction> Shl(object argument, Context context)
        {
            return BinOp(context, PcOpcode.Shl, PcOpcode.Sli, true);
        }

        private static IEnumerable<Instruction> Shr(object argument, Context context)
        {
            return BinOp(context, PcOpcode.Shr, PcOpcode.Sri, true);
        }

        private static IEnumerable<Instruction> And(object argument, Context context)
        {
            return BinOp(context, PcOpcode.And, PcOpcode.Andi, false);
        }

        private static IEnumerable<Instruction> Or(object argument, Context context)
        {
            return BinOp(context, PcOpcode.Or, PcOpcode.Ori, false);
        }

        private static IEnumerable<Instruction> Xor(object argument, Context context)
        {
            return BinOp(context, PcOpcode.Xor, PcOpcode.Xori, false);
        }
    }
}
