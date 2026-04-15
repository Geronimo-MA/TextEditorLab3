using System.Collections.Generic;

namespace TextEditorLab.Models
{
    public class SemanticAnalysisResult
    {
        public List<SemanticError> Errors { get; set; } = new();
        public bool HasErrors => Errors.Count > 0;
    }
}