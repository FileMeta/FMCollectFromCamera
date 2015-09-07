using System;
using System.Text;
using System.Diagnostics;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using System.IO;

// Based on code by Stephen Toub - "IFIleOperation in Windows Vista" - MSDN Magazine December 2007

namespace WinShell
{

    public static class SHFileOperation
    {

        public static void MoveFiles(IntPtr hwndOwner, string progressTitle, IEnumerable<string> sourcePaths, string destinationPath)
        {
            FileOperation(FileFuncFlags.FO_MOVE, hwndOwner, progressTitle, sourcePaths, destinationPath);
        }

        public static void CopyFiles(IntPtr hwndOwner, string progressTitle, IEnumerable<string> sourcePaths, string destinationPath)
        {
            FileOperation(FileFuncFlags.FO_COPY, hwndOwner, progressTitle, sourcePaths, destinationPath);
        }

        private static void FileOperation(FileFuncFlags operation, IntPtr hwndOwner, string progressTitle, IEnumerable<string> sourcePaths, string destinationPath)
        {
            SHFILEOPSTRUCT fos = new SHFILEOPSTRUCT();
            fos.hwnd = hwndOwner;
            fos.wFunc = operation;

            // Build up the "from" string as a null-delimited set of strings terminated by two nulls.
            StringBuilder paths = new StringBuilder();
            foreach(string path in sourcePaths)
            {
                paths.Append(path);
                paths.Append('\0');
            }
            paths.Append('\0'); // Must end with a double-null. Marshalling may result in a triple-null which is OK.
            fos.pFrom = paths.ToString();

            // The "to" string requires a double-null termination
            fos.pTo = string.Concat(destinationPath, "\0");

            fos.fFlags = FILEOP_FLAGS.FOF_FILESONLY|FILEOP_FLAGS.FOF_NOCONFIRMMKDIR|FILEOP_FLAGS.FOF_NOCOPYSECURITYATTRIBS|FILEOP_FLAGS.FOF_NORECURSION|FILEOP_FLAGS.FOF_RENAMEONCOLLISION;
            fos.fAnyOperationsAborted = false;
            fos.hNameMappings = IntPtr.Zero;
            fos.lpszProgressTitle = progressTitle;

            int result = SHFileOperationW(ref fos);
            if (result != 0) throw new ApplicationException(string.Format("SHFIleOperation failed with code 0x{0:x8}", result));          
        }

        // Do NOT use a pack parameter here. Packing is different for
        // Win32 and Win64. Leaving out the parameter results in correct
        // code for both platforms.
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private struct SHFILEOPSTRUCT
        {
            public IntPtr hwnd;
            public FileFuncFlags wFunc;
            [MarshalAs(UnmanagedType.LPWStr)]
            public string pFrom;
            [MarshalAs(UnmanagedType.LPWStr)]
            public string pTo;
            public FILEOP_FLAGS fFlags;
            [MarshalAs(UnmanagedType.Bool)]
            public bool fAnyOperationsAborted;
            public IntPtr hNameMappings;
            [MarshalAs(UnmanagedType.LPWStr)]
            public string lpszProgressTitle;
        }

        private enum FileFuncFlags : uint
        {
            FO_MOVE = 0x1,
            FO_COPY = 0x2,
            FO_DELETE = 0x3,
            FO_RENAME = 0x4
        }

        [Flags]
        private enum FILEOP_FLAGS : ushort
        {
            FOF_MULTIDESTFILES = 0x1,
            FOF_CONFIRMMOUSE = 0x2,
            /// <summary>
            /// Don't create progress/report
            /// </summary>
            FOF_SILENT = 0x4,
            FOF_RENAMEONCOLLISION = 0x8,
            /// <summary>
            /// Don't prompt the user.
            /// </summary>
            FOF_NOCONFIRMATION = 0x10,
            /// <summary>
            /// Fill in SHFILEOPSTRUCT.hNameMappings.
            /// Must be freed using SHFreeNameMappings
            /// </summary>
            FOF_WANTMAPPINGHANDLE = 0x20,
            FOF_ALLOWUNDO = 0x40,
            /// <summary>
            /// On *.*, do only files
            /// </summary>
            FOF_FILESONLY = 0x80,
            /// <summary>
            /// Don't show names of files
            /// </summary>
            FOF_SIMPLEPROGRESS = 0x100,
            /// <summary>
            /// Don't confirm making any needed dirs
            /// </summary>
            FOF_NOCONFIRMMKDIR = 0x200,
            /// <summary>
            /// Don't put up error UI
            /// </summary>
            FOF_NOERRORUI = 0x400,
            /// <summary>
            /// Dont copy NT file Security Attributes
            /// </summary>
            FOF_NOCOPYSECURITYATTRIBS = 0x800,
            /// <summary>
            /// Don't recurse into directories.
            /// </summary>
            FOF_NORECURSION = 0x1000,
            /// <summary>
            /// Don't operate on connected elements.
            /// </summary>
            FOF_NO_CONNECTED_ELEMENTS = 0x2000,
            /// <summary>
            /// During delete operation,
            /// warn if nuking instead of recycling (partially overrides FOF_NOCONFIRMATION)
            /// </summary>
            FOF_WANTNUKEWARNING = 0x4000,
            /// <summary>
            /// Treat reparse points as objects, not containers
            /// </summary>
            FOF_NORECURSEREPARSE = 0x8000
        }

        [DllImport("shell32.dll", CharSet = CharSet.Unicode, ExactSpelling=true, SetLastError=false)]
        private static extern int SHFileOperationW([In] ref SHFILEOPSTRUCT lpFileOp);
    }

    public static class NativeMethods
    {

    } // Class NativeMethods

}
