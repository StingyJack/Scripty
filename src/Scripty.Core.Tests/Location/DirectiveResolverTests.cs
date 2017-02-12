namespace Scripty.Core.Tests.Location
{
    using System.Collections.Generic;
    using NUnit.Framework;
    using ProjectTree;
    using Resolvers;

    public class DirectiveResolverTests : BaseFixture
    {
        public TestHelpers TestHelpers { get; set; } = new TestHelpers("TestCsx");

        private string _validProjectFilePath;
        private string _simpleSuccessScriptFilePath;
        private string _simpleSuccessScriptFileCode;

        [OneTimeSetUp]
        public void Setup()
        {
            _simpleSuccessScriptFilePath = TestHelpers.GetTestFilePath("SimpleSuccess.csx");
            _simpleSuccessScriptFileCode = TestHelpers.GetFileContent(_simpleSuccessScriptFilePath);
            _validProjectFilePath = TestHelpers.ProjectFilePath;
        }
        
        [Test]
        public void ParseDirectives()
        {
            var scriptSource = BuildSimpleValidScriptSource();

            var result = InterceptDirectiveResolver.ParseDirectives(scriptSource.FilePath);
            
            Assert.IsNull(result);
        }

        private ProjectRoot BuildValidProjectRoot()
        {
            return new ProjectRoot(_validProjectFilePath);
        }

        private ScriptSource BuildSimpleValidScriptSource()
        {
            return new ScriptSource(_simpleSuccessScriptFilePath, _simpleSuccessScriptFileCode);
        }
    }
}
