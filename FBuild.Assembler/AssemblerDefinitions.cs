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
        new Type("raw",                 size:null, [0x00]),
        new Type("string",              size:null, [0x01]),
        new Type("interpolated_string", size:null, [0x02], "fried_string", "fstring"),

        new Type("lazy",            size:null, [0x0A]),
        //new Type("constant",        size:null, [0x0B], "const"),
        new Type("label",           size:null, [0x0B]),
        new Type("complex_type",    size:null, [0x0C]),
        new Type("struct",          size:null, [0x0D]),
        new Type("array",           size:null, [0x0E]),
        new Type("pointer",         size:null, [0x0F]),

        new Type("boolean",         size:null, [0x10], "bool"),
        

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
    //private static byte opcode_index = 0;
    private static KeyValuePair<string, InstructionDefinition> OP(byte opcode_index, string name, byte argcount)
    {
        return new KeyValuePair<string, InstructionDefinition>(name, new InstructionDefinition(name, opcode_index, argcount));
    }
    public static IReadOnlyDictionary<string, InstructionDefinition> Instruction_definitions = new Dictionary<string, InstructionDefinition>(new[]
    {
        OP(0x0E, "EXIT",          1),
        OP(0x0F, "SYSCALL",       1),


        OP(0x10, "PUSH",          1),
        OP(0x11, "STORE",         1),
        OP(0x12, "SET_VAR",       0),
        OP(0x13, "POP",           0),
        OP(0x14, "SWAP",          0),
        OP(0x15, "DUP",           0),


        OP(0x30, "MATH",          1),
        OP(0x31, "INC",           1),
        OP(0x32, "DEC",           1),

        OP(0x40, "COMP",          1),
        OP(0x41, "CHECK_STACK",   1),
        OP(0x42, "NOT",           0),

        OP(0x50, "JUMP",          1),
        OP(0x51, "JUMP_IF",       1),   //will pop from stack
        OP(0x52, "JUMP_IF_STACK", 2),   //will pop from stack

        OP(0x5A, "SET_CASE",      1),
        OP(0x5B, "SET_CASE_MODE", 1),
        OP(0x5C, "JUMP_IF_CASE",  2),
        
        OP(0x60, "CALL",          1),
        OP(0x61, "CALL_IF",       1),   //will pop from stack
        OP(0x62, "CALL_IF_STACK", 2),   //will pop from stack

        OP(0x6F, "RET",           0),   //return to previous call stack

        OP(0xB0, "SET_BUFFER",	  1),//unused
        OP(0xB1, "PUSH_BUFFER",	  1),//unused
        OP(0xB2, "GET_BUFFER",    1),//unused

        OP(0xB9, "BUFFER_UTIL",   1),//unused


        OP(0xBA, "SET_STRUCT",    2),//unused
        OP(0xBB, "GET_STRUCT",    2),//unused
        OP(0xBC, "CREATE_STRUCT", 2),//unused

    });
    public static List<string> syscalls = new List<string>()
    {
        "PAUSE",
        "CLEAR_CONSOLE",
        "READ",
        "PRINT",
        "PRINTLN",
        "DUMP",
        //"TO_STRING_UNSIGNED",
        "TO_STRING_SIGNED", //unused
        "PARSE",
        //"TO_NUMBER_UNSIGNED",
        "TO_NUMBER_SIGNED",//unused

        "INPUT_MODE_READ",//unused
        "INPUT_MODE_WRITE",//unused
        "INPUT_TO_STRUCT",//unused

        "SET_CONSOLE_CURSOR",//unused
        "GET_CONSOLE_CURSOR",//unused
    };
    public static List<string> math_modes = new List<string>()
    {
        "ADD",
        "SUB",
        "INC",
        "DEC",
        "MUL",
        "DIV",
        "POW",//unused
        "ROOT",//unused
        "SQRT",//unused
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
