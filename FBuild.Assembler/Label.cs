using System.Diagnostics;

namespace FBuild.Assembler;

[DebuggerDisplay("{name}:{address}")]
public class Label
{
    public Label(string name, int address)
    {
        this.name = name;
        this.address = address;
    }
    public string name;
    public int address;
    public bool used = false;
}
