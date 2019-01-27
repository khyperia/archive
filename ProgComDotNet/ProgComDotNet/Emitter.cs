namespace ProgComDotNet
{
/*
    static class Emitter
    {
        public static string Emit(Type type)
        {
            var fields = type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
            var context = new EmitContext(fields);
            context.EmitBaseline(".data");
            context.EmitBaseline("__GLOBALS:");
            context.EmitBaseline(string.Format("#allocate {0}", context.GlobalTotalSize()));
            context.EmitBaseline(".text");
            foreach (var method in type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static))
                EmitMethod(method, context);
            return context.EmittedCode;
        }

        private static void EmitMethod(MethodInfo info, EmitContext context)
        {
            if (info.IsStatic == false)
                throw new NotSupportedException("Cannot compile instance methods");
            var body = info.GetMethodBody();
            var cil = IlReader.Read(body, info.Module).ToList();
            Resolver.Labels(cil, context);
            var uid = info.MethodUid();
            context.CurrentlyEmitting = new MethodDescriber(info);
            context.EmitBaseline("; " + info);
            if (info.IsPublic || uid == "main")
                context.EmitBaseline("#global " + uid);
            context.EmitBaseline(string.Format("{0}:", uid));
            var pcDecl = info.IsProgComDecl();
            if (pcDecl)
            {
                var parameters = context.CurrentlyEmitting.GetParameters();
                foreach (var parameter in parameters)
                    context.PushStack(context.GenerateRegister(parameter.Type));
                foreach (var parameter in parameters.Reverse())
                    EmitStoreArg(parameter.Index, context);
            }
            EmitBody(cil, context);
        }

        private static void EmitBody(IEnumerable<CilOpcode> body, EmitContext context)
        {
            foreach (var opcode in body)
            {
                Console.WriteLine(opcode);
                var op = opcode.OpCode.Value;
                if (EmitMethods.ContainsKey(op) == false)
                    throw new NotSupportedException("Opcode " + opcode.OpCode.Name + " is not implemented");
                var emitMethod = EmitMethods[op];
                context.EmitComment(opcode.ToString());
                emitMethod(opcode.Argument, context);
            }
            context.ClearStack();
        }

        private static void AssertStack(Label label, EmitContext context)
        {
            if (label.StackAtLabel == null)
                label.StackAtLabel = context.Stack;
            else
            {
                if (label.StackAtLabel.SequenceEqual(context.Stack) == false)
                    throw new Exception("Stack equality assertion failed");
            }
        }

        private static readonly Dictionary<short, Action<object, EmitContext>> EmitMethods = new Dictionary<short, Action<object, EmitContext>>
                                                                            {
                                                                                {OpCodes.Nop.Value, EmitNop},
                                                                                {OpCodes.Ldnull.Value, EmitLoadNull},
                                                                                {OpCodes.Ldc_I4.Value, LoadConstantInt},
                                                                                {OpCodes.Ldc_I4_S.Value, (arg, context) => LoadConstantInt((int)(byte)arg, context)},
                                                                                {OpCodes.Ldc_I4_M1.Value, (arg, context) => LoadConstantInt(-1, context)},
                                                                                {OpCodes.Ldc_I4_0.Value, (arg, context) => LoadConstantInt(0, context)},
                                                                                {OpCodes.Ldc_I4_1.Value, (arg, context) => LoadConstantInt(1, context)},
                                                                                {OpCodes.Ldc_I4_2.Value, (arg, context) => LoadConstantInt(2, context)},
                                                                                {OpCodes.Ldc_I4_3.Value, (arg, context) => LoadConstantInt(3, context)},
                                                                                {OpCodes.Ldc_I4_4.Value, (arg, context) => LoadConstantInt(4, context)},
                                                                                {OpCodes.Ldc_I4_5.Value, (arg, context) => LoadConstantInt(5, context)},
                                                                                {OpCodes.Ldc_I4_6.Value, (arg, context) => LoadConstantInt(6, context)},
                                                                                {OpCodes.Ldc_I4_7.Value, (arg, context) => LoadConstantInt(7, context)},
                                                                                {OpCodes.Ldc_I4_8.Value, (arg, context) => LoadConstantInt(8, context)},
                                                                                {OpCodes.Add.Value, EmitAdd},
                                                                                {OpCodes.Sub.Value, EmitSub},
                                                                                {OpCodes.Mul.Value, EmitMul},
                                                                                {OpCodes.Div.Value, EmitDiv},
                                                                                {OpCodes.Rem.Value, EmitRem},
                                                                                {OpCodes.Jmp.Value, EmitJmp},
                                                                                {OpCodes.Call.Value, EmitCall},
                                                                                {OpCodes.Ret.Value, EmitRet},
                                                                                {OpCodes.Br.Value, EmitBr},
                                                                                {OpCodes.Br_S.Value, EmitBr},
                                                                                {OpCodes.Brfalse.Value, EmitBrFalse},
                                                                                {OpCodes.Brfalse_S.Value, EmitBrFalse},
                                                                                {OpCodes.Brtrue.Value, EmitBrTrue},
                                                                                {OpCodes.Brtrue_S.Value, EmitBrTrue},
                                                                                {OpCodes.Ceq.Value, EmitCompareEquals},
                                                                                {OpCodes.Cgt.Value, EmitCompareGreaterThan},
                                                                                {OpCodes.Clt.Value, EmitCompareLessThan},
                                                                                {OpCodes.Ldftn.Value, EmitLdFtn},
                                                                                {OpCodes.Stloc.Value, EmitStoreLocal},
                                                                                {OpCodes.Stloc_S.Value, EmitStoreLocal},
                                                                                {OpCodes.Stloc_0.Value, (arg, context) => EmitStoreLocal(0, context)},
                                                                                {OpCodes.Stloc_1.Value, (arg, context) => EmitStoreLocal(1, context)},
                                                                                {OpCodes.Stloc_2.Value, (arg, context) => EmitStoreLocal(2, context)},
                                                                                {OpCodes.Stloc_3.Value, (arg, context) => EmitStoreLocal(3, context)},
                                                                                {OpCodes.Ldloca.Value, EmitLoadLocalAddress},
                                                                                {OpCodes.Ldloca_S.Value, EmitLoadLocalAddress},
                                                                                {OpCodes.Ldloc.Value, EmitLoadLocal},
                                                                                {OpCodes.Ldloc_S.Value, EmitLoadLocal},
                                                                                {OpCodes.Ldloc_0.Value, (arg, context) => EmitLoadLocal(0, context)},
                                                                                {OpCodes.Ldloc_1.Value, (arg, context) => EmitLoadLocal(1, context)},
                                                                                {OpCodes.Ldloc_2.Value, (arg, context) => EmitLoadLocal(2, context)},
                                                                                {OpCodes.Ldloc_3.Value, (arg, context) => EmitLoadLocal(3, context)},
                                                                                {OpCodes.Starg.Value, EmitStoreArg},
                                                                                {OpCodes.Starg_S.Value, EmitStoreArg},
                                                                                {OpCodes.Ldarga.Value, EmitLoadArgAddress},
                                                                                {OpCodes.Ldarga_S.Value, EmitLoadArgAddress},
                                                                                {OpCodes.Ldarg.Value, EmitLoadArg},
                                                                                {OpCodes.Ldarg_S.Value, EmitLoadArg},
                                                                                {OpCodes.Ldarg_0.Value, (arg, context) => EmitLoadArg(0, context)},
                                                                                {OpCodes.Ldarg_1.Value, (arg, context) => EmitLoadArg(1, context)},
                                                                                {OpCodes.Ldarg_2.Value, (arg, context) => EmitLoadArg(2, context)},
                                                                                {OpCodes.Ldarg_3.Value, (arg, context) => EmitLoadArg(3, context)},
                                                                                {OpCodes.Stsfld.Value, EmitStoreStaticField},
                                                                                {OpCodes.Ldsfld.Value, EmitLoadStaticField},
                                                                                {OpCodes.Ldsflda.Value, EmitLoadStaticFieldAddress},
                                                                                {OpCodes.Ldelem.Value, EmitLoadElement},
                                                                                {OpCodes.Ldelem_I4.Value, (arg, context) => EmitLoadElement(typeof(int), context)},
                                                                                {OpCodes.Stelem.Value, EmitStoreElement},
                                                                                {OpCodes.Stelem_I4.Value, (arg, context) => EmitStoreElement(typeof(int), context)},
                                                                            };

        private static void EmitNop(object argument, EmitContext context)
        {
            var arg = argument;
            if (arg == null)
                return;
            var label = arg as Label;
            if (label != null)
            {
                context.Emit(label.Uid + ":");
                AssertStack(label, context);
            }
        }

        private static void EmitLoadNull(object argument, EmitContext context)
        {
            var register = context.GenerateRegister(typeof(int));
            context.Emit(string.Format("movi {0}, 0", register));
        }

        private static void LoadConstantInt(object argument, EmitContext context)
        {
            var register = context.GenerateRegister(typeof(int));
            var value = (int)argument;
            if (Math.Abs(value) <= ushort.MaxValue)
            {
                context.Emit(value < 0
                                     ? string.Format("subi {0}, r0, {1}", register.RegisterName, -value)
                                     : string.Format("movi {0}, {1}", register.RegisterName, value));
            }
            else
            {
                var uintvalue = (uint)value;
                var hi = uintvalue >> 16 & ushort.MaxValue;
                var lo = uintvalue & ushort.MaxValue;
                context.Emit(string.Format("movhi {0}, {1}", register.RegisterName, hi));
                if (lo != 0)
                    context.Emit(string.Format("ori {0}, {0}, {1}", register.RegisterName, lo));
            }
            context.PushStack(register);
        }

        private static void EmitAdd(object argument, EmitContext context)
        {
            var right = context.PopStack();
            var left = context.PopStack();
            context.Emit(string.Format("add {0}, {0}, {1}", left.RegisterName, right.RegisterName));
            context.PushStack(left);
        }

        private static void EmitSub(object argument, EmitContext context)
        {
            var right = context.PopStack();
            var left = context.PopStack();
            context.Emit(string.Format("sub {0}, {0}, {1}", left.RegisterName, right.RegisterName));
            context.PushStack(left);
        }

        private static void EmitMul(object argument, EmitContext context)
        {
            var right = context.PopStack();
            var left = context.PopStack();
            context.Emit(string.Format("mul {0}, {0}, {1}", left.RegisterName, right.RegisterName));
            context.PushStack(left);
        }

        private static void EmitDiv(object argument, EmitContext context)
        {
            var right = context.PopStack();
            var left = context.PopStack();
            context.Emit(string.Format("div {0}, {0}, {1}", left.RegisterName, right.RegisterName));
            context.PushStack(left);
        }

        private static void EmitRem(object argument, EmitContext context)
        {
            var right = context.PopStack();
            var left = context.PopStack();
            context.Emit(string.Format("div r0, {0}, {1}", left.RegisterName, right.RegisterName));
            context.Emit(string.Format("ax {0}, r0", left.RegisterName));
            context.PushStack(left);
        }

        private static void EmitJmp(object argument, EmitContext context)
        {
            EmitCall(argument, context);
            EmitRet(null, context);
        }

        private static void EmitCall(object argument, EmitContext context)
        {
            var callee = new MethodDescriber((MethodInfo)argument);
            var methodName = callee.Method.MethodUid();
            var totalSize = context.CurrentlyEmitting.TotalSize();
            var pcDecl = callee.Method.IsProgComDecl();
            var isStatic = callee.Method.IsStatic;
            context.Emit(string.Format("addi r14, r14, {0}", totalSize));
            foreach (var parameter in callee.GetParameters().Reverse())
            {
                if (pcDecl)
                    context.PopStack();
                else
                    context.Emit(string.Format("wr {0}, r14, {1}", context.PopStack().RegisterName, parameter.Offset));
            }
            if (pcDecl && (isStatic ? context.Stack.Count != 0 : context.Stack.Count != 1))
                throw new Exception("Calling progcomdecl method had extra items on stack after call");
            if (isStatic)
                context.Emit(string.Format("call {0}", methodName));
            else
            {
                var obj = context.PopStack();
                if (typeof (Delegate).IsAssignableFrom(obj.Type))
                {
                    context.Emit(string.Format("callr {0}", obj.RegisterName));
                }
                else
                    throw new NotSupportedException("Calling instance methods is not supported");
            }
            if (callee.Method.ReturnType != typeof(void))
            {
                var returnRegister = context.GenerateRegister(callee.Method.ReturnType);
                if (pcDecl == false)
                    context.Emit(string.Format("rd {0}, r14, {1}", returnRegister.RegisterName, callee.OffsetOfReturnValue()));
                context.PushStack(returnRegister);
            }
            context.Emit(string.Format("subi r14, r14, {0}", totalSize));
        }

        private static void EmitRet(object argument, EmitContext context)
        {
            if (context.CurrentlyEmitting.Method.IsMainMethod())
            {
                context.Emit("halt");
                return;
            }
            if (context.CurrentlyEmitting.Method.ReturnType != typeof(void))
            {
                if (context.CurrentlyEmitting.Method.ReturnType != typeof(int))
                    throw new NotSupportedException("Types other than int are not supported");
                var register = context.PopStack();
                if (context.CurrentlyEmitting.Method.IsProgComDecl())
                {
                    if (context.Stack.Count != 0)
                        throw new Exception("ProgComDecl method had extra items on stack on return");
                }
                else
                    context.Emit(string.Format("wr {0}, r14, {1}", register.RegisterName, context.CurrentlyEmitting.OffsetOfReturnValue()));
            }
            var returnRegister = context.GenerateRegister(typeof(int));
            context.Emit(string.Format("rd {0}, r14, {1}", returnRegister.RegisterName, context.CurrentlyEmitting.OffsetOfReturnPointer()));
            context.Emit(string.Format("jmpr {0}", returnRegister.RegisterName));
            context.ClearStack();
        }

        private static void EmitBr(object argument, EmitContext context)
        {
            var label = (Label)argument;
            context.Emit(string.Format("jmp {0}", label.Uid));
            AssertStack(label, context);
        }

        private static void EmitBrFalse(object argument, EmitContext context)
        {
            var label = (Label)argument;
            var register = context.PopStack();
            context.Emit(string.Format("beq {0}, r0, {1}", register.RegisterName, label.Uid));
            AssertStack(label, context);
        }

        private static void EmitBrTrue(object argument, EmitContext context)
        {
            var label = (Label)argument;
            var register = context.PopStack();
            context.Emit(string.Format("bi {0}, {1}", register.RegisterName, label.Uid));
            AssertStack(label, context);
        }

        private static void EmitCompareEquals(object argument, EmitContext context)
        {
            var right = context.PopStack();
            var left = context.PopStack();
            context.Emit(string.Format("cmp {0}, {0}, {1}", left.RegisterName, right.RegisterName));
            context.Emit(string.Format("andi {0}, {0}, 1", left.RegisterName));
            context.Emit(string.Format("xori {0}, {0}, 1", left.RegisterName));
            context.PushStack(new Register(left.RegisterName, typeof(bool)));
        }

        private static void EmitCompareGreaterThan(object argument, EmitContext context)
        {
            var right = context.PopStack();
            var left = context.PopStack();
            context.Emit(string.Format("cmp {0}, {1}, {0}", left.RegisterName, right.RegisterName));
            context.Emit(string.Format("movi {0}, 1", right.RegisterName));
            context.Emit(string.Format("cmp {0}, {1}, {0}", left.RegisterName, right.RegisterName));
            context.PushStack(new Register(left.RegisterName, typeof(bool)));
        }

        private static void EmitCompareLessThan(object argument, EmitContext context)
        {
            var right = context.PopStack();
            var left = context.PopStack();
            context.Emit(string.Format("cmp {0}, {1}, {0}", left.RegisterName, right.RegisterName));
            context.Emit(string.Format("movi {0}, 1", right.RegisterName));
            context.Emit(string.Format("cmp {0}, {1}, {0}", left.RegisterName, right.RegisterName));
            context.Emit(string.Format("xori {0}, {0}, 1", left.RegisterName));
            context.PushStack(new Register(left.RegisterName, typeof(bool)));
        }

        private static void EmitLdFtn(object argument, EmitContext context)
        {
            var method = (MethodInfo) argument;
            var register = context.GenerateRegister(typeof(IntPtr));
            context.Emit(string.Format("movi {0}, {1}", register, method.MethodUid()));
            context.PushStack(register);
        }

        private static void EmitStoreLocal(object argument, EmitContext context)
        {
            var local = context.CurrentlyEmitting.GetLocal(Convert.ToInt32(argument));
            var register = context.PopStack();
            context.Emit(string.Format("wr {0}, r14, {1}", register.RegisterName, local.Offset));
        }

        private static void EmitLoadLocal(object argument, EmitContext context)
        {
            var local = context.CurrentlyEmitting.GetLocal(Convert.ToInt32(argument));
            var register = context.GenerateRegister(local.Type);
            context.Emit(string.Format("rd {0}, r14, {1}", register.RegisterName, local.Offset));
            context.PushStack(register);
        }

        private static void EmitLoadLocalAddress(object argument, EmitContext context)
        {
            var local = context.CurrentlyEmitting.GetLocal(Convert.ToInt32(argument));
            var register = context.GenerateRegister(local.Type);
            context.Emit(string.Format("addi {0}, r14, {1}", register.RegisterName, local.Offset));
            context.PushStack(register);
        }

        private static void EmitStoreArg(object argument, EmitContext context)
        {
            var parameter = context.CurrentlyEmitting.GetParameter(Convert.ToInt32(argument));
            var register = context.PopStack();
            context.Emit(string.Format("wr {0}, r14, {1}", register.RegisterName, parameter.Offset));
        }

        private static void EmitLoadArg(object argument, EmitContext context)
        {
            var parameter = context.CurrentlyEmitting.GetParameter(Convert.ToInt32(argument));
            var register = context.GenerateRegister(parameter.Type);
            context.Emit(string.Format("rd {0}, r14, {1}", register.RegisterName, parameter.Offset));
            context.PushStack(register);
        }

        private static void EmitLoadArgAddress(object argument, EmitContext context)
        {
            var parameter = context.CurrentlyEmitting.GetParameter(Convert.ToInt32(argument));
            var register = context.GenerateRegister(parameter.Type);
            context.Emit(string.Format("addi {0}, r14, {1}", register.RegisterName, parameter.Offset));
            context.PushStack(register);
        }

        private static void EmitStoreStaticField(object argument, EmitContext context)
        {
            var field = (FieldInfo)argument;
            var tempRegister = context.GenerateRegister(typeof(int));
            var register = context.PopStack();
            context.Emit(string.Format("movi {0}, {1}", tempRegister.RegisterName, context.OffsetOfGlobal(field)));
            context.Emit(string.Format("wr {0}, {1}, __GLOBALS", register.RegisterName, tempRegister.RegisterName));
        }

        private static void EmitLoadStaticField(object argument, EmitContext context)
        {
            var field = (FieldInfo)argument;
            var register = context.GenerateRegister(field.FieldType);
            context.PushStack(register);
            var isArray = field.FieldType.IsArray;
            if (isArray == false && field.IsInitOnly && field.Name.All(c => char.IsUpper(c) || c == '_'))
                context.Emit(string.Format("movi {0}, {1}", register.RegisterName, field.Name));
            else if (isArray && field.Name.ToLower() == "pcmem")
                context.Emit(string.Format("movi {0}, 0", register.RegisterName));
            else
            {
                context.Emit(string.Format("movi {0}, {1}", register.RegisterName, context.OffsetOfGlobal(field)));
                if (isArray)
                    context.Emit(string.Format("addi {0}, {0}, __GLOBALS", register.RegisterName));
                else
                    context.Emit(string.Format("rd {0}, {0}, __GLOBALS", register.RegisterName));
            }
        }

        private static void EmitLoadStaticFieldAddress(object argument, EmitContext context)
        {
            var field = (FieldInfo)argument;
            var register = context.GenerateRegister(field.FieldType);
            context.PushStack(register);
            if (field.FieldType.IsArray && field.Name.ToLower() == "pcmem")
                context.Emit(string.Format("movi {0}, 0", register.RegisterName));
            else
            {
                context.Emit(string.Format("movi {0}, {1}", register.RegisterName, context.OffsetOfGlobal(field)));
                context.Emit(string.Format("addi {0}, {0}, __GLOBALS", register.RegisterName));
            }
        }

        private static void EmitLoadElement(object argument, EmitContext context)
        {
            var type = (Type)argument;
            var typeSize = TypeSizer.SizeofType(type);
            var index = context.PopStack();
            var array = context.PopStack();
            if (typeSize == 0)
                throw new Exception("Cannot index a type of size 0");
            if (typeSize != 1)
                context.Emit(string.Format("muli {0}, {0}, {1}", index.RegisterName, typeSize));
            context.Emit(string.Format("add {0}, {0}, {1}", array.RegisterName, index.RegisterName));
            context.Emit(string.Format("rd {0}, {0}, 0", array.RegisterName));
            context.PushStack(new Register(array.RegisterName, type));
        }

        private static void EmitStoreElement(object argument, EmitContext context)
        {
            var type = (Type)argument;
            var typeSize = TypeSizer.SizeofType(type);
            var index = context.PopStack();
            var array = context.PopStack();
            var value = context.PopStack();
            if (typeSize == 0)
                throw new Exception("Cannot index a type of size 0");
            if (typeSize != 1)
                context.Emit(string.Format("muli {0}, {0}, {1}", index.RegisterName, typeSize));
            context.Emit(string.Format("add {0}, {0}, {1}", array.RegisterName, index.RegisterName));
            context.Emit(string.Format("wr {0}, {1}, 0", value.RegisterName, array.RegisterName));
        }
    }
*/
}
