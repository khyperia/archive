using ProgComC.Expressions;
using ProgComC.Expressions.BasicValue;
using ProgComC.Parser;
using ProgComC.Statement;
using ProgComC.TypeIdentifier;

namespace ProgComC.HighLevelContent
{
    class GlobalField : IHighLevelContent
    {
        private readonly ITypeIdentifier _type;
        private readonly Identifier _name;
        private readonly IValue _defaultValue;
        public int Offset { get; set; }

        private GlobalField(bool isPublic, bool isConst, string filename, ITypeIdentifier type, Identifier name, IValue defaultValue)
        {
            _type = type;
            _name = name;
            _defaultValue = defaultValue;
            Filename = filename;
            IsPublic = isPublic;
            if (isConst && defaultValue != null)
                ConstValue = DefaultValue.ConstantFold();
        }

        public static GlobalField Parse(CharStream stream)
        {
            var marked = stream.MarkPosition();
            var isPublic = stream.TakeIf("public", "private") == "public";
            stream.TakeWhitespace();
            var isConst = stream.TakeIf("const");
            if (isConst)
                stream.TakeWhitespace();
            var vds = VariableDeclarationStatement.Parse(stream);
            if (vds == null)
            {
                stream.ResetPosition(marked);
                return null;
            }
            var field = new GlobalField(isPublic, isConst, stream.Filename, vds.Type, vds.Variable, vds.Value);
            if (isConst && field.ConstValue == null)
                stream.Error("Constant field '" + vds.Variable + "' not initialized with constant value", false, null);
            return field;
        }

        public void Accept<T>(IHighLevelContentVisitor<T> visitor, T data)
        {
            visitor.Visit(this, data);
        }

        public string Filename { get; private set; }
        public bool IsPublic { get; private set; }

        public ITypeIdentifier Type
        {
            get { return _type; }
        }

        public Identifier Name
        {
            get { return _name; }
        }

        public IValue DefaultValue
        {
            get { return _defaultValue; }
        }

        public object ConstValue { get; private set; }
    }
}
