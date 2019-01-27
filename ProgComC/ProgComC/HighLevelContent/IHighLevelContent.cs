namespace ProgComC.HighLevelContent
{
    interface IHighLevelContent
    {
        void Accept<T>(IHighLevelContentVisitor<T> visitor, T data);
        string Filename { get; }
        bool IsPublic { get; }
    }

    interface IHighLevelContentVisitor<in T>
    {
        void Visit(T data);
        void Visit(GlobalField node, T data);
        void Visit(AsmInclude node, T data);
        void Visit(Method node, T data);
        void Visit(StructDefinition node, T data);
    }
}