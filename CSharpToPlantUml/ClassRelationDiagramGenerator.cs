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
	public class ClassRelationDiagramGenerator : ClassDiagramGenerator {
		class RelationSymbol {
			public ISymbol Symbol;
			public bool AsEnumerable;
			public INamedTypeSymbol? IEnumerableArgType;
			public RelationSymbol(ISymbol symbol, bool asEnumerable, INamedTypeSymbol? iEnumerableArgType)	{
				Symbol = symbol;
				AsEnumerable = asEnumerable;
				IEnumerableArgType = iEnumerableArgType;
			}
		}
		public new readonly ClassRelationDiagramConfiguration Config;
		public string AnchorType => Config.AnchorType;
		NamedTypeVisitor mNamedTypeVisitor;
		HashSet<string> mVisitedTypes = new HashSet<string>();
		protected SimpleTypeMatcher? mEndpointTypeMatcher = null;

		Queue<KeyValuePair<string, INamedTypeSymbol>> mTypesToVisit = new Queue<KeyValuePair<string, INamedTypeSymbol>>();
		Queue<RelationSymbol> mRelationsSymbols = new Queue<RelationSymbol> { };

		public ClassRelationDiagramGenerator(UmlProject project, TextWriter writer, ClassRelationDiagramConfiguration config)
	: base(project, writer, config) {
			Config = config;
			mNamedTypeVisitor = project.GetNamedTypeVisitor();
			if (config.Endpoints != null && config.Endpoints.Count > 0) {
				mEndpointTypeMatcher = new SimpleTypeMatcher(config.TypeMatching, config.Endpoints);
			}
		}
		public override void RunDiagram() {
			INamedTypeSymbol? typeSymbol = mNamedTypeVisitor.FindType(AnchorType);
			if (typeSymbol != null) {
				Enqueue(AnchorType, typeSymbol);
			}
			if (mTypesToVisit.Count == 0) {
				Project.LogWarn(string.Format("Type '{0}' not found", AnchorType));
			}
			Generate();
		}
		/// <summary>
		/// used for type hierarchy diagrams
		/// </summary>
		/// <param name="namedTypes"></param>
		public void Generate() {
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
			while (mTypesToVisit.Count > 0) {
				var kv = mTypesToVisit.Dequeue();
				VisitNamedType(kv.Key, kv.Value);
			}
			while (mRelationsSymbols.Count > 0) {
				RelationSymbol rSymbol = mRelationsSymbols.Dequeue();
				ISymbol symbol = rSymbol.Symbol;
				INamedTypeSymbol? typeSymbol;
				INamedTypeSymbol containingType;
				string? symbolName;
				string arrowSymbol = rSymbol.AsEnumerable ? "\"1\"-->\"many\"" : "-->";
				if (symbol is IFieldSymbol fieldSymbol) {
					typeSymbol = (INamedTypeSymbol)fieldSymbol.Type;
					containingType = symbol.ContainingType;
					symbolName = symbol.Name;
				} else if (symbol is IPropertySymbol propertySymbol) {
					typeSymbol = (INamedTypeSymbol)propertySymbol.Type;
					containingType = symbol.ContainingType;
					if (propertySymbol.IsIndexer && symbol.Name == "this[]") {
						symbolName = "this";
						arrowSymbol = "\"1\"-->\"many\"";
					} else {
						symbolName = symbol.Name;
					}
				} else {
					typeSymbol = null;
					containingType = (INamedTypeSymbol)symbol;
					symbolName = null;
				}
				string sourceName = symbolName == null ? containingType.GetFullMetadataName() : string.Format("{0}::{1}", containingType.GetFullMetadataName(), symbolName);
				INamedTypeSymbol? targetType = rSymbol.AsEnumerable ? rSymbol.IEnumerableArgType : typeSymbol;
				if(targetType != null ) { 
					WriteLine(string.Format("\"{0}\" {1} \"{2}\"", sourceName, arrowSymbol, targetType.GetFullMetadataName()));
				}
			}
			WriteLine("@enduml");
		}
		enum EEnqueueResult { Excluded, AlreadyVisited, Enqueued }
		EEnqueueResult Enqueue(string name, INamedTypeSymbol type) {
			if (!IncludeType(name, type)) return EEnqueueResult.Excluded;
			if (!mVisitedTypes.Contains(name)) {
				mVisitedTypes.Add(name);
				mTypesToVisit.Enqueue(new KeyValuePair<string, INamedTypeSymbol>(name, type));
				return EEnqueueResult.Enqueued;
			} else {
				return EEnqueueResult.AlreadyVisited;
			}
		}
		void VisitNamedType(string name, INamedTypeSymbol symbol) {
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
			foreach(INamedTypeSymbol collectionType in symbol.AllInterfaces.Where(t => t.IsGenericType && t.OriginalDefinition?.ToString() == "System.Collections.Generic.IEnumerable<T>")) { 
				INamedTypeSymbol? typeArgument = collectionType.TypeArguments[0] as INamedTypeSymbol;
				if (typeArgument != null && Enqueue(typeArgument.GetFullMetadataName(), typeArgument) != EEnqueueResult.Excluded) {
					mRelationsSymbols.Enqueue(new RelationSymbol(symbol, true, typeArgument));
				}
			}

		}
		protected override bool DoVisitField(IFieldSymbol symbol) {
			bool rv = base.DoVisitField(symbol);
			if (rv) CheckRelationSymbol(symbol);
			return rv;
		}
		protected override bool DoVisitProperty(IPropertySymbol symbol) {
			bool rv = base.DoVisitProperty(symbol);
			if (rv) CheckRelationSymbol(symbol);
			return rv;
		}
		void CheckRelationSymbol(ISymbol symbol) {
			if (IsEndpoint(symbol.ContainingType)) return;
			ITypeSymbol typeSymbol;
			if (symbol is IFieldSymbol fieldSymbol) typeSymbol = fieldSymbol.Type;
			else typeSymbol = (INamedTypeSymbol)((IPropertySymbol)symbol).Type;
			if (typeSymbol is INamedTypeSymbol namedType) {
				if (typeSymbol.TypeKind == TypeKind.Enum) return;
				string typeName = namedType.GetFullMetadataName();
				if (Enqueue(typeName, namedType) != EEnqueueResult.Excluded) {
					mRelationsSymbols.Enqueue(new RelationSymbol(symbol, false, null));
				} else {
					INamedTypeSymbol collectionType = namedType.AllInterfaces.FirstOrDefault(t => t.IsGenericType && t.OriginalDefinition?.ToString() == "System.Collections.Generic.IEnumerable<T>");
					if (collectionType != null) { 
						INamedTypeSymbol? typeArgument = collectionType.TypeArguments[0] as INamedTypeSymbol;
						if (typeArgument != null && Enqueue(typeArgument.GetFullMetadataName(), typeArgument) != EEnqueueResult.Excluded) {
							mRelationsSymbols.Enqueue(new RelationSymbol(symbol, true, typeArgument));
						}
					}
				}
			}
		}
		bool IsEndpoint(string typeName) {
			if (mEndpointTypeMatcher == null) return false;
			return mEndpointTypeMatcher.Match(typeName);
		}
		bool IsEndpoint(INamedTypeSymbol typeSymbol) {
			if (mEndpointTypeMatcher == null) return false;
			return IsEndpoint(typeSymbol.GetFullMetadataName());
		}
	}
}


