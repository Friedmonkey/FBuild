using FBuild.Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection.Emit;
using System.Runtime.ConstrainedExecution;
using System.Xml.Linq;

namespace FBuild.Assembler;

public class FriedAssembler : AnalizerBase<char>
{
    private string CurrentlyConsuming = "start"; // Tell the consumer what we are consuming so we can use it to form a detailed exception
    private string ExtraConsumingInfo = "start"; // Any other information that can be usefull to form a more specific exception
    private readonly Ilogger logger;
    public FriedAssembler(Ilogger logger) : base('\0') 
    {
        this.logger = logger;
    }
    //private IReadOnlyList<InstructionDefinition> Instruction_definitions = new List<InstructionDefinition>();
    static byte opcode_index = 0;
    static KeyValuePair<string, InstructionDefinition> OP(string name, byte argcount)
    {
        opcode_index++;
        return new KeyValuePair<string, InstructionDefinition>(name, new InstructionDefinition(name, opcode_index++, argcount));
    }
    private IReadOnlyDictionary<string, InstructionDefinition> Instruction_definitions = new Dictionary<string, InstructionDefinition>(new[]
    {
        OP("PUSH", 1),
        OP("POP", 0),
        OP("DUP", 0),
        OP("VAR", 2),
        OP("GET_VAR", 1),
        OP("POP_VAR", 1),
        OP("PSH_VAR", 1),
        OP("MOV_VAR", 2),
        OP("DEL", 1),
        OP("ADD", 0),
        OP("SUB", 0),
        OP("MUL", 0),
        OP("DIV", 0),
        OP("AND", 0),
        OP("OR", 0),
        OP("NOT", 0),
        OP("EQ", 0),
        OP("NEQ", 0),
        OP("GT", 0),
        OP("GTEQ", 0),
        OP("LTEQ", 0),
        OP("LTEQ", 0),
        OP("JMP", 1),
        OP("JMP_IF", 1),
        OP("CALL", 1),
        OP("CALL_IF", 1),
        OP("RET", 0),
        OP("SYSCALL", 1),
        OP("EXIT", 0),
    });
    private IReadOnlyList<string> syscalls = new List<string>() 
    {
        "PAUSE",
        "CLEAR",
        "READ",
        "PRINT",
        "DUMP",
    };
    private Dictionary<string, int> Labels = new Dictionary<string, int>();
    private List<Declare> Declares = new List<Declare>();
    private List<Instruction> Instructions = new List<Instruction>();
    //transform the text over time and eventually turn it into byte array
    public byte[] Parse(string input)
    {
        input = $"\n{input}\n"; //padd with newline to make parsing easier
        void UpdateAndReset()
        { //we have changed the input so we update our list
            this.Analizable = input.ToList();
            this.Position = 0; //we reset our position so we can start from the beginning again
        }

        //initial
        UpdateAndReset();

        //parse includes
        input = ParseIncludes(input);
        UpdateAndReset();

        input = ParseDeclares(input);
        UpdateAndReset();

        GetLabels(input);
        this.Position = 0;

        ParseInstructions(input);
        this.Position = 0;

        //replace all :labelname with labals[labalname]
        //replace all declarename with declares[declarename].index
        // by going troguh labels.keys and foreach declares (for loop cus we need index)
        //input = ParseAndResolveLabelAndInstructionAddresses(input); 
        //UpdateAndReset();

        //return input.ToArray();
        return null;
    }

