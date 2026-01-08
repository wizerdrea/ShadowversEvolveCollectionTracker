namespace ShadowverseEvolveCardTracker.Services
{
    public interface IFolderDialogService
    {
        // Returns selected folder path or null/empty if cancelled
        string? ShowFolderDialog(string? description = null);
    }
}