namespace ProgComC.HighLevelContent
{
    internal class AsmInclude : IHighLevelContent
    {
        private readonly string _filename;

        public AsmInclude(string filename)
        {
            _filename = filename;
        }

        public void Accept<T>(IHighLevelContentVisitor<T> visitor, T data)
        {
            visitor.Visit(this, data);
        }

        public string Filename { get { return _filename; } }
        public bool IsPublic { get { return false; } }
    }
}