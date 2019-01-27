using ProgComC.Parser;

namespace ProgComC.Expressions.Operation
{
    internal class ShiftLeftOperation : BinaryOperation
    {
        public ShiftLeftOperation(CharStream.Mark mark, IValue left, IValue right)
            : base(mark, left, right)
        {
        }

        public override string Opcode
        {
            get { return "shl"; }
        }

        public override string ImmediateOpcode
        {
            get { return "sli"; }
        }

        public override bool OrderMatters
        {
            get { return true; }
        }
    }
}