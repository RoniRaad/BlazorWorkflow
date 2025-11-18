namespace BlazorExecutionFlow.Models.DTOs;

/// <summary>
/// This class previously contained nested types for Drawflow export/import.
/// All types have been moved to separate files for better maintainability.
/// This file is kept for backward compatibility but can be removed if not referenced.
/// </summary>
[Obsolete("Use the individual types (DrawflowDocument, DrawflowGraph, DrawflowNode, etc.) directly instead of nesting them under DfExport.")]
public sealed record DfExport
{
}
