using ProgComC.Parser;

namespace ProgComC.Expressions.Operation
{
    class AndOperation : BinaryOperation
    {
        public AndOperation(CharStream.Mark mark, IValue left, IValue right)
            : base(mark, left, right)
        {
        }

        public override string Opcode
        {
            get { return "and"; }
        }

        public override string ImmediateOpcode
        {
            get { return "andi"; }
        }

        public override bool OrderMatters
        {
            get { return false; }
        }
    }
}