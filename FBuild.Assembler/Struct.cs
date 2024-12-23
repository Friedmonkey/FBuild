using System.Collections.Generic;
using System.Diagnostics;

namespace FBuild.Assembler;

[DebuggerDisplay("{name}:{value}")]
public class Struct
{
    public Struct(string name)
    {
        this.name = name;
    }
    public string name;
    public List<StructField> fields;
    public bool used = false;
    public Declare MakeDeclare()
    { 
        return new Declare(name);
    }
}

[DebuggerDisplay("{name}:{value}")]
public class StructField
{
    public StructField(string name)
    {
        this.name = name;
    }
    public string name;
    public int size;
    public bool immidiate;
    public byte[] inital_value;
    public string address = null;
}
