using System.Diagnostics;

namespace FBuild.Assembler;

[DebuggerDisplay("{def.name}:{arg_size}")]
public class Instruction
{
    public Instruction(InstructionDefinition def, byte arg_size, bool immediate, byte[] arguments)
    {                                             
        this.def = def;
        this.arg_size = arg_size;
        this.immediate = immediate;
        this.arguments = arguments;
    }
    public InstructionDefinition def;
    public byte arg_size; // Argument size (0–3)
    public bool immediate;   // Immediate flag
    public byte[] arguments;
    public byte GetByte()
    {
        //generate its byte
        return def.op_code;
    }
}
