namespace Scripty.Core.Resolvers
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Diagnostics;
    using System.IO;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.Text;

    /// <summary>
    ///     Examines the directives present in the script source and attempts
    /// to locate specific files for any loosely defined or relative paths.
    /// </summary>
    /// <remarks>
    /// The goal here is to take a named script source, find its target directives and resolve thier
    /// paths (SourceFileResolver and ScriptSourceResolver does do this), as well as pick
    ///     NamedScriptSource.csx
    /// </remarks>
    public class InterceptDirectiveResolver : SourceFileResolver
    {
        //public ScriptSource ScriptSource { get; }
        //public ProjectRoot Project { get; }
        //private PathResolver _pathResolver;

        /*
        /// <summary>
        ///     Initializes a new instance of the <see cref="DirectiveResolver"/> class.
        /// </summary>
        /// <param name="source">The source.</param>
        /// <param name="project">The project, if available.</param>
        public DirectiveResolver(ScriptSource source, ProjectRoot project = null)
        {
            ScriptSource = source;
            Project = project;
            _pathResolver = new PathResolver(source, project);
        }
        */

        //http://www.strathweb.com/2016/06/implementing-custom-load-behavior-in-roslyn-scripting/

        #region "fields"

        private readonly Dictionary<string, RewrittenFile> _rewrittenFiles = new Dictionary<string, RewrittenFile>(StringComparer.OrdinalIgnoreCase);
        private readonly SourceFileResolver _sourcefileResolver;

        #endregion //#region "fields"

        #region "ctors"

        /// <summary>
        ///     Initializes a new instance of the <see cref="InterceptDirectiveResolver"/> class.
        /// </summary>
        /// <param name="searchPaths">The search paths.</param>
        /// <param name="baseDirectory">The base directory.</param>
        public InterceptDirectiveResolver(IEnumerable<string> searchPaths, string baseDirectory) : base(searchPaths, baseDirectory)
        {
            _sourcefileResolver = new SourceFileResolver(searchPaths, baseDirectory);
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="InterceptDirectiveResolver"/> class.
        /// </summary>
        /// <param name="searchPaths">The search paths.</param>
        /// <param name="baseDirectory">The base directory.</param>
        public InterceptDirectiveResolver(ImmutableArray<string> searchPaths, string baseDirectory) : base(searchPaths, baseDirectory)
        {
            _sourcefileResolver = new SourceFileResolver(searchPaths, baseDirectory);
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="InterceptDirectiveResolver"/> class.
        /// </summary>
        /// <param name="searchPaths">The search paths.</param>
        /// <param name="baseDirectory">The base directory.</param>
        /// <param name="pathMap">The path map.</param>
        public InterceptDirectiveResolver(ImmutableArray<string> searchPaths, string baseDirectory, ImmutableArray<KeyValuePair<string, string>> pathMap) : base(searchPaths, baseDirectory, pathMap)
        {
            _sourcefileResolver = new SourceFileResolver(searchPaths, baseDirectory);
        }

        #endregion //#region "ctors"


        /// <summary>
        /// Normalizes specified source path with respect to base file path.
        /// </summary>
        /// <param name="path">The source path to normalize. May be absolute or relative.</param>
        /// <param name="baseFilePath">Path of the source file that contains the <paramref name="path"/> (may also be relative), or null if not available.</param>
        /// <returns>Normalized path, or null if <paramref name="path"/> can't be normalized. The resulting path doesn't need to exist.</returns>
        public override string NormalizePath(string path, string baseFilePath)
        {
            var normalizedPath = base.NormalizePath(path, baseFilePath);
            var candidateType = GetResolutionTargetType(path);

            if (candidateType == ResolutionTargetType.Cs)
            {
                var rewrittenFile = GetRewrittenFile(normalizedPath);

                if (rewrittenFile == null)
                {
                    return normalizedPath;
                }

                return rewrittenFile.RewrittenFilePath;
            }

            return normalizedPath;
        }

        private RewrittenFile GetRewrittenFile(string normalizedPath)
        {
            if (_rewrittenFiles.ContainsKey(normalizedPath))
            {
                return _rewrittenFiles[normalizedPath];
            }

            var rewritePath = CsRewriter.GetRewriteFilePath(normalizedPath);
            var rewrite = new RewrittenFile { RewrittenFilePath = rewritePath, OriginalFilePath = normalizedPath };

            if (CreateRewriteFile(rewrite) == false)
            {
                return null;
            }

            _rewrittenFiles.Add(normalizedPath, rewrite);

            return rewrite;
        }


        private bool CreateRewriteFile(RewrittenFile rewrite)
        {
            try
            {
                var rewriteResult = CsRewriter.CreateRewriteFile(rewrite);
                if (rewriteResult != null)
                {
                    return true;
                }
            }
            catch (Exception e)
            {
                Trace.TraceError($"Failed to create rewrite file. {e}");
            }

            return false;
        }

        /// <summary>
        /// Opens a <see cref="Stream"/> that allows reading the content of the specified file.
        /// </summary>
        /// <param name="resolvedPath">Path returned by <see cref="ResolveReference(string, string)"/>.</param>
        /// <exception cref="ArgumentNullException"><paramref name="resolvedPath"/> is null.</exception>
        /// <exception cref="ArgumentException"><paramref name="resolvedPath"/> is not a valid absolute path.</exception>
        /// <exception cref="IOException">Error reading file <paramref name="resolvedPath"/>. See <see cref="Exception.InnerException"/> for details.</exception>
        public override Stream OpenRead(string resolvedPath)
        {
            return base.OpenRead(resolvedPath);
        }

        /// <summary>
        /// Reads the contents of <paramref name="resolvedPath"/> and returns a <see cref="SourceText"/>.
        /// </summary>
        /// <param name="resolvedPath">Path returned by <see cref="ResolveReference(string, string)"/>.</param>
        public override SourceText ReadText(string resolvedPath)
        {
            return base.ReadText(resolvedPath);
        }

        /// <summary>
        /// Resolves specified path with respect to base file path.
        /// </summary>
        /// <param name="path">The path to resolve. May be absolute or relative.</param>
        /// <param name="baseFilePath">Path of the source file that contains the <paramref name="path"/> (may also be relative), or null if not available.</param>
        /// <returns>Normalized path, or null if the file can't be resolved.</returns>
        public override string ResolveReference(string path, string baseFilePath)
        {
            return base.ResolveReference(path, baseFilePath);
        }

        #region "file handling"

        private ResolutionTargetType GetResolutionTargetType(string resolutionCandidateFilePath)
        {
            if (resolutionCandidateFilePath.EndsWith(".cs", StringComparison.OrdinalIgnoreCase))
            {
                return ResolutionTargetType.Cs;
            }
            if (resolutionCandidateFilePath.EndsWith(".csx", StringComparison.OrdinalIgnoreCase))
            {
                return ResolutionTargetType.Csx;
            }
            return ResolutionTargetType.Other;
        }

        #endregion // #region "file handling"

        #region "muh stuff"

        /// <summary>
        ///     Parses the directives from the 
        /// </summary>
        /// <param name="scriptFullPath">The script full path.</param>
        /// <returns></returns>
        public static List<ScriptDirective> ParseDirectives(string scriptFullPath)
        {
            var directives = new List<ScriptDirective>();

            using (var sr = new StreamReader(scriptFullPath))
            {
                var lineNumber = -1;
                while (sr.EndOfStream == false)
                {
                    lineNumber++;
                    var line = sr.ReadLine();
                    var trimmedLine = line.Trim();

                    if (trimmedLine.StartsWith("using", StringComparison.OrdinalIgnoreCase)
                        || trimmedLine.StartsWith("n", StringComparison.OrdinalIgnoreCase) //namespace
                        || trimmedLine.StartsWith("p", StringComparison.OrdinalIgnoreCase) //public
                        || trimmedLine.StartsWith("i", StringComparison.OrdinalIgnoreCase) //internal
                        || trimmedLine.StartsWith("c", StringComparison.OrdinalIgnoreCase) //class
                        || trimmedLine.StartsWith("{"))
                    {
                        break; //thus begins code, no more directives
                    }

                    var directiveType = GetDirectiveType(trimmedLine);
                    if (directiveType == null)
                    {
                        continue;
                    }

                    var unresolvedValue = GetDirectiveReference(trimmedLine, directiveType);

                    var d = new ScriptDirective(scriptFullPath, lineNumber,
                            unresolvedValue, directiveType.Value);

                    directives.Add(d);
                }
            }
            return directives;
        }

        public static DirectiveType? GetDirectiveType(string trimmedLine)
        {
            // keep these in this order for proper result
            //#loadr
            //#load
            //#r
            if (trimmedLine.StartsWith(Consts.DIRECTIVE_LOAD_AS_ASSEMBLY, StringComparison.OrdinalIgnoreCase))
            {
                return DirectiveType.LoadScriptAsAssemblyRef;
            }
            else if (trimmedLine.StartsWith(Consts.DIRECTIVE_SCRIPT_LOAD, StringComparison.OrdinalIgnoreCase))
            {
                return DirectiveType.ScriptRef;
            }
            else if (trimmedLine.StartsWith(Consts.DIRECTIVE_ASSEMBLY_LOAD, StringComparison.OrdinalIgnoreCase))
            {
                return DirectiveType.AssemblyRef;
            }
            return null;
        }

        /// <summary>
        ///     Gets the directive reference.
        /// </summary>
        /// <param name="trimmedLine">The trimmed line.</param>
        /// <param name="directiveType">Type of the directive.</param>
        /// <returns>
        ///     if the trimmed line was ' #load ".\folder\myscript.csx"`, 
        /// this should return .\folder\myscript.csx (without quotes)
        /// </returns>
        public static string GetDirectiveReference(string trimmedLine, DirectiveType? directiveType)
        {
            // #loadr "myclass.csx"
            // #load "myscript.csx"
            //#r "myassembly.dll"

            string directiveText = null;
            switch (directiveType)
            {
                case DirectiveType.AssemblyRef:
                    directiveText = Consts.DIRECTIVE_ASSEMBLY_LOAD;
                    break;
                case DirectiveType.ScriptRef:
                    directiveText = Consts.DIRECTIVE_SCRIPT_LOAD;
                    break;
                case DirectiveType.LoadScriptAsAssemblyRef:
                    directiveText = Consts.DIRECTIVE_LOAD_AS_ASSEMBLY;
                    break;
            }

            var directiveReference = trimmedLine.Replace(directiveText, string.Empty)
                            .Replace("\"", string.Empty).Trim();

            return directiveReference;
        }

        /// <summary>
        ///     Attempts to resolves the directive paths.
        /// </summary>
        /// <param name="directives">The directives.</param>
        /// <returns></returns>
        public ImmutableList<ScriptDirective> ResolveDirectivePaths(string baseFilePath, List<ScriptDirective> directives)
        {
            var scriptDirectory = Path.GetDirectoryName(baseFilePath);

            foreach (var ud in directives)
            {
                if (ud.Type == DirectiveType.AssemblyRef || ud.Type == DirectiveType.ScriptRef)
                {
                    //the built in resolver takes care of these two. 
                    // yet internal or sealed means we have to make all this other
                    // code to add directives 
                    ud.ResolvedValue = ud.UnresolvedValue;
                    continue;
                }

                if (ud.Type == DirectiveType.LoadScriptAsAssemblyRef)
                {
                    var simpleResolution = AttemptSimplePathResolution(ud.UnresolvedValue, scriptDirectory);
                    if (string.IsNullOrWhiteSpace(simpleResolution) == false)
                    {
                        ud.ResolvedValue = simpleResolution;
                        continue;
                    }
                    var pathResolver = new PathResolver(new ScriptSource(baseFilePath, ""));
                    var pathResult = pathResolver.GetBestGuessForFilePath(ud.UnresolvedValue);
                    if (pathResult.WasResolved)
                    {
                        ud.ResolvedValue = pathResult.ResolvedFile.FullName;
                    }
                }
            }

            return directives.ToImmutableList();
        }

        /// <summary>
        ///     Attempts simple path resolution.
        /// </summary>
        /// <param name="unresolvedValue">The unresolved value.</param>
        /// <param name="scriptDirectory">The script directory.</param>
        /// <returns></returns>
        private static string AttemptSimplePathResolution(string unresolvedValue, string scriptDirectory)
        {

            try
            {
                var fullPath = Path.Combine(scriptDirectory, unresolvedValue);
                var resolvedPath = Path.GetFullPath(fullPath);
                if (File.Exists(resolvedPath) == false)
                {
                    throw new FileNotFoundException($"#load directive points to invalid " +
                                                    $"file name ({unresolvedValue}|{resolvedPath})."
                        , unresolvedValue);
                }

                return resolvedPath;
            }
            catch (Exception ex)
            {
                Trace.TraceError($"Failed to resolve path '{unresolvedValue}'. Ex: {ex}");
            }
            return null;
        }

        #endregion //#region "muh stuff"

    }


}
