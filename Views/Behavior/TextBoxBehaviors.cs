using System;
using System.Windows;
using System.Windows.Controls;

namespace ShotgunMetagenome.Views.Behavior
{
    public class TextBoxBehaviors
    {
        public static readonly DependencyProperty AutoScrollToEndProperty = DependencyProperty.RegisterAttached("AutoScrollToEnd", typeof(bool), typeof(TextBoxBehaviors), new UIPropertyMetadata(false, IsTextChanged));

        [AttachedPropertyBrowsableForType(typeof(TextBox))]
        public static bool GetAutoScrollToEnd(DependencyObject obj)
        {
            return (bool)obj.GetValue(AutoScrollToEndProperty);
        }

        public static void SetAutoScrollToEnd(DependencyObject obj, bool value)
        {
            obj.SetValue(AutoScrollToEndProperty, value);
        }

        private static void IsTextChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            TextBox textBox = sender as TextBox;
            if (textBox != null)
            {
                textBox.TextChanged -= OnTextChanged;
                Console.Write("-");
                if ((bool)e.NewValue)
                {
                    textBox.TextChanged += OnTextChanged;
                }
            }
        }

        private static void OnTextChanged(object sender, TextChangedEventArgs e)
        {
            TextBox textBox = sender as TextBox;
            if (textBox != null && !string.IsNullOrEmpty(textBox.Text))
            {
                textBox.ScrollToEnd();
                Console.Write("*");
            }
        }
    }
}
