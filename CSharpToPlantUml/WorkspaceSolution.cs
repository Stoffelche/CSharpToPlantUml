using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CSharpToPlantUml {
	public class WorkspaceSolution : WorkspaceBase {
		private readonly Solution _solution;

		private readonly IDictionary<string, WorkspaceProject> _openProjects =
				new Dictionary<string, WorkspaceProject?>();

		public static WorkspaceSolution Load(string fileName) {
			Console.Out.WriteLine("Loading solution...");
			var workspace = NewMsBuildWorkspace();
			var task = workspace.OpenSolutionAsync(fileName);
			task.Wait();
			return new WorkspaceSolution(task.Result);
		}

		public WorkspaceSolution(Solution solution) {
			_solution = solution;
		}

		public WorkspaceProject? GetProject(string projectName) {
			if (!_openProjects.TryGetValue(projectName, out var project)) {
				project = WorkspaceProject.LoadFromSolution(this, _solution.Projects.SingleOrDefault(x =>
						string.Equals(x.Name, projectName, StringComparison.OrdinalIgnoreCase)));
				_openProjects[projectName] = project;

			}
			return project;

		}
	}
}
