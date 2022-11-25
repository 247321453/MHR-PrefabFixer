using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;

namespace MHR___Prefab_Fixer
{
    public static class Program
    {

        public static DirectoryInfo CreateFolder(params string[] path)
        {
            var folder = new DirectoryInfo(Path.Combine(path));

            if (!folder.Exists)
            {
                folder.Create();
            }

            return folder;
        }

        public static string CurrentDirectory;

        private static void Main(string[] args)
        {
            CurrentDirectory = Path.GetDirectoryName(typeof(Program).Assembly.Location);
            string conversionFolder;
            if (args.Length == 0)
            {
                conversionFolder = CurrentDirectory;
            }
            else
            {
                conversionFolder = args[0];
            }
            Console.WriteLine("Current Folder:" + conversionFolder);

            //Create conversion folder
            //Copy over all files in same format to folder, and attempt conversion on the folder

            var prefabs = Directory.GetFiles(conversionFolder, "*.pfb.17", SearchOption.AllDirectories);

            //TU1 Conversion
            TUConversion(prefabs, "66 e1 a6 8f 06 6d d5 ed d1 07 28 e8 bb dd 1d 11", "66 e1 a6 8f 46 5f 73 52 d1 07 28 e8 bb dd 1d 11");

            //TU2 Conversion
            TUConversion(prefabs, "46 5F 73 52 D1 07 28 E8 BB DD 1D 11", "7F D7 47 7F D1 07 28 E8 68 20 A6 CB");

            //TU3 Conversion
            TUConversion(prefabs, "7F D7 47 7F D1 07", "71 7F C2 1A D1 07");

            //Open Folder Location with file explorer
        }

        public static void TUConversion(string[] prefabs, string oldPrefabHex, string newPrefabHex)
        {
            var oldPrefabBytes = HexStringToByte(oldPrefabHex);
            var newPrefabBytes = HexStringToByte(newPrefabHex);

            ConvertPrefabs(prefabs, oldPrefabBytes, newPrefabBytes);
        }

        private static void ConvertPrefabs(string[] prefabs, byte[] oldPrefabBytes, byte[] newPrefabBytes)
        {
            foreach (var prefab in prefabs)
            {
                var prefabBytes = File.ReadAllBytes(prefab);

                if (ContainsBytes(prefabBytes, oldPrefabBytes))
                {
                    //Attempt conversion, and copy over
                    Console.WriteLine($"{prefab} contains old prefab bytes, will attempt to convert");

                    //File.WriteAllBytes(prefab + ".bak", prefabBytes);
                    var newPrefab = ReplaceBytes(prefabBytes, oldPrefabBytes, newPrefabBytes);

                    File.WriteAllBytes(prefab, newPrefab);

                    //Add check to make sure that the bytes got written properly and are the correct length
                    var tmpNewFileBytes = File.ReadAllBytes(prefab);

                    if (newPrefab.Length != tmpNewFileBytes.Length || !BytesSimilar(newPrefab, tmpNewFileBytes))
                    {
                        throw new Exception($"{prefab} has encountered an issue where the written bytes do not match the actual bytes, this may be caused due to the folder being on a different drive or folder permissions. Please try moving the prefab fixer to the same drive as the folder, or run this program as administrator.");
                    }
                }
                else if (ContainsBytes(prefabBytes, newPrefabBytes))
                {
                    //Don't convert, just output message
                    Console.WriteLine($"{prefab} contains new prefab bytes, no need to convert.");
                }
                else
                {
                    //Throw exception and warn of issue
                    //throw new Exception($"{prefab} does not contain any sequence for new and old prefab bytes, this has been thrown to avoid converting it.");
                }
            }

        }

        private static bool BytesSimilar(byte[] bytes1, byte[] bytes2)
        {
            if (bytes1.Length != bytes2.Length)
            {
                return false;
            }

            for (var i = 0; i < bytes1.Length; i++)
            {
                if (bytes1[i] != bytes2[i])
                {
                    return false;
                }
            }

            return true;
        }

        private static void CloneDirectory(DirectoryInfo root, DirectoryInfo dest, string searchPattern = "*")
        {
            foreach (var directory in root.GetDirectories())
            {
                string dirName = Path.GetFileName(directory.FullName);
                if (!Directory.Exists(Path.Combine(dest.FullName, dirName)))
                {
                    try
                    {
                        Directory.CreateDirectory(Path.Combine(dest.FullName, dirName));
                    }
                    catch (Exception ex)
                    {
                        if (ex is System.IO.PathTooLongException)
                        {
                            var newDirName = @"\\?\" + dest.FullName + @"\" + dirName;
                            Directory.CreateDirectory(newDirName);
                        }
                        else
                        {
                            throw ex;
                        }
                    }
                }
                CloneDirectory(directory, new DirectoryInfo(Path.Combine(dest.FullName, dirName)), searchPattern);
            }

            foreach (var file in root.GetFiles(searchPattern))
            {
                File.Copy(file.FullName, Path.Combine(dest.FullName, Path.GetFileName(file.FullName)), true);
            }
        }


        private static byte[] ReplaceBytes(byte[] bytes, byte[] search, byte[] replace)
        {
            var byteString = ByteToHexString(bytes);
            var searchString = ByteToHexString(search);
            var replaceString = ByteToHexString(replace);

            var newString = byteString.Replace(searchString, replaceString);

            return HexStringToByte(newString);
        }

        private static bool ContainsBytes(byte[] haystack, byte[] needle)
        {
            return SearchBytes(haystack, needle) >= 0;
        }

        private static int SearchBytes(byte[] haystack, byte[] needle)
        {
            var len = needle.Length;
            var limit = haystack.Length - len;
            for (var i = 0; i <= limit; i++)
            {
                var k = 0;
                for (; k < len; k++)
                {
                    if (needle[k] != haystack[i + k]) break;
                }
                if (k == len) return i;
            }
            return -1;
        }

        private static string ByteToHexString(byte[] bytes)
        {
            var hexString = new StringBuilder();

            foreach (int bytePart in bytes)
            {
                hexString.Append(string.Format("{0:X2}", bytePart));
            }

            return hexString.ToString();
        }

        private static byte[] HexStringToByte(string hexString)
        {
            if (hexString.Contains(" "))
            {
                hexString = hexString.Replace(" ", "");
            }

            if (hexString.Length % 2 != 0)
            {
                throw new ArgumentException(String.Format(CultureInfo.InvariantCulture, "The binary key cannot have an odd number of digits: {0}", hexString));
            }

            var data = new byte[hexString.Length / 2];
            for (var index = 0; index < data.Length; index++)
            {
                var byteValue = hexString.Substring(index * 2, 2);
                data[index] = byte.Parse(byteValue, NumberStyles.HexNumber, CultureInfo.InvariantCulture);
            }

            return data;
        }
    }
}