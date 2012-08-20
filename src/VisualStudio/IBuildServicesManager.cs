using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NuGet.VisualStudio
{
    public interface IBuildServicesManager
    {
        /// <summary>
        /// Gets a value indicating whether NuGet Build Services are installed in the current solution.
        /// </summary>
        bool AreBuildServicesInstalledForSolution { get; }

        /// <summary>
        /// Installs NuGet Build Services in the current solution.
        /// </summary>
        /// <remarks>
        /// IMPORTANT: This method will NOT produce any UI, will block the calling thread and will throw exceptions if errors occur.
        /// </remarks>
        /// <param name="fromActivation">Indicates if the operation is being performed due to direct user action.</param>
        void EnsureBuildServicesInstalledInSolution(bool fromActivation);
    }
}
