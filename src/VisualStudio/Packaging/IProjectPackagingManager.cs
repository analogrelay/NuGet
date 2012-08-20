using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NuGet.VisualStudio.Packaging
{
    public interface IProjectPackagingManager
    {
        /// <summary>
        /// Gets a value indicating whether the current solution is configured for Packaging.
        /// </summary>
        bool IsCurrentSolutionEnabledForPackaging { get; }

        /// <summary>
        /// Configures the current solution for Packaging.
        /// </summary>
        /// <param name="fromActivation">if set to <c>false</c>, the method will not show any error message.</param>
        void EnableCurrentSolutionForPackaging(bool fromActivation);
    }
}
