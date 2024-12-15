using FBuild.Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

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
    private Dictionary<string, UInt64> Labels = new Dictionary<string, UInt64>();
    private List<Declare> Declares = new List<Declare>();
    private List<Instruction> Instructions = new List<Instruction>();
    private UInt64 MaxDeclareSize = 0;
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

        input = ParseComments(input);
        UpdateAndReset();

        input = ParseIncludes(input);
        UpdateAndReset();

        input = ParseDeclares(input);
        UpdateAndReset();

        GetLabels(input);
        this.Position = 0;

        input = ParseInstructions(input, out UInt64 address_offset);
        UpdateAndReset();

        //replace all :labelname with labals[labalname]
        //replace all declarename with declares[declarename].index
        // by going troguh labels.keys and foreach declares (for loop cus we need index)
        input = ParseAndResolveLabelAndInstructionAddresses(input);
        UpdateAndReset();

        bool hasSymbols = true;
        if (Declares.Count() == 0)
            hasSymbols = false;
        var MetaSize = MaxDeclareSize.GetAmountOfBytesNeeded();
        input = FinalGenerator(input, address_offset, MetaSize, hasSymbols, 1);
        UpdateAndReset();

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
    byte GetSize(Size size)
    {
        return size switch
        {
            Size.One =>     1,
            Size.Two =>     2,
            Size.Four =>    4,
            Size.Eight =>   8,
            _ => throw new NotSupportedException()
        };
    }
    private byte PackVersion(Size header, Size meta, bool includeSymbols, byte version)
    {
        // Ensure version does not exceed 3 bits
        version &= 0b00000111;

        // Pack all fields into a single byte
        byte versionByte = (byte)(
            ((byte)header << 6) |            // Header: shift 6 bits to the left
            ((byte)meta << 4) |              // Meta: shift 4 bits to the left
            (includeSymbols ? 0b00001000 : 0) | // IncludeSymbols: add 3rd bit
            (version & 0b00000111)           // Version: mask lower 3 bits
        );

        return versionByte;
    }
    private string FinalGenerator(string instructions, UInt64 instructions_offset, Size meta, bool includeSymbols, byte version)
    {
        const string instructionHeader = "%instructionHeader%";
        const string constPoolHeader = "%constPoolHeader%";
        const string symbolHeader = "%symbolHeader%";
        const string versionByteTemp = "%versionByte%";
        byte meta_size = GetSize(meta);
        UInt64 instructionHeaderPos = 0;
        UInt64 constPoolHeaderPos = 0;
        UInt64 symbolHeaderPos = 0;
        UInt64 byteCount = 0;
        StringBuilder sb = new StringBuilder();
        // Add the magic "FXE" in byte format
        sb.Append("FXE".ToByteString()); // Converts "FXE" to hex (e.g., "46 58 45")
        sb.Append(versionByteTemp);
        byteCount += 4;//magic
        sb.Append(instructionHeader);
        byteCount += 0; //calculate later
        sb.Append(constPoolHeader);
        byteCount += 0; //calculate later
        if (includeSymbols)
        {
            sb.Append(symbolHeader);
            byteCount += 0; //calculate later
        }
        foreach (Declare dec in Declares)
        {
            sb.Append(((UInt64)dec.value.Count()).ToFixedByteArray(meta_size));
            byteCount += meta_size;
        }
        instructionHeaderPos = byteCount;
        sb.Append(instructions);
        byteCount += instructions_offset;
        constPoolHeaderPos = byteCount;
        foreach (Declare dec in Declares)
        {
            sb.Append(dec.value.ToByteString());
            byteCount += (UInt64)dec.value.Count();
        }
        if (includeSymbols)
        { 
            symbolHeaderPos = byteCount;
            foreach (Declare dec in Declares)
            { 
                sb.Append(dec.name.ToByteString());
                byteCount += (UInt64)dec.name.Count();
                sb.Append(((byte)0xBB).ToByteString()); //symbol split char, needs to be appended
                byteCount++;
            }
        }

        string finalText = sb.ToString();

        //calculate the bytes needed for header size
        int numHeaders = includeSymbols ? 3 : 2;
        UInt64 largestHeader = includeSymbols ? symbolHeaderPos : constPoolHeaderPos;
        largestHeader += 8; //go off the largest

        Size bytesNeeded = largestHeader.GetAmountOfBytesNeeded();
        byte header_size = GetSize(bytesNeeded);
        byte versionByte = PackVersion(bytesNeeded, meta, includeSymbols, version);

        UInt64 headerOffset = (UInt64)numHeaders * header_size;

        instructionHeaderPos += headerOffset;
        constPoolHeaderPos += headerOffset;
        if (includeSymbols)
            symbolHeaderPos += headerOffset;

        byteCount += headerOffset;


        finalText = finalText.Replace(versionByteTemp, versionByte.ToByteString()); //replace the version byte

        finalText = finalText.Replace(instructionHeader, instructionHeaderPos.ToFixedByteArray(header_size));
        finalText = finalText.Replace(constPoolHeader, constPoolHeaderPos.ToFixedByteArray(header_size));
        if (includeSymbols) 
            finalText = finalText.Replace(symbolHeader, symbolHeaderPos.ToFixedByteArray(header_size));

        return finalText;
    }

    public List<byte> ParseBytes(out string address)
    {
        address = string.Empty;
        List<byte> bytes = new List<byte>();
        do
        {
            SkipWhitespace();
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
                    //bytes.AddRange(declare.value);
                    bytes.AddRange(Declares.IndexOf(declare).ToByteArrayWithNegative());
                    address = varName;
                }
                else if (syscalls.Contains(varName.ToUpper()))
                { 
                    //address = varName; //maby im not sure
                    bytes.AddRange(syscalls.IndexOf(varName.ToUpper()).ToByteArrayWithNegative());
                }
                else if (Labels.ContainsKey(varName))
                {
#warning should this be address or no?
                    //address = varName; //maby im not sure
                    var addr = Labels[varName];
                    if (addr != UInt64.MaxValue)
                        bytes.AddRange(((int)addr).ToByteArrayWithNegative());
                    else
                    {
                        address = varName;
                        //throw new Exception($"Label \"{varName}\" doest have an address yet!");
                    }
                }
                else
                {
                    throw new Exception($"Identifier:{varName} not found, its not a label nor syscall nor a declared varible");
                }
            }
            else
            {
                throw new Exception($"Unexpected \"{Current}\" while parsing declare, expected either a byte(00) char('H') or string(\"hello\")");
            }
            SkipWhitespace();
        }
        while (Safe && IfConsume(','));
        return bytes;
    }
    public void CheckName(string name)
    {
        if (Declares.Any(d => d.name == name)) throw new Exception($"Error parsing: {CurrentlyConsuming} declare with name \"{name}\" already exists!");
        if (Labels.ContainsKey(name)) throw new Exception($"Error parsing: {CurrentlyConsuming} label with name \"{name}\" already exists!");
        if (Instruction_definitions.ContainsKey(name)) throw new Exception($"Error parsing: {CurrentlyConsuming} instruction with name \"{name}\" already exists!");
        if (syscalls.Contains(name)) throw new Exception($"Error parsing: {CurrentlyConsuming} syscall with name \"{name}\" already exists!");
    }
    public string ParseAndResolveLabelAndInstructionAddresses(string input)
    {
        CurrentlyConsuming = "resolving addresses";

        string FinalText = input;

        var label_keys = Labels.Keys.ToArray();
        for (int i = 0; i < Labels.Count; i++)
        {
            var byte_arr = ((int)Labels[label_keys[i]]).ToByteArrayWithNegative();
            string buffer = string.Empty;
            foreach (byte bite in byte_arr)
            {
                buffer += bite.ToString("X2") + " ";
            }
            FinalText = FinalText.Replace(label_keys[i], buffer);
        }

        return FinalText;
    }
    public string ParseInstructions(string input, out UInt64 address_offset)
    {
        address_offset = 0;
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

#warning this may be not great
                        if (string.IsNullOrEmpty(addr) || arg_bytes.Count() != 0)
                        {
                            foreach (byte bite in arg_bytes)
                            {
                                arguments += bite.ToString("X2") + " ";
                            }
                        }
                        else
                        { 
                            arguments += addr + " "; //embed into the string to be replaced later
                        }
                        isAddr = !string.IsNullOrEmpty(addr);
                        bytes.AddRange(arg_bytes);
                    }

                    var instruction = new Instruction(def, arg_size, !isAddr, bytes.ToArray());
                    Instructions.Add(instruction);
                    address_offset += 1; //the byte for the opcode
                    address_offset += (UInt64)def.paramCount * (UInt64)arg_size; //the amount of parameters

                    FinalText += instruction.GetByte().ToString("X2")+" ";
                    FinalText += arguments;
                }
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
                Labels.Add(labelName , UInt64.MaxValue);
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
                if ((UInt64)bytes.Count() > MaxDeclareSize)
                    MaxDeclareSize = (UInt64)bytes.Count();

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
    public string ParseComments(string input)
    {
        CurrentlyConsuming = "comments";

        string FinalText = string.Empty;

        while (Safe)
        {
            if (FindStart("//"))
            {
                var comment = ConsumeUntilEnter();
                logger?.LogDetail("Comment removed:"+comment);
                SkipWhitespace();
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
    public string ConsumeUntilEnter()
    {
        string consumed = string.Empty;
        while (Safe && !Current.IsEnter())
        {
            consumed += Current;
            Position++;
        }
        return consumed;
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
    public bool IfConsume(char character)
    {
        if (Current == character)
        {
            Position++;
            return true;
        }
        else
            return false; 
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
