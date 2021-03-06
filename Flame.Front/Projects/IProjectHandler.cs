﻿using Flame;
using Flame.Compiler;
using Flame.Compiler.Projects;
using Flame.Front.Target;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Flame.Front.Passes;

namespace Flame.Front.Projects
{
    public struct ParsedProject
    {
        public ParsedProject(PathIdentifier CurrentPath, IProject Project)
        {
            this = default(ParsedProject);
            this.CurrentPath = CurrentPath;
            this.Project = Project;
        }

        public PathIdentifier CurrentPath { get; private set; }
        public IProject Project { get; private set; }
    }

    public class ProjectDependency
    {
        public ProjectDependency(ParsedProject Project, IProjectHandler Handler)
        {
            this.Project = Project;
            this.LibraryDependencies = new HashSet<PathIdentifier>(
                Project.Project.GetProjectReferences().Select(Project.CurrentPath.GetAbsolutePath), 
                PathNameComparer.Instance);
            this.Handler = Handler;
        }

        public PathIdentifier Identifier { get { return Project.CurrentPath; } }
        public ParsedProject Project { get; private set; }
        public IProjectHandler Handler { get; private set; }
        public HashSet<PathIdentifier> LibraryDependencies { get; private set; }

        /// <summary>
        /// Gets a "root" project from the given set
        /// of project dependencies: a project that
        /// does not depend on any of the other projects 
        /// in the dependency set.
        /// If no such element exists, null is returned.
        /// </summary>
        /// <param name="Dependencies"></param>
        /// <returns></returns>
        public static ProjectDependency GetRootProject(IEnumerable<ProjectDependency> Dependencies)
        {
            var allProjectIdentifiers = new HashSet<PathIdentifier>(Dependencies.Select(item => item.Identifier), PathPossiblyExtendedNameComparer.Instance);
            foreach (var item in Dependencies)
            {
                if (!item.LibraryDependencies.Intersect(allProjectIdentifiers, PathPossiblyExtendedNameComparer.Instance)
                                             .Except(new PathIdentifier[] { item.Identifier }, PathPossiblyExtendedNameComparer.Instance)
                                             .Any())
                {
                    return item;
                }
            }
            return null;
        }
    }

    public interface IProjectHandler
    {
        IEnumerable<string> Extensions { get; }

        /// <summary>
        /// Parses the project at the given path.
        /// </summary>
        /// <param name="Path">The path where the project resides.</param>
        /// <param name="Log">The log.</param>
        /// <returns></returns>
        ParsedProject Parse(ProjectPath Path, ICompilerLog Log);

        /// <summary>
        /// "Partitions" the given projects, unifying zero or more of them.
        /// </summary>
        /// <param name="Projects"></param>
        /// <returns></returns>
        IEnumerable<ParsedProject> Partition(IEnumerable<ParsedProject> Projects);

        Task<IAssembly> CompileAsync(IProject Project, CompilationParameters Parameters);

        /// <summary>
        /// Creates a new project at the given path from the
        /// specified project.
        /// </summary>
        /// <param name="Project"></param>
        /// <param name="Path"></param>
        /// <param name="Log"></param>
        /// <returns></returns>
        IProject MakeProject(IProject Project, ProjectPath Path, ICompilerLog Log);

        /// <summary>
        /// Gets the project handler's pass preferences.
        /// </summary>
        PassPreferences GetPassPreferences(ICompilerLog Log);
    }
}
