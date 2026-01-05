using System;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using System.Windows.Interop;

namespace ShadowversEvolveCardTracker.Services
{
    public sealed class FolderDialogService : IFolderDialogService
    {
        public string? ShowFolderDialog(string? description = null)
        {
            var owner = IntPtr.Zero;
            if (Application.Current?.MainWindow != null)
            {
                owner = new WindowInteropHelper(Application.Current.MainWindow).Handle;
            }

            var bi = new BROWSEINFO
            {
                hwndOwner = owner,
                lpszTitle = description ?? "Select folder",
                ulFlags = BIF_RETURNONLYFSDIRS | BIF_NEWDIALOGSTYLE | BIF_USENEWUI
            };

            IntPtr pidl = SHBrowseForFolder(ref bi);
            if (pidl == IntPtr.Zero)
                return null;

            var path = new StringBuilder(260);
            bool ok = SHGetPathFromIDList(pidl, path);
            CoTaskMemFree(pidl);
            return ok ? path.ToString() : null;
        }

        // P/Invoke for SHBrowseForFolder
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private struct BROWSEINFO
        {
            public IntPtr hwndOwner;
            public IntPtr pidlRoot;
            public IntPtr pszDisplayName;
            [MarshalAs(UnmanagedType.LPWStr)]
            public string lpszTitle;
            public uint ulFlags;
            public IntPtr lpfn;
            public IntPtr lParam;
            public int iImage;
        }

        private const uint BIF_RETURNONLYFSDIRS = 0x00000001;
        private const uint BIF_NEWDIALOGSTYLE = 0x00000040;
        private const uint BIF_EDITBOX = 0x00000010;
        private const uint BIF_USENEWUI = BIF_NEWDIALOGSTYLE | BIF_EDITBOX;

        [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
        private static extern IntPtr SHBrowseForFolder(ref BROWSEINFO lpbi);

        [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool SHGetPathFromIDList(IntPtr pidl, StringBuilder pszPath);

        [DllImport("ole32.dll")]
        private static extern void CoTaskMemFree(IntPtr pv);
    }
}