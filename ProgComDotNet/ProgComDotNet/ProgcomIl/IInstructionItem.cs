using System;
using System.Globalization;

namespace ProgComDotNet.ProgcomIl
{
    interface IInstructionItem
    {
        Type Type { get; }
    }

    class LabelLiteral : IInstructionItem
    {
        private readonly string _label;

        public LabelLiteral(string label)
        {
            _label = label;
        }

        public string Label
        {
            get { return _label; }
        }

        public Type Type { get { return typeof (int); } }

        public override string ToString()
        {
            return _label;
        }
    }

    class NumberLiteral : IInstructionItem
    {
        private readonly int _number;

        public NumberLiteral(int number)
        {
            _number = number;
        }

        public int Number
        {
            get { return _number; }
        }

        public Type Type { get { return typeof(int); } }

        public override string ToString()
        {
            return _number.ToString(CultureInfo.InvariantCulture);
        }
    }

    class ZeroRegister : IInstructionItem
    {
        public Type Type { get { return typeof(int); } }

        public override string ToString()
        {
            return "r0";
        }
    }

    interface IRegister
    {
        
    }

    class AllocatedRegister : IInstructionItem, IRegister
    {
        private readonly Type _type;
        private readonly int _slot;

        public AllocatedRegister(Type type, int slot)
        {
            _type = type;
            _slot = slot;
        }

        public Type Type
        {
            get { return _type; }
        }

        public int Slot
        {
            get { return _slot; }
        }

        public override string ToString()
        {
            return "alcrg[" + _slot + "]";
        }
    }

    class InfiniteRegister : IInstructionItem, IRegister
    {
        private readonly Type _type;
        private readonly int _slot;

        private InfiniteRegister(Type type, int slot)
        {
            _type = type;
            _slot = slot;
        }

        public Type Type
        {
            get { return _type; }
        }

        public int Slot
        {
            get { return _slot; }
        }

        public override string ToString()
        {
            return "infrg[" + _slot + "]";
        }

        public class Generator
        {
            private int _current;

            public InfiniteRegister CreateNew(Type type)
            {
                return new InfiniteRegister(type, ++_current);
            }
        }
    }
}