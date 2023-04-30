using System.Windows;
using System.Windows.Controls;

namespace Bitwarden.AutoType.Desktop.Helpers;

public class PasswordBoxBindingBehavior : DependencyObject
{
    public static readonly DependencyProperty PasswordBindingProperty =
        DependencyProperty.RegisterAttached("PasswordBinding", typeof(string), typeof(PasswordBoxBindingBehavior), new FrameworkPropertyMetadata(string.Empty, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, AttachedPropertyValueChanged));

    public static string GetPasswordBinding(DependencyObject obj)
    {
        return (string)obj.GetValue(PasswordBindingProperty);
    }

    public static void SetPasswordBinding(DependencyObject obj, string value)
    {
        obj.SetValue(PasswordBindingProperty, value);
    }

    private static void AttachedPropertyValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is PasswordBox passwordBox)
        {
            // Remove the previous event subscription if any
            passwordBox.PasswordChanged -= PasswordBox_PasswordChanged;

            // Subscribe to the event
            passwordBox.PasswordChanged += PasswordBox_PasswordChanged;

            // Update the PasswordBox when the property is changed from ViewModel

            string? newValue = e.NewValue as string;
            if (newValue != null && !passwordBox.Password.Equals(newValue))
            {
                passwordBox.Password = newValue;
            }
            else if (newValue == null && !string.IsNullOrEmpty(passwordBox.Password))
            {
                passwordBox.Password = string.Empty;
            }
        }
    }

    private static void PasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
    {
        if (sender is PasswordBox passwordBox)
        {
            SetPasswordBinding(passwordBox, passwordBox.Password);
        }
    }
}