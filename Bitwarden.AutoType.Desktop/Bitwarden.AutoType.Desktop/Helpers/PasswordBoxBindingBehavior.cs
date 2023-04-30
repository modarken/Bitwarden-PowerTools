using System.Windows;
using System.Windows.Controls;

namespace Bitwarden.AutoType.Desktop.Helpers
{
    //public class PasswordBoxBindingBehavior : Behavior<PasswordBox>
    //{
    //    public static readonly DependencyProperty PasswordProperty =
    //        DependencyProperty.Register("Password", typeof(string), typeof(PasswordBoxBindingBehavior),
    //            new FrameworkPropertyMetadata(string.Empty, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, PropertyChangedCallback));

    //    public string Password
    //    {
    //        get { return (string)GetValue(PasswordProperty); }
    //        set { SetValue(PasswordProperty, value); }
    //    }

    //    protected override void OnAttached()
    //    {
    //        base.OnAttached();
    //        AssociatedObject.PasswordChanged += AssociatedObject_PasswordChanged;
    //    }

    //    protected override void OnDetaching()
    //    {
    //        base.OnDetaching();
    //        AssociatedObject.PasswordChanged -= AssociatedObject_PasswordChanged;
    //    }

    //    private void AssociatedObject_PasswordChanged(object sender, RoutedEventArgs e)
    //    {
    //        Password = AssociatedObject.Password;
    //    }

    //    private static void PropertyChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e)
    //    {
    //        var behavior = (PasswordBoxBindingBehavior)d;
    //        behavior.AssociatedObject.Password = (string)e.NewValue;
    //    }
    //}

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

        //private static void AttachedPropertyValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        //{
        //    if (d is PasswordBox passwordBox)
        //    {
        //        // passwordBox.PasswordChanged -= PasswordBox_PasswordChanged;

        //        if (e.NewValue == null)
        //        {
        //            passwordBox.PasswordChanged += PasswordBox_PasswordChanged;
        //        }
        //    }
        //}

        //private static void AttachedPropertyValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        //{
        //    if (d is PasswordBox passwordBox)
        //    {
        //        // Remove the previous event subscription if any
        //        passwordBox.PasswordChanged -= PasswordBox_PasswordChanged;

        //        // Subscribe to the event
        //        passwordBox.PasswordChanged += PasswordBox_PasswordChanged;

        //        // Update the PasswordBox when the property is changed from ViewModel
        //        if (e.NewValue != null && !passwordBox.Password.Equals((string)e.NewValue))
        //        {
        //            passwordBox.Password = (string)e.NewValue;
        //        }
        //    }
        //}

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
}