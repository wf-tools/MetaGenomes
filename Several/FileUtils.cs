using System;
using System.IO;
using static WfComponent.Utils.FileUtils;

namespace ShotgunMetagenome.Several
{
    public static class FileUtils
    {

        public static bool IsValidFile(string filePath, ref string message)
        {
            if (string.IsNullOrEmpty(filePath))
            {
                message += "file path is not varid. (null or Empty)" + filePath + Environment.NewLine;
                return false;
            }

            if(!File.Exists(filePath))
            {
                message += "file not exist. "+ filePath + Environment.NewLine;
                return false;
            }

            if(FileSize(filePath, ref message) < 100)
            {
                message += "file is 100 bytes or less. " + filePath + Environment.NewLine;
                return false;
            }

            return true;
        }

        //Assetsディレクトリ以下にあるTestディレクトリを削除
        /// <summary>
        /// 指定したディレクトリとその中身を全て削除する
        /// </summary>
        public static void Delete(string targetDirectoryPath)
        {
            if (!Directory.Exists(targetDirectoryPath))
            {
                return;
            }

            //ディレクトリ以外の全ファイルを削除
            string[] filePaths = Directory.GetFiles(targetDirectoryPath);
            foreach (string filePath in filePaths)
            {
                File.SetAttributes(filePath, FileAttributes.Normal);
                File.Delete(filePath);
            }

            //ディレクトリの中のディレクトリも再帰的に削除
            string[] directoryPaths = Directory.GetDirectories(targetDirectoryPath);
            foreach (string directoryPath in directoryPaths)
            {
                Delete(directoryPath);
            }

            //中が空になったらディレクトリ自身も削除
            Directory.Delete(targetDirectoryPath, false);
        }

    }



}
