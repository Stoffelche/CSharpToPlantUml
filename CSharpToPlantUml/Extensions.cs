using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CSharpToPlantUml {
	public static class Extensions {
		public static string GetFullMetadataName(this ISymbol s) {
			if (s == null || IsRootNamespace(s)) {
				return string.Empty;
			}
			//var sb = new StringBuilder(s.MetadataName); used 2 sb's to see where the results differ
			var sb2 = new StringBuilder(s.MetadataName);
			var last = s;

			s = s.ContainingSymbol;

			while (!IsRootNamespace(s)) {
//				if (s is ITypeSymbol && last is ITypeSymbol) {
////					sb.Insert(0, '+');
//					sb.Insert(0, '.');  // use . for nested classes as well, as + offends plantuml
//				} else {
//					sb.Insert(0, '.');
//				}
				sb2.Insert(0, '.');
				//sb.Insert(0, s.OriginalDefinition.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat));
				sb2.Insert(0, s.MetadataName);
				//sb.Insert(0, s.MetadataName);
				last = s;
				s = s.ContainingSymbol;
			}

			//var s1 = sb.ToString();
			var s2 = sb2.ToString();
			return s2;
		}
		public static string GetTypeParameterString(this INamedTypeSymbol symbol) {
			if (!symbol.IsGenericType || symbol.TypeParameters.Length == 0) {
				return string.Empty ;
			}
			StringBuilder sb = new StringBuilder("<");
			for (int i = 0; i < symbol.TypeParameters.Length; i++) {
				var parm = symbol.TypeParameters[i];
				if (i > 0) sb.Append(",");
				sb.Append(parm.Name);
			}
			sb.Append(">");
			return sb.ToString();
		}
		public static string GetTypeArgumentString(this INamedTypeSymbol symbol) {
			if (!symbol.IsGenericType || symbol.TypeArguments.Length == 0) {
				return string.Empty;
			}
			StringBuilder sb = new StringBuilder("<");
			for (int i = 0; i < symbol.TypeArguments.Length; i++) {
				var parm = symbol.TypeArguments[i];
				if (i > 0) sb.Append(",");
				sb.Append(parm.Name);
			}
			sb.Append(">");
			return sb.ToString();
		}
		private static bool IsRootNamespace(ISymbol symbol) {
			INamespaceSymbol s = null;
			return ((s = symbol as INamespaceSymbol) != null) && s.IsGlobalNamespace;
		}
		public static List<INamedTypeSymbol> GetBaseTypes(this INamedTypeSymbol symbol) {
			var rv = new List<INamedTypeSymbol>();
			var baseTyp = symbol.BaseType;
			if (baseTyp != null) {
				rv.Add(baseTyp);
			}
			foreach (var interfaceSymbol in symbol.Interfaces) {
				if (interfaceSymbol != null) {
					rv.Add(interfaceSymbol);
				}
			}
			return rv;
		}
		public static SyntaxNode? GetSyntaxNode(this ISymbol symbol) {
			var refs = symbol.DeclaringSyntaxReferences;
			if (refs.Length != 1) return null;
			return refs[0].GetSyntax();
		}
		public static VariableDeclaratorSyntax? FindField(this FieldDeclarationSyntax syntax, string name) {
			foreach (var field in syntax.Declaration.Variables) {
				if (field.Identifier.Text == name) return field;
			}
			return null;
		}
	}
}
