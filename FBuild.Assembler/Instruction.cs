using System.Diagnostics;

namespace FBuild.Assembler;

[DebuggerDisplay("{def.name}:{arg_size}")]
public class Instruction
{
    public Instruction(InstructionDefinition def, byte arg_size, bool immediate)
    {                                             
        this.def = def;
        this.arg_size = arg_size;
        this.immediate = immediate;
    }
    public InstructionDefinition def;
    public byte arg_size; // Argument size (0–3)
    public bool immediate;   // Immediate flag
    public byte GetByte()
    {
        //generate its byte
        return 0;
    }
}
