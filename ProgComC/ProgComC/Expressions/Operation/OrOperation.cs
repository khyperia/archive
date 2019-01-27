using ProgComC.Parser;

namespace ProgComC.Expressions.Operation
{
    class OrOperation : BinaryOperation
    {
        public OrOperation(CharStream.Mark mark, IValue left, IValue right)
            : base(mark, left, right)
        {
        }

        public override string Opcode
        {
            get { return "or"; }
        }

        public override string ImmediateOpcode
        {
            get { return "ori"; }
        }

        public override bool OrderMatters
        {
            get { return false; }
        }
    }
}