using System.Threading.Tasks;

namespace BlazorWP.Pages;

public partial class Edit
{
    private async Task EnsureEditorContentAsync()
    {
        if (editorComp != null)
        {
            await editorComp.SetContentAsync(_content);
            pendingEditorContent = false;
        }
        else
        {
            pendingEditorContent = true;
        }
    }
}
