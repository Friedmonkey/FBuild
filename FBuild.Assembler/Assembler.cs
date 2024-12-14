using FBuild.Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

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
    private List<Declare> Declares = new List<Declare>();
    private List<Declare> Labels = new List<Declare>();
    private List<Declare> Instructions = new List<Declare>();
    //transform the text over time and eventually turn it into byte array
    public byte[] Parse(string input)
    {
        void UpdateAndReset()
        { //we have changed the input so we update our list
            this.Analizable = input.ToList();
            this.Position = 0; //we reset our position so we can start from the beginning again
        }

        //initial
        UpdateAndReset();

        //parse includes
        input = ParseIncludes(input);

        input = ParseDeclares(input);


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
        if (Declares.Any(d => d.name == name)) throw new Exception($"declare with name \"{name}\" already exists!");
        if (Labels.Any(l => l.name == name)) throw new Exception($"label with name \"{name}\" already exists!");
        if (Instructions.Any(i => i.name == name)) throw new Exception($"instruction with name \"{name}\" already exists!");
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

                Declares.Add(new Declare(declareName, bytes.ToArray()));

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
