# CSharpToPlantUml
Generates plantuml inheritance diagrams and class relation diagrams from C# projects.<br>
This projects works with Compilation generated with Roslyn from C# projects or solutions.<br>
Inheritance iagrams are generated from a base type, by including all descendants (recursive inheriting types) in one plantuml diagram.
The code generated for planutuml was initially borrowed from https://github.com/pierre3/PlantUmlClassDiagramGenerator project.
The main differences to this project are:
1. Usage of symbol tree instead of syntax tree from Roslyn, in order to correctly create type names including namespaces for the base types and types.
2. Combining a set of types (classes and intefaces) by their inheritance relations in one inheritance diagram by providing a single "seed" type only.
3. Directly generate svg as well by use of PlantUml server.

The code for remotely accessing plantuml server was borrowed from https://github.com/KevReed/PlantUml.Net. 
But as only the remote feature is used here, the project itself is not needed.

Class relationship diagrams connect classes related by properties and fields.