    public List<byte> ParseBytes()
    {
        List<byte> bytes = new List<byte>();
        do
        {
            if (Current == '"') //string
            {
                string str = ConsumeString();
                foreach (byte b in str) bytes.Add(b);
            }
            else if (Current == '\'') //char
            {
                char chr = ConsumeChar();
                bytes.Add((byte)chr);
            }
            else if (Current == '0' && Peek(1) is 'x' or 'X') //byte 0xFF (only parse 2 hex to make up the byte)
            {
                Consume('0');
                Position++; // Consume 'x' or 'X'

                string hexText = "";
                for (int i = 0; i < 2; i++) // Read exactly 2 characters
                {
                    char c = Peek(i);
                    if (!c.IsHexDigit()) // Validate if character is in 0-9, A-F, a-f
                        throw new FormatException($"Invalid hex character: '{c}'");

                    hexText += c;
                }
                Position += 2; // Advance the position by 2

                byte result = Convert.ToByte(hexText, 16); // Convert hex string to byte
                bytes.Add(result);
            }
            else if (Current == '0' && Peek(1) is 'b' or 'B') //binary 0b00001111 (only parse 8 (0 or 1) to make up the byte)
            {
                Consume('0');
                Position++; // Consume 'b' or 'B'

                string binaryText = "";
                for (int i = 0; i < 8; i++) // Read next 8 characters
                {
                    char c = Peek(i);
                    if (c != '0' && c != '1')
                        throw new FormatException($"Invalid binary character: '{c}'");

                    binaryText += c;
                }
                Position += 8; // Advance position by 8

                byte result = Convert.ToByte(binaryText, 2); // Convert binary to byte
                bytes.Add(result);
            }
            else if (char.IsDigit(Current))
            {
                throw new NotImplementedException("normal numbers are not supported yet!!11!");
            }
            else if (Current.IsVarible())
            {
                throw new NotImplementedException("varibles/labels are not supported yet!!11!");
            }
            else
            {
                throw new Exception($"Unexpected \"{Current}\" while parsing declare, expected either a byte(00) char('H') or string(\"hello\")");
            }
        }
        while (Safe && Current == ',');
        return bytes;
    }
    public void CheckName(string name)
    {
        if (Declares.Any(d => d.name == name)) throw new Exception($"Error parsing: {CurrentlyConsuming} declare with name \"{name}\" already exists!");
        if (Labels.ContainsKey(name)) throw new Exception($"Error parsing: {CurrentlyConsuming} label with name \"{name}\" already exists!");
        if (Instruction_definitions.ContainsKey(name)) throw new Exception($"Error parsing: {CurrentlyConsuming} instruction with name \"{name}\" already exists!");
    }
    public void ParseInstructions(string input)
    {
        int address_offset = 0;
        CurrentlyConsuming = "instructions and label addresses";
        while (Safe)
        {
            SkipWhitespace();
            if (Current == ':') //label addresses
            {
                Consume(':');
                string labelName = string.Empty;
                while (Safe && Current.IsVarible())
                {
                    labelName += Current;
                    Position++;
                }
                ExtraConsumingInfo = $"labelName: {labelName}";

                if (Labels.ContainsKey(labelName))
                {
                    Labels[labelName] = address_offset; //resolve the address of the label
                }
                else
                {
                    throw new Exception("big fat error, should never happen!11!");
                }
            }
            else
            {
                string instructionName = string.Empty;
                while (Safe && Current.IsVarible())
                {
                    instructionName += Current;
                    Position++;
                }
                ExtraConsumingInfo = $"instructionName: {instructionName}";

                if (Instruction_definitions.ContainsKey(instructionName.ToUpper()))
                {
                    var def = Instruction_definitions[instructionName.ToUpper()];
                    byte arg_size = 1;
                    bool imidiate = false;
                    Instructions.Add(new Instruction(def, arg_size, imidiate));
                    address_offset += 1; //the byte for the opcode
                    address_offset += def.paramCount * arg_size; //the amount of parameters
                }
                if (FindStart("declare "))
                {


                    CheckName(instructionName);

                    SkipWhitespace();
                    Consume('=');
                    SkipWhitespace();
                    List<byte> bytes = ParseBytes();
                    Consume(';');

                    logger?.LogDetail($"declare {instructionName} was added");
                    Declares.Add(new Declare(instructionName, bytes.ToArray()));
                }
                else
                {
                    Position++;
                }
            }
        }
    }
    public void GetLabels(string input)
    {
        CurrentlyConsuming = "labels";

        while (Safe)
        {
            if (FindStart(":"))
            {
                string labelName = string.Empty;
                while (Safe && Current.IsVarible())
                {
                    labelName += Current;
                    Position++;
                }
                ExtraConsumingInfo = $"labelName: {labelName}";

                CheckName(labelName);
                Labels.Add(labelName , -1);
                logger?.LogDetail($"label {labelName} was added but not initialized yet");
            }
            else
            {
                Position++;
            }
        }
    }
    public string ParseDeclares(string input)
    {
        CurrentlyConsuming = "declares";

        string FinalText = string.Empty;

        while (Safe)
        {
            if (FindStart("declare "))
            {
                string declareName = string.Empty;
                while (Safe && Current.IsVarible())
                {
                    declareName += Current;
                    Position++;
                }
                ExtraConsumingInfo = $"declareName: {declareName}";

                CheckName(declareName);

                SkipWhitespace();
                Consume('=');
                SkipWhitespace();
                List<byte> bytes = ParseBytes();
                Consume(';');

                logger?.LogDetail($"declare {declareName} was added");
                Declares.Add(new Declare(declareName, bytes.ToArray()));
            }
            else
            {
                FinalText += Current;
                Position++;
            }
        }
        return FinalText;
    }
    public string ParseIncludes(string input)
    {
        CurrentlyConsuming = "includes";

        string FinalText = string.Empty;

        while (Safe)
        {
            if (FindStart("#include "))
            {
                if (Current == '"') //include other files
                {

                    Consume('"');
                    string filename = ConsumeUntil('"');
                    ExtraConsumingInfo = filename;
                    Consume('"');
                    if (File.Exists(filename))
                    {
                        logger?.LogInfo($"including file:{Path.GetFileName(filename)}");
                        string contents = File.ReadAllText(filename);
                        FinalText += contents;
                    }
                    else
                    {
                        throw new Exception($"include failed, file \"{filename}\" cannot be found.");
                    }
                }
                else if (Current == '<') //include default libraries
                {
                    throw new NotImplementedException("including default libaries is not supported yet");
                }
                else
                {
                    throw new Exception($"expected either a quote (\") or left arrow (<) for includes but got \"{Current}\" instead!");
                }
            }
            else
            {
                FinalText += Current;
                Position++;
            }
        }
        return FinalText;
    }

