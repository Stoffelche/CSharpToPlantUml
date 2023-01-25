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
	public class ClassDiagramGenerator : SymbolVisitor {
		private readonly Dictionary<string, string> mEscapeDictionary = new Dictionary<string, string>
		{
						{@"(?<before>[^{]){(?<after>{[^{])", "${before}&#123;${after}"},
						{@"(?<before>[^}])}(?<after>[^}])", "${before}&#125;${after}"},
				};

		private readonly TextWriter mWriter;
		private string mTitle;
		private readonly string mIndent = "  ";
		private int mNestingDepth = 0;
		private readonly Accessibilities mMemberAccessibilities;
		private readonly EMemberType mMemberTypes;
		readonly NamedTypeVisitor mNamedTypeVisitor;
		private readonly bool mAllowStaticMembers;
		private readonly bool mGenerateInheritanceRelations;
		private readonly bool mShowTemplateArgsInInheritanceRelations = true;
		private readonly bool mExcludeSystemObjectFromInheritance = true;
		private readonly EFollowBaseTypeMode mFollowAnchorTypeMode = EFollowBaseTypeMode.None;
		private readonly EFollowBaseTypeMode mFollowOtherTypesMode = EFollowBaseTypeMode.None;
		private readonly bool mEnableNameSpace = true;
		private readonly decimal mScale = 1.0m;
		bool mRenderTypeLinks = false;
		string mRenderSearchChars = "`";
		string mRenderReplaceChars = "-";
		string mRenderRegexForTypes = string.Empty;
		string mRenderRegexForTypeReplacement = string.Empty;
		private EDiagramDirection mDiagramDirection = EDiagramDirection.Default;
		SimpleTypeMatcher? mExcludeTypeMatcher = null;
		public ClassDiagramGenerator(TextWriter writer, NamedTypeVisitor namedTypeVisitor, ProjectConfiguration pConfig, DiagramConfiguration config)
	: this(writer, namedTypeVisitor,
	config.MemberTypes,
	config.Accessibilities,
	config.StaticMembers,
	config.InheritanceRelations) {
			mEnableNameSpace = config.EnableNameSpace;
			mDiagramDirection = config.Direction;
			mTitle = config.Title;
			if (config.InheritanceRelations)
				mShowTemplateArgsInInheritanceRelations = config.TemplateArgsInInheritanceRelations;
			if (config is InheritanceDiagramConfiguration inheritance) {
				mExcludeSystemObjectFromInheritance = inheritance.ExcludeSystemObject;
				mFollowAnchorTypeMode = inheritance.FollowAnchorTypeMode;
				mFollowOtherTypesMode = inheritance.FollowOtherTypesMode;
				if (inheritance.ExcludeTypes.Count > 0) {
					mExcludeTypeMatcher = new SimpleTypeMatcher(inheritance.TypeMatching, inheritance.ExcludeTypes);
				}
			}
			mScale = config.Scale;
			mRenderTypeLinks = pConfig.RenderTypeLinks;
			if (mRenderTypeLinks) {
				mRenderSearchChars = pConfig.RenderSearchChars;
				mRenderReplaceChars = pConfig.RenderReplaceChars;
				mRenderRegexForTypes = pConfig.RenderRegexForTypes;
				mRenderRegexForTypeReplacement = pConfig.RenderRegexForTypeReplacement;
			}
		}
		public ClassDiagramGenerator(TextWriter mWriter, NamedTypeVisitor mNamedTypeVisitor,
		EMemberType memberTypes,
		Accessibilities allowMembers,
		bool allowStaticMembers,
		bool generateInheritanceRelations) {
			this.mWriter = mWriter;
			this.mNamedTypeVisitor = mNamedTypeVisitor;
			mMemberTypes = memberTypes;
			mAllowStaticMembers = allowStaticMembers;
			this.mMemberAccessibilities = allowMembers;
			mGenerateInheritanceRelations = generateInheritanceRelations;
		}
		private void WriteLine(string line) {
			var space = string.Concat(Enumerable.Repeat(mIndent, mNestingDepth));
			mWriter.WriteLine(space + line);
		}
		/// <summary>
		/// used for type hierarchy diagrams
		/// </summary>
		/// <param name="namedTypes"></param>
		public void Generate(string anchortype, IReadOnlyDictionary<string, INamedTypeSymbol> inputTypes) {
			WriteLine("@startuml");
			if (!string.IsNullOrEmpty(mTitle)) {
				WriteLine(string.Format("title {0}", mTitle));
			}
			if (mDiagramDirection != EDiagramDirection.Default) {
				WriteLine(mDiagramDirection == EDiagramDirection.TopToBottom ? "top to bottom direction" : "left to right direction");
			}
			if (!mEnableNameSpace) {
				WriteLine("set namespaceSeparator none");
			}
			if (mScale != 1) {
				WriteLine(string.Format(System.Globalization.CultureInfo.InvariantCulture, "scale {0}", mScale));
			}
			Dictionary<string, INamedTypeSymbol> outputTypes = new Dictionary<string, INamedTypeSymbol>();
			foreach (var kv in inputTypes) {
				if (mExcludeTypeMatcher == null || !mExcludeTypeMatcher.Match(kv.Key)) {
					outputTypes.Add(kv.Key, kv.Value);
				}
			}
			if (mGenerateInheritanceRelations) {
				for (int i = 0; i < 2; i++) {
					EFollowBaseTypeMode followMode = i == 0 ? mFollowAnchorTypeMode : mFollowOtherTypesMode;
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
								if (baseType.Value.SpecialType == SpecialType.System_Object && mExcludeSystemObjectFromInheritance) continue;
								if (mExcludeTypeMatcher != null && mExcludeTypeMatcher.Match(baseType.Key)) continue;
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
				HashSet<string> checkSet = new HashSet<string>();
				foreach (var type in outputTypes.Keys) {
					checkSet.Add(type);
				}
				foreach (var item in outputTypes) {
					VisitNamedType(item.Key, item.Value, ERelationMode.Check, checkSet);
				}
			} else {
				foreach (var item in outputTypes) {
					VisitNamedType(item.Key, item.Value, ERelationMode.Skip);
				}
			}
			WriteLine("@enduml");
		}
		enum ERelationMode { Skip, Show, Check }
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
			if (mRenderTypeLinks) {
				link = name;
				for (int i = 0; i < mRenderSearchChars.Length; i++) {
					link = link.Replace(mRenderSearchChars[i], mRenderReplaceChars[i]);
				}
				link = Regex.Replace(link, mRenderRegexForTypes, mRenderRegexForTypeReplacement);
				link = string.Format(" [[{0}]]", link);
			}
			string typeName = string.Format("\"{0}\"{1}", name, symbol.GetTypeParameterString());
			WriteLine($"{typ} {typeName} {additionalType}{link} {{");
			mNestingDepth++;
			if (mMemberTypes != EMemberType.None) {
				foreach (var childSymbol in symbol.GetMembers()) {
					childSymbol.Accept(this);
				}
			}
			mNestingDepth--;
			WriteLine("}");
			if (mGenerateInheritanceRelations && skipRelations != ERelationMode.Skip) {
				var basetypes = symbol.GetBaseTypes();
				foreach (var baseType in basetypes) {
					if (mExcludeSystemObjectFromInheritance && baseType.SpecialType == SpecialType.System_Object)
						continue;
					string baseTypeName = baseType.GetFullMetadataName();
					if (skipRelations == ERelationMode.Check && checkSet != null && !checkSet.Contains(baseTypeName)) {
						continue;
					}
					string baseTypeArguments = mShowTemplateArgsInInheritanceRelations
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
		public override void VisitMethod(IMethodSymbol symbol) {
			if ((symbol.MethodKind == MethodKind.PropertyGet
			|| symbol.MethodKind == MethodKind.PropertySet) && symbol.Parameters.Length == 0) return;
			if (!AllowAccess(symbol)) return;
			if (symbol.IsStatic && !mAllowStaticMembers) return;
			if (symbol.MethodKind == MethodKind.Constructor && !mMemberTypes.HasFlag(EMemberType.Ctor)) return;
			else if (!mMemberTypes.HasFlag(EMemberType.Method)) return;
			string modifiers = GetMemberModifiersText(symbol);
			string name = symbol.Name;
			if (symbol.MethodKind == MethodKind.Constructor) {
				name = symbol.ContainingType.Name;
			}
			string returnType = symbol.ReturnType.ToString();
			var argList = symbol.Parameters.Select(p => $"{p.Name}:{p.Type}");
			string args = string.Join(", ", argList);
			WriteLine($"{modifiers}{name}({args}) : {returnType}");

		}
		public override void VisitProperty(IPropertySymbol symbol) {
			if (!AllowAccess(symbol)) return;
			if (symbol.IsStatic && !mAllowStaticMembers) return;
			if (!mMemberTypes.HasFlag(EMemberType.Property)) return;
			string modifiers = GetMemberModifiersText(symbol);
			string name = symbol.Name;
			string type = symbol.Type.ToString();
			string accessorStr = string.Empty;
			if (!symbol.IsWriteOnly) accessorStr = "<<get>>";
			if (!symbol.IsReadOnly) {
				if (accessorStr.Length > 0) accessorStr += " ";
				accessorStr += "<<set>>";
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
		}
		public override void VisitField(IFieldSymbol symbol) {
			if (!AllowAccess(symbol)) return;
			if (symbol.IsStatic && !mAllowStaticMembers) return;
			if (!mMemberTypes.HasFlag(EMemberType.Field)) return;
			string modifiers = GetMemberModifiersText(symbol);
			string name = symbol.Name;
			string type = symbol.Type.ToString();
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


		}
		bool AllowAccess(ISymbol symbol) {
			if (mMemberAccessibilities == Accessibilities.All) return true;
			if (mMemberAccessibilities == Accessibilities.None)
				return false;
			switch (symbol.DeclaredAccessibility) {
				case Accessibility.NotApplicable:
					return false;
				case Accessibility.Private:
					return mMemberAccessibilities.HasFlag(Accessibilities.Private) ||
					(mMemberAccessibilities.HasFlag(Accessibilities.Explicit)
					&& ((symbol is IMethodSymbol m && m.ExplicitInterfaceImplementations.Length > 0)
					|| (symbol is IPropertySymbol p && p.ExplicitInterfaceImplementations.Length > 0))
					);
				case Accessibility.ProtectedAndInternal:
					return mMemberAccessibilities.HasFlag(Accessibilities.ProtectedInternal);
				case Accessibility.Protected:
					return mMemberAccessibilities.HasFlag(Accessibilities.Protected);
				case Accessibility.Internal:
					return mMemberAccessibilities.HasFlag(Accessibilities.Internal);
				case Accessibility.ProtectedOrInternal:
					return mMemberAccessibilities.HasFlag(Accessibilities.Protected)
					|| mMemberAccessibilities.HasFlag(Accessibilities.Internal)
					|| mMemberAccessibilities.HasFlag(Accessibilities.ProtectedInternal);
				case Accessibility.Public:
					return mMemberAccessibilities.HasFlag(Accessibilities.Public);
				default:
					return false;
			}
		}
		string GetMemberModifiersText(ISymbol symbol) {
			string access = string.Empty;
			switch (symbol.DeclaredAccessibility) {
				case Accessibility.NotApplicable:
					break;
				case Accessibility.Private:
					access = "-";
					break;
				case Accessibility.ProtectedAndInternal:
					access = "# <<internal>>";
					break;
				case Accessibility.ProtectedOrInternal:
				case Accessibility.Protected:
					access = "#";
					break;
				case Accessibility.Internal:
					access = "<<internal>>";
					break;
				case Accessibility.Public:
					access = "+";
					break;
			}
			string modifier = string.Empty;
			if (symbol.IsAbstract && symbol.ContainingType.TypeKind != TypeKind.Interface) {
				modifier = "{abstract}";
			} else if (symbol.IsVirtual) {
				modifier = "<<virtual>>";
			} else if (symbol.IsOverride) {
				modifier = "<<override>>";
			}
			if (symbol is IFieldSymbol fieldSymbol) {
				if (fieldSymbol.IsReadOnly) {
					if (modifier.Length > 0) modifier += " ";
					modifier += "<<readonly>>";
				}
				if (fieldSymbol.Type is IEventSymbol) {
					if (modifier.Length > 0) modifier += " ";
					modifier += "<<event>>";
				}
			} else if (symbol is IPropertySymbol propertySymbol) {
				if (propertySymbol.Type is IEventSymbol) {
					if (modifier.Length > 0) modifier += " ";
					modifier += "<<event>>";
				}
			}
			return string.Format("{0} {1} {2} ", symbol.IsStatic ? "{static}" : string.Empty, access, modifier).TrimStart();
		}
	}
}


