using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CSharpToPlantUml {
	[Flags]
	public enum EMemberType {
		None = 0x0000,
		Method = 0x0001,
		Field =	0x0002,
		Property = 0x0004,
		Ctor = 0x0008,
		InnerType = 0x0010,
		Enum = 0x0020,  // special inner type
		NoInnerTypes = Method | Field | Property | Ctor,
		All = Method | Field | Property | Ctor | InnerType | Enum
	}
}
