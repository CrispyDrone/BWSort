using ReplayParser.ReplaySorter.UserInput;
using System;
using System.IO;
using System.Windows;

namespace ReplayParser.ReplaySorter.IO
{
    public static class FileHandler
    {
        public static string GetParentDirectory(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
                return null;

            //TODO exceptions? invalid chars?
            return Directory.GetParent(filePath).FullName;
        }

        public static string GetFileName(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
                return null;

            var dirName = GetParentDirectory(filePath);

            if (string.IsNullOrWhiteSpace(dirName))
                return null;

            if (dirName.Length >= filePath.Length)
                return null;

            return filePath.Substring(GetParentDirectory(filePath).Length);
        }

        public static string GetFileNameWithoutExtension(string fullPath)
        {
            if (string.IsNullOrWhiteSpace(fullPath))
                return null;

            return Path.GetFileNameWithoutExtension(fullPath);
        }

        public static string GetExtension(string fullPath)
        {
            if (string.IsNullOrWhiteSpace(fullPath))
                return null;

            return Path.GetExtension(fullPath);
        }

        public static string AdjustName(string fullPath, bool isDirectory)
        {
            int count = 1;

            string fileNameOnly = GetFileNameWithoutExtension(fullPath);
            string extension = GetExtension(fullPath);
            string path = GetParentDirectory(fullPath);
            string newFullPath = fullPath;

            if (isDirectory)
            {
                while (Directory.Exists(newFullPath))
                {
                    newFullPath = IncrementName(fileNameOnly, extension, path, ref count);
                }
            }
            else
            {
                while (File.Exists(newFullPath))
                {
                    newFullPath = IncrementName(fileNameOnly, extension, path, ref count);
                }
            }
            return newFullPath;
        }

        //TODO get rid of this ridiculous coupling to console and messagebox
        public static string CreateDirectory(string sortDirectory, bool UI = false)
        {
            if (!UI)
            {
                if (Directory.Exists(sortDirectory))
                {
                    Console.WriteLine("Sort directory already exists.");
                    Console.WriteLine("Write to same directory? Yes/No.");
                    var WriteToSameDirectory = User.AskYesNo();
                    if (WriteToSameDirectory.Yes != null)
                    {
                        if ((bool)!WriteToSameDirectory.Yes)
                        {
                            sortDirectory = AdjustName(sortDirectory, true);
                            Directory.CreateDirectory(sortDirectory);
                        }
                    }
                }
                else
                {
                    Directory.CreateDirectory(sortDirectory);
                }
            }
            else
            {
                if (Directory.Exists(sortDirectory))
                {
                    var result = MessageBox.Show("Directory already exists. Write to a new directory?", "Warning", MessageBoxButton.YesNo, MessageBoxImage.Question, MessageBoxResult.Yes);
                    if (result == MessageBoxResult.Yes)
                    {
                        sortDirectory = AdjustName(sortDirectory, true);
                        Directory.CreateDirectory(sortDirectory);
                    }
                }
                else
                {
                    Directory.CreateDirectory(sortDirectory);
                }
            }
            return sortDirectory;
        }

        //TODO ref count, makes sense? 
        //TODO should loop internally? has to take additional parameter isDirectory (?)
        private static string IncrementName(string fileNameOnly, string extension, string path, ref int count)
        {
            string tempFileName = string.Format("{0}({1})", fileNameOnly, count++);
            return Path.Combine(path, tempFileName + extension);
        }
    }
}
