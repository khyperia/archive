using ProgComC.Parser;

namespace ProgComC.Expressions.Operation
{
    class MulOperation : BinaryOperation
    {
        public MulOperation(CharStream.Mark mark, IValue left, IValue right)
            : base(mark, left, right)
        {
        }

        public override string Opcode
        {
            get { return "mul"; }
        }

        public override string ImmediateOpcode
        {
            get { return "muli"; }
        }

        public override bool OrderMatters
        {
            get { return false; }
        }
    }
}