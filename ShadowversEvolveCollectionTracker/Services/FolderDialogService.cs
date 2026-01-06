using System;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using System.Windows.Interop;
using Microsoft.WindowsAPICodePack.Dialogs;

namespace ShadowversEvolveCardTracker.Services
{
    public sealed class FolderDialogService : IFolderDialogService
    {
        public string? ShowFolderDialog(string? description = null)
        {
            var dlg = new CommonOpenFileDialog
            {
                IsFolderPicker = true,
                Title = description ?? "Select folder",
                EnsurePathExists = true
            };

            var result = dlg.ShowDialog();
            return result == CommonFileDialogResult.Ok ? dlg.FileName : null;
        }
    }
}