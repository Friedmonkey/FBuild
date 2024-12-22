using FBuild.Assembler;
using FBuild.Common;
using System;
using System.IO;

namespace FBuild;

internal class Program
{
    static void Main(string[] args)
    {
        // Default options
        string inputFile = null;
        string outputFile = null;
        bool assemble = false, disassemble = false, compile = false, decompile = false;
        bool keepUnusedCode = false; // -d (debug flag)
        bool removeSymbols = false;  // -r (release flag)

        // Default action is compile if no parameters are given
        if (args.Length == 0)
        {
            //Console.WriteLine("No arguments provided. Defaulting to -compile.");
            //compile = true;
            Console.WriteLine("No arguments provided. Defaulting to -assemble.");
            assemble = true;
        }
        else
        {
            // Parse arguments
            for (int i = 0; i < args.Length; i++)
            {
                switch (args[i].ToLower())
                {
                    case "-assemble":
                    case "-a":
                        assemble = true;
                        break;
                    case "-disassemble":
                    case "-da":
                        disassemble = true;
                        break;
                    case "-compile":
                    case "-c":
                        compile = true;
                        break;
                    case "-decompile":
                    case "-dc":
                        decompile = true;
                        break;
                    case "-o":
                    case "-output":
                        if (i + 1 < args.Length) // Ensure there's a next argument
                        {
                            outputFile = args[++i];
                        }
                        else
                        {
                            Console.WriteLine("Error: -o requires a file path.");
                            return;
                        }
                        break;
                    case "-d":
                    case "-debug":
                        keepUnusedCode = true;
                        break;
                    case "-r":
                    case "-release":
                    case "-remove":
                        removeSymbols = true;
                        break;
                    default:
                        // If the argument is not a flag, assume it's the input file
                        if (inputFile == null)
                            inputFile = args[i];
                        else
                            Console.WriteLine($"Warning: Ignoring unknown parameter '{args[i]}'.");
                        break;
                }
            }
        }

        // Validate input file
        if (string.IsNullOrEmpty(inputFile))
        {
            Console.WriteLine("Error: No input file specified, using default: input.flasm");
            inputFile = "input.flasm";
            //return;
        }
        ConsoleLogger logger = new ConsoleLogger();
        FriedAssembler assembler = new FriedAssembler(logger);

        // Determine action
        if (assemble)
        {
            Console.WriteLine($"Assembling '{inputFile}' -> '{outputFile ?? "output.fxe"}'.");
            string text = File.ReadAllText(inputFile);
            var bytes = assembler.Parse(text);
            logger.Refresh(LogType.Info | LogType.Warning | LogType.Error | LogType.Detail);
            File.WriteAllBytes(@"C:\Users\marti\source\repos\FriedVM\assembled.fxe", bytes);
            // Call assembler logic here
        }
        else if (disassemble)
        {
            Console.WriteLine($"Disassembling '{inputFile}' -> '{outputFile ?? "output.flasm"}'.");
            // Call disassembler logic here
        }
        else if (compile)
        {
            Console.WriteLine($"Compiling '{inputFile}' -> '{outputFile ?? "output.fxe"}'.");
            // Call compiler logic here
        }
        else if (decompile)
        {
            Console.WriteLine($"Decompiling '{inputFile}' -> '{outputFile ?? "output.fln"}'.");
            // Call decompiler logic here
        }
        else
        {
            Console.WriteLine("Error: No valid action specified.");
        }

        // Debug/Release settings
        if (keepUnusedCode)
            Console.WriteLine("Option: Keeping unused code (-debug).");
        if (removeSymbols)
            Console.WriteLine("Option: Removing symbols (-release).");
    }
}
