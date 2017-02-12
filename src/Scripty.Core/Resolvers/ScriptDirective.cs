namespace Scripty.Core.Resolvers
{
    /// <summary>
    ///     A script directive
    /// </summary>
    public class ScriptDirective
    {
        public DirectiveType Type { get; set; }
        public string RawValue { get; set; }
        public string UnresolvedValue { get; set; }
        public string ResolvedValue { get; set; }
        //public List<string> ResolvedCandidates
        public string CallingScriptPath { get;  }
        /// <summary>
        ///     Gets or sets the calling script line number. Zero based
        /// </summary>
        public int CallingScriptLineNumber { get; set; }

        public ScriptDirective(string callingScriptPath, int callingScriptLineNumber, string unresolvedValue, DirectiveType type)
        {
            CallingScriptPath = callingScriptPath;
            CallingScriptLineNumber = callingScriptLineNumber;
            UnresolvedValue = unresolvedValue;
            Type = type;
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="ScriptDirective"/> class.
        /// </summary>
        public ScriptDirective()
        {
            
        }

    }
}