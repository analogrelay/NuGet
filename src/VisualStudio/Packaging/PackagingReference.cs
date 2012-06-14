using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NuGet.VisualStudio.Packaging
{
    /// <summary>
    /// Abstraction over the references and dependencies.
    /// </summary>
    /// <remarks>
    /// Abstracts over:
    /// * Framework References
    /// * Assemblies in lib (except the primary project output)
    /// * Project References
    /// * Package Dependencies
    /// </remarks>
    public abstract class PackagingReference
    {
    }

    public class ProjectReference : PackagingReference { }
    public class AssemblyReference : PackagingReference { }
    public class PackageReference : PackagingReference { }
}
