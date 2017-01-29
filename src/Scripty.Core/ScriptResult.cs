using System;
using System.Collections.Generic;
using Scripty.Core.Output;

namespace Scripty.Core
{
    public class ScriptResult
    {
        public ICollection<IOutputFileInfo> OutputFiles { get; }
        public ICollection<ScriptError> Errors { get; }

        internal ScriptResult(ICollection<IOutputFileInfo> outputFiles)
            : this(outputFiles, Array.Empty<ScriptError>())
        {
        }

        internal ScriptResult(ICollection<IOutputFileInfo> outputFiles, ICollection<ScriptError> errors)
        {
            OutputFiles = outputFiles;
            Errors = errors;
        }

        public override string ToString()
        {
            var sb = new System.Text.StringBuilder();
            sb.AppendLine($"OutputFiles: {OutputFiles.Count}, Errors {Errors.Count}");
            
            foreach (var of in OutputFiles)
            {
                sb.AppendLine($"OutFileTemp : {of.TempFilePath}");
                sb.AppendLine($"OutFileTarget : {of.TargetFilePath}");
            }

            foreach (var err in Errors)
            {
                sb.AppendLine($"Err - {err.Line}:{err.Column} - {err.Message} ");
            }
            return sb.ToString();
        }
    }
}