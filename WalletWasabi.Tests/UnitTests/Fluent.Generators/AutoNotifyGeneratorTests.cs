using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System.Linq;
using System.Reflection;
using WalletWasabi.Fluent.Generators;
using Xunit;

namespace WalletWasabi.Tests.UnitTests.Fluent.Generators
{
	/// <summary>
	/// Tests for <see cref="AutoNotifyGenerator"/>.
	/// </summary>
	/// <seealso href="https://github.com/dotnet/roslyn/blob/master/docs/features/source-generators.cookbook.md#unit-testing-of-generators"/>
	public class AutoNotifyGeneratorTests
	{
		[Fact]
		public void SimpleGeneratorTest()
		{
			// Input for our generator.
			Compilation inputCompilation = CreateCompilation(@"
namespace WalletWasabi.Fluent.ViewModels
{
	public class TestViewModel2
	{
		[AutoNotify(PropertyName = ""TestName"")] private bool _prop1;
		[AutoNotify(SetterModifier = AccessModifier.None)] private bool _prop2 = false;
		[AutoNotify(SetterModifier = AccessModifier.Public)] private bool _prop3;
		[AutoNotify(SetterModifier = AccessModifier.Protected)] private bool _prop4;
		[AutoNotify(SetterModifier = AccessModifier.Private)] private bool _prop5;
		[AutoNotify(SetterModifier = AccessModifier.Internal)] private bool _prop6;
	}
}
");

			AutoNotifyGenerator generator = new();
			GeneratorDriver driver = CSharpGeneratorDriver.Create(generator);

			// Run the generation pass
			driver = driver.RunGeneratorsAndUpdateCompilation(inputCompilation, out var outputCompilation, out var diagnostics);

			Assert.True(diagnostics.IsEmpty);
			Assert.Equal(4, outputCompilation.SyntaxTrees.Count());

			GeneratorDriverRunResult runResult = driver.GetRunResult();
			Assert.Equal(3, runResult.GeneratedTrees.Length);
			Assert.True(runResult.Diagnostics.IsEmpty);

			GeneratorRunResult generatorResult = runResult.Results[0];
			Assert.True(generatorResult.Exception is null);
			Assert.Equal(generatorResult.Generator, generator);
			Assert.True(generatorResult.Diagnostics.IsEmpty);
			Assert.Equal(3, generatorResult.GeneratedSources.Length);

			string expectedGeneratedSourceCode = @"
// <auto-generated />
#nullable enable
using ReactiveUI;

namespace WalletWasabi.Fluent.ViewModels
{
    public partial class TestViewModel2 : ReactiveUI.ReactiveObject
    {
        public bool TestName
        {
            get => _prop1;
            set => this.RaiseAndSetIfChanged(ref _prop1, value);
        }
        public bool Prop2
        {
            get => _prop2;
        }
        public bool Prop3
        {
            get => _prop3;
            set => this.RaiseAndSetIfChanged(ref _prop3, value);
        }
        public bool Prop4
        {
            get => _prop4;
            protected set => this.RaiseAndSetIfChanged(ref _prop4, value);
        }
        public bool Prop5
        {
            get => _prop5;
            private set => this.RaiseAndSetIfChanged(ref _prop5, value);
        }
        public bool Prop6
        {
            get => _prop6;
            internal set => this.RaiseAndSetIfChanged(ref _prop6, value);
        }
    }
}".Trim();

			Assert.Equal(expectedGeneratedSourceCode, generatorResult.GeneratedSources[2].SourceText.ToString());
		}

		private static Compilation CreateCompilation(string source)
		{
			SyntaxTree[] syntaxTrees = new[] { CSharpSyntaxTree.ParseText(source) };
			CSharpCompilationOptions options = new(OutputKind.ConsoleApplication);
			PortableExecutableReference[] references = new[]
			{
				MetadataReference.CreateFromFile(typeof(Binder).GetTypeInfo().Assembly.Location),
				MetadataReference.CreateFromFile(typeof(ReactiveUI.ReactiveObject).GetTypeInfo().Assembly.Location),
			};

			return CSharpCompilation.Create("compilation", syntaxTrees, references, options);
		}
	}
}