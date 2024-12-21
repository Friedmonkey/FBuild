using System.Collections.Generic;

namespace FBuild.Assembler;
public class AssemblerDefinitions
{
    private static byte opcode_index = 0;
    private static KeyValuePair<string, InstructionDefinition> OP(string name, byte argcount)
    {
        return new KeyValuePair<string, InstructionDefinition>(name, new InstructionDefinition(name, opcode_index++, argcount));
    }
    public static IReadOnlyDictionary<string, InstructionDefinition> Instruction_definitions = new Dictionary<string, InstructionDefinition>(new[]
    {
        OP("PUSH",      1),
        OP("POP",       0),
        OP("DUP",       0),
        OP("MATH",      1),
        OP("AND",       0),
        OP("OR",        0),
        OP("NOT",       0),
        OP("COMP",      1),
        OP("JUMP",      1),
        OP("JUMP_IF",   2),
        OP("CALL",      1),
        OP("CALL_IF",   2),
        OP("RET",       0),
        OP("SYSCALL",   1),
        OP("EXIT",      0),

        OP("VAR",       1),
        OP("POP_TO_VAR",1),
        OP("VAR_MOV",   2),
        OP("VAR_PUSH",  1),
        OP("VAR_POP",   1),
        OP("STRUCT_SET",2),
        OP("STRUCT_GET",2),
    });
    public static List<string> syscalls = new List<string>()
    {
        "PAUSE",
        "CLEAR",
        "READ",
        "PRINT",
        "DUMP",
    };
    public static List<string> math_modes = new List<string>()
    {
        "ADD",
        "SUB",
        "INC",
        "DEC",
        "MUL",
        "DIV",
        "POW",
        "ROOT",
        "SQRT",
    };
    public static List<string> compare_modes = new List<string>()
    {
        "GT",
        "GTE",
        "LT",
        "LTE",
        "EQ",
        "NEQ",
    };
}
