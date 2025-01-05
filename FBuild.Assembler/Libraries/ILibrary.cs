using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;

namespace FBuild.Assembler.Libraries
{
    public interface ILibrary
    {
        string Namespace { get; init; }
        string Declares { get; init; }
        string Setup { get; init; }
        string Code { get; init; }
        string GetCode()
        {
            string code = $""" 
		    {Declares}
            {Setup}
            jump {Namespace}_endoflibrary
            {Code}
            :{Namespace}_endoflibrary
""";
            StringBuilder sb = new StringBuilder();
            foreach (var line in code.Split('\n'))
            { 
                string ln = line.Trim();
                if (string.IsNullOrEmpty(ln))
                    continue;

                sb.AppendLine(ln);
            }
            return sb.ToString();
        }
        private static Dictionary<string, ILibrary> AllLibraries = new Dictionary<string, ILibrary>() 
        {
            { "input", new LibInput() },
        };
        public static bool TryGetLibrary(string name, out string code)
        {
            code = string.Empty;
            name = name.ToLower();
            if (!AllLibraries.TryGetValue(name, out ILibrary library))
                return false;
            
            code = library.GetCode();
            return true;
        }
    }
}
