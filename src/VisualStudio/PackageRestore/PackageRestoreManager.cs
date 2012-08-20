using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using EnvDTE;
using Microsoft.VisualStudio.Shell.Interop;
using NuGet.VisualStudio.Resources;
using MsBuildProject = Microsoft.Build.Evaluation.Project;

namespace NuGet.VisualStudio
{
    [Export(typeof(IPackageRestoreManager))]
    internal class PackageRestoreManager : IPackageRestoreManager
    {
        private readonly IBuildServicesManager _buildServicesManager;
        private readonly ISolutionManager _solutionManager;
        private readonly IVsThreadedWaitDialogFactory _waitDialogFactory;
        private readonly IVsPackageManagerFactory _packageManagerFactory;
        private readonly DTE _dte;
        private readonly ISettings _settings;

        [ImportingConstructor]
        public PackageRestoreManager(
            IBuildServicesManager buildServicesManager,
            ISolutionManager solutionManager,
            IVsPackageManagerFactory packageManagerFactory,
            IVsPackageInstallerEvents packageInstallerEvents,
            ISettings settings) :
            this(ServiceLocator.GetInstance<DTE>(),
                 buildServicesManager,
                 solutionManager,
                 packageManagerFactory,
                 packageInstallerEvents,
                 ServiceLocator.GetGlobalService<SVsThreadedWaitDialogFactory, IVsThreadedWaitDialogFactory>(),
                 settings)
        {
        }

        internal PackageRestoreManager(
            DTE dte,
            IBuildServicesManager buildServicesManager,
            ISolutionManager solutionManager,
            IVsPackageManagerFactory packageManagerFactory,
            IVsPackageInstallerEvents packageInstallerEvents,
            IVsThreadedWaitDialogFactory waitDialogFactory,
            ISettings settings)
        {
            Debug.Assert(solutionManager != null);
            _dte = dte;
            _buildServicesManager = buildServicesManager;
            _solutionManager = solutionManager;
            _waitDialogFactory = waitDialogFactory;
            _packageManagerFactory = packageManagerFactory;
            _settings = settings;
            _solutionManager.ProjectAdded += OnProjectAdded;
            _solutionManager.SolutionOpened += OnSolutionOpenedOrClosed;
            _solutionManager.SolutionClosed += OnSolutionOpenedOrClosed;
            packageInstallerEvents.PackageReferenceAdded += OnPackageReferenceAdded;
        }

        public bool IsCurrentSolutionEnabledForRestore
        {
            get
            {
                return _buildServicesManager.AreBuildServicesInstalledForSolution;
            }
        }

        public void EnableCurrentSolutionForRestore(bool fromActivation)
        {
            if (!_solutionManager.IsSolutionOpen)
            {
                throw new InvalidOperationException(VsResources.SolutionNotAvailable);
            }

            if (fromActivation)
            {
                // if not in quiet mode, ask user for confirmation before proceeding
                bool? result = MessageHelper.ShowQueryMessage(
                    VsResources.PackageRestoreConfirmation,
                    VsResources.DialogTitle,
                    showCancelButton: false);
                if (result != true)
                {
                    return;
                }
            }

            Exception exception = null;

            IVsThreadedWaitDialog2 waitDialog;
            _waitDialogFactory.CreateInstance(out waitDialog);
            try
            {
                waitDialog.StartWaitDialog(
                    VsResources.DialogTitle,
                    VsResources.PackageRestoreWaitMessage,
                    String.Empty,
                    varStatusBmpAnim: null,
                    szStatusBarText: null,
                    iDelayToShowDialog: 0,
                    fIsCancelable: false,
                    fShowMarqueeProgress: true);

                if (fromActivation)
                {
                    // only enable package restore consent if this is called as a result of user enabling package restore
                    SetPackageRestoreConsent();
                }

                EnablePackageRestore(fromActivation);
            }
            catch (Exception ex)
            {
                exception = ex;
                ExceptionHelper.WriteToActivityLog(exception);
            }
            finally
            {
                int canceled;
                waitDialog.EndWaitDialog(out canceled);
            }

            if (fromActivation)
            {
                if (exception != null)
                {
                    // show error message
                    MessageHelper.ShowErrorMessage(
                        VsResources.PackageRestoreErrorMessage +
                            Environment.NewLine +
                            Environment.NewLine +
                            ExceptionUtility.Unwrap(exception).Message,
                        VsResources.DialogTitle);
                }
                else
                {
                    // show success message
                    MessageHelper.ShowInfoMessage(
                        VsResources.PackageRestoreCompleted,
                        VsResources.DialogTitle);
                }
            }
        }

        public event EventHandler<PackagesMissingStatusEventArgs> PackagesMissingStatusChanged = delegate { };

        public Task RestoreMissingPackages()
        {
            TaskScheduler uiScheduler;
            try
            {
                uiScheduler = TaskScheduler.FromCurrentSynchronizationContext();
            }
            catch (InvalidOperationException)
            {
                // this exception occurs during unit tests
                uiScheduler = TaskScheduler.Default;
            }

            Task task = Task.Factory.StartNew(() =>
            {
                IVsPackageManager packageManager = _packageManagerFactory.CreatePackageManager();
                IPackageRepository localRepository = packageManager.LocalRepository;
                var projectReferences = GetAllPackageReferences(packageManager);
                foreach (var reference in projectReferences)
                {
                    if (!localRepository.Exists(reference.Id, reference.Version))
                    {
                        packageManager.InstallPackage(reference.Id, reference.Version, ignoreDependencies: true, allowPrereleaseVersions: true);
                    }
                }
            });

            task.ContinueWith(originalTask =>
            {
                if (originalTask.IsFaulted)
                {
                    ExceptionHelper.WriteToActivityLog(originalTask.Exception);
                }
                else
                {
                    // we don't allow canceling
                    Debug.Assert(!originalTask.IsCanceled);

                    // after we're done with restoring packages, do the check again
                    CheckForMissingPackages();
                }
            }, uiScheduler);

            return task;
        }

        public void CheckForMissingPackages()
        {
            bool missing = IsCurrentSolutionEnabledForRestore && CheckForMissingPackagesCore();
            PackagesMissingStatusChanged(this, new PackagesMissingStatusEventArgs(missing));
        }

        private void EnablePackageRestore(bool fromActivation)
        {
            _buildServicesManager.EnsureBuildServicesInstalledInSolution(fromActivation);
            
            IVsPackageManager packageManager = _packageManagerFactory.CreatePackageManager();
            foreach (Project project in _solutionManager.GetProjects())
            {
                EnablePackageRestore(project, packageManager);
            }
        }

        private void SetPackageRestoreConsent()
        {
            var consent = new PackageRestoreConsent(_settings);
            if (!consent.IsGranted)
            {
                consent.IsGranted = true;
            }
        }

        private void EnablePackageRestore(Project project, IVsPackageManager packageManager)
        {
            var projectManager = packageManager.GetProjectManager(project);
            if (projectManager.LocalRepository.GetPackages().IsEmpty())
            {
                // don't enable package restore for the project if it doesn't have at least one 
                // nuget package installed
                return;
            }

            EnablePackageRestore(project);
        }

        private void EnablePackageRestore(Project project)
        {
            if (project.IsWebSite() || project.IsJavaScriptProject())
            {
                // Can't do anything with Website
                // Also, the Javascript Metro project system has some weird bugs 
                // that cause havoc with the package restore mechanism
                return;
            }

            MsBuildProject buildProject = project.AsMSBuildProject();

            AddSolutionDirProperty(project, buildProject);
            AddNuGetTargets(project, buildProject);
            SetMsBuildProjectProperty(project, buildProject, "RestorePackages", "true");

            if (project.IsJavaScriptProject())
            {
                // JavaScript project requires an extra kick
                // in order to save changes to the project file.
                // TODO: Check with VS team to ask them to fix 
                buildProject.Save();
            }
        }

        private void AddNuGetTargets(Project project, MsBuildProject buildProject)
        {
            string targetsPath = Path.Combine(@"$(SolutionDir)", BuildServicesManager.NuGetTargetsFile);

            // adds an <Import> element to this project file.
            if (buildProject.Xml.Imports == null ||
                buildProject.Xml.Imports.All(import => !targetsPath.Equals(import.Project, StringComparison.OrdinalIgnoreCase)))
            {
                buildProject.Xml.AddImport(targetsPath);
                project.Save();
                buildProject.ReevaluateIfNecessary();
            }
        }

