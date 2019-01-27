using ProgComC.Parser;

namespace ProgComC.Expressions.Operation
{
    internal class ShiftRightOperation : BinaryOperation
    {
        public ShiftRightOperation(CharStream.Mark mark, IValue left, IValue right)
            : base(mark, left, right)
        {
        }

        public override string Opcode
        {
            get { return "shr"; }
        }

        public override string ImmediateOpcode
        {
            get { return "sri"; }
        }

        public override bool OrderMatters
        {
            get { return true; }
        }
    }
}