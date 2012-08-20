using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using EnvDTE;
using NuGet.VisualStudio.Resources;

namespace NuGet.VisualStudio
{
    [Export(typeof(IBuildServicesManager))]
    internal class BuildServicesManager : IBuildServicesManager
    {
        internal static readonly string NuGetTargetsFile = Path.Combine(VsConstants.NuGetSolutionSettingsFolder, "nuget.targets");
        private const string NuGetBuildPackageName = "NuGet.Build";
        private const string NuGetBootstrapperPackageName = "NuGet.Bootstrapper";

        private readonly DTE _dte;
        private readonly ISolutionManager _solutionManager;
        private readonly IFileSystemProvider _fileSystemProvider;
        private readonly IPackageSourceProvider _packageSourceProvider;
        private readonly IPackageRepositoryFactory _packageRepositoryFactory;
        private readonly IPackageRepository _localCacheRepository;
        private IPackageRepository _officialNuGetRepository;

        [ImportingConstructor]
        public BuildServicesManager(
            ISolutionManager solutionManager,
            IFileSystemProvider fileSystemProvider,
            IPackageSourceProvider packageSourceProvider,
            IPackageRepositoryFactory packageRepositoryFactory) :
            this(ServiceLocator.GetInstance<DTE>(),
                 solutionManager,
                 fileSystemProvider,
                 packageSourceProvider,
                 packageRepositoryFactory,
                 MachineCache.Default)
        {
        }

        internal BuildServicesManager(
            DTE dte,
            ISolutionManager solutionManager,
            IFileSystemProvider fileSystemProvider,
            IPackageSourceProvider packageSourceProvider,
            IPackageRepositoryFactory packageRepositoryFactory,
            IPackageRepository localCacheRepository)
        {
            _dte = dte;
            _solutionManager = solutionManager;
            _fileSystemProvider = fileSystemProvider;
            _packageSourceProvider = packageSourceProvider;
            _packageRepositoryFactory = packageRepositoryFactory;
            _localCacheRepository = localCacheRepository;
        }

        public bool AreBuildServicesInstalledForSolution
        {
            get
            {
                if (!_solutionManager.IsSolutionOpen)
                {
                    return false;
                }

                string solutionDirectory = _solutionManager.SolutionDirectory;
                if (String.IsNullOrEmpty(solutionDirectory))
                {
                    return false;
                }

                IFileSystem fileSystem = _fileSystemProvider.GetFileSystem(solutionDirectory);
                return fileSystem.FileExists(NuGetTargetsFile);
            }
        }

        public void EnsureBuildServicesInstalledInSolution(bool fromActivation)
        {
            string solutionDirectory = _solutionManager.SolutionDirectory;
            string nugetFolderPath = Path.Combine(solutionDirectory, VsConstants.NuGetSolutionSettingsFolder);

            IFileSystem fileSystem = _fileSystemProvider.GetFileSystem(solutionDirectory);

            if (!fileSystem.DirectoryExists(VsConstants.NuGetSolutionSettingsFolder) ||
                !fileSystem.FileExists(NuGetTargetsFile))
            {
                // download NuGet.Build and NuGet.Bootstrapper packages into the .nuget folder
                IPackageRepository repository = _packageSourceProvider.GetAggregate(_packageRepositoryFactory, ignoreFailingRepositories: true);

                // Ensure we have packages before we attempt to add them.
                var installPackages = new[] { GetPackage(repository, NuGetBuildPackageName, fromActivation), 
                                               GetPackage(repository, NuGetBootstrapperPackageName, fromActivation) };
                foreach (var package in installPackages)
                {
                    fileSystem.AddFiles(package.GetFiles(Constants.ToolsDirectory), VsConstants.NuGetSolutionSettingsFolder, preserveFilePath: false);
                }

                // IMPORTANT: do this BEFORE adding the .nuget folder to solution so that 
                // the generated .nuget\nuget.config is included in the solution folder too. 
                DisableSourceControlMode();

                // now add the .nuget folder to the solution as a solution folder.
                _dte.Solution.AddFolderToSolution(VsConstants.NuGetSolutionSettingsFolder, nugetFolderPath);
            }
        }

        /// <summary>
        /// Try to retrieve the package with the specified Id from machine cache first. 
        /// If not found, download it from the specified repository and add to machine cache.
        /// </summary>
        private IPackage GetPackage(IPackageRepository repository, string packageId, bool fromActivation)
        {
            // first, find the package from the remote repository
            IPackage package = repository.FindPackage(packageId, version: null, allowPrereleaseVersions: true, allowUnlisted: false);

            if (package == null && fromActivation)
            {
                // if we can't find the package from the remote repositories, look for it
                // from nuget.org feed, provided that it's not already specified in one of the remote repositories
                if (!ContainsSource(_packageSourceProvider, NuGetConstants.DefaultFeedUrl) &&
                    !ContainsSource(_packageSourceProvider, NuGetConstants.V2LegacyFeedUrl))
                {
                    if (_officialNuGetRepository == null)
                    {
                        _officialNuGetRepository = _packageRepositoryFactory.CreateRepository(NuGetConstants.DefaultFeedUrl);
                    }

                    package = _officialNuGetRepository.FindPackage(packageId, version: null, allowPrereleaseVersions: true, allowUnlisted: false);
                }
            }

            bool fromCache = false;

            // if package == null, we use whatever version is in the machine cache
            IPackage cachedPackage = _localCacheRepository.FindPackage(packageId, package != null ? package.Version : null);
            if (cachedPackage != null)
            {
                var dataServicePackage = package as DataServicePackage;
                if (dataServicePackage != null)
                {
                    var cachedHash = cachedPackage.GetHash(dataServicePackage.PackageHashAlgorithm);
                    if (!dataServicePackage.PackageHash.Equals(cachedHash, StringComparison.OrdinalIgnoreCase))
                    {
                        // if the remote package has the same hash as with the one in the machine cache, use the one from machine cache
                        package = cachedPackage;
                        fromCache = true;
                    }
                    else
                    {
                        // if the hash has changed, delete the stale package
                        _localCacheRepository.RemovePackage(cachedPackage);
                    }
                }
                else if (package == null)
                {
                    // in this case, we didn't find the package from remote repository.
                    // fallback to using the one in the machine cache.
                    package = cachedPackage;
                    fromCache = true;
                }
            }

            if (package == null)
            {
                throw new InvalidOperationException(
                            String.Format(
                                CultureInfo.CurrentCulture,
                                VsResources.PackageRestoreDownloadPackageFailed,
                                packageId));
            }

            if (!fromCache)
            {
                _localCacheRepository.AddPackage(package);

                // swap to the Zip package to avoid potential downloading package twice
                package = _localCacheRepository.FindPackage(package.Id, package.Version);
                Debug.Assert(package != null);
            }

            return package;
        }

        private static bool ContainsSource(IPackageSourceProvider provider, string source)
        {
            return provider.GetEnabledPackageSources().Any(p => p.Source.Equals(source, StringComparison.OrdinalIgnoreCase));
        }

        private void DisableSourceControlMode()
        {
            // get the settings for this solution
            var nugetFolder = Path.Combine(_solutionManager.SolutionDirectory, VsConstants.NuGetSolutionSettingsFolder);
            var settings = new Settings(_fileSystemProvider.GetFileSystem(nugetFolder));
            settings.DisableSourceControlMode();
        }
    }
}
