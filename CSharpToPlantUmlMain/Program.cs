// See https://aka.ms/new-console-template for more information
using Microsoft.Build.Locator;
using CSharpToPlantUml;
using System.Xml.Serialization;

internal class Program {
	private static void Main(string[] args) {
		if (!MSBuildLocator.IsRegistered) {
			var instances = MSBuildLocator.QueryVisualStudioInstances().ToArray();
			MSBuildLocator.RegisterInstance(instances.OrderByDescending(x => x.Version).First());
		}

	
		XmlSerializer serializer = new XmlSerializer(typeof(ProjectConfiguration));
		ProjectConfiguration config;
		using (StreamReader reader = new StreamReader(args[0])) {
			config = (ProjectConfiguration)serializer.Deserialize(reader);
		}

		////	generate config file first time

		//	using (StreamWriter writer = new StreamWriter(@"c:\temp\configOut.xml", false)) {
		//		serializer.Serialize(writer, CreateConfigFromCode());
		//	}
		if (config != null) {
			new UmlProject(config).Run();
		} else Console.WriteLine("no configuration obtained");
	}
	public static ProjectConfiguration CreateConfigFromCode(){
		 ProjectConfiguration config = new ProjectConfiguration(@"C:\temp");
		// programConfiguration.Solution = @"somesolution.sln";  // optional to go thru solution
		config.AddProjects(@"Proj1.csproj"); // when going with solution, just use the name without extension
		config.AddProjects(@"Proj2.csproj");
		var diagram1 = new InheritanceDiagramConfiguration(
				"SomeNamespace.SomeClassOrInterface",
				"SomeOutputFile", // extension .puml will be added
				false, // no template args in inheritance relations, too long to decipher
				EMemberType.None, // only class names no methods, properties... 
				Accessibilities.Public | Accessibilities.Explicit | Accessibilities.Internal,
				false // no static members
			);
		diagram1.FollowOtherTypesMode = EFollowBaseTypeMode.Parent;
		diagram1.FollowAnchorTypeMode = EFollowBaseTypeMode.Recursive;
		config.AddDiagrams(diagram1);
		config.Options.DumpNamedTypeFileName = @"c:\namedtypes.lst";
		return config;
	}
}