using System.Reflection.Emit;

namespace ProgComDotNet
{
    struct CilOpcode
    {
        private readonly long _location;
        private readonly OpCode _opCode;
        private object _argument;

        public CilOpcode(long location, OpCode opCode, object argument)
        {
            _location = location;
            _opCode = opCode;
            _argument = argument;
        }

        public long Location
        {
            get { return _location; }
        }

        public OpCode OpCode
        {
            get { return _opCode; }
        }

        public object Argument
        {
            get { return _argument; }
            set { _argument = value; }
        }

        public override string ToString()
        {
            return string.Format("{0}: {1} - {2}", _location, _opCode.Name, _argument);
        }
    }
}