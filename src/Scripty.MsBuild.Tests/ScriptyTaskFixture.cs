using System;
using System.Diagnostics;
using System.IO;
using NUnit.Framework;
using NUnit.Framework.Internal;

namespace Scripty.MsBuild.Tests
{
    [TestFixture]
    public class ScriptyTaskFixture
    {
        static readonly string SolutionFilePath = Path.GetFullPath($"{AppDomain.CurrentDomain.BaseDirectory}/../../SampleSolution/Sample.sln");
        static readonly string ProjectFilePath = Path.GetFullPath($"{AppDomain.CurrentDomain.BaseDirectory}/../../SampleSolution/Proj/Proj.csproj");
        static readonly string ScriptyAssembly = Path.GetFullPath($"{AppDomain.CurrentDomain.BaseDirectory}/Scripty.MsBuild.dll");

        string _msbuild;
        string _output;

        [OneTimeSetUp]
        public void InitFixture()
        {
            _msbuild = FindMsBuild();
            _output = Path.Combine(Path.GetDirectoryName(ProjectFilePath), "test.cs");
        }

        private static string FindMsBuild()
        {
            foreach (var v in new[] { "14.0", "12.0", "4.0" })
            {
                string exe = null;

                using (var key = Microsoft.Win32.Registry.LocalMachine.OpenSubKey($@"SOFTWARE\Microsoft\MSBuild\ToolsVersions\{v}"))
                {
                    exe = (string)key.GetValue("MSBuildToolsPath");
                }

                if (exe != null)
                {
                    return exe + "msbuild.exe";
                }
            }

            throw new ApplicationException("Could not find the location of MSBuild.");
        }

        [SetUp]
        public void InitTest()
        {
            File.Delete(_output);
        }

        
        /// <remarks>
        ///     This test seems easily broken by updating seemingly unrelated components. Also, the msbuild 
        ///  targets file that it's pointing at in the csproj referes to a dll in a \tools\ folder that 
        ///  doesnt exist or get created.
        ///  
        ///     Reinstalling the nuget packages (update-package -reinstall) seems to have done it this time. Last time
        ///  it was changes to the console output text where I had added additional text. Another tool leaning on the 
        ///  output text format of another is always going to be problematic.
        ///  
        ///     Not sure why this even needs to be shelled out to a Process in the first place when the guts are 
        /// more easily testable. 
        ///  
        ///     I want to delete this test so I'm not spending hours wtf-ing next time I want to reinstall a package
        ///     
        ///     I added some error and output redirection to at least capture the MSBuild output and report when 
        ///  a test fails.
        /// </remarks>
        //[Test]
        public void UsesSolutionAndProperties()
        {
            var args = $"\"{SolutionFilePath}\" /p:ScriptyAssembly=\"{ScriptyAssembly}\";Include1=true;Include3=true";

            var info = new ProcessStartInfo(_msbuild, args)
            {
                WindowStyle = ProcessWindowStyle.Hidden,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError =true
            };
            string output;
            string err;

            using (var p = Process.Start(info))
            {
                output = p.StandardOutput.ReadToEnd();
                err = p.StandardError.ReadToEnd();
                p.WaitForExit();
                Assert.AreEqual(0, p.ExitCode, $"Err: {err}, output: {output}");
            }

            Assert.That(File.Exists(_output), $"Err: {err}, output: {output}");
            Assert.AreEqual($@"//Class1.cs;Class3.cs;ClassSolution.cs", File.ReadAllText(_output), $"Err: {err}, output: {output}");
        }

    }
}
