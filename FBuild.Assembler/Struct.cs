using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace FBuild.Assembler;

[DebuggerDisplay("{name}:{value}")]
public class Struct
{
    public Struct(string name)
    {
        this.name = name;
    }
    public string name;
    public List<StructField> fields = new();
    public bool used = false;
    public Declare MakeDeclare()
    { 
        int count = fields.Count;
        if (count > byte.MaxValue) throw new Exception("Too many fields");

        List<byte> bytes = new();
        bytes.Add(0xFF);
        bytes.Add((byte)count);
        bytes.AddRange(fields.Select(f => (byte)f.size));
        foreach (StructField f in fields)
        {
            bytes.AddRange(f.inital_value);
        }
        return new Declare(AssemblerDefinitions.FindType("struct"), name, bytes.ToArray()) { used = this.used};
    }
}

[DebuggerDisplay("{name}:{value}")]
public class StructField
{
    public StructField(Type type, string name)
    {
        this.type = type;
        this.name = name;
    }
    public Type type;
    public string name;
    public byte[] inital_value;
    //public int size;
    //public bool immidiate;
    //public string address = null;
}
