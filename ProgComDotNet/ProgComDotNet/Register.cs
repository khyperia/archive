using System;

namespace ProgComDotNet
{
    struct Register : IEquatable<Register>
    {
        private readonly string _registerName;
        private readonly Type _type;

        public Register(string registerName, Type type)
        {
            _registerName = registerName;
            _type = type;
        }

        public string RegisterName
        {
            get { return _registerName; }
        }

        public Type Type
        {
            get { return _type; }
        }

        public bool Equals(Register other)
        {
            return _registerName == other._registerName && _type == other._type;
        }
    }
}