using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CSharpToPlantUml {
	public class ProjectFile {
		private readonly WorkspaceProject _project;
		private readonly SyntaxNode _fileRoot;

		public ProjectFile(WorkspaceProject project, SyntaxNode fileRoot) {
			_project = project;
			_fileRoot = fileRoot;
		}
	}
}
