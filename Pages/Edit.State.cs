using System.Text.Json;
using Microsoft.JSInterop;
using WordPressPCL;
using WordPressPCL.Models;
using WordPressPCL.Utility;

namespace BlazorWP.Pages;

public partial class Edit
{
    private void ResetEditorState()
    {
        postId = null;
        postTitle = string.Empty;
        _content = string.Empty;
        lastSavedTitle = string.Empty;
        lastSavedContent = string.Empty;
        showRetractReview = false;
        isEditing = false;
    }

    private async Task SetEditorContentAsync(string html)
    {
        if (editorComp == null) return;
        await editorComp.SetContentAsync(html);
    }
}
