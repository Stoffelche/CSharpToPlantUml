using Microsoft.Build.Construction;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace CSharpToPlantUml {
	public class InheritanceDiagramGenerator : ClassDiagramGenerator {
		public new readonly InheritanceDiagramConfiguration Config;
		public string BaseType => Config.BaseType;
		private bool ExcludeSystemObjectFromInheritance => Config.ExcludeSystemObject;
		private EFollowBaseTypeMode FollowAnchorTypeMode => Config.FollowAnchorTypeMode;
		private EFollowBaseTypeMode FollowOtherTypesMode => Config.FollowOtherTypesMode;
		NamedTypeVisitor mNamedTypeVisitor;
		public InheritanceDiagramGenerator(UmlProject project, TextWriter writer, InheritanceDiagramConfiguration config)
	: base(project, writer, config) {
			Config = config;
			mNamedTypeVisitor = project.GetNamedTypeVisitor();
		}
		public override void RunDiagram() {
			var typeList = mNamedTypeVisitor.GetRelatedTypes(BaseType);
			if (typeList.Count == 0) {
				Project.LogWarn(string.Format("Type '{0}' not found", BaseType));
			}
			Generate(BaseType, typeList);
		}
		/// <summary>
		/// used for type hierarchy diagrams
		/// </summary>
		/// <param name="namedTypes"></param>
		public void Generate(string anchortype, IReadOnlyDictionary<string, INamedTypeSymbol> inputTypes) {
			WriteLine("@startuml");
			if (!string.IsNullOrEmpty(Title)) {
				WriteLine(string.Format("title {0}", Title));
			}
			if (DiagramDirection != EDiagramDirection.Default) {
				WriteLine(DiagramDirection == EDiagramDirection.TopToBottom ? "top to bottom direction" : "left to right direction");
			}
			if (!EnableNameSpace) {
				WriteLine("set namespaceSeparator none");
			}
			if (Scale != 1) {
				WriteLine(string.Format(System.Globalization.CultureInfo.InvariantCulture, "scale {0}", Scale));
			}
			Dictionary<string, INamedTypeSymbol> outputTypes = new Dictionary<string, INamedTypeSymbol>();
			foreach (var kv in inputTypes) {
				if (IncludeType(kv.Key, kv.Value)) {
					outputTypes.Add(kv.Key, kv.Value);
				}
			}
			HashSet<string>? checkSet = null;
			if (Config.InheritanceRelations) {
				for (int i = 0; i < 2; i++) {
					EFollowBaseTypeMode followMode = i == 0 ? FollowAnchorTypeMode : FollowOtherTypesMode;
					if (followMode == EFollowBaseTypeMode.None) continue;
					Dictionary<string, INamedTypeSymbol> prevTypes = new Dictionary<string, INamedTypeSymbol>(); ;
					if (i == 0) {
						if (inputTypes.TryGetValue(anchortype, out var anchor) && anchor != null)
							prevTypes.Add(anchortype, anchor);
					} else {
						foreach (var kv in inputTypes) {
							if (kv.Key != anchortype) prevTypes.Add(kv.Key, kv.Value);
						}
					}
					int recursionDepth = 0;
					while (true) {
						recursionDepth++;
						if (followMode == EFollowBaseTypeMode.Parent && recursionDepth > 1) break;
						Dictionary<string, INamedTypeSymbol> additionalTypes = new Dictionary<string, INamedTypeSymbol>();
						foreach (var key in prevTypes.Keys) {
							foreach (var baseType in mNamedTypeVisitor.GetBaseTypes(key)) {
								if (baseType.Value.SpecialType == SpecialType.System_Object && ExcludeSystemObjectFromInheritance) continue;
								if (!IncludeType(baseType.Key, baseType.Value)) continue;
								if (!outputTypes.ContainsKey(baseType.Key) && !additionalTypes.ContainsKey(baseType.Key)) {
									additionalTypes.Add(baseType.Key, baseType.Value);
								}
							}
						}
						if (additionalTypes.Count == 0) break;
						else {
							foreach (var baseType in additionalTypes) {
								outputTypes.Add(baseType.Key, baseType.Value);
							}
							prevTypes = additionalTypes;
						}
					}
				}
				checkSet = new HashSet<string>();
				foreach (var type in outputTypes.Keys) {
					checkSet.Add(type);
				}
			}
			foreach (var item in outputTypes) {
				VisitNamedType(item.Key, item.Value, ERelationMode.Check, checkSet);
			}
			WriteLine("@enduml");
		}
		void VisitNamedType(string name, INamedTypeSymbol symbol, ERelationMode skipRelations, HashSet<string>? checkSet = null) {
			string typ, additionalType = string.Empty;
			switch (symbol.TypeKind) {
				case TypeKind.Interface: typ = "interface"; break;
				case TypeKind.Struct: typ = "class"; additionalType = "struct"; break;
				default: typ = (symbol.IsAbstract ? "abstract " : "") + "class"; break;
			}
			if (!string.IsNullOrEmpty(additionalType)) {
				additionalType = string.Format("<<{0}>> ", additionalType);
			}
			string link = string.Empty;
			if (RenderTypeLinks) {
				link = name;
				for (int i = 0; i < RenderSearchChars.Length; i++) {
					link = link.Replace(RenderSearchChars[i], RenderReplaceChars[i]);
				}
				link = Regex.Replace(link, RenderRegexForTypes, RenderRegexForTypeReplacement);
				link = string.Format(" [[{0}]]", link);
			}
			string typeName = string.Format("\"{0}\"{1}", name, symbol.GetTypeParameterString());
			WriteLine($"{typ} {typeName} {additionalType}{link} {{");
			mNestingDepth++;
			if (MemberTypes != EMemberType.None) {
				foreach (var childSymbol in symbol.GetMembers()) {
					childSymbol.Accept(this);
				}
			}
			mNestingDepth--;
			WriteLine("}");
			if (skipRelations != ERelationMode.Skip) {
				List<INamedTypeSymbol> basetypes = symbol.GetBaseTypes();
				foreach (INamedTypeSymbol baseType in basetypes) {
					if (ExcludeSystemObjectFromInheritance && baseType.SpecialType == SpecialType.System_Object)
						continue;
					string baseTypeName = baseType.GetFullMetadataName();
					if (skipRelations == ERelationMode.Check && checkSet != null && !checkSet.Contains(baseTypeName)) {
						continue;
					}
					string baseTypeArguments = ShowTemplateArgsInInheritanceRelations
					? baseType.GetTypeArgumentString() : string.Empty;
					if (!string.IsNullOrEmpty(baseTypeArguments)) {
						baseTypeArguments = string.Format(" \"{0}\"", baseTypeArguments);
					}
					string divider = "<|--";
					string subLabel = string.Empty;
					string centerLabel = string.Empty;
					WriteLine(string.Format("\"{0}\"{1} {2} \"{3}\"",
					baseTypeName, baseTypeArguments, divider, name));
				}
			}
		}
	}
}


