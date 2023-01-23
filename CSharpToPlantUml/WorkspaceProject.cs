using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.FindSymbols;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CSharpToPlantUml {
	public class WorkspaceProject : WorkspaceBase {
		private readonly Project _project;
		private readonly Compilation _compilation;
		public WorkspaceSolution Solution { get; }
		public Project Project { get { return _project; } }
		public Compilation Compilation{ get { return _compilation; } }		

		public static WorkspaceProject? Load(string fileName) {
			var workspace = NewMsBuildWorkspace();
			var task = workspace.OpenProjectAsync(fileName);
			task.Wait();
			return LoadProject(task.Result, null);
		}

		public static WorkspaceProject? LoadFromSolution(WorkspaceSolution solution, Project project) {
			return project != null
					? LoadProject(project, solution)
					: null;
		}

		private static WorkspaceProject? LoadProject(Project project, WorkspaceSolution? solution) {
			var task = project.GetCompilationAsync();
			task.Wait();
			var compilation = task.Result;
			return compilation != null ? new WorkspaceProject(solution ?? new WorkspaceSolution(project.Solution), project, compilation) : null;
		}

		private WorkspaceProject(WorkspaceSolution solution, Project project, Compilation compilation) {
			Solution = solution;
			_project = project;
			_compilation = compilation;
		}

		public ProjectFile? LoadFile(string fileName) {
			var document = _project.Documents.Single(x => x.Name == fileName);
			var task = document.GetSyntaxRootAsync();
			task.Wait();
			var syntaxNode = task.Result;
			return syntaxNode != null ? new ProjectFile(this, syntaxNode) : null; 
		}

	}
}
