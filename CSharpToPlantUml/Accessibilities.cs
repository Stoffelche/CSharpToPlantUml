using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CSharpToPlantUml {
	[Flags]
	public enum Accessibilities {
		None = 0x0000,
		Private = 1,
		Protected = 1 << 1,
		Internal = 1 << 2,
		ProtectedInternal = 1 << 4,
		Public =  1 << 5,
		Explicit = 1 << 6, 
		All = 0b1111111,
	}

}
