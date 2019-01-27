using ProgComC.Parser;

namespace ProgComC.Expressions.Operation
{
    internal class GreaterThanOrEqualOperation : ComparisionOperation
    {
        public GreaterThanOrEqualOperation(CharStream.Mark mark, IValue left, IValue right)
            : base(mark, left, right)
        {
        }

        public override T1 Accept<T1, T2>(IValueVisitor<T1, T2> visitor, T2 data)
        {
            return visitor.Visit(this, data);
        }

        protected override string Operator
        {
            get { return ">="; }
        }

        protected override bool ConstantFold(int left, int right)
        {
            return left >= right;
        }
    }
}