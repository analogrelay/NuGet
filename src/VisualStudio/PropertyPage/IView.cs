using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NuGet.VisualStudio.PropertyPage
{
    public interface IView<TViewModel>
    {
        void SetViewModel(TViewModel vm);
    }
}
