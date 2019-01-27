using System.Linq;
using ProgComC.Expressions;
using ProgComC.Expressions.BasicValue;
using ProgComC.Expressions.Operation;
using ProgComC.HighLevelContent;
using ProgComC.Statement;
using ProgComC.TypeIdentifier;

namespace ProgComC.ProgComBackend
{
    class ProgComLineVisitor : ILineVisitor<MethodEmitContext>
    {
        private static ILineVisitor<MethodEmitContext> _fetch;

        public static ILineVisitor<MethodEmitContext> Fetch
        {
            get { return _fetch ?? (_fetch = new ProgComLineVisitor()); }
        }

        public void Visit(Block node, MethodEmitContext context)
        {
            context.CurrentScope.Push(node);
            context.EmitComment("{");
            foreach (var line in node.Lines)
            {
                line.Accept(this, context);
                if (context.AllocatedRegsiterCount() == 0)
                    continue;
                context.FreeAllRegisters();
                context.CompilerContext.AddError(line.Mark, "Line had remaining registers allocated");
            }
            context.EmitComment("}");
            context.CurrentScope.Pop();
        }

        public void Visit(BreakStatement node, MethodEmitContext context)
        {
            var whileStatement = context.CurrentScope.OfType<WhileStatement>().FirstOrDefault();
            if (whileStatement == null)
            {
                context.CompilerContext.AddError(node.Mark, "Break statement not valid, no enclosing while loop");
                return;
            }
            context.EmitComment("break");
            context.EmitLine(string.Format("jmp {0}", whileStatement.GetEndLabel(context.CompilerContext)));
        }

        public void Visit(ContinueStatement node, MethodEmitContext context)
        {
            var whileStatement = context.CurrentScope.OfType<WhileStatement>().FirstOrDefault();
            if (whileStatement == null)
            {
                context.CompilerContext.AddError(node.Mark, "Continue statement not valid, no enclosing while loop");
                return;
            }
            context.EmitComment("continue");
            context.EmitLine(string.Format("jmp {0}", whileStatement.ConditionLabel));
        }

        public void Visit(IfStatement node, MethodEmitContext context)
        {
            context.CurrentScope.Push(node);
            var constantObj = node.Value.ConstantFold();
            if (constantObj is int)
            {
                var constant = (int)constantObj;
                if (constant == 0)
                {
                    if (node.IfFalse != null)
                        node.IfFalse.Accept(this, context);
                }
                else
                    node.IfTrue.Accept(this, context);
            }
            else
            {
                context.EmitComment(string.Format("if ({0})", node.Value.Source()));
                var register = node.Value.Accept(ProgComValueVisitor.Fetch, context);
                if (node.IfFalse == null)
                {
                    var zeroRegister = context.AllocateRegister();
                    var endLabel = context.CompilerContext.GenerateLabel();
                    context.EmitLine(string.Format("movi {0}, 0", zeroRegister));
                    context.EmitLine(string.Format("beq {0}, {1}, {2}", register.RegisterName, zeroRegister, endLabel));
                    context.FreeRegister(zeroRegister);
                    context.FreeRegister(register.RegisterName);
                    node.IfTrue.Accept(this, context);
                    context.EmitLine(string.Format("{0}:", endLabel));
                }
                else
                {
                    var endlabelRequired = node.IfFalse.Returns() == false;
                    var middleLabel = context.CompilerContext.GenerateLabel();
                    var endLabel = endlabelRequired ? context.CompilerContext.GenerateLabel() : null;
                    context.EmitLine(string.Format("bi {0}, {1}", register.RegisterName, middleLabel));
                    context.FreeRegister(register.RegisterName);
                    context.EmitComment("else");
                    node.IfFalse.Accept(this, context);
                    if (endlabelRequired)
                        context.EmitLine(string.Format("jmp {0}", endLabel));
                    context.EmitComment("then");
                    context.EmitLine(string.Format("{0}:", middleLabel));
                    node.IfTrue.Accept(this, context);
                    if (endlabelRequired)
                        context.EmitLine(string.Format("{0}:", endLabel));
                }
            }
            context.CurrentScope.Pop();
        }

        public void Visit(InlineAsmStatement node, MethodEmitContext context)
        {
            context.EmitComment("Begin inline asm");
            context.EmitLine(node.Asm);
            context.EmitComment("End inline asm");
        }

        public void Visit(ReturnStatement node, MethodEmitContext context)
        {
            context.EmitComment(node.Value == null ? "return" : string.Format("return {0}", node.Value.Source()));
            if (context.Method.MethodName.Name == "main")
            {
                context.EmitLine("halt");
                return;
            }
            if (node.Value != null)
            {
                var returnValue = node.Value.Accept(ProgComValueVisitor.Fetch, context);
                new Identifier(node.Mark, "%retval").Accept(ProgComAssignableVisitor.Fetch, returnValue, context);
                context.FreeRegister(returnValue.RegisterName);
            }
            var gotoLocation = new Identifier(node.Mark, "%retptr").Accept(ProgComValueVisitor.Fetch, context);
            context.EmitLine(string.Format("jmpr {0}", gotoLocation.RegisterName));
            context.FreeRegister(gotoLocation.RegisterName);
        }

