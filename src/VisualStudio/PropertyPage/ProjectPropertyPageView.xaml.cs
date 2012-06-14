using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace NuGet.VisualStudio.PropertyPage
{
    /// <summary>
    /// Interaction logic for ProjectPropertyPageView.xaml
    /// </summary>
    public partial class ProjectPropertyPageView : UserControl, IView<ProjectPropertyPageViewModel>
    {
        public static readonly DependencyProperty ViewModelProperty = DependencyProperty.Register(
            "ViewModel", typeof(ProjectPropertyPageViewModel), typeof(ProjectPropertyPageView));

        public ProjectPropertyPageViewModel ViewModel
        {
            get { return (ProjectPropertyPageViewModel)GetValue(ViewModelProperty); }
            set { SetValue(ViewModelProperty, value); }
        }

        public ProjectPropertyPageView()
        {
            InitializeComponent();
        }

        public void SetViewModel(ProjectPropertyPageViewModel vm)
        {
            ViewModel = vm;
        }
    }
}
