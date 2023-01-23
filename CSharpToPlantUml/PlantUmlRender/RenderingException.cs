using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CSharpToPlantUml.PlantUmlRender {
	/// <summary>
	/// Borrowed from https://github.com/KevReed/PlantUml.Net
	/// </summary>
	public class RenderingException : Exception {
		public string Code { get; }
		public RenderingException(string code, string error)
				: base(error) {
			Code = code;
		}
	}
}
