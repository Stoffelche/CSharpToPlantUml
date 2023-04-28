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
	public enum EDiagramType { Inheritance = 1, ClassRelation = 2 }
	[XmlInclude(typeof(InheritanceDiagramConfiguration))]
	[XmlInclude(typeof(ClassRelationDiagramConfiguration))]

	public abstract class DiagramConfiguration {
		public string OutputName;
		public string Title;
		public EMemberType MemberTypes;
		public Accessibilities Accessibilities;
		public bool StaticMembers;
		public EDiagramDirection Direction = EDiagramDirection.Default;
		public bool EnableNameSpace = true;
		public ETypeMatching TypeMatching = ETypeMatching.Exact;  // for included and excluded types
		public List<string> IncludeTypes = new List<string>();
		public List<string> ExcludeTypes = new List<string>();
		public MetaDataDict MetaData = new MetaDataDict ();
		public bool OmitNameSpaceInMembers = false;
		public decimal Scale = 1;
		protected DiagramConfiguration() {
		}
		protected DiagramConfiguration(string outputName, EMemberType memberTypes, Accessibilities accessibilities, bool staticMembers) {
			OutputName = outputName;
			MemberTypes = memberTypes;
			Accessibilities = accessibilities;
			StaticMembers = staticMembers;
		}

		public bool TemplateArgsInInheritanceRelations { get; set; }
		public abstract EDiagramType DiagramType { get; }
	}
	public class InheritanceDiagramConfiguration : DiagramConfiguration {
		public string BaseType;
		public bool ExcludeSystemObject = true;
		public EFollowBaseTypeMode FollowAnchorTypeMode = EFollowBaseTypeMode.None;
		public EFollowBaseTypeMode FollowOtherTypesMode =	EFollowBaseTypeMode.None;
		public bool InheritanceRelations = true;
		public InheritanceDiagramConfiguration() {
		}
		public InheritanceDiagramConfiguration(string baseType, string outputName, bool templateArgsInInheritance, EMemberType memberTypes, Accessibilities accessibilities, bool staticMembers) : base(outputName, memberTypes, accessibilities, staticMembers) {
			BaseType = baseType;
			TemplateArgsInInheritanceRelations = templateArgsInInheritance;
		}
		public override EDiagramType DiagramType => EDiagramType.Inheritance;
	}
	public class ClassRelationDiagramConfiguration : DiagramConfiguration {
		public string AnchorType;
		public ClassRelationDiagramConfiguration() {
		}
		public ClassRelationDiagramConfiguration(string anchorType, string outputName, EMemberType memberTypes, Accessibilities accessibilities, bool staticMembers) : base(outputName, memberTypes, accessibilities, staticMembers) {
			AnchorType = anchorType	;
		}
		public List<string> Endpoints = new List<string>();
		public override EDiagramType DiagramType => EDiagramType.ClassRelation;
	}
}
