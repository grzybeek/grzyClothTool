using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace grzyClothTool.Controls;

public class ModernLabelBaseControl : UserControl
{
    protected bool IsUserInitiated = false;
    public event EventHandler<UpdatedEventArgs> IsUpdated;

    protected static void OnUpdate(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var control = (ModernLabelBaseControl)d;

        control.IsUpdated?.Invoke(control, new UpdatedEventArgs
        {
            DependencyPropertyChangedEventArgs = e,
            IsUserInitiated = control.IsUserInitiated
        });


        control.Dispatcher.BeginInvoke((Action)(() => control.IsUserInitiated = false), DispatcherPriority.ContextIdle);
    }
}
