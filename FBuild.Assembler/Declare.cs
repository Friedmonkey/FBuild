using System;
using System.Diagnostics;
using System.Linq;
using System.Security;

namespace FBuild.Assembler;

[DebuggerDisplay("{type.name} {name}:{type.name == \"string\" ? System.Text.Encoding.Default.GetString(value) : (value.Length == 1 ? (\"\"+value[0]) : value.ToString()) }")]
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
    public byte[] GetValue()
    {
        if (type.size is null) return value;
        if (value.Length == type.size) return value;

        if (value.Length > type.size)
            throw new Exception("value size is too big for its type");

        //the value but padded to the size it needs
        var newValue = value.ToList();
        while (newValue.Count < type.size)
            newValue.Add(0x00);

        return newValue.ToArray();
    }
}
