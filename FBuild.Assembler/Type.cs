using System.Collections.Generic;
using System.Diagnostics;

namespace FBuild.Assembler;

[DebuggerDisplay("{name}:{value}")]
public class Type
{
    public Type(string name, int? size = null, byte[] value = null, params List<string> aliases)
    {
        this.size = size;
        this.name = name;
        this.value = value;
        this.aliases = aliases;
    }
    public int? size;
    public string name;
    public List<string> aliases;
    public byte[] value;
    public bool used = false;
    public byte[] default_value = new byte[] { 0x00 };

    public Struct structDef = null;
}
