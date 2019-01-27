using ProgComC.Parser;

namespace ProgComC.Expressions.Operation
{
    class SubOperation : BinaryOperation
    {
        public SubOperation(CharStream.Mark mark, IValue left, IValue right)
            : base(mark, left, right)
        {
        }

        public override string Opcode
        {
            get { return "sub"; }
        }

        public override string ImmediateOpcode
        {
            get { return "subi"; }
        }

        public override bool OrderMatters
        {
            get { return false; }
        }
    }
}