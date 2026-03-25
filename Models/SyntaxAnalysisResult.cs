using System.Collections.Generic;

namespace TextEditorLab.Models
{
    public class SyntaxAnalysisResult
    {
        public bool Success => Errors.Count == 0;
        public List<SyntaxError> Errors { get; } = new();
    }
}