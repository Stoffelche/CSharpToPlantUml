using CSharpToPlantUml.PlantUmlRender;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;

namespace CSharpToPlantUml {
	public class UmlProjectOptions {
		public bool DumpNamedTypes { get { return !string.IsNullOrEmpty(DumpNamedTypeFileName); } }
		public string DumpNamedTypeFileName;
	}

	public class UmlProject {
		public ProjectConfiguration Configuration;
		public WorkspaceSolution? Solution;
		public List<WorkspaceProject> Projects = new List<WorkspaceProject>();
		public NamedTypeVisitor NamedTypeVisitor;
		public UmlProject(ProjectConfiguration configuration) {
			Configuration = configuration;
		}
		public void Run() {
			if (!Directory.Exists(Configuration.OutputFolder)) {
				Directory.CreateDirectory(Configuration.OutputFolder);
			}
			if (!string.IsNullOrEmpty(Configuration.RenderFolder)
			&& Configuration.RenderUml
			&& !Directory.Exists(Configuration.RenderFolder)) {
				Directory.CreateDirectory(Configuration.RenderFolder);
			}
			LoadCSharpCode();
			InitNamedTypeVisitor();
			foreach (var diagram in Configuration.DiagramConfigurations) {
				RunDiagram(diagram);
			}
		}
		void LoadCSharpCode() {
			if (Configuration.UseSolution) {
				LogMessage(string.Format("Loading solution {0}", Configuration.Solution));
				Solution = WorkspaceSolution.Load(Configuration.Solution);
				foreach (string fileName in Configuration.CSharpProjects) {
					string projectName = fileName;
					if (System.IO.File.Exists(fileName)) {
						System.IO.FileInfo fInfo = new FileInfo(fileName);
						projectName = fInfo.Name;
						if (!string.IsNullOrEmpty(fInfo.Extension))
							projectName = projectName.Substring(0, projectName.Length - fInfo.Extension.Length);
					}
					LogMessage(string.Format("Loading project {0} from solution", projectName));
					WorkspaceProject? project = Solution.GetProject(projectName);
					if (project == null) throw new Exception(string.Format("{0} not found", projectName));
					else Projects.Add(project);
				}
			} else {
				foreach (string fileName in Configuration.CSharpProjects) {
					LogMessage(string.Format("Loading project {0}", fileName));
					WorkspaceProject? project = WorkspaceProject.Load(fileName);
					if (project == null) throw new Exception(string.Format("{0} not found", fileName));
					else Projects.Add(project);
				}
			}
		}
		void InitNamedTypeVisitor() {
			NamedTypeVisitor = new NamedTypeVisitor(false);
			foreach (var project in Projects) {
				NamedTypeVisitor.Visit(project);
			}
			if (Configuration.Options.DumpNamedTypes) {
				LogMessage("dumping named type list to " + Configuration.Options.DumpNamedTypeFileName);
				using (StreamWriter writer = new StreamWriter(Configuration.Options.DumpNamedTypeFileName)) {
					NamedTypeVisitor.DumpNamedTypes(writer); ;
				}
			}
		}
		void RunDiagram(DiagramConfiguration configuration) {
			LogMessage("Creating diagram " + configuration.OutputName);
			string path = Path.Combine(Configuration.OutputFolder, configuration.OutputName + ".puml");
			using (StreamWriter writer = new StreamWriter(path, false)) {
				ClassDiagramGenerator generator = new ClassDiagramGenerator(writer, NamedTypeVisitor, Configuration, configuration);
				if (configuration is InheritanceDiagramConfiguration inheritanceDiagramConfiguration) {
					RunInheritanceDiagram(inheritanceDiagramConfiguration, generator);
				}
			}
			if (Configuration.RenderUml) {
				LogMessage(string.Format("Rendering diagram {0} to format {1}", configuration.OutputName, Configuration.RenderFormat));
				try {
					RenderRemote.RenderFile(Configuration.RenderUrl, path, Configuration.RenderFormat, Configuration.RenderFolder);
				} catch (Exception ex) {
					LogErr(string.Format("Unable to render {0}. Error is {1}", path, ex.Message));
				}
			}
		}
		void RunInheritanceDiagram(InheritanceDiagramConfiguration configuration, ClassDiagramGenerator generator) {
			var typeList = NamedTypeVisitor.GetRelatedTypes(configuration.BaseType);
			if (typeList.Count == 0) {
				LogWarn(string.Format("Type '{0}' not found", configuration.BaseType));
			}
			generator.Generate(configuration.BaseType, typeList);
		}
		// todo handle warnings
		public void LogErr(string message) {
			System.Console.WriteLine("Error: " + message);
		}
		public void LogWarn(string message) {
			System.Console.WriteLine("Warning: " + message);
		}
		public void LogMessage(string message) {
			System.Console.WriteLine(message);
		}

	}
}
