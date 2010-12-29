﻿using System;
using System.Collections.Generic;
using EnvDTE;

namespace NuGet.VisualStudio.Test.Mocks {
    public abstract class MockSolutionManager : ISolutionManager {
        public event EventHandler SolutionOpened;

        public event EventHandler SolutionClosing;

        public abstract string SolutionDirectory {
            get;
        }

        public abstract bool IsSolutionOpen { get; }

        public string DefaultProjectName {
            get;
            set;
        }

        public abstract Project DefaultProject {
            get;
        }

        public abstract Project GetProject(string projectName);

        public abstract IEnumerable<Project> GetProjects();

        public void CloseSolution() {
            if (SolutionClosing != null) {
                SolutionClosing(this, EventArgs.Empty);
            }
        }

        public void OpenSolution() {
            if (SolutionOpened != null) {
                SolutionOpened(this, EventArgs.Empty);
            }
        }
    }
}