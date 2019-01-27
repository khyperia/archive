using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;

namespace ProgComDotNet.ProgcomIl
{
    static class Resolver
    {
        public static void Labels(IList<CilOpcode> body, MethodBase method)
        {
            for (var i = 0; i < body.Count; i++)
            {
                var op = body[i];
                int brTarget;
                if (op.OpCode.OperandType == OperandType.InlineBrTarget)
                    brTarget = (int)op.Argument;
                else if (op.OpCode.OperandType == OperandType.ShortInlineBrTarget)
                    brTarget = (int)(op.Location + (sbyte)(byte)op.Argument + 2); // offset from beginning of NEXT instruction
                else
                    continue;
                var j = 0;
                while (body[j].Location != brTarget)
                    j++;
                var opcode = body[j];
                LabelLiteral label;
                if (opcode.OpCode.Value == OpCodes.Nop.Value && opcode.Argument is LabelLiteral)
                    label = (LabelLiteral)opcode.Argument;
                else
                {
                    label = new LabelLiteral(string.Format("{0}_{1}", method.Label(), brTarget));
                    if (opcode.OpCode.Value == OpCodes.Nop.Value && opcode.Argument == null)
                        body[j] = new CilOpcode(opcode.Location, opcode.OpCode, label);
                    else
                    {
                        body.Insert(j, new CilOpcode(opcode.Location, OpCodes.Nop, label));
                        if (j <= i)
                            i++;
                    }
                }
                body[i] = new CilOpcode(op.Location, op.OpCode, label);
            }
        }
    }
}