using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO.Pipelines;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace CSharpToPlantUml {
public enum EDiagramDirection { Default, LeftToRight, TopToBottom }
	public enum EDiagramType { Inheritance = 1 }
	[XmlInclude(typeof(InheritanceDiagramConfiguration))]

	public abstract class DiagramConfiguration {
		public string OutputName;
		public string Title;
		public EMemberType MemberTypes;
		public Accessibilities Accessibilities;
		public bool StaticMembers;
		public EDiagramDirection Direction = EDiagramDirection.Default;
		public bool EnableNameSpace = true;
		public MetaDataDict MetaData = new MetaDataDict ();
		public decimal Scale = 1;
		protected DiagramConfiguration() {
		}
		protected DiagramConfiguration(string outputName, EMemberType memberTypes, Accessibilities accessibilities, bool staticMembers) {
			OutputName = outputName;
			MemberTypes = memberTypes;
			Accessibilities = accessibilities;
			StaticMembers = staticMembers;
		}

		public virtual bool InheritanceRelations { get; set; }
		public bool TemplateArgsInInheritanceRelations { get; set; }
		public abstract EDiagramType DiagramType { get; }
	}
	public class InheritanceDiagramConfiguration : DiagramConfiguration {
		public string BaseType;
		public bool ExcludeSystemObject = true;
		public EFollowBaseTypeMode FollowAnchorTypeMode = EFollowBaseTypeMode.None;
		public EFollowBaseTypeMode FollowOtherTypesMode =	EFollowBaseTypeMode.None;
		public List<string> ExcludeTypes = new List<string>();
		public ETypeMatching TypeMatching = ETypeMatching.Exact;
		public InheritanceDiagramConfiguration() {
		}
		public InheritanceDiagramConfiguration(string baseType, string outputName, bool templateArgsInInheritance, EMemberType memberTypes, Accessibilities accessibilities, bool staticMembers) : base(outputName, memberTypes, accessibilities, staticMembers) {
			BaseType = baseType;
			TemplateArgsInInheritanceRelations = templateArgsInInheritance;
		}
		public override EDiagramType DiagramType => EDiagramType.Inheritance;
		public override bool InheritanceRelations {
			get { return true; }
		}
	}
}
