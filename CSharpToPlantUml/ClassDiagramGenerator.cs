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
	public abstract class ClassDiagramGenerator : SymbolVisitor {
		public enum ERelationMode { Skip, Show, Check }
		public readonly UmlProject Project;
		public readonly DiagramConfiguration Config;
		public ProjectConfiguration ProjectConfig => Project.Configuration;
		protected readonly Dictionary<string, string> mEscapeDictionary = new Dictionary<string, string>
		{
						{@"(?<before>[^{]){(?<after>{[^{])", "${before}&#123;${after}"},
						{@"(?<before>[^}])}(?<after>[^}])", "${before}&#125;${after}"},
				};
		protected readonly TextWriter Writer;
		protected readonly string mIndent = "  ";
		protected int mNestingDepth = 0;
		public Accessibilities MemberAccessibilities => Config.Accessibilities;
		public EMemberType MemberTypes => Config.MemberTypes;
		public bool AllowStaticMembers => Config.StaticMembers;
		public bool EnableNameSpace => Config.EnableNameSpace;
		public EDiagramDirection DiagramDirection => Config.Direction;
		public string Title => Config.Title;
		public bool ShowTemplateArgsInInheritanceRelations => Config.TemplateArgsInInheritanceRelations;
		public decimal Scale=>Config.Scale;
		public bool RenderTypeLinks => ProjectConfig.RenderTypeLinks;
		public string RenderSearchChars => ProjectConfig.RenderSearchChars ??  "`";
		public string RenderReplaceChars => ProjectConfig.RenderReplaceChars ??  "-";
		public string RenderRegexForTypes => ProjectConfig.RenderRegexForTypes ?? string.Empty;
		public string RenderRegexForTypeReplacement => ProjectConfig.RenderRegexForTypeReplacement;
		protected SimpleTypeMatcher? mExcludeTypeMatcher = null;
		protected SimpleTypeMatcher? mIncludeTypeMatcher = null;
		public ClassDiagramGenerator(UmlProject project, TextWriter writer, DiagramConfiguration config)	 {
			this.Writer = writer;
			Project = project;
			Config = config;
			if (config.IncludeTypes != null && config.IncludeTypes.Count > 0) {
				mIncludeTypeMatcher = new SimpleTypeMatcher(config.TypeMatching, config.IncludeTypes);
			}
			if (config.ExcludeTypes != null && config.ExcludeTypes.Count > 0) {
				mExcludeTypeMatcher = new SimpleTypeMatcher(config.TypeMatching, config.ExcludeTypes);
			}
		}
		public abstract void RunDiagram();
		protected void WriteLine(string line) {
			var space = string.Concat(Enumerable.Repeat(mIndent, mNestingDepth));
			Writer.WriteLine(space + line);
		}
		protected bool IncludeType(string typeName, INamedTypeSymbol type) {
			return (mExcludeTypeMatcher == null || !mExcludeTypeMatcher.Match(typeName))
					&& (mIncludeTypeMatcher == null || mIncludeTypeMatcher.Match(typeName));
		}
		public override void VisitMethod(IMethodSymbol symbol) {
			DoVisitMethod(symbol);
		}
		protected virtual bool DoVisitMethod(IMethodSymbol symbol) {
			if ((symbol.MethodKind == MethodKind.PropertyGet && symbol.Parameters.Length == 0)
			|| (symbol.MethodKind == MethodKind.PropertySet && symbol.Parameters.Length == 1)) return false;
			if (!AllowAccess(symbol)) return false;
			if (symbol.IsStatic && !AllowStaticMembers) return false;
			if (symbol.MethodKind == MethodKind.Constructor && !MemberTypes.HasFlag(EMemberType.Ctor)) return false;
			else if (!MemberTypes.HasFlag(EMemberType.Method)) return false;
			string modifiers = GetMemberModifiersText(symbol, true);
			string name = symbol.Name;
			if (symbol.MethodKind == MethodKind.Constructor) {
				name = symbol.ContainingType.Name;
			}
			bool omitNamespace = Config.OmitNameSpaceInMembers;
			string returnType = omitNamespace ? symbol.ReturnType.Name : symbol.ReturnType.ToString();
			var argList = omitNamespace ?
				symbol.Parameters.Select(p => $"{p.Name}:{p.Type.Name}") :
				symbol.Parameters.Select(p => $"{p.Name}:{p.Type.ToString()}");
			string args = string.Join(", ", argList);
			WriteLine($"{modifiers}{name}({args}) : {returnType}");
			return true;
		}
		protected virtual bool DoVisitProperty(IPropertySymbol symbol) {
			if (!AllowAccess(symbol)) return false;
			if (symbol.IsStatic && !AllowStaticMembers) return false;
			if (!MemberTypes.HasFlag(EMemberType.Property)) return false;
			string modifiers = GetMemberModifiersText(symbol, true);
			string name = symbol.Name;
			bool omitNamespace = Config.OmitNameSpaceInMembers;
			string type = omitNamespace ? symbol.Type.Name : symbol.Type.ToString();
			string accessorStr = string.Empty;
			if (symbol.IsWriteOnly) accessorStr = "<<set>>";
			else if (symbol.IsReadOnly) {
				accessorStr = "<<get>>";
			}
			PropertyDeclarationSyntax? syntaxNode = symbol.GetSyntaxNode() as PropertyDeclarationSyntax;
			string initValue = string.Empty;
			if (syntaxNode != null && syntaxNode.Initializer != null) {
				var useLiteralInit = syntaxNode.Initializer.Value?.Kind().ToString().EndsWith("LiteralExpression") ?? false;
				initValue = useLiteralInit
						? (" = " + mEscapeDictionary.Aggregate(syntaxNode.Initializer.Value.ToString(),
								(n, e) => Regex.Replace(n, e.Key, e.Value)))
						: "";

			}
			WriteLine($"{modifiers}{name} : {type} {accessorStr}{initValue}");
			return true;
		}
		public override void VisitProperty(IPropertySymbol symbol) {
			DoVisitProperty(symbol);
		}
		protected virtual bool DoVisitField(IFieldSymbol symbol) {
			if (!AllowAccess(symbol)) return false;
			if (symbol.IsStatic && !AllowStaticMembers) return false;
			if (!MemberTypes.HasFlag(EMemberType.Field)) return false;
			string modifiers = GetMemberModifiersText(symbol, true);
			string name = symbol.Name;
			bool omitNamespace = Config.OmitNameSpaceInMembers;
			string type = omitNamespace ? symbol.Type.Name : symbol.Type.ToString();
			VariableDeclaratorSyntax? field = symbol.GetSyntaxNode() as VariableDeclaratorSyntax;
			string initValue = string.Empty;
			if (field != null && field.Initializer != null) {
				var useLiteralInit = field.Initializer.Value?.Kind().ToString().EndsWith("LiteralExpression") ?? false;
				initValue = useLiteralInit
										 ? (" = " + mEscapeDictionary.Aggregate(field.Initializer.Value.ToString(),
												 (f, e) => Regex.Replace(f, e.Key, e.Value)))
										 : "";
			}

			WriteLine($"{modifiers}{name} : {type}{initValue}");
			return true;

		}
		public override void VisitField(IFieldSymbol symbol) {
			DoVisitField(symbol);
		}
		protected bool AllowAccess(ISymbol symbol) {
			if (MemberAccessibilities == Accessibilities.All) return true;
			if (MemberAccessibilities == Accessibilities.None)
				return false;
			switch (symbol.DeclaredAccessibility) {
				case Accessibility.NotApplicable:
					return false;
				case Accessibility.Private:
					return MemberAccessibilities.HasFlag(Accessibilities.Private) ||
					(MemberAccessibilities.HasFlag(Accessibilities.Explicit)
					&& ((symbol is IMethodSymbol m && m.ExplicitInterfaceImplementations.Length > 0)
					|| (symbol is IPropertySymbol p && p.ExplicitInterfaceImplementations.Length > 0))
					);
				case Accessibility.ProtectedAndInternal:
					return MemberAccessibilities.HasFlag(Accessibilities.ProtectedInternal);
				case Accessibility.Protected:
					return MemberAccessibilities.HasFlag(Accessibilities.Protected);
				case Accessibility.Internal:
					return MemberAccessibilities.HasFlag(Accessibilities.Internal);
				case Accessibility.ProtectedOrInternal:
					return MemberAccessibilities.HasFlag(Accessibilities.Protected)
					|| MemberAccessibilities.HasFlag(Accessibilities.Internal)
					|| MemberAccessibilities.HasFlag(Accessibilities.ProtectedInternal);
				case Accessibility.Public:
					return MemberAccessibilities.HasFlag(Accessibilities.Public);
				default:
					return false;
			}
		}
		protected string GetMemberModifiersText(ISymbol symbol, bool useAbbreviations) {
			string intern = string.Format("<<{0}>>", useAbbreviations ? "i" : "internal");
			string ab = string.Format("<<{0}>>", useAbbreviations ? "a" : "abstract");
			string virt = string.Format("<<{0}>>", useAbbreviations ? "v" : "virtual");
			string ov = string.Format("<<{0}>>", useAbbreviations ? "o" : "override");
			string ro = string.Format("<<{0}>>", useAbbreviations ? "r" : "readonly");
			string access = string.Empty;
			switch (symbol.DeclaredAccessibility) {
				case Accessibility.NotApplicable:
					break;
				case Accessibility.Private:
					access = "-";
					break;
				case Accessibility.ProtectedAndInternal:
					access = "# " + intern;
					break;
				case Accessibility.ProtectedOrInternal:
				case Accessibility.Protected:
					access = "#";
					break;
				case Accessibility.Internal:
					access = intern;
					break;
				case Accessibility.Public:
					access = "+";
					break;
			}
			string modifier = string.Empty;
			if (symbol.IsAbstract && symbol.ContainingType.TypeKind != TypeKind.Interface) {
				modifier = ab;
			} else if (symbol.IsVirtual) {
				modifier = virt;
			} else if (symbol.IsOverride) {
				modifier = ov;
			}
			if (symbol is IFieldSymbol fieldSymbol) {
				if (fieldSymbol.IsReadOnly) {
					if (modifier.Length > 0) modifier += " ";
					modifier += ro;
				}
				if (fieldSymbol.Type is IEventSymbol) {
					if (modifier.Length > 0) modifier += " ";
					modifier += "<<event>>";
				}
			} else if (symbol is IPropertySymbol propertySymbol) {

				if (propertySymbol.Type is IEventSymbol) {
					if (modifier.Length > 0) modifier += " ";
					modifier += "<<event>>";
				} else {
					// display properties with same icon as methods
					modifier = "{method}" + modifier;
				}
			}
			return string.Format("{0} {1} {2} ", symbol.IsStatic ? "{static}" : string.Empty, access, modifier).TrimStart();
		}
	}
}


