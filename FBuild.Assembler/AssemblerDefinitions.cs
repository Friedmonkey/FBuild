using System.Collections.Generic;

namespace FBuild.Assembler;
public class AssemblerDefinitions
{
#if DEBUG
    static AssemblerDefinitions() //static warning about reused names
    {
        Dictionary<string,int> strings = new Dictionary<string,int>();
        foreach (string item in syscalls)
            strings.Add(item.ToUpper(),0);
        foreach (string item in math_modes)
            strings.Add(item.ToUpper(),0);
        foreach (string item in compare_modes)
            strings.Add(item.ToUpper(),0);
        foreach (string item in buffer_modes)
            strings.Add(item.ToUpper(),0);
    }
#endif
    private static byte opcode_index = 0;
    private static KeyValuePair<string, InstructionDefinition> OP(string name, byte argcount)
    {
        return new KeyValuePair<string, InstructionDefinition>(name, new InstructionDefinition(name, opcode_index++, argcount));
    }
    public static IReadOnlyDictionary<string, InstructionDefinition> Instruction_definitions = new Dictionary<string, InstructionDefinition>(new[]
    {
        OP("PUSH",      1),
        OP("POP",       0),
        OP("DUP",       1),
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

        OP("SET_BUFFER",	1),
        OP("GET_BUFFER",	1),
        OP("PUSH_BUFFER",	1),
        OP("BUFFER_UTIL",   1),
        OP("SET_VAR",       0),
        OP("SET_STRUCT",    2),
        OP("GET_STRUCT",    2),
        OP("CREATE_STRUCT", 2),
    });
    public static List<string> syscalls = new List<string>()
    {
        "PAUSE",
        "CLEAR_CONSOLE",
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
    public static List<string> buffer_modes = new List<string>()
    {
        "CLEAR",
        "POP_TO_STACK",
        "PUSH_FROM_STACK",
        "REMOVE_FROM_END",
        "FORMAT",
    };
}
