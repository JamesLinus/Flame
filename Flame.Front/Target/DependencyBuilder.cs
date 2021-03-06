﻿using Flame;
using Flame.Compiler;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Front.Target
{
    public class DependencyBuilder : IDependencyBuilder
    {
        public DependencyBuilder(
            IAssemblyResolver RuntimeLibaryResolver, IAssemblyResolver ExternalResolver,
            IBinder EnvironmentBinder, PathIdentifier CurrentPath,
            PathIdentifier OutputFolder, ICompilerLog Log)
        {
            this.RuntimeLibaryResolver = RuntimeLibaryResolver;
            this.ExternalResolver = ExternalResolver;
            this.CurrentPath = CurrentPath;
            this.OutputFolder = OutputFolder;
            this.EnvironmentBinder = EnvironmentBinder;
            this.Properties = Properties;
            this.Log = Log;
            this.registeredAssemblies = new HashSet<IAssembly>();
            this.Binder = new DependencyBinder(this);
            this.Properties = new TypedDictionary<string>();
        }

        public IAssemblyResolver RuntimeLibaryResolver { get; private set; }
        public IAssemblyResolver ExternalResolver { get; private set; }
        
        /// <summary>
        /// Gets the binder for the runtime environment.
        /// </summary>
        /// <returns>The binder for the runtime environment.</returns>
        public IBinder EnvironmentBinder { get; private set; }

        /// <summary>
        /// Gets the runtime environment for this dependency builder.
        /// </summary>
        /// <returns>The runtime environment.</returns>
        public IEnvironment Environment { get { return EnvironmentBinder.Environment; } }

        public ICompilerLog Log { get; private set; }
        public PathIdentifier CurrentPath { get; private set; }
        public PathIdentifier OutputFolder { get; private set; }
        public TypedDictionary<string> Properties { get; private set; }
        public IBinder Binder { get; private set; }

        public IEnumerable<IAssembly> RegisteredAssemblies { get { return registeredAssemblies; } }
        private HashSet<IAssembly> registeredAssemblies;

        protected Action<IAssembly> AssemblyRegisteredCallback
        {
            get
            {
                return this.GetAssemblyRegisteredCallback();
            }
        }

        protected Task<IAssembly> ResolveRuntimeLibraryAsync(ReferenceDependency Reference)
        {
            if (Reference.UseCopy)
            {
                return RuntimeLibaryResolver.CopyAndResolveAsync(Reference.Identifier, OutputFolder, this);
            }
            else
            {
                return RuntimeLibaryResolver.ResolveAsync(Reference.Identifier, this);
            }
        }

        protected Task<IAssembly> ResolveReferenceAsync(ReferenceDependency Reference)
        {
            if (Reference.UseCopy)
            {
                return ExternalResolver.CopyAndResolveAsync(Reference.Identifier, CurrentPath, OutputFolder, this);
            }
            else
            {
                return ExternalResolver.ResolveAsync(Reference.Identifier, CurrentPath, this);
            }
        }

        protected virtual void RegisterAssembly(IAssembly Assembly)
        {
            registeredAssemblies.Add(Assembly);
            var callback = AssemblyRegisteredCallback;
            if (callback != null)
            {
                callback(Assembly);
            }
        }

        private bool RegisterAssemblySafe(IAssembly Assembly)
        {
            if (Assembly != null)
            {
                RegisterAssembly(Assembly);
                return true;
            }
            else
            {
                return false;
            }
        }

        public async Task AddRuntimeLibraryAsync(ReferenceDependency Reference)
        {
            if (!RegisterAssemblySafe(await ResolveRuntimeLibraryAsync(Reference)) &&
                Warnings.Instance.MissingDependency.UseWarning(Log.Options))
            {
                Log.LogWarning(new LogEntry(
                    "missing dependency",
                    Warnings.Instance.MissingDependency.CreateMessage(
                        "could not resolve runtime library '" + Reference.Identifier.ToString() + "'. ")));
            }
        }

        public async Task AddReferenceAsync(ReferenceDependency Reference)
        {
            // Try to add a library dependency first.
            // If that can't be done, try to create a runtime reference.
            if (!RegisterAssemblySafe(await ResolveReferenceAsync(Reference)) &&
                !RegisterAssemblySafe(await ResolveRuntimeLibraryAsync(Reference)) &&
                Warnings.Instance.MissingDependency.UseWarning(Log.Options))
            {
                Log.LogWarning(new LogEntry(
                    "missing dependency",
                    Warnings.Instance.MissingDependency.CreateMessage(
                        "could not resolve library '" + Reference.Identifier.ToString() + "'. ")));
            }
        }
    }
}