        private void AddSolutionDirProperty(Project project, MsBuildProject buildProject)
        {
            const string solutiondir = "SolutionDir";

            if (buildProject.Xml.Properties == null ||
                buildProject.Xml.Properties.All(p => p.Name != solutiondir))
            {
                string relativeSolutionPath = PathUtility.GetRelativePath(
                    project.FullName,
                    PathUtility.EnsureTrailingSlash(_solutionManager.SolutionDirectory));
                relativeSolutionPath = PathUtility.EnsureTrailingSlash(relativeSolutionPath);

                var solutionDirProperty = buildProject.Xml.AddProperty(solutiondir, relativeSolutionPath);
                solutionDirProperty.Condition =
                    String.Format(
                        CultureInfo.InvariantCulture,
                        @"$({0}) == '' Or $({0}) == '*Undefined*'",
                        solutiondir);

                project.Save();
            }
        }

        private static void SetMsBuildProjectProperty(Project project, MsBuildProject buildProject, string name, string value)
        {
            if (!value.Equals(buildProject.GetPropertyValue(name), StringComparison.OrdinalIgnoreCase))
            {
                buildProject.SetProperty(name, value);
                project.Save();
            }
        }

        private void OnProjectAdded(object sender, ProjectEventArgs e)
        {
            if (IsCurrentSolutionEnabledForRestore)
            {
                EnablePackageRestore(e.Project, _packageManagerFactory.CreatePackageManager());
                CheckForMissingPackages();
            }
        }

        private void OnPackageReferenceAdded(IVsPackageMetadata metadata)
        {
            if (IsCurrentSolutionEnabledForRestore)
            {
                var packageMetadata = (VsPackageMetadata)metadata;
                var fileSystem = packageMetadata.FileSystem as IVsProjectSystem;
                if (fileSystem != null)
                {
                    var project = _solutionManager.GetProject(fileSystem.UniqueName);
                    if (project != null)
                    {
                        // in this case, we know that this project has at least one nuget package,
                        // so enable package restore straight away
                        EnablePackageRestore(project);
                    }
                }
            }
        }

        private void OnSolutionOpenedOrClosed(object sender, EventArgs e)
        {
            CheckForMissingPackages();
        }

        private bool CheckForMissingPackagesCore()
        {
            // this can happen during unit tests
            if (_packageManagerFactory == null)
            {
                return false;
            }

            IVsPackageManager packageManager = _packageManagerFactory.CreatePackageManager();
            IPackageRepository localRepository = packageManager.LocalRepository;
            var projectReferences = GetAllPackageReferences(packageManager);
            return projectReferences.Any(reference => !localRepository.Exists(reference.Id, reference.Version));
        }

        /// <summary>
        /// Gets all package references in all projects of the current solution plus package 
        /// references specified in the solution packages.config
        /// </summary>
        private IEnumerable<PackageReference> GetAllPackageReferences(IVsPackageManager packageManager)
        {
            IEnumerable<PackageReference> projectReferences = from project in _solutionManager.GetProjects()
                                                              from reference in
                                                                  GetPackageReferences(
                                                                      packageManager.GetProjectManager(project))
                                                              select reference;

            var localRepository = packageManager.LocalRepository as SharedPackageRepository;
            if (localRepository != null)
            {
                IEnumerable<PackageReference> solutionReferences = localRepository.PackageReferenceFile.GetPackageReferences();
                return projectReferences.Concat(solutionReferences).Distinct();
            }

            return projectReferences.Distinct();
        }

        /// <summary>
        /// Gets the package references of the specified project.
        /// </summary>
        private IEnumerable<PackageReference> GetPackageReferences(IProjectManager projectManager)
        {
            var packageRepository = projectManager.LocalRepository as PackageReferenceRepository;
            if (packageRepository != null)
            {
                return packageRepository.ReferenceFile.GetPackageReferences();
            }

            return new PackageReference[0];
        }
    }
}