using System;
using System.IO;

namespace Janphe
{
    public static class Helper
    {
        /**
            GetDirectoryName('C:\MyDir\MySubDir\myfile.ext') returns 'C:\MyDir\MySubDir'
            GetDirectoryName('C:\MyDir\MySubDir') returns 'C:\MyDir'
            GetDirectoryName('C:\MyDir\') returns 'C:\MyDir'
            GetDirectoryName('C:\MyDir') returns 'C:\'
            GetDirectoryName('C:\') returns ''
         */
        public static string GetFolderRoot(string folderPath)
        {
            string folderName = Path.GetDirectoryName(folderPath);
            if (folderName.Length <= 2)
                return folderName;
            var idx = folderName.IndexOf("/", 1, StringComparison.InvariantCulture);
            return idx != -1 ? folderName.Substring(0, idx) : folderName;
        }
    }
}
