using ProgComC.Parser;

namespace ProgComC.Expressions.Operation
{
    class AddOperation : BinaryOperation
    {
        public AddOperation(CharStream.Mark mark, IValue left, IValue right)
            : base(mark, left, right)
        {
        }

        public override string Opcode
        {
            get { return "add"; }
        }

        public override string ImmediateOpcode
        {
            get { return "addi"; }
        }

        public override bool OrderMatters
        {
            get { return false; }
        }
    }
}