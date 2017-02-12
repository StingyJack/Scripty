namespace Scripty.Core.Tests
{
    using System;
    using System.Collections.Generic;
    using System.IO;

    public class TestHelpers
    {
        public const string TEST_FILE_CONTENT = "TESTCONTENT";
        public string ProjectFilePath { get; }
        private string _TestFileSubfolder;

        /// <summary>
        ///     Initializes a new instance of the <see cref="TestHelpers"/> class.
        /// </summary>
        /// <param name="testFileSubfolder">The test file subfolder relative to the project root.</param>
        public TestHelpers(string testFileSubfolder = "")
        {
            ProjectFilePath = Path.Combine(GetProjectRootFolder(), "Scripty.Core.Tests.csproj");
            _TestFileSubfolder = testFileSubfolder;
        }

        public ScriptEngine BuildScriptEngine()
        {
            var se = new ScriptEngine(ProjectFilePath);
            return se;
        }

        public static string GetProjectRootFolder()
        {
            return Path.GetFullPath($"{AppDomain.CurrentDomain.BaseDirectory}/../../");
        }

        public string GetTestFileSubFolder()
        {
            return Path.Combine(GetProjectRootFolder(), _TestFileSubfolder);
        }

        public string GetTestFilePath(string fileName)
        {
            return Path.Combine(GetTestFileSubFolder(), fileName);
        }

        public string GetFileContent(string fileName)
        {
            return File.ReadAllText(GetTestFilePath(fileName));
        }

        public void WriteFileContent(string fileName, string fileContent)
        {
            File.WriteAllText(fileName, fileContent);
        }

        public void RemoveFiles(List<string> filesToRemoveIfPresent)
        {

            if (filesToRemoveIfPresent != null)
            {
                foreach (var file in filesToRemoveIfPresent)
                {
                    var filePath = GetTestFilePath(file);

                    if (File.Exists(filePath))
                    {
                        File.Delete(filePath);
                    }
                }
            }

        }


        public void RemoveFiles(string filePattern)
        {
            foreach (var file in Directory.GetFiles(GetTestFileSubFolder(), filePattern))
            {
                File.Delete(file);
            }
        }

        public void CreateFiles(List<string> filesToCreateIfNotPresent)
        {
            foreach (var file in filesToCreateIfNotPresent)
            {
                var filePath = GetTestFilePath(file);

                if (File.Exists(filePath) == false)
                {
                    File.WriteAllText(filePath, TEST_FILE_CONTENT);
                }
            }

        }

    }
}