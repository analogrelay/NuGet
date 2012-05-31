﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NuGet
{
    /// <summary>
    /// Provides an interface to an operation-aware repository, which is a repository which can report client data to
    /// a server, such as the operation being performed, in a protocol header (such as an HTTP Header).
    /// </summary>
    public interface IOperationAwareRepository
    {
        IDisposable StartOperation(string operation);
    }
}
