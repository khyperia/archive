using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using ProgComC.Expressions.BasicValue;
using ProgComC.HighLevelContent;
using ProgComC.Parser;
using ProgComC.Statement;
using ProgComC.TypeIdentifier;

namespace ProgComC.ProgComBackend
{
    class ProgComHighLevelContentVisitor : IHighLevelContentVisitor<CompilerContext>
    {
        private static ProgComHighLevelContentVisitor _fetch;

        public static ProgComHighLevelContentVisitor Fetch
        {
            get { return _fetch ?? (_fetch = new ProgComHighLevelContentVisitor()); }
        }

        public void Visit(CompilerContext context)
        {
            var globals = context.AccessableHighLevelContents(null).OfType<GlobalField>().ToArray();
            var globalOffset = 0;
            foreach (var global in globals.Where(g => g.ConstValue == null))
            {
                global.Offset = globalOffset;
                globalOffset += global.Type.Size(context);
            }
            if (globalOffset > 0)
            {
                context.EmitLine(0, ".data");
                context.EmitLine(0, "__GLOBALS:");
                context.EmitLine(0, "#allocate " + globalOffset);
                var list = new List<string>();
                foreach (var methodBody in context.AccessableHighLevelContents(null)
                    .OfType<Method>().Select(m => m.MethodBlock).Where(m => m != null))
                    methodBody.Traverse<StringLiteral>(l => list.Add(l.Value));
                foreach (var str in list.Distinct())
                {
                    var hash = (uint)str.GetHashCode();
                    context.EmitLine(0, string.Format("__string{0}:", hash));
                    context.EmitLine(0, string.Join(" ", str
                                                             .Select(c => (int) c)
                                                             .Concat(new[] {0})
                                                             .Select(i => i.ToString(CultureInfo.InvariantCulture))
                                                             .ToArray()));
                }
            }
            context.EmitLine(0, ".text");
            foreach (var content in context.AccessableHighLevelContents(null))
                content.Accept(this, context);
            var outputFilename = context.ParsedFiles[0] + ".txt";
            var emittedText = context.EmittedText;
            if (context.Errors.Count == 0)
                context.FileWriter(outputFilename, emittedText);
        }

        public void Visit(GlobalField node, CompilerContext context)
        {
        }

        public void Visit(AsmInclude node, CompilerContext context)
        {
            context.EmitLine(0, "#include " + node.Filename);
        }

        public void Visit(Method node, CompilerContext context)
        {
            if (node.MethodName.Name == "main")
            {
                if (node.IsExtern)
                {
                    context.AddError(node.Mark, "Main method cannot be extern");
                }
                if (node.ReturnType is VoidTypeIdentifier == false)
                {
                    context.AddError(node.Mark, "Main method must have a 'void' return type");
                }
                if (node.IsPublic == false)
                {
                    context.AddError(node.Mark, "Main method must be declared with 'public'");
                }
            }
            if (node.IsExtern)
            {
                if (node.MethodBlock != null)
                    context.AddError(node.Mark, "Extern methods cannot declare a body");
                return;
            }
            if (node.MethodBlock == null)
            {
                context.AddError(node.Mark, "Methods not marked with extern must declare a body");
                return;
            }
            var duplicateFields = node.MethodStruct.DuplicateFields();
            if (duplicateFields.Length > 0)
            {
                context.AddError(node.Mark, string.Format("Method '{0}' contains duplicate field definitions {1}", node.MethodName.Name, string.Join(", ", duplicateFields)));
            }
            var paramSource = string.Join(", ", node.Parameters.Select(p => p.Key is FunctionPointerType ? p.Key.ToString() : string.Format("{0} {1}", p.Key, p.Value.Source())).ToArray());
            context.EmitLine(0, string.Format("; {0} {1} {2}({3})", node.IsPublic ? "public" : "private", node.ReturnType, node.MethodName.Source(), paramSource));
            var methodUid = node.MethodUid();
            if (node.IsPublic)
                context.EmitLine(0, "#global " + methodUid);
            context.EmitLine(0, methodUid + ":");
            var mec = new MethodEmitContext(context, node);
            if (node.MethodName.Name == "main")
            {
                var isGlobalFields = false;
                foreach (var field in context.AccessableHighLevelContents(null).OfType<GlobalField>().Where(g => g.ConstValue == null))
                {
                    if (isGlobalFields == false)
                    {
                        isGlobalFields = true;
                        mec.EmitComment("Global field initialization");
                    }
                    if (field.DefaultValue == null)
                        return;
                    mec.EmitComment(string.Format("{0} = {1}", field.Name.Source(), field.DefaultValue.Source()));
                    var valueRegister = field.DefaultValue.Accept(ProgComValueVisitor.Fetch, mec);
                    field.Name.EmitAssign(mec, valueRegister, mec.Locals, true);
                    mec.FreeRegister(valueRegister.RegisterName);
                }
                if (isGlobalFields)
                    mec.EmitComment("Finish global field initialization");
            }
            else
            {
                mec.EmitComment("%retptr = r15");
                new Identifier(node.Mark, "%retptr").Accept(ProgComAssignableVisitor.Fetch, new Register("r15", new IntTypeIdentifier()), mec);
            }
            node.MethodBlock.Accept(ProgComLineVisitor.Fetch, mec);
            if (node.MethodBlock.Returns() == false)
            {
                if (node.ReturnType is VoidTypeIdentifier == false)
                    context.AddError(node.Mark, "Method " + node.MethodName.Name + " does not return a value");
                new ReturnStatement(node.Mark, null).Accept(ProgComLineVisitor.Fetch, mec);
            }
            if (mec.AllocatedRegsiterCount() != 0)
                context.AddError(node.Mark, "Internal error: Allocated registers at end of method");
        }

        public void Visit(StructDefinition node, CompilerContext context)
        {
        }
    }
}
