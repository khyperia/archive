using ProgComC.Parser;

namespace ProgComC.Expressions.Operation
{
    class XorOperation : BinaryOperation
    {
        public XorOperation(CharStream.Mark mark, IValue left, IValue right)
            : base(mark, left, right)
        {
        }

        public override string Opcode
        {
            get { return "xor"; }
        }

        public override string ImmediateOpcode
        {
            get { return "xori"; }
        }

        public override bool OrderMatters
        {
            get { return false; }
        }
    }
}