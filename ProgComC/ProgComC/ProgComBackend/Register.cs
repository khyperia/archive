using ProgComC.TypeIdentifier;

namespace ProgComC.ProgComBackend
{
    internal struct Register
    {
        public static readonly Register NullRegister = new Register("invalidRegister", new VoidTypeIdentifier());

        private readonly string _registerName;
        private readonly ITypeIdentifier _type;

        public Register(string registerName, ITypeIdentifier type)
        {
            _registerName = registerName;
            _type = type;
        }

        public string RegisterName
        {
            get { return _registerName; }
        }

        public ITypeIdentifier Type
        {
            get { return _type; }
        }
    }
}