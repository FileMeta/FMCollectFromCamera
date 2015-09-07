using System;
using System.Threading;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace FMCollectFromCamera
{
    class CollectorThread
    {
        delegate void ConsoleWrite(string text);

        private static readonly string[] s_mediaExtensions = new string[] { ".jpg", ".mp4", ".avi", ".mpg", ".mov", ".wav", ".mp3", ".jpeg", ".mpeg" };

        int m_started;  // Actually used as a bool but Interlocked works better with an int.
        Thread m_thread;
        MainWindow m_mainWindow;

        public CollectorThread(MainWindow mainWindow)
        {
            m_thread = new Thread(ThreadMain);
            m_mainWindow = mainWindow;
        }

        public void Start()
        {
            int started = Interlocked.CompareExchange(ref m_started, 1, 0);
            if (started == 0)
            {
                m_thread.Start();
            }
        }

        private void ThreadMain()
        {
            GC.KeepAlive(this); // Paranoid - probably not necessary as the thread has a reference to this and the system has a reference to the thread.

            // Get the destination folder name from the command line
            string destFolder;
            {
                string[] args = Environment.GetCommandLineArgs();
                destFolder = (args.Length == 2) ? args[1] : string.Empty;
            }

            bool showSyntax = false;
            if (destFolder.Length == 0
                || string.Equals(destFolder, "-h", StringComparison.OrdinalIgnoreCase)
                || string.Equals(destFolder, "/h", StringComparison.OrdinalIgnoreCase))
            {
                showSyntax = true;
            }
            else if (!Directory.Exists(destFolder))
            {
                m_mainWindow.OutputWrite("Destination folder '{0}' does not exist.\r\n", destFolder);
                showSyntax = true;
            }
            else
            {
                destFolder = Path.GetFullPath(destFolder);
            }
            if (showSyntax)
            {
                m_mainWindow.OutputWrite("Command-Line Syntax: FMCollectFromCamera <destination folder path>\r\n");
                return;
            }

            m_mainWindow.OutputWrite("Collecting images from cameras and cards to '{0}'.\r\n", destFolder);

            IntPtr hwndOwner = m_mainWindow.GetWindowHandle();

            // Process each removable drive
            foreach (DriveInfo drv in DriveInfo.GetDrives())
            {
                if (drv.IsReady && drv.DriveType == DriveType.Removable)
                {
                    // File system structure is according to JEITA "Design rule for Camera File System (DCF) which is JEITA specification CP-3461
                    // See if the DCIM folder exists
                    DirectoryInfo dcim = new DirectoryInfo(Path.Combine(drv.RootDirectory.FullName, "DCIM"));
                    if (dcim.Exists)
                    {
                        List<string> sourceFolders = new List<string>();
                        List<string> sourcePaths = new List<string>();

                        // Folders containing images must be named with three digits followed
                        // by five alphanumeric characters. First digit cannot be zero.
                        foreach (DirectoryInfo di in dcim.EnumerateDirectories())
                        {
                            if (di.Name.Length == 8)
                            {
                                int dirnum;
                                if (int.TryParse(di.Name.Substring(0, 3), out dirnum) && dirnum >= 100 && dirnum <= 999)
                                {
                                    sourceFolders.Add(di.FullName);

                                    foreach (string ext in s_mediaExtensions)
                                    {
                                        if (di.EnumerateFiles(string.Concat("*", ext)).Any())
                                        {
                                            sourcePaths.Add(Path.Combine(di.FullName, string.Concat("*", ext)));
                                        }
                                    }
                                }
                            }
                        }

                        if (sourcePaths.Count > 0)
                        {
                            // Write the names out
                            foreach (string path in sourcePaths)
                            {
                                m_mainWindow.OutputWrite(path);
                                m_mainWindow.OutputWrite("\r\n");
                            }

                            // Perform the move
                            WinShell.SHFileOperation.MoveFiles(hwndOwner, "Collecting Photos from Camera", sourcePaths, destFolder);

                            // Clean up the folders
                            try
                            {
                                foreach (string folderName in sourceFolders)
                                {
                                    DirectoryInfo di = new DirectoryInfo(folderName);

                                    // Get rid of thumbnails unless a matching file still exists
                                    // (matching files have to be of a type we don't yet recognize)
                                    foreach (FileInfo fi in di.GetFiles("*.thm"))
                                    {
                                        bool hasMatch = false;
                                        foreach (FileInfo fi2 in di.GetFiles(Path.GetFileNameWithoutExtension(fi.Name) + ".*"))
                                        {
                                            if (!string.Equals(fi2.Extension, ".thm", StringComparison.OrdinalIgnoreCase))
                                            {
                                                hasMatch = true;
                                                break;
                                            }
                                        }
                                        if (!hasMatch) fi.Delete();
                                    }

                                    // Get rid of Windows thumbnails file (if it exists)
                                    {
                                        string thumbName = Path.Combine(di.FullName, "Thumbs.db");
                                        if (File.Exists(thumbName)) File.Delete(thumbName);
                                    }

                                    // If the folder is empty, delete it
                                    if (!di.EnumerateFileSystemInfos().Any()) di.Delete();
                                }
                            }
                            catch (Exception err)
                            {
                                // Report errors during cleanup but proceed with other files.
                                m_mainWindow.OutputWrite("Error cleaning up folders on drive '{0}'.\r\n{0}\r\n", drv.Name, err.Message);
                            }
                        }

                    } // If DCIM exists
                } // If drive is ready and removable
            } // for each drive

            m_mainWindow.OutputWrite("Transfer complete. Exiting in 5 seconds.");
            Thread.Sleep(5000);
            m_mainWindow.Dispatcher.BeginInvokeShutdown(System.Windows.Threading.DispatcherPriority.Normal);
        } // Function ThreadMain

    } // Class CollectorThread
}
