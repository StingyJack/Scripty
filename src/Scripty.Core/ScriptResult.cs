namespace Scripty.Core
{
    using System;
    using System.Collections.Generic;
    using Output;

    public class ScriptResult
    {
        public ICollection<IOutputFileInfo> OutputFiles { get; }
        public ICollection<ScriptError> Errors { get; }

        internal ScriptResult(ICollection<IOutputFileInfo> outputFiles)
            : this(outputFiles, Array.Empty<ScriptError>())
        {
        }

        protected internal ScriptResult(ICollection<IOutputFileInfo> outputFiles, ICollection<ScriptError> errors)
        {
            OutputFiles = outputFiles;
            Errors = errors;
        }


        internal void AddErrors(ICollection<ScriptError> errors)
        {
            foreach (var error in errors)
            {
                Errors.Add(error);
            }
        }
    }
}