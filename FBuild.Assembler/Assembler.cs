using FBuild.Common;
using System;
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

        //return input.ToArray();
        return null;
    }

    public string ParseIncludes(string input)
    {
        CurrentlyConsuming = "includes";

        string FinalText = string.Empty;

        while (Safe)
        {
            if (FindStart("#include "))
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
            else
            {
                FinalText += Current;
                Position++;
            }
        }
        return FinalText;
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
}
