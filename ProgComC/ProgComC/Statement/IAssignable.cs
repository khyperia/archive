namespace ProgComC.Statement
{
    internal interface IAssignable
    {
        void Accept<T1, T2>(IAssignableVisitor<T1, T2> visitor, T1 assignedValue, T2 data);
    }
}