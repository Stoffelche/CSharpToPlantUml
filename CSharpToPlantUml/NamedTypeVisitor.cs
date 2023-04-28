using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CSharpToPlantUml {
	public class NamedTypeVisitor : SymbolVisitor {
		string mCurrentAssemblyName;
		bool mUseAssemblyNameFilter;
		Dictionary<string, INamedTypeSymbol> mNamedTypeDict = new Dictionary<string, INamedTypeSymbol>();
		Dictionary<string, HashSet<string>> mBaseTypes = new Dictionary<string, HashSet<string>>();
		Dictionary<string, HashSet<string>> mDerivedTypes = new Dictionary<string, HashSet<string>>();
		HashSet<string> mVisitedAssemblyNames = new HashSet<string>();
		public NamedTypeVisitor(bool useAssemblyNameFilter) {
			mUseAssemblyNameFilter = useAssemblyNameFilter;
		}
		public void Visit(WorkspaceProject project) {
			mCurrentAssemblyName = project.Project.AssemblyName;
			mVisitedAssemblyNames.Add(mCurrentAssemblyName);
			LogMessage(string.Format("Loading types of assembly {0}", mCurrentAssemblyName));
			Visit(project.Compilation.GlobalNamespace);
		}
		public override void VisitNamespace(INamespaceSymbol symbol) {
			foreach (var childSymbol in symbol.GetMembers()) {
				childSymbol.Accept(this);
			}
		}
		void AddNames(string baseTypeName, string name) {
			if (!mBaseTypes.TryGetValue(name, out var baseTypeNames)) {
				baseTypeNames = new HashSet<string>();
				mBaseTypes[name] = baseTypeNames;
			}
			baseTypeNames.Add(baseTypeName);
			if (!mDerivedTypes.TryGetValue(baseTypeName, out var derivedTypeNames)) {
				derivedTypeNames = new HashSet<string>();
				mDerivedTypes[baseTypeName] = derivedTypeNames;
			}
			derivedTypeNames.Add(name);
		}
		public override void VisitNamedType(INamedTypeSymbol symbol) {
			if (!mUseAssemblyNameFilter || string.IsNullOrEmpty(mCurrentAssemblyName) || symbol.ContainingAssembly.Name == mCurrentAssemblyName) {
				var name = symbol.GetFullMetadataName();
				if (mNamedTypeDict.ContainsKey(name)) {
					if (mUseAssemblyNameFilter) LogWarn(string.Format("Type {0} has already been added. Skip this instance", name));
					return;
				}
				mNamedTypeDict.Add(name, symbol);
				var baseType = symbol.BaseType;
				if (baseType != null) {
					string baseTypeName = baseType.GetFullMetadataName();
					AddNames(baseTypeName, name);
				}
				foreach (var interfaceSymbol in symbol.Interfaces) {
					if (interfaceSymbol != null) {
						AddNames(interfaceSymbol.GetFullMetadataName(), name);
					}
				}
				foreach (var childSymbol in symbol.GetTypeMembers()) {
					childSymbol.Accept(this);
				}
			}
		}
		public INamedTypeSymbol? FindType(string typeName) {
			if(mNamedTypeDict.TryGetValue(typeName, out INamedTypeSymbol? found) )
				return found;
			return null;
		}
		public Dictionary<string, INamedTypeSymbol> GetRelatedTypes(string typeName) {
			var rv = new Dictionary<string, INamedTypeSymbol>();
			if (mNamedTypeDict.TryGetValue(typeName, out var type)) {
				rv.Add(typeName, type);
				RecursAddDerivedTypes(typeName, type, rv);
			}
			return rv;
		}
		void RecursAddDerivedTypes(string name, INamedTypeSymbol symbol, Dictionary<string, INamedTypeSymbol> dict) {
			if (mDerivedTypes.TryGetValue(name, out var derivedTypes)) {
				foreach (var derivedTypeName in derivedTypes) {
					if (mNamedTypeDict.TryGetValue(derivedTypeName, out var derivedType)) {
						dict[derivedTypeName] = derivedType;
						RecursAddDerivedTypes(derivedTypeName, derivedType, dict);
					}
				}
			}
		}
		public List<KeyValuePair<string, INamedTypeSymbol>> GetBaseTypes(string name) {
			List<KeyValuePair<string, INamedTypeSymbol>> rv = new List<KeyValuePair<string, INamedTypeSymbol>>();
			if (mBaseTypes.TryGetValue(name, out var baseTypeNames)) {
				foreach (var baseTypeName in baseTypeNames) {
					if (mNamedTypeDict.TryGetValue(baseTypeName, out var baseType)) {
						rv.Add(new KeyValuePair<string, INamedTypeSymbol>(baseTypeName, baseType));
					}
				}
			}
			return rv;
		}
		// todo handle warnings
		public void LogWarn(string message) {
			System.Console.WriteLine("Warning: " + message);
		}
		public void LogMessage(string message) {
			System.Console.WriteLine(message);
		}
		public void DumpNamedTypes(TextWriter writer) {
			foreach (var key in mNamedTypeDict.Keys) { writer.WriteLine(key); }
		}
	}
}