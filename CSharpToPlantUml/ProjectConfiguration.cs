using CSharpToPlantUml.PlantUmlRender;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CSharpToPlantUml {
	public class ProjectConfiguration {
		public string Solution;
		public string OutputFolder;
		public bool RenderUml = false;
		public string RenderUrl = "http://www.plantuml.com/plantuml/";
		public EOutputFormat RenderFormat = EOutputFormat.Svg;
		public string RenderFolder = string.Empty;
		public bool RenderTypeLinks = false;
		public string RenderSearchChars = "`";
		public string RenderReplaceChars = "-";
		public string RenderRegexForTypes = string.Empty;
		public string RenderRegexForTypeReplacement = string.Empty;
		public List<string> CSharpProjects = new List<string>();
		public List<DiagramConfiguration> DiagramConfigurations = new List<DiagramConfiguration>();
		public bool UseSolution { get { return !string.IsNullOrEmpty(Solution); } }
		public UmlProjectOptions Options = new UmlProjectOptions();
		public ProjectConfiguration(string outputFolder) {
			OutputFolder = outputFolder;
		}
		public ProjectConfiguration() { 
		}	
		public void AddProjects(params string[] projects) {
			CSharpProjects.AddRange(projects);
		}
		public void AddDiagrams(params DiagramConfiguration[] diagrams) {
			DiagramConfigurations.AddRange(diagrams);
		}
	}
}
