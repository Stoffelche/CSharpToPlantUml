using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CSharpToPlantUml {
	[Flags]
	public enum Accessibilities {
		None = 0x0000,
		Private = 0x0001,
		Protected = 0x0002,
		Internal = 0x0004,
		ProtectedInternal = 0x0008,
		Public = 0x0010,
		Explicit = 0x0020, 
		PublicOrInternal = Public | Internal,
		All = Private | Protected | Internal | ProtectedInternal | Public | Explicit,
		PublicOrExplicit = Public | Explicit
	}

}
