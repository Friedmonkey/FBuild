global using Arg = System.Int32;
using FBuild.Common;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace FBuild.Assembler;

[DebuggerDisplay("{def.name}:{arg_size}")]
public class Instruction
{
    public Instruction(InstructionDefinition def, List<Arg> args)
    {                                             
        this.def = def;
        this.args = args;
    }
    public InstructionDefinition def;
    public List<Arg> args;
    public IEnumerable<byte> GetBytes()
    {
        yield return def.op_code;

        if (args.Count != def.paramCount)
            throw new Exception("amount of paramters does not match the definition!");

        foreach (var arg in args)
        {
            foreach (byte @byte in arg.VLQ())
                yield return @byte;
        }
    }
}
