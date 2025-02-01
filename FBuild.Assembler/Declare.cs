using System.Diagnostics;

namespace FBuild.Assembler;

[DebuggerDisplay("{type.name} {name}:{value}")]
public class Declare
{
    public Declare(Type type, string name, byte[] value = null)
    {
        this.type = type;
        this.name = name;
        this.value = value;
    }
    public Type type;
    public string name;
    public byte[] value = null;
    public bool used = false;
    public bool isConst = false;
}
