using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace BatUtils.Services
{
    internal static class FolderPicker
    {
        private const int Cancelled = unchecked((int)0x800704C7);

        public static bool TryPick(Window owner, string title, string initialPath, out string path)
        {
            IFileDialog dialog = (IFileDialog)new FileOpenDialog();

            try
            {
                dialog.GetOptions(out FileDialogOptions options);
                dialog.SetOptions(options | FileDialogOptions.PickFolders |
                    FileDialogOptions.ForceFileSystem | FileDialogOptions.PathMustExist);
                dialog.SetTitle(title);
                SetInitialFolder(dialog, initialPath);

                IntPtr ownerHandle = owner == null
                    ? IntPtr.Zero
                    : new WindowInteropHelper(owner).Handle;

                int result = dialog.Show(ownerHandle);
                if (result == Cancelled)
                {
                    path = null;
                    return false;
                }

                Marshal.ThrowExceptionForHR(result);
                dialog.GetResult(out IShellItem item);

                try
                {
                    item.GetDisplayName(ShellItemDisplayName.FileSystemPath, out IntPtr value);

                    try
                    {
                        path = Marshal.PtrToStringUni(value);
                        return true;
                    }
                    finally
                    {
                        Marshal.FreeCoTaskMem(value);
                    }
                }
                finally
                {
                    Marshal.ReleaseComObject(item);
                }
            }
            finally
            {
                Marshal.ReleaseComObject(dialog);
            }
        }

        private static void SetInitialFolder(IFileDialog dialog, string path)
        {
            if (string.IsNullOrWhiteSpace(path) || !Directory.Exists(path))
                return;

            Guid shellItemId = typeof(IShellItem).GUID;
            int result = SHCreateItemFromParsingName(path, IntPtr.Zero, ref shellItemId, out IShellItem item);
            if (result < 0)
                return;

            try
            {
                dialog.SetFolder(item);
            }
            finally
            {
                Marshal.ReleaseComObject(item);
            }
        }

        [DllImport("shell32.dll", CharSet = CharSet.Unicode, PreserveSig = true)]
        private static extern int SHCreateItemFromParsingName(
            string path,
            IntPtr bindingContext,
            ref Guid interfaceId,
            [MarshalAs(UnmanagedType.Interface)] out IShellItem shellItem);

        [Flags]
        private enum FileDialogOptions : uint
        {
            PickFolders = 0x20,
            ForceFileSystem = 0x40,
            PathMustExist = 0x800,
        }

        private enum ShellItemDisplayName : uint
        {
            FileSystemPath = 0x80058000,
        }

        [ComImport]
        [Guid("DC1C5A9C-E88A-4DDE-A5A1-60F82A20AEF7")]
        private class FileOpenDialog
        {
        }

        [ComImport]
        [Guid("42F85136-DB7E-439C-85F1-E4075D135FC8")]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface IFileDialog
        {
            [PreserveSig]
            int Show(IntPtr owner);

            void SetFileTypes(uint count, IntPtr filters);
            void SetFileTypeIndex(uint index);
            void GetFileTypeIndex(out uint index);
            void Advise(IntPtr events, out uint cookie);
            void Unadvise(uint cookie);
            void SetOptions(FileDialogOptions options);
            void GetOptions(out FileDialogOptions options);
            void SetDefaultFolder(IShellItem item);
            void SetFolder(IShellItem item);
            void GetFolder(out IShellItem item);
            void GetCurrentSelection(out IShellItem item);
            void SetFileName([MarshalAs(UnmanagedType.LPWStr)] string name);
            void GetFileName([MarshalAs(UnmanagedType.LPWStr)] out string name);
            void SetTitle([MarshalAs(UnmanagedType.LPWStr)] string title);
            void SetOkButtonLabel([MarshalAs(UnmanagedType.LPWStr)] string text);
            void SetFileNameLabel([MarshalAs(UnmanagedType.LPWStr)] string label);
            void GetResult(out IShellItem item);
            void AddPlace(IShellItem item, uint alignment);
            void SetDefaultExtension([MarshalAs(UnmanagedType.LPWStr)] string extension);
            void Close(int result);
            void SetClientGuid(ref Guid guid);
            void ClearClientData();
            void SetFilter(IntPtr filter);
        }

        [ComImport]
        [Guid("43826D1E-E718-42EE-BC55-A1E261C37BFE")]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface IShellItem
        {
            void BindToHandler(IntPtr bindingContext, ref Guid handlerId, ref Guid interfaceId, out IntPtr result);
            void GetParent(out IShellItem parent);
            void GetDisplayName(ShellItemDisplayName name, out IntPtr value);
            void GetAttributes(uint mask, out uint attributes);
            void Compare(IShellItem item, uint hint, out int order);
        }
    }
}
