using System;
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
        // Ensure the opcode fits in 5 bits
        if (def.op_code > 0b11111)
            throw new ArgumentException("Opcode exceeds 5 bits!");

        // Ensure arg_size is within valid range (1–4)
        if (arg_size < 1 || arg_size > 4)
            throw new ArgumentException("Argument size must be between 1 and 4!");

        // Map arg_size (1–4) to 2-bit binary values: (1 -> 00, 2 -> 01, 3 -> 10, 4 -> 11)
        byte argSizeBits = (byte)((arg_size - 1) << 5);

        // Immediate flag: Convert 'immediate' bool to 1 bit (1 for false, 0 for true)
        byte immediateBit = immediate ? (byte)0 : (byte)(1 << 7);

        // Combine all parts: Immediate Flag | Arg Size | Opcode
        byte result = (byte)(immediateBit | argSizeBits | def.op_code);

        return result; //takes flags and parameters into account
        //return def.op_code; //simple vesion
    }
}
