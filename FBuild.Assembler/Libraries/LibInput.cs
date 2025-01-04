using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FBuild.Assembler.Libraries;

public class LibInput : Library
{
    new public string Namespace = "lib_input";
    /// <summary>
    /// input_buffer, input_key
    /// </summary>
    new public string Declares = """ 
	declare input_buffer;
    declare_struct input_key
	{
		field num_0:1 = 0x30;
		field num_1:1 = 0x31;
		field num_2:1 = 0x32;
		field num_3:1 = 0x33;
		field num_4:1 = 0x34;
		field num_5:1 = 0x35;
		field num_6:1 = 0x36;
		field num_7:1 = 0x37;
		field num_8:1 = 0x38;
		field num_9:1 = 0x39;
	
		field a:1 = 0x41;
		field b:1 = 0x42;
		field c:1 = 0x43;
		field d:1 = 0x44;
		field e:1 = 0x45;
		field f:1 = 0x46;
		field g:1 = 0x47;
		field h:1 = 0x48;
		field i:1 = 0x49;
		field j:1 = 0x4A;
		field k:1 = 0x4B;
		field l:1 = 0x4C;
		field m:1 = 0x4D;
		field n:1 = 0x4E;
		field o:1 = 0x4F;
		field p:1 = 0x50;
		field q:1 = 0x51;
		field r:1 = 0x52;
		field s:1 = 0x53;
		field t:1 = 0x54;
		field u:1 = 0x55;
		field v:1 = 0x56;
		field w:1 = 0x57;
		field x:1 = 0x58;
		field y:1 = 0x59;
		field z:1 = 0x5A;
	
		field numpad_0:1 = 0x60;
		field numpad_1:1 = 0x61;
		field numpad_2:1 = 0x62;
		field numpad_3:1 = 0x63;
		field numpad_4:1 = 0x64;
		field numpad_5:1 = 0x65;
		field numpad_6:1 = 0x66;
		field numpad_7:1 = 0x67;
		field numpad_8:1 = 0x68;
		field numpad_9:1 = 0x69;
	};
""";
    new public string Setup = """ 
	call lib_input_setup
""";
    new public string Code = """ 
	:lib_input_setup
	create_struct input_key input_buffer
	ret

	:lib_input_get
	push input_buffer
	syscall input_to_struct
	ret
""";
}
