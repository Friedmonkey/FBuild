using FBuild.Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection.Emit;
using System.Runtime.ConstrainedExecution;
using System.Xml.Linq;
using static System.Runtime.InteropServices.JavaScript.JSType;

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
        OP("LT", 0),
        OP("LTEQ", 0),
        OP("JMP", 1),
        OP("JMP_IF", 1),
        OP("CALL", 1),
        OP("CALL_IF", 1),
        OP("RET", 0),
        OP("SYSCALL", 1),
        OP("EXIT", 0),
    });
    private List<string> syscalls = new List<string>() 
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

        input = ParseInstructions(input);
        UpdateAndReset();

        //replace all :labelname with labals[labalname]
        //replace all declarename with declares[declarename].index
        // by going troguh labels.keys and foreach declares (for loop cus we need index)
        //input = ParseAndResolveLabelAndInstructionAddresses(input); 
        //UpdateAndReset();

        //convert string like "20 54 6A" to actual bytes
        string[] hexValuesSplit = input.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        byte[] bytes = new byte[hexValuesSplit.Length];

        for (int i = 0; i < hexValuesSplit.Length; i++)
        {
            if (string.IsNullOrEmpty(hexValuesSplit[i]))
                continue;
            bytes[i] = Convert.ToByte(hexValuesSplit[i], 16);
        }

        //return input.ToArray();
        return bytes;
    }

    public List<byte> ParseBytes(out string address)
    {
        address = string.Empty;
        List<byte> bytes = new List<byte>();
        do
        {
            if (Current == '"') //string
            {
                string str = ConsumeString();
                foreach (byte b in str) bytes.Add(b);
                address = string.Empty;
            }
            else if (Current == '\'') //char
            {
                char chr = ConsumeChar();
                bytes.Add((byte)chr);
                address = string.Empty;
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
                address = string.Empty;
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
                address = string.Empty;
            }
            else if (char.IsDigit(Current)) //normal numbers
            {
                string numberText = "";

                while (char.IsDigit(Current))
                {
                    numberText += Current;
                    Position++;
                }

                //convert string to integer
                if (!int.TryParse(numberText, out int number))
                    throw new FormatException($"Invalid number format: {numberText}");

                // make bytes
                bytes.AddRange(number.ToByteArrayWithNegative());
            }
            else if (Current.IsVarible()) //either label/address or meta/varible/declare
            {
                string varName = string.Empty;
                while (Safe && Current.IsVarible())
                {
                    varName += Current;
                    Position++;
                }
                ExtraConsumingInfo = $"varName: {varName}";

                var declare = Declares.FirstOrDefault(d => d.name == varName);
                if (declare is not null)
                {
                    declare.used = true;
                    bytes.AddRange(declare.value);
                    address = varName;
                }
                else if (syscalls.Contains(varName))
                { 
                    //address = varName; //maby im not sure
                    bytes.AddRange(syscalls.IndexOf(varName).ToByteArrayWithNegative());
                }
                else if (Labels.ContainsKey(varName))
                {
#warning should this be address or no?
                    //address = varName; //maby im not sure
                    var addr = Labels[varName];
                    if (addr != -1)
                        bytes.AddRange(addr.ToByteArrayWithNegative());
                    else
                        throw new Exception($"Label \"{varName}\" doest have an address yet!");
                }
                else
                {
                    throw new Exception("big fat error, should never happen!11!");
                }
                throw new NotImplementedException("varibles/labels are not supported yet!!11!");
            }
            else
            {
                throw new Exception($"Unexpected \"{Current}\" while parsing declare, expected either a byte(00) char('H') or string(\"hello\")");
            }
            SkipWhitespace();
        }
        while (Safe && Current == ',');
        return bytes;
    }
    public void CheckName(string name)
    {
        if (Declares.Any(d => d.name == name)) throw new Exception($"Error parsing: {CurrentlyConsuming} declare with name \"{name}\" already exists!");
        if (Labels.ContainsKey(name)) throw new Exception($"Error parsing: {CurrentlyConsuming} label with name \"{name}\" already exists!");
        if (Instruction_definitions.ContainsKey(name)) throw new Exception($"Error parsing: {CurrentlyConsuming} instruction with name \"{name}\" already exists!");
        if (syscalls.Contains(name)) throw new Exception($"Error parsing: {CurrentlyConsuming} syscall with name \"{name}\" already exists!");
    }
    public string ParseInstructions(string input)
    {
        int address_offset = 0;
        CurrentlyConsuming = "instructions and label addresses";

        string FinalText = string.Empty;
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
                instructionName = instructionName.ToUpper();
                SkipWhitespace();
                ExtraConsumingInfo = $"instructionName: {instructionName}";

                if (Instruction_definitions.TryGetValue(instructionName, out InstructionDefinition def))
                {
                    byte arg_size = 1;
                    bool isAddr = false;

                    List<byte> bytes = new List<byte>();
                    string arguments = string.Empty;
                    for (int i = 0; i < def.paramCount; i++)
                    {
                        var arg_bytes = ParseBytes(out string addr);
                        //isAddr |= (!string.IsNullOrEmpty(addr));
                        if (arg_bytes.Count() > 4)
                        {
                            //maby auto generate declaration for this
                            throw new Exception($"Error {ExtraConsumingInfo} Arguments with size greather than 4 is not supported! But argument number {i} got {arg_bytes.Count()} bytes!");
                        }
                        if (arg_bytes.Count() > arg_size)
                            arg_size = (byte)arg_bytes.Count();

                        if (string.IsNullOrEmpty(addr))
                        {
                            foreach (byte bite in arg_bytes)
                            {
                                arguments += bite.ToString("X2") + " ";
                            }
                        }
                        else
                        { 
                            isAddr = true;
                            arguments += addr; //embed into the string to be replaced later
                        }
                        bytes.AddRange(arg_bytes);
                    }

                    var instruction = new Instruction(def, arg_size, !isAddr, bytes.ToArray());
                    Instructions.Add(instruction);
                    address_offset += 1; //the byte for the opcode
                    address_offset += def.paramCount * arg_size; //the amount of parameters

                    FinalText += instruction.GetByte().ToString("X2")+" ";
                    FinalText += arguments;
                }
                //if (FindStart("declare "))
                //{


                //    CheckName(instructionName);

                //    SkipWhitespace();
                //    Consume('=');
                //    SkipWhitespace();
                //    List<byte> bytes = ParseBytes();
                //    Consume(';');

                //    logger?.LogDetail($"declare {instructionName} was added");
                //    Declares.Add(new Declare(instructionName, bytes.ToArray()));
                //}
                //else
                //{
                //    Position++;
                //}
            }
        }
        return FinalText;
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
                List<byte> bytes = ParseBytes(out _);
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
