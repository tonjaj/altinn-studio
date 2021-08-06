using System;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;

using Manatee.Json;
using Manatee.Json.Schema;
using Manatee.Json.Serialization;

namespace Designer.Tests.Utils
{
    public static class TestDataHelper
    {
        public static JsonSchema LoadDataFromEmbeddedResourceAsJsonSchema(string resourceName)
        {
            var resourceStream = LoadDataFromEmbeddedResource(resourceName);

            using StreamReader streamReader = new StreamReader(resourceStream);
            JsonValue jsonValue = JsonValue.Parse(streamReader);
            return new JsonSerializer().Deserialize<JsonSchema>(jsonValue);
        }

        public static string LoadDataFromEmbeddedResourceAsString(string resourceName)
        {
            var resourceStream = LoadDataFromEmbeddedResource(resourceName);

            using StreamReader reader = new StreamReader(resourceStream);
            string text = reader.ReadToEnd();

            return text;
        }

        public static Stream LoadDataFromEmbeddedResource(string resourceName)
        {
            var assembly = Assembly.GetExecutingAssembly();
            Stream resource = assembly.GetManifestResourceStream(resourceName);

            if (resource == null)
            {
                throw new InvalidOperationException("Unable to find test data embedded in the test assembly.");
            }

            return resource;
        }

        public static Stream LoadTestDataFromFile(string resourceName)
        {
            var assembly = Assembly.GetExecutingAssembly();
            string unitTestFolder = Path.GetDirectoryName(new Uri(assembly.Location).LocalPath);
            unitTestFolder = Path.Combine(unitTestFolder, @"..\..\..\_TestData\");
            Stream resource = File.OpenRead(unitTestFolder + resourceName);

            if (resource == null)
            {
                throw new InvalidOperationException("Unable to find test data.");
            }

            return resource;
        }

        public static string GetTestDataDirectory()
        {
            var unitTestFolder = Path.GetDirectoryName(new Uri(Assembly.GetExecutingAssembly().Location).LocalPath);
            return Path.GetFullPath(Path.Combine(unitTestFolder, @"..\..\..\_TestData\"));
        }

        public static string GetTestDataRepositoriesRootDirectory()
        {
            var unitTestFolder = GetTestDataDirectory();
            return Path.Combine(unitTestFolder, @"Repositories\");
        }

        public static string GetTestDataRepositoryDirectory(string org, string repository, string developer)
        {
            var unitTestFolder = GetTestDataDirectory();
            return Path.Combine(unitTestFolder, $"Repositories\\{developer}\\{org}\\{repository}");
        }

        public static string GetTestDataRemoteRepositoryDirectory()
        {
            var unitTestFolder = GetTestDataDirectory();
            return Path.Combine(unitTestFolder, "Remote");
        }

        public static string GetTestDataRemoteRepository(string org, string repository)
        {
            var unitTestFolder = GetTestDataDirectory();
            return Path.Combine(unitTestFolder, $"Remote\\{org}\\{repository}");
        }

        public async static Task<string> CopyAppRepositoryForTest(string org, string repository, string developer, string targetRepsository)
        {
            var sourceAppRepository = GetTestDataRepositoryDirectory(org, repository, developer);
            var targetDirectory = Path.Combine(GetTestDataRepositoriesRootDirectory(), developer, org, targetRepsository);

            await CopyDirectory(sourceAppRepository, targetDirectory);

            return targetDirectory;
        }

        public static void DeleteAppRepository(string org, string repository, string developer)
        {
            var repositoryDirectory = GetTestDataRepositoryDirectory(org, repository, developer);
            DeleteDirectory(repositoryDirectory);
        }

        public static void DeleteDirectory(string directoryToDelete, bool deleteSubDirs = true)
        {
            DirectoryInfo directoryToDeleteInfo = new DirectoryInfo(directoryToDelete);

            if (!directoryToDeleteInfo.Exists)
            {
                throw new DirectoryNotFoundException($"Directory does not exist or could not be found: {directoryToDelete}");
            }

            DirectoryInfo[] subDirectories = directoryToDeleteInfo.GetDirectories();

            FileInfo[] files = directoryToDeleteInfo.GetFiles();
            foreach (FileInfo file in files)
            {
                File.Delete(file.FullName);
            }

            if (deleteSubDirs)
            {
                foreach (DirectoryInfo directory in subDirectories)
                {
                    DeleteDirectory(directory.FullName);
                }
            }

            Directory.Delete(directoryToDeleteInfo.FullName);
        }

        public static string CreateEmptyDirectory(string path)
        {
            string fullPath = $"{GetTestDataRepositoriesRootDirectory()}/{path}";
            Directory.CreateDirectory(fullPath);
            return fullPath;
        }

        public async static Task CopyDirectory(string sourceDirectory, string targetDirectory, bool copySubDirs = true)
        {
            DirectoryInfo sourceDirectoryInfo = new DirectoryInfo(sourceDirectory);

            if (!sourceDirectoryInfo.Exists)
            {
                throw new DirectoryNotFoundException($"Source directory does not exist or could not be found: {sourceDirectory}");
            }

            DirectoryInfo[] sourceSubDirectories = sourceDirectoryInfo.GetDirectories();

            Directory.CreateDirectory(targetDirectory);

            FileInfo[] files = sourceDirectoryInfo.GetFiles();
            foreach (FileInfo file in files)
            {
                string tempPath = Path.Combine(targetDirectory, file.Name);

                var sourceBytes = ReadAllBytesWithoutLocking(file.FullName);
                await File.WriteAllBytesAsync(tempPath, sourceBytes);
            }

            if (copySubDirs)
            {
                foreach (DirectoryInfo subdir in sourceSubDirectories)
                {
                    string tempPath = Path.Combine(targetDirectory, subdir.Name);
                    await CopyDirectory(subdir.FullName, tempPath, copySubDirs);
                }
            }
        }

        public static void CleanUpRemoteRepository(string org, string repository)
        {
            string dir = GetTestDataRemoteRepository(org, repository);
            if (Directory.Exists(dir))
            {
                Directory.Delete(dir);
            }
        }

        public static void CleanUpReplacedRepositories(string org, string repository, string developer)
        {
            string dir = Path.Combine(GetTestDataRepositoriesRootDirectory(), $"{developer}\\{org}\\");

            foreach (string subDir in Directory.GetDirectories(dir))
            {
                if (subDir.Contains($"{repository}_REPLACED_BY_NEW_CLONE_"))
                {
                    Directory.Delete(subDir, true);
                }
            } 
        }

        public static string GetFileFromRepo(string org, string repository, string developer, string relativePath)
        {            
            string filePath = Path.Combine(GetTestDataRepositoryDirectory(org, repository, developer), relativePath);
            if (File.Exists(filePath))
            {
                return File.ReadAllText(filePath);
            }

            return string.Empty;
        }

        /// <summary>
        /// File.ReadAllBytes alternative to avoid read and/or write locking
        /// </summary>
        private static byte[] ReadAllBytesWithoutLocking(string filePath, FileAccess fileAccess = FileAccess.Read, FileShare shareMode = FileShare.ReadWrite)
        {
            using (var fs = new FileStream(filePath, FileMode.Open, fileAccess, shareMode))
            {
                using (var ms = new MemoryStream())
                {
                    fs.CopyTo(ms);
                    return ms.ToArray();
                }
            }
        }
    }
}
