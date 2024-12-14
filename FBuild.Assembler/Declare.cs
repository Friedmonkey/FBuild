using System.Diagnostics;

namespace FBuild.Assembler;

[DebuggerDisplay("{name}:{value}")]
public class Declare
{
    public Declare(string name, byte[] value = null)
    {
        this.name = name;
        this.value = value;
    }
    public string name;
    public byte[] value;
    public bool used = false;
}
