namespace WpfVisualTreeMcp.Server.Services;

/// <summary>
/// Normalizes handles that agents commonly confuse with visual-tree element handles.
/// </summary>
public static class ElementHandleNormalizer
{
    /// <summary>
    /// <c>wpf_attach</c> returns <c>main_window_handle</c> as <c>window_0xHWND</c>.
    /// That value is metadata only — it is not registered in the Inspector handle cache
    /// (real handles look like <c>elem_XXXXXXXX</c>). Treat <c>window_*</c> as "whole
    /// window" by mapping it to <c>null</c> so tools like screenshot/tree/export work
    /// when agents pass the attach handle by mistake.
    /// </summary>
    public static string? ForVisualTree(string? handle)
    {
        if (string.IsNullOrWhiteSpace(handle))
            return null;

        if (handle.StartsWith("window_", StringComparison.OrdinalIgnoreCase))
            return null;

        return handle;
    }
}
