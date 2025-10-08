using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FBuild.Assembler;

class TypeValueEqualityComparer : IEqualityComparer<Type>
{
    public bool Equals(Type x, Type y)
    {
        if (x == null || y == null) return false;
        return x.value.SequenceEqual(y.value);
    }
    public int GetHashCode(Type obj)
    {
        if (obj?.value == null) return 0;
        // Use a simple hash code based on the byte array contents
        unchecked
        {
            int hash = 17;
            foreach (var b in obj.value)
                hash = hash * 31 + b;
            return hash;
        }
    }

}
