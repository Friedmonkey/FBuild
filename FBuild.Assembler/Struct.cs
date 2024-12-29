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
        return new Declare(name, bytes.ToArray()) { used = this.used};
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
