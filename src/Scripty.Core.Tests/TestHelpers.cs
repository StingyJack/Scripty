namespace Scripty.Core.Tests
{
    using System;
    using System.Collections.Generic;
    using System.IO;

    public static class TestHelpers
    {
        public const string TEST_FILE_CONTENT = "TESTCONTENT";
        private static readonly string _ProjectFilePath = GetFilePathRelativeToProjectRoot("Scripty.Core.Tests.csproj");

        public static ScriptEngine BuildScriptEngine()
        {
            var se = new ScriptEngine(_ProjectFilePath);
            return se;
        }

        public static string GetProjectRootFolder()
        {
            return Path.GetFullPath($"{AppDomain.CurrentDomain.BaseDirectory}/../../");
        }

        public static string GetFilePathRelativeToProjectRoot(string fileName)
        {
            return Path.Combine(GetProjectRootFolder(), fileName);
        }

        public static string GetFileContent(string fileName)
        {
            return File.ReadAllText(GetFilePathRelativeToProjectRoot(fileName));
        }

        public static void RemoveFiles(List<string> filesToRemoveIfPresent)
        {

            if (filesToRemoveIfPresent != null)
            {
                foreach (var file in filesToRemoveIfPresent)
                {
                    if (File.Exists(file))
                    {
                        File.Delete(file);
                    }
                }
            }

        }

        public static void CreateFiles(List<string> filesToCreateIfNotPresent)
        {
            foreach (var file in filesToCreateIfNotPresent)
            {
                if (File.Exists(file) == false)
                {
                    File.WriteAllText(file, TEST_FILE_CONTENT);
                }
            }

        }

    }
}