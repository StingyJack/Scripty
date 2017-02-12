namespace Scripty.Core.Resolvers
{
    using System;
    using System.IO;
    using System.Reflection;
    using ProjectTree;

    /// <summary>
    ///     Provides best guess absolute path determination when some relative paths are present
    /// </summary>
    public class PathResolver
    {
        public ScriptSource ScriptSource { get; }
        public ProjectRoot Project { get; }

        public PathResolver(ScriptSource source, ProjectRoot project = null)
        {
            ScriptSource = source;
            Project = project;
        }

        public PathResult GetBestGuessForFilePath(string testFilePath)
        {
            var pathResult = new PathResult { TestFilePath = testFilePath, WasAttempted = true };
            if (string.IsNullOrWhiteSpace(testFilePath))
            {
                return pathResult;
            }

            //easy and very specific path first
            var isRooted = Path.IsPathRooted(testFilePath);
            var fullPath = Path.GetFullPath(testFilePath);
            var exists = File.Exists(fullPath);

            if (isRooted && (string.IsNullOrWhiteSpace(fullPath) == false) && exists)
            {
                pathResult.ResolvedFile = new FileInfo(fullPath);
                return pathResult;
            }

            //now we start guessing

            //if relative, 

            return pathResult;
        }

        public PathResult RelativeToCurrentlyExecutingAssembly(string testFilePath)
        {
            var pathResult = new PathResult { TestFilePath = testFilePath, WasAttempted = true };
            if (string.IsNullOrWhiteSpace(testFilePath))
            {
                return pathResult;
            }

            var asmFolder = GetAssemblyFolder(AsmGet.Executing);

            var withAsmLoc = Path.Combine(asmFolder, testFilePath);

            return pathResult;
        }


        //add app domain assembly resolver helper
        // and put in AsmGet.Named perhaps

        #region "static path gets"

        public static string GetAssemblyFolder(AsmGet asmGet, string assemblyQualifiedName = null)
        {
            string asmLoc;
            switch (asmGet)
            {
                case AsmGet.Entry:
                    asmLoc = Assembly.GetEntryAssembly().Location;
                    break;
                case AsmGet.Calling:
                    asmLoc = Assembly.GetCallingAssembly().Location;
                    break;
                case AsmGet.Executing:
                    asmLoc = Assembly.GetExecutingAssembly().Location;
                    break;
                case AsmGet.Named:

                    if (string.IsNullOrWhiteSpace(assemblyQualifiedName))
                    {
                        throw new ArgumentNullException(nameof(assemblyQualifiedName));
                    }

                    try
                    {
                        //hey! Resolve this with itself !!!
                        asmLoc = Assembly.Load(assemblyQualifiedName).Location;
                    }
                    catch (Exception)
                    {
                        asmLoc = Assembly.GetEntryAssembly().Location;
                    }

                    break;
                default:
                    throw new NotImplementedException($"Enum value for {asmGet} not supported");
            }


            var asmFolder = Path.GetDirectoryName(asmLoc);
            return asmFolder;
        }



        #endregion #region "static path gets"
    }
}