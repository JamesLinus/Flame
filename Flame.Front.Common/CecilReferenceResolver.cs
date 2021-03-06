﻿using Flame;
using Flame.Cecil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using Flame.Compiler;
using Flame.Front.Target;
using Flame.Front;

namespace Flame.Front
{
    public class CecilReferenceResolver : IAssemblyResolver
    {
        static CecilReferenceResolver()
        {
            ConversionCache = new ConverterCache();
        }

        public static ConverterCache ConversionCache { get; private set; }

        // Copy debugging files and xml docs
        private static string[] SecondaryExtensions = new[] { "pdb", "dll.mdb", "exe.mdb", "xml" };

        public Task<IAssembly> ResolveAsync(PathIdentifier Identifier, IDependencyBuilder DependencyBuilder)
        {
            var absPath = Identifier.AbsolutePath.Path;
            if (!File.Exists(absPath))
            {
                DependencyBuilder.Log.LogError(new LogEntry("file not found", "file '" + Identifier.AbsolutePath + "' could not be found."));
            }
            var readerParams = DependencyBuilder.GetCecilReaderParameters();
            return Task.FromResult<IAssembly>(new CecilAssembly(Mono.Cecil.AssemblyDefinition.ReadAssembly(absPath, readerParams), ConversionCache));
        }

        public Task<PathIdentifier?> CopyAsync(PathIdentifier SourceIdentifier, PathIdentifier TargetIdentifier, ICompilerLog Log)
        {
            return CopyAsync(SourceIdentifier, TargetIdentifier, Log, SecondaryExtensions);
        }

        public static Task<PathIdentifier?> CopyAsync(PathIdentifier SourceIdentifier, PathIdentifier TargetIdentifier, ICompilerLog Log, string[] SecondaryExtensions)
        {
            var mainTask = CopyFileAsync(SourceIdentifier, TargetIdentifier, Log);

            var allTasks = new List<Task>();
            allTasks.Add(mainTask);

            var sourceDirPath = SourceIdentifier.Parent;
            var sourceFileName = SourceIdentifier.NameWithoutExtension;
            var targetDirPath = TargetIdentifier.Parent;
            var targetFileName = TargetIdentifier.NameWithoutExtension;

            foreach (var item in SecondaryExtensions)
            {
                var itemSourcePath = sourceDirPath.Combine(sourceFileName + "." + item);
                if (File.Exists(itemSourcePath.AbsolutePath.Path))
                {
                    var itemTargetPath = targetDirPath.Combine(targetFileName + "." + item);
                    allTasks.Add(CopyFileAsync(itemSourcePath, itemTargetPath, Log));
                }
            }

            return Task.WhenAll(allTasks).ContinueWith(task => mainTask.Result);
        }

        public static async Task<PathIdentifier?> CopyFileAsync(PathIdentifier sourcePath, PathIdentifier destinationPath, ICompilerLog Log)
        {
            var absSourcePath = sourcePath.AbsolutePath.Path;
            var absTargetPath = destinationPath.AbsolutePath.Path;
            if (absSourcePath != absTargetPath)
            {
                if (!File.Exists(absSourcePath))
                {
                    Log.LogError(new LogEntry("file not found", "file '" + sourcePath + "' could not be found."));
                    return null;
                }
                string dirName = Path.GetDirectoryName(absTargetPath);
                if (!Directory.Exists(dirName))
                {
                    Directory.CreateDirectory(dirName);
                }
                using (var source = new FileStream(absSourcePath, FileMode.Open, FileAccess.Read))
                {
                    using (var destination = new FileStream(absTargetPath, FileMode.Create, FileAccess.Write))
                    {
                        await source.CopyToAsync(destination);
                    }
                }
            }
            return destinationPath;
        }
    }
}
