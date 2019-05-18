using ReplayParser.ReplaySorter.UserInput;
using System;
using System.Collections.Generic;
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

            return filePath.Substring(GetParentDirectory(filePath).Length).TrimStart(new char[] { '\\' });
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
        public static string IncrementName(string fileNameOnly, string extension, string path, ref int count)
        {
            string tempFileName = string.Format("{0}({1})", fileNameOnly, count++);
            return Path.Combine(path, tempFileName + extension);
        }

        /// <summary>
        /// Extract individual directories from a path
        /// </summary>
        /// <param name="replayFilePath">The path from which to extract directories.</param>
        /// <param name="rootDirectory">The directory after which to start extracting.</param>
        /// <returns>Returns an ordered enumerable of directories contained in a path.</returns>
        public static IEnumerable<string> ExtractDirectoriesFromPath(string path, string rootDirectory)
        {
            if (string.IsNullOrWhiteSpace(path)) throw new InvalidOperationException("Can not extract directories from an empty string!");
            if (!string.IsNullOrWhiteSpace(rootDirectory) && !path.Contains(rootDirectory)) throw new InvalidOperationException("Path does not contain root directory!");

            if (string.IsNullOrWhiteSpace(rootDirectory))
                rootDirectory = string.Empty;
            else
            {
                if (GetDirectorySeparatorIndex(rootDirectory) == -1)
                    rootDirectory = rootDirectory + Path.DirectorySeparatorChar;
            }

            path = path.Substring(rootDirectory.Length);

            while (path != string.Empty)
            {
                int indexOfSeparator = GetDirectorySeparatorIndex(path);
                if (indexOfSeparator == -1)
                {
                    yield break;
                }

                yield return path.Substring(0, indexOfSeparator);
                path = path.Substring(indexOfSeparator + 1);
            }
        }

        private static int GetDirectorySeparatorIndex(string path)
        {
            int indexOfSeparator = path.IndexOf(Path.DirectorySeparatorChar);
            if (indexOfSeparator != -1)
                return indexOfSeparator;
            
            return path.IndexOf(Path.AltDirectorySeparatorChar);
        }
    }
}
