using ProgComC.Expressions.BasicValue;
using ProgComC.Expressions.Operation;
using ProgComC.HighLevelContent;
using ProgComC.Statement;
using ProgComC.TypeIdentifier;

namespace ProgComC.ProgComBackend
{
    class ProgComAssignableVisitor : IAssignableVisitor<Register, MethodEmitContext>
    {
        private static ProgComAssignableVisitor _fetch;

        public static ProgComAssignableVisitor Fetch
        {
            get { return _fetch ?? (_fetch = new ProgComAssignableVisitor()); }
        }

        public void Visit(DereferenceOperation node, Register assignedValue, MethodEmitContext context)
        {
            var value = node.Value.Accept(ProgComValueVisitor.Fetch, context);
            if (assignedValue.Type.IsRegisterLiteral())
                context.EmitLine(string.Format("wr {0}, {1}, 0", assignedValue.RegisterName, value.RegisterName));
            else
            {
                var tempLocal = context.AllocateRegister();
                var size = value.Type.Size(context.CompilerContext);
                for (var i = 0; i < size; i++)
                {
                    context.EmitLine(string.Format("rd {0}, {1}, {2}", tempLocal, assignedValue.RegisterName, i));
                    context.EmitLine(string.Format("wr {0}, {1}, {2}", tempLocal, value.RegisterName, i));
                }
                context.FreeRegister(tempLocal);
            }
            context.FreeRegister(value.RegisterName);
        }

        public void Visit(DotOperation node, Register assignedValue, MethodEmitContext context)
        {
            var register = node.Obj.Accept(ProgComValueVisitor.Fetch, context);
            var bti = register.Type as CustomTypeIdentifier;
            if (bti == null)
            {
                context.CompilerContext.AddError(node.Mark, "Cannot access a field of an object of type '" + register.Type + "'");
                return;
            }
            var structType = bti.ResolveStruct(context.CompilerContext);
            var fieldType = structType.TypeOf(context.CompilerContext, node.Fieldname.Name);
            var fieldOffset = structType.OffsetOf(context.CompilerContext, node.Fieldname.Name);
            if (fieldType == null)
            {
                context.CompilerContext.AddError(node.Mark, "Field '" + node.Fieldname.Name + "' does not exist in type '" + bti + "'");
                return;
            }
            if (assignedValue.Type.Size(context.CompilerContext) != fieldType.Size(context.CompilerContext))
            {
                context.CompilerContext.AddError(node.Mark, "Cannot assign a value of type '" + assignedValue.Type + "' to a variable of type '" + fieldType + "'");
                return;
            }
            if (assignedValue.Type.IsRegisterLiteral())
                context.EmitLine(string.Format("wr {0}, {1}, {2}", assignedValue.RegisterName, register.RegisterName, fieldOffset));
            else
            {
                var size = register.Type.Size(context.CompilerContext);
                var tempRegister = context.AllocateRegister();
                for (var i = 0; i < size; i++)
                {
                    context.EmitLine(string.Format("rd {0}, {1}, {2}", tempRegister, register.RegisterName, fieldOffset + i));
                    context.EmitLine(string.Format("wr {0}, {1}, {2}", tempRegister, assignedValue.RegisterName, fieldOffset + i));
                }
                context.FreeRegister(tempRegister);
            }
            context.FreeRegister(register.RegisterName);
        }

        public void Visit(Identifier node, Register assignedValue, MethodEmitContext context)
        {
            node.EmitAssign(context, assignedValue, context.Locals);
        }

        public void Visit(IndexerOperation node, Register assignedValue, MethodEmitContext context)
        {
            var pointerToElement = node.GetPointerToElement(ProgComValueVisitor.Fetch, context);
            if (assignedValue.Type.IsRegisterLiteral())
                context.EmitLine(string.Format("wr {0}, {1}, 0", assignedValue.RegisterName, pointerToElement.RegisterName));
            else
            {
                var tempLocal = context.AllocateRegister();
                var size = pointerToElement.Type.Size(context.CompilerContext);
                for (var i = 0; i < size; i++)
                {
                    context.EmitLine(string.Format("rd {0}, {1}, {2}", tempLocal, assignedValue.RegisterName, i));
                    context.EmitLine(string.Format("wr {0}, {1}, {2}", tempLocal, pointerToElement.RegisterName, i));
                }
                context.FreeRegister(tempLocal);
            }
            context.FreeRegister(pointerToElement.RegisterName);
        }

        public void Visit(SpecialIdentifier node, Register assignedValue, MethodEmitContext context)
        {
            context.EmitLine(string.Format("wr {0}, r0, {1}", assignedValue.RegisterName, node.Name));
        }
    }
}
