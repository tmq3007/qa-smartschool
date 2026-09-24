using System;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using Xunit;
using QASmartTouch.Helpers;

namespace QASmartClass.Tests
{
    public class V42TouchActivationSubMenuTests
    {
        [Fact]
        public void WireButton_SetsFocusableFalse_AndPressAndHoldDisabled()
        {
            var thread = new Thread(() =>
            {
                var button = new Button();
                Assert.True(button.Focusable); // Default in WPF is true

                TouchActivationHelper.WireButton(button);

                Assert.False(button.Focusable);
                Assert.False(System.Windows.Input.Stylus.GetIsPressAndHoldEnabled(button));
                Assert.True(TouchActivationHelper.GetIsTouchWired(button));
            });
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
            thread.Join();
        }

        [Fact]
        public void WireButton_DoesNotDuplicateWiring_WhenCalledMultipleTimes()
        {
            var thread = new Thread(() =>
            {
                var button = new Button();
                TouchActivationHelper.WireButton(button);
                Assert.True(TouchActivationHelper.GetIsTouchWired(button));

                // Call again
                TouchActivationHelper.WireButton(button);
                Assert.True(TouchActivationHelper.GetIsTouchWired(button));
                Assert.False(button.Focusable);
            });
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
            thread.Join();
        }

        [Fact]
        public void WireAllInteractiveControls_RecursivelyWiresButtons()
        {
            var thread = new Thread(() =>
            {
                var grid = new Grid();
                var panel = new StackPanel();
                var btn1 = new Button { Content = "MultiUser" };
                var btn2 = new Button { Content = "NewBoard" };
                var radio = new RadioButton { Content = "Tab1" };
                var excludedBtn = new Button { Content = "Close" };

                panel.Children.Add(btn1);
                panel.Children.Add(btn2);
                panel.Children.Add(radio);
                panel.Children.Add(excludedBtn);
                grid.Children.Add(panel);

                TouchActivationHelper.WireAllInteractiveControls(grid, exclude: excludedBtn);

                Assert.True(TouchActivationHelper.GetIsTouchWired(btn1));
                Assert.False(btn1.Focusable);
                Assert.True(TouchActivationHelper.GetIsTouchWired(btn2));
                Assert.False(btn2.Focusable);
                Assert.True(TouchActivationHelper.GetIsTouchWired(radio));
                Assert.False(radio.Focusable);

                // Excluded button should NOT be wired by WireAllInteractiveControls
                Assert.False(TouchActivationHelper.GetIsTouchWired(excludedBtn));
            });
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
            thread.Join();
        }

        [Fact]
        public void Apply_WithNullWindow_DoesNotThrow()
        {
            TouchActivationHelper.Apply(null!);
            TouchActivationHelper.ApplyToSubMenu(null!);
            TouchActivationHelper.WireAllInteractiveControls(null!);
            TouchActivationHelper.WireButton(null!);
        }
    }
}
