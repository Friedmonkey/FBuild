using System.Diagnostics;

namespace FBuild.Assembler;

[DebuggerDisplay("{name}:{op_code}")]
public class InstructionDefinition
{
    public InstructionDefinition(string name, byte op_code, byte paramCount)
    {
        this.name = name;
        this.op_code = op_code;
        this.paramCount = paramCount;
    }
    public string name;
    public byte op_code;
    public byte paramCount;
}
