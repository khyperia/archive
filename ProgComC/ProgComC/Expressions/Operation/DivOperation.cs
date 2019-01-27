using ProgComC.Parser;

namespace ProgComC.Expressions.Operation
{
    class DivOperation : BinaryOperation
    {
        public DivOperation(CharStream.Mark mark, IValue left, IValue right)
            : base(mark, left, right)
        {
        }

        public override string Opcode
        {
            get { return "div"; }
        }

        public override string ImmediateOpcode
        {
            get { return "divi"; }
        }

        public override bool OrderMatters
        {
            get { return false; }
        }
    }
}