using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Runtime.Modules.Screens;

namespace Runtime.Views.Controls;

public partial class RuntimeTab : UserControl
{
    public RuntimeTab()
    {
        InitializeComponent();
    }

    public void ApplyDescriptor(ComponentDescriptor d)
    {
        if (d == null) return;
        IsVisible = d.Visible;
        IsEnabled = d.Enabled;
    }
}
