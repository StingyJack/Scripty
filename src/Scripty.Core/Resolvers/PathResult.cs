namespace Scripty.Core.Resolvers
{
    using System.IO;

    public class PathResult
    {
        public string TestFilePath { get; set; }
        public bool WasAttempted { get; set; }

        public bool WasResolved
        {
            get
            {
                if (ResolvedDirectory == null && ResolvedFile == null)
                {
                    return false;
                }
                return true;
            }
        }

        public FileInfo ResolvedFile { get; set; }
        public DirectoryInfo ResolvedDirectory { get; set; }
    }
}