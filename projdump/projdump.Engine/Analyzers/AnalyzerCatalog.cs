using projdump.Engine.Analyzers.CSharp;
using projdump.Engine.Analyzers.ReactNative;
using projdump.Engine.Analyzers.Vue;
using projdump.Engine.Core;

namespace projdump.Engine.Analyzers;

// The single place that knows which project types ship with the tool. Lives outside
// Core so Core never has to reference a concrete stack.
public static class AnalyzerCatalog
{
	/// <summary>
	/// Builds a registry holding every project type the tool ships with.
	/// </summary>
	/// <returns>A registry populated in detection-precedence order.</returns>
	public static ProjectTypeRegistry CreateRegistry() =>
		// Order is detection precedence: C# first so a directory holding both a
		// solution and a package.json resolves to C#, as it always has.
		new([new CSharpAnalyzer(), new VueProjectAnalyzer(), new ReactNativeProjectAnalyzer()]);
}