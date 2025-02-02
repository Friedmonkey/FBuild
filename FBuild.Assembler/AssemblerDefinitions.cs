using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Reflection.Metadata;
using System.Runtime.InteropServices;

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
    public static List<Type> Types = new List<Type>()
    { 
        new Type("raw",             [0x00]),
        new Type("string",          [0x01]),

        new Type("uint8",           [0x10], "char",   "byte", "u8"),
        new Type("uint16",          [0x12], "ushort", "u16"),
        new Type("uint32",          [0x13], "uint",   "u32"),
        new Type("uint64",          [0x14], "ulong",  "u64"),
        new Type("int8",            [0x15], "sbyte",  "i8"),
        new Type("int16",           [0x16], "short",  "i16"),
        new Type("int32",           [0x17], "int",    "i32"),
        new Type("int64",           [0x18], "long",   "i64"),
        new Type("float32",         [0x19], "float",  "f32"),
        new Type("float64",         [0x1A], "double", "f64"),

        new Type("label",           [0xE0]),

        new Type("lazy",            [0xFA]),
        new Type("constant",        [0xFB], "const"),
        new Type("complex_type",    [0xFC]),
        new Type("struct",          [0xFD]),
        new Type("array",           [0xFE]),
        new Type("pointer",         [0xFF]),
    };
    public static Dictionary<string, string> ShortTypes = new Dictionary<string, string>() 
    {
        { "u",  "uint32"},  
        { "ul", "uint64"}, 
        //{ "",   "int32"}, //no specifier means int32 by default (but this is context specific)
        { "l",  "int64"},  
        { "f",  "float32"},  
        { "d",  "float64"},  
    };
    public static Dictionary<string, HashSet<string>> CompatibleTypes = new Dictionary<string, HashSet<string>>()
    {
        { "raw",            []},
        { "string",         []},
        { "uint8",          []},
        { "uint16",         ["uint8"]},
        { "uint32",         ["uint16", "uint8"]},
        { "uint64",         ["uint32", "uint16", "uint8"]},
        { "int8",           []},
        { "int16",          ["int8"]},
        { "int32",          ["int16", "int8"]},
        { "int64",          ["int32", "int16", "int8"]},
        { "float32",        []},
        { "float64",        ["float32"]},
        { "label",          []},
        { "constant",       []},
        { "complex_type",   []},
        { "struct",         []},
        { "array",          []},
        { "pointer",        []},
    };
    public static bool TryGetType(string name, out Type type)
    {
        type = Types.FirstOrDefault(t => t.name == name || (t.aliases?.Contains(name) ?? false));
        bool success = (type is not null);
        return success;
    }
    public static Type FindType(string name)
    {
        if (TryGetType(name, out Type type))
            return type;
        else
            throw new KeyNotFoundException($"Type:\"{name}\" not found");
    }
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
        OP("JUMP_IF",   1),
        OP("CALL",      1),
        OP("CALL_IF",   1),
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

        OP("CHECK_STACK", 1),
    });
    public static List<string> syscalls = new List<string>()
    {
        "PAUSE",
        "CLEAR_CONSOLE",
        "READ",
        "PRINT",
        "DUMP",
        "TO_STRING_UNSIGNED",
        "TO_STRING_SIGNED",
        "TO_NUMBER_UNSIGNED",
        "TO_NUMBER_SIGNED",

        "INPUT_MODE_READ",
        "INPUT_MODE_WRITE",
        "INPUT_TO_STRUCT",

        "SET_CONSOLE_CURSOR",
        "GET_CONSOLE_CURSOR",
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
        "RAND",
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