        public void Visit(VariableDeclarationStatement node, MethodEmitContext context)
        {
            if (node.Type is FunctionPointerType)
                context.EmitComment(string.Format("{0}", node.Type));
            else
                context.EmitComment(string.Format("{0} {1}", node.Type, node.Variable.Source()));
            if (node.Value != null)
                new AssignmentOperation(node.Mark, node.Variable, node.Value).Accept(this, context);
        }

        public void Visit(VoidStatement node, MethodEmitContext context)
        {
        }

        public void Visit(WhileStatement node, MethodEmitContext context)
        {
            context.CurrentScope.Push(node);
            var constantObj = node.Value.ConstantFold();
            if (constantObj is int)
            {
                var constant = (int)constantObj;
                if (constant != 0)
                {
                    context.EmitComment("while (true)");
                    var label = context.CompilerContext.GenerateLabel();
                    context.EmitLine(string.Format("{0}:", label));
                    node.BodyContents.Accept(this, context);
                    context.EmitLine(string.Format("jmp {0}", label));
                }
            }
            else
            {
                var beginLabel = context.CompilerContext.GenerateLabel();
                node.ConditionLabel = context.CompilerContext.GenerateLabel();
                context.EmitComment("(goto while condition)");
                context.EmitLine(string.Format("jmp {0}", node.ConditionLabel));
                context.EmitLine(string.Format("{0}:", beginLabel));
                node.BodyContents.Accept(this, context);
                context.EmitComment(string.Format("while ({0})", node.Value.Source()));
                context.EmitLine(string.Format("{0}:", node.ConditionLabel));
                var valueRegister = node.Value.Accept(ProgComValueVisitor.Fetch, context);
                context.EmitLine(string.Format("bi {0}, {1}", valueRegister.RegisterName, beginLabel));
                context.FreeRegister(valueRegister.RegisterName);
                if (node.EndLabel != null)
                    context.EmitLine(string.Format("{0}:", node.EndLabel));
            }
            context.CurrentScope.Pop();
        }

        public void Visit(AssignmentOperation node, MethodEmitContext context)
        {
            context.EmitComment(string.Format("{0} = {1}", node.Target.Source(), node.Value.Source()));
            var valueRegister = ((IValue)node).Accept(ProgComValueVisitor.Fetch, context);
            context.FreeRegister(valueRegister.RegisterName);
        }

        public void Visit(MethodCall node, MethodEmitContext context)
        {
            context.EmitComment(node.Source());
            var register = node.Accept(ProgComValueVisitor.Fetch, context);
            if (register.Type != null && register.Type is VoidTypeIdentifier == false)
                context.FreeRegister(register.RegisterName);
        }

        public void Visit(PostDecOperation node, MethodEmitContext context)
        {
            context.EmitComment(node.Source());
            context.FreeRegister(node.Accept(ProgComValueVisitor.Fetch, context).RegisterName);
        }

        public void Visit(PreDecOperation node, MethodEmitContext context)
        {
            context.EmitComment(node.Source());
            context.FreeRegister(node.Accept(ProgComValueVisitor.Fetch, context).RegisterName);
        }

        public void Visit(PostIncOperation node, MethodEmitContext context)
        {
            context.EmitComment(node.Source());
            context.FreeRegister(node.Accept(ProgComValueVisitor.Fetch, context).RegisterName);
        }

        public void Visit(PreIncOperation node, MethodEmitContext context)
        {
            context.EmitComment(node.Source());
            context.FreeRegister(node.Accept(ProgComValueVisitor.Fetch, context).RegisterName);
        }

        public void Visit(ForStatement node, MethodEmitContext context)
        {
            context.EmitComment(string.Format("for ({0} {1}; {2})",
                node.Init == null ? "" : node.Init.Source().Replace(System.Environment.NewLine, ""),
                node.Condition == null ? "" : node.Condition.Source(),
                node.Increment == null ? "" : node.Increment.Source()));
            if (node.Init != null)
                node.Init.Accept(this, context);
            var conditionLabel = context.CompilerContext.GenerateLabel();
            context.EmitLine(string.Format("jmp {0}", conditionLabel));
            var beginLabel = context.CompilerContext.GenerateLabel();
            context.EmitLine(string.Format("{0}:", beginLabel));
            node.Block.Accept(this, context);
            if (node.Increment != null)
            {
                if (node.Increment is ILine == false)
                {
                    context.CompilerContext.AddError(node.Increment.Mark, "Line is not a valid statement");
                }
                context.EmitComment(node.Increment.Source());
                var incrementResult = node.Increment.Accept(ProgComValueVisitor.Fetch, context);
                context.FreeRegister(incrementResult.RegisterName);
            }
            context.EmitLine(string.Format("{0}:", conditionLabel));
            if (node.Condition != null)
            {
                context.EmitComment(node.Condition.Source());
                var conditionResult = node.Condition.Accept(ProgComValueVisitor.Fetch, context);
                context.EmitLine(string.Format("bi {0}, {1}", conditionResult.RegisterName, beginLabel));
                context.FreeRegister(conditionResult.RegisterName);
            }
        }
    }
}
