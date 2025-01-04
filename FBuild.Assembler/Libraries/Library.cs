using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FBuild.Assembler.Libraries
{
    public abstract class Library
    {
        public string Namespace = "lib_unnamed";
        public string Declares;
        public string Setup;
        public string Code;
        string GetCode()
        {
            return $""" 
		    {Declares}
            {Setup}
            jump {Namespace}_endoflibrary
            {Code}
            :{Namespace}_endoflibrary
""";
        }
        private static Dictionary<string, Library> AllLibraries = new Dictionary<string, Library>() 
        {
            { "input", new LibInput() },
        };
        public static bool TryGetLibrary(string name, out string code)
        {
            code = string.Empty;
            name = name.ToLower();
            if (!AllLibraries.TryGetValue(name, out Library library))
                return false;
            
            code = library.GetCode();
            return true;
        }
    }
}
