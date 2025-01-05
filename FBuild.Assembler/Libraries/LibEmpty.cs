using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FBuild.Assembler.Libraries;

public class LibEmpty : ILibrary
{
    public string Namespace { get; init; } = "lib_empty";
    public string Declares { get; init; } = """ 

""";
    public string Setup { get; init; } = """ 
	
""";
    public string Code { get; init; } = """ 
	
""";
}