    private string ConsumeString()
    {
        Consume('"');
        string str = ConsumeUntil('"');
        Consume('"');
        return str;
    }
    private char ConsumeChar()
    {
        Consume('\'');
        char chr = Current;
        Position++;
        if (Current != '\'')
            throw new Exception($"Error while parsing a char, Expected closing ' because a char can only be a single on, got {Current} instead.");
        Consume('\'');
        return chr;
    }
    public bool FindStart(string find)
    {
        if (Find(find))
        {
            int length = find.Length + 1;
            if (Peek(-length).IsEnter())
            {
                return true;
            }
            else
            {
                Console.WriteLine($"`{find}` found, but was not at the start of a line");
                Position -= find.Length;
                return false;
            }
        }
        return false;
    }
    public string ConsumeUntil(char stop)
    {
        string consumed = string.Empty;
        while (Safe && Current != stop)
        {
            consumed += Current;
            Position++;
        }
        return consumed;
    }
    public char Consume(char character)
    {
        if (Current == character)
        {
            Position++;
            return character;
        }
        else
            throw new Exception($"Error while parsing {CurrentlyConsuming} {ExtraConsumingInfo}, Expected `{character}` got `{Current}` instead.");
    }
    public bool Find(string find)
    {
        for (int i = 0; i < find.Length; i++)
        {
            if (Peek(i) == find[i])
            {
                continue;
            }
            else return false;
        }
        Position += find.Length;
        return true;
    }
    public void SkipWhitespace()
    {
        while (Safe && char.IsWhiteSpace(Current))
        {
            Position++;
        }
    }
}
