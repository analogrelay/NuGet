using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.Shell.Interop;

namespace NuGet.VisualStudio.UI
{
    [Export(typeof(IUIService))]
    internal class VsUIService : IUIService
    {
        IVsThreadedWaitDialogFactory _waitDialogFactory;

        [ImportingConstructor]
        public VsUIService(
            IVsThreadedWaitDialogFactory waitDialogFactory) {
            _waitDialogFactory = waitDialogFactory;
        }

        public IDisposable ShowWaitDialog(string title, string message)
        {
            IVsThreadedWaitDialog2 waitDialog;
            _waitDialogFactory.CreateInstance(out waitDialog);
            waitDialog.StartWaitDialog(
                title,
                message,
                String.Empty,
                varStatusBmpAnim: null,
                szStatusBarText: null,
                iDelayToShowDialog: 0,
                fIsCancelable: false,
                fShowMarqueeProgress: true);
            return new DisposableAction(() =>
            {
                int _; // Don't care
                waitDialog.EndWaitDialog(out _);
            });
        }
    }
}
