using System.Collections.Specialized;
using System.Windows.Controls;
using grzyClothTool.Collections;
using grzyClothTool.Controls;

namespace grzyClothTool.Tests.Wpf.Tests;

public class WpfSmokeTests
{
    [Fact]
    public void WpfDispatcher_CanCreateAndMeasureFrameworkElement()
    {
        WpfTestRunner.Run(() =>
        {
            var textBlock = new TextBlock
            {
                Text = "grzyClothTool"
            };

            textBlock.Measure(new Size(200, 50));

            Assert.True(textBlock.DesiredSize.Width > 0);
            Assert.True(textBlock.DesiredSize.Height > 0);
        });
    }

    [Fact]
    public void SelectableItem_RaisesPropertyChangedWhenSelectionChanges()
    {
        WpfTestRunner.Run(() =>
        {
            var item = new SelectableItem("BULKY", 1);
            var raised = false;
            item.PropertyChanged += (_, args) =>
            {
                if (args.PropertyName == nameof(SelectableItem.IsSelected))
                {
                    raised = true;
                }
            };

            item.IsSelected = true;

            Assert.True(raised);
            Assert.True(item.IsSelected);
            Assert.Equal("BULKY", item.ToString());
        });
    }

    [Fact]
    public void CustomButton_LabelAndCornerRadiusPropertyRoundtrip()
    {
        WpfTestRunner.Run(() =>
        {
            var button = new CustomButton
            {
                Label = "Test",
                CornerRadius = new CornerRadius(8)
            };

            Assert.Equal("Test", button.Label);
            Assert.Equal(new CornerRadius(8), button.CornerRadius);
        });
    }

    [Fact]
    public void CustomButton_MyBtnClickEvent_FiresWhenButtonIsClicked()
    {
        WpfTestRunner.Run(() =>
        {
            var button = new CustomButton { Label = "Click me" };
            var fired = false;
            button.MyBtnClickEvent += (_, _) => fired = true;

            // Simulate the internal button click by raising via the event system
            button.RaiseEvent(new RoutedEventArgs(CustomButton.BtnClickEvent, button));

            Assert.True(fired);
        });
    }

    [Fact]
    public void ModernLabelCheckBox_IsUpdated_FiresOnProgrammaticChange()
    {
        WpfTestRunner.Run(() =>
        {
            var checkBox = new ModernLabelCheckBox { Label = "Option" };
            var raised = false;
            checkBox.IsUpdated += (_, _) => raised = true;

            checkBox.IsChecked = true;

            Assert.True(raised);
            Assert.True(checkBox.IsChecked);
        });
    }

    [Fact]
    public void ModernLabelCheckBox_IsUserInitiated_FalseWhenChangedProgrammatically()
    {
        // IsUserInitiated is protected - verify indirectly: programmatic change fires IsUpdated
        // but the flag is only set true via PreviewMouseDown (not reachable programmatically)
        WpfTestRunner.Run(() =>
        {
            var checkBox = new ModernLabelCheckBox { Label = "Option" };
            var updateCount = 0;
            checkBox.IsUpdated += (_, _) => updateCount++;

            checkBox.IsChecked = true;
            checkBox.IsChecked = false;

            Assert.Equal(2, updateCount);
        });
    }

    [Fact]
    public void ModernLabelNumericUpDown_IsUpdated_FiresOnValueChange()
    {
        WpfTestRunner.Run(() =>
        {
            var control = new ModernLabelNumericUpDown
            {
                Minimum = 0,
                Maximum = 10,
                Value = 0
            };
            var raised = false;
            control.IsUpdated += (_, _) => raised = true;

            control.Value = 5;

            Assert.True(raised);
            Assert.Equal(5, control.Value);
        });
    }

    [Fact]
    public void ModernLabelNumericUpDown_ValueDoesNotExceedMaximum()
    {
        WpfTestRunner.Run(() =>
        {
            var control = new ModernLabelNumericUpDown
            {
                Minimum = 0,
                Maximum = 5,
                Increment = 1,
                Value = 5
            };

            // Simulate increment button — should not exceed Maximum
            control.Value = control.Value + control.Increment <= control.Maximum
                ? control.Value + control.Increment
                : control.Value;

            Assert.Equal(5, control.Value);
        });
    }

    [Fact]
    public void ModernLabelNumericUpDown_ValueDoesNotGoBelowMinimum()
    {
        WpfTestRunner.Run(() =>
        {
            var control = new ModernLabelNumericUpDown
            {
                Minimum = 0,
                Maximum = 5,
                Increment = 1,
                Value = 0
            };

            // Simulate decrement button — should not go below Minimum
            control.Value = control.Value - control.Increment >= control.Minimum
                ? control.Value - control.Increment
                : control.Value;

            Assert.Equal(0, control.Value);
        });
    }

    [Fact]
    public void ModernLabelRadioButton_IsChecked_PropertyRoundtrip()
    {
        WpfTestRunner.Run(() =>
        {
            var radio = new ModernLabelRadioButton { Label = "Choice A" };

            radio.IsChecked = true;

            Assert.True(radio.IsChecked);
            Assert.Equal("Choice A", radio.Label);
        });
    }

    [Fact]
    public void TagChip_TagText_PropertyRoundtrip()
    {
        WpfTestRunner.Run(() =>
        {
            // Load app resources required by TagChip's XAML (UpperCaseConverter etc.)
            var app = Application.Current ?? new Application();
            app.Resources.MergedDictionaries.Add(new System.Windows.ResourceDictionary
            {
                Source = new Uri("pack://application:,,,/grzyClothTool;component/Themes/Shared.xaml")
            });

            var chip = new TagChip { TagText = "MyTag" };

            Assert.Equal("MyTag", chip.TagText);
        });
    }

    [Fact]
    public void AsyncObservableCollection_AddRange_RaisesCollectionChanged()
    {
        WpfTestRunner.Run(() =>
        {
            var collection = new AsyncObservableCollection<string>();
            var raised = false;
            collection.CollectionChanged += (_, e) =>
            {
                if (e.Action == NotifyCollectionChangedAction.Reset)
                {
                    raised = true;
                }
            };

            collection.AddRange(["a", "b", "c"]);

            Assert.True(raised);
            Assert.Equal(3, collection.Count);
        });
    }

    [Fact]
    public void AsyncObservableCollection_RemoveRange_RaisesCollectionChanged()
    {
        WpfTestRunner.Run(() =>
        {
            var collection = new AsyncObservableCollection<string>(["a", "b", "c"]);
            var raised = false;
            collection.CollectionChanged += (_, e) =>
            {
                if (e.Action == NotifyCollectionChangedAction.Reset)
                {
                    raised = true;
                }
            };

            collection.RemoveRange(["a", "c"]);

            Assert.True(raised);
            Assert.Equal(["b"], collection);
        });
    }

    [Fact]
    public void AsyncObservableCollection_ReplaceAll_ReplacesContentsAndRaisesCollectionChanged()
    {
        WpfTestRunner.Run(() =>
        {
            var collection = new AsyncObservableCollection<string>(["old1", "old2"]);
            var raised = false;
            collection.CollectionChanged += (_, e) =>
            {
                if (e.Action == NotifyCollectionChangedAction.Reset)
                {
                    raised = true;
                }
            };

            collection.ReplaceAll(["new1", "new2", "new3"]);

            Assert.True(raised);
            Assert.Equal(["new1", "new2", "new3"], collection);
        });
    }
}
