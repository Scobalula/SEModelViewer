using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace SEModelViewer.Util
{
    public class FolderUtil
    {
        public static IntPtr INVALID_HANDLE_VALUE = new IntPtr(-1);

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        public struct WIN32_FIND_DATA
        {
            public uint dwFileAttributes;
            public System.Runtime.InteropServices.ComTypes.FILETIME ftCreationTime;
            public System.Runtime.InteropServices.ComTypes.FILETIME ftLastAccessTime;
            public System.Runtime.InteropServices.ComTypes.FILETIME ftLastWriteTime;
            public uint nFileSizeHigh;
            public uint nFileSizeLow;
            public uint dwReserved0;
            public uint dwReserved1;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
            public string cFileName;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 14)]
            public string cAlternateFileName;
        }

        public enum FINDEX_SEARCH_OPS
        {
            FindExSearchNameMatch = 0,
            FindExSearchLimitToDirectories = 1,
            FindExSearchLimitToDevices = 2
        }

        public enum FINDEX_INFO_LEVELS
        {
            FindExInfoStandard = 0,
            FindExInfoBasic = 1
        }

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        public static extern IntPtr FindFirstFileEx(string lpFileName, FINDEX_INFO_LEVELS fInfoLevelId, out WIN32_FIND_DATA lpFindFileData, FINDEX_SEARCH_OPS fSearchOp, IntPtr lpSearchFilter, int dwAdditionalFlags);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto)]
        public static extern bool FindNextFile(IntPtr hFindFile, out WIN32_FIND_DATA lpFindFileData);

        [DllImport("kernel32.dll")]
        static extern bool FindCloseChangeNotification(IntPtr hChangeHandle);


        public static List<string> GetAllFiles(string folderPath)
        {
            List<string> files = new List<string>();
            WIN32_FIND_DATA lpFindFileData = new WIN32_FIND_DATA();
            IntPtr firstFileEx = FindFirstFileEx(Path.Combine(folderPath, "*.*"), FINDEX_INFO_LEVELS.FindExInfoBasic, out lpFindFileData, FINDEX_SEARCH_OPS.FindExSearchNameMatch, IntPtr.Zero, 0);


            if (firstFileEx != INVALID_HANDLE_VALUE)
            {
                do
                {
                    string filename = lpFindFileData.cFileName;
                    if ((FileAttributes)(lpFindFileData.dwFileAttributes & (int)FileAttributes.Directory) == FileAttributes.Directory)
                    {
                        if (!(filename == ".") && !(filename == ".."))
                        {
                            files.AddRange(GetAllFiles(Path.Combine(folderPath, filename)));
                        }
                    }
                    else
                    {
                        string str = Path.Combine(folderPath, filename);
                        files.Add(str);
                    }
                }
                while (FindNextFile(firstFileEx, out lpFindFileData));
            }


            return files;
        }
    }
}
