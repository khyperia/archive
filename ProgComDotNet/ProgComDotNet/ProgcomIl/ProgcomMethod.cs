namespace ProgComDotNet.ProgcomIl
{
    struct Instruction
    {
        private readonly PcOpcode _opcode;
        private readonly IInstructionItem _first;
        private readonly IInstructionItem _second;
        private readonly IInstructionItem _third;

        public Instruction(PcOpcode opcode)
        {
            _opcode = opcode;
            _first = null;
            _second = null;
            _third = null;
        }

        public Instruction(PcOpcode opcode, IInstructionItem first)
        {
            _opcode = opcode;
            _first = first;
            _second = null;
            _third = null;
        }

        public Instruction(PcOpcode opcode, IInstructionItem first, IInstructionItem second)
        {
            _opcode = opcode;
            _first = first;
            _second = second;
            _third = null;
        }

        public Instruction(PcOpcode opcode, IInstructionItem first, IInstructionItem second, IInstructionItem third)
        {
            _opcode = opcode;
            _first = first;
            _second = second;
            _third = third;
        }

        public PcOpcode Opcode
        {
            get { return _opcode; }
        }

        public IInstructionItem First
        {
            get { return _first; }
        }

        public IInstructionItem Second
        {
            get { return _second; }
        }

        public IInstructionItem Third
        {
            get { return _third; }
        }

        public override string ToString()
        {
            if (_third != null)
                return string.Format("{0} {1} {2} {3}", _opcode.ToString().ToLower(), _first, _second, _third);
            if (_second != null)
                return string.Format("{0} {1} {2}", _opcode.ToString().ToLower(), _first, _second);
            if (_first != null)
                return string.Format("{0} {1}", _opcode.ToString().ToLower(), _first);
            return string.Format("{0}", _opcode.ToString().ToLower());
        }
    }

    enum PcOpcode
    {
        Label,
        Add,
        Addi,
        Sub,
        Subi,
        Mul,
        Muli,
        Div,
        Divi,
        Mov,
        Movi,
        Movil,
        Movhi,
        Shl,
        Sli,
        Shr,
        Sri,
        And,
        Andi,
        Or,
        Ori,
        Xor,
        Xori,
        Not,
        Ax,
        Sx,
        Br,
        Brr,
        Jmp,
        Jmpr,
        Beq,
        Bi,
        Bne,
        Bl,
        Ble,
        Call,
        Callr,
        Bx,
        Rd,
        Rdx,
        Wr,
        Push,
        Pop,
        Halt,
        Nop,
        Cmp,
        Flcmp,
        Int,
        Eret
    }
}