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
    public const byte ConstantBitFlag = 0b1000_0000;
    public static List<Type> Types = new List<Type>()
    { 
        new Type("raw",             size:null, [0x00]),
        new Type("string",          size:null, [0x01]),

        new Type("lazy",            size:null, [0x0A]),
        //new Type("constant",        size:null, [0x0B], "const"),
        new Type("label",           size:null, [0x0B]),
        new Type("complex_type",    size:null, [0x0C]),
        new Type("struct",          size:null, [0x0D]),
        new Type("array",           size:null, [0x0E]),
        new Type("pointer",         size:null, [0x0F]),
        

        new Type("uint8",           size:1,    [0x20], "char",   "byte", "u8"),
        new Type("uint16",          size:2,    [0x22], "ushort", "u16"),
        new Type("uint32",          size:4,    [0x23], "uint",   "u32"),
        new Type("uint64",          size:8,    [0x24], "ulong",  "u64"),
        new Type("int8",            size:1,    [0x25], "sbyte",  "i8"),
        new Type("int16",           size:2,    [0x26], "short",  "i16"),
        new Type("int32",           size:4,    [0x27], "int",    "i32"),
        new Type("int64",           size:8,    [0x28], "long",   "i64"),
        new Type("float32",         size:4,    [0x29], "float",  "f32"),
        new Type("float64",         size:8,    [0x2A], "double", "f64"),

    };
    public static Dictionary<string, string> ShortTypes = new Dictionary<string, string>() 
    {
        { "b",  "uint8"},
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
        { "complex_type",   []},
        { "struct",         []},
        { "array",          []},
        { "pointer",        []},
    };

    // somewhere in your parser class
    public static readonly Dictionary<string, (bool isSigned, bool isFloat)> NumberProperties = new()
    {
        ["int8"] = (true, false),
        ["int16"] = (true, false),
        ["int32"] = (true, false),
        ["int64"] = (true, false),
        ["uint8"] = (false, false),
        ["uint16"] = (false, false),
        ["uint32"] = (false, false),
        ["uint64"] = (false, false),
        ["float32"] = (true, true),
        ["float64"] = (true, true),
    };

    public static bool TryGetType(string name, out Type type)
    {
        type = Types.FirstOrDefault(t => t.name == name || (t.aliases?.Contains(name) ?? false));
        bool success = (type is not null);
        //if (success)
        //    type = new Type(type);
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
