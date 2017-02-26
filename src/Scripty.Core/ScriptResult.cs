namespace Scripty.Core
{
    using System;
    using System.Collections.Generic;
    using Output;

    public class ScriptResult
    {
        public ICollection<IOutputFileInfo> OutputFiles { get; }
        public ICollection<ScriptError> Errors { get; private set; }

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

            var errs = new List<ScriptError>(Errors);
            errs.AddRange(errors);
            Errors = errs;

        }
    }
}