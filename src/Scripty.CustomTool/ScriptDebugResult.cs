namespace Scripty
{
    using System.Collections.Generic;
    using Core;
    using Core.Output;

    public class ScriptDebugResult : ScriptResult
    {
        public ScriptDebugResult(ICollection<IOutputFileInfo> outputFiles, ICollection<ScriptError> errors) : base(outputFiles, errors)
        {
        }
    }
}