using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Bitwarden.AutoType.Desktop.Services;
using Bitwarden.AutoType.Desktop.Windows;
using Bitwarden.AutoType.Desktop.Windows.Native;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Bitwarden.AutoType.Desktop.Views
{
    [INotifyPropertyChanged]
    public partial class HotkeyControl : UserControl
    {
        public event EventHandler ChangeHotkeyButtonClicked;

        public event EventHandler<WindowsHotKey> HotkeyChanged;

        public event EventHandler CancelButtonClicked;

        private WindowsHotKey? _hotkey;

        public string HotkeyText
        {
            get => $"{_hotkey?.KeyModifiers} + {_hotkey?.Key}";
        }


        [ObservableProperty]
        private bool _isHotKeyActive = false;


#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

        public HotkeyControl()
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        {
            InitializeComponent();
            ChangeHotkeyButtonClicked += HotkeyControl_ChangeHotkeyButtonClicked;
            HotkeyChanged += HotkeyControl_HotkeyChanged;
            CancelButtonClicked += HotkeyControl_CancelButtonClicked;

        }

        private bool _wasEnabled = false;

        private void HotkeyControl_ChangeHotkeyButtonClicked(object? sender, EventArgs e)
        {
            if (DataContext is HotkeyService hotkeyService)
            {
                _wasEnabled = hotkeyService.IsEnabled;
                if (_wasEnabled)
                {
                    hotkeyService.Disable();
                }
            }
        }

        private void HotkeyControl_CancelButtonClicked(object? sender, EventArgs e)
        {
            if (DataContext is HotkeyService hotkeyService)
            {
                _hotkey = hotkeyService.GetHotkey();

                if (_wasEnabled)
                {
                    hotkeyService.Enable();
                }
            }
        }

        private void HotkeyControl_HotkeyChanged(object? sender, WindowsHotKey e)
        {
            if (DataContext is HotkeyService hotkeyService)
            {
                hotkeyService.SetHotKey(e);
                if (_wasEnabled)
                {
                    hotkeyService.Enable();
                }
            }
        }

        public void SetHotkey(WindowsHotKey hotkey)
        {
            _hotkey = hotkey;
            HotkeyTextBox.Text = HotkeyText;
        }

        private void ChangeHotkeyButton_Click(object sender, RoutedEventArgs e)
        {
            ChangeHotkeyButtonClicked?.Invoke(this, EventArgs.Empty);
            _hotkey?.Dispose();
            _hotkey = null;
            HotkeyTextBox.Text = "Press new hotkey";
            this.PreviewKeyDown += HotkeyControl_PreviewKeyDown;
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            CancelButtonClicked?.Invoke(this, EventArgs.Empty);
            HotkeyTextBox.Text = HotkeyText;
            this.PreviewKeyDown -= HotkeyControl_PreviewKeyDown;
        }

        private void HotkeyControl_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (_hotkey != null) return;

            if (e.Key == Key.LeftCtrl || e.Key == Key.RightCtrl
                || e.Key == Key.LeftAlt || e.Key == Key.RightAlt
                || e.Key == Key.LeftShift || e.Key == Key.RightShift
                || e.Key == Key.LWin || e.Key == Key.RWin
                || e.Key == Key.System) // Ignore the Alt key
            {
                return;
            }

            VirtualKeys key = (VirtualKeys)KeyInterop.VirtualKeyFromKey(e.Key);
            RegisterHotKeyModifiers keyModifiers = RegisterHotKeyModifiers.None;

            if (Keyboard.Modifiers.HasFlag(ModifierKeys.Control)) keyModifiers |= RegisterHotKeyModifiers.Ctrl;
            if (Keyboard.Modifiers.HasFlag(ModifierKeys.Alt)) keyModifiers |= RegisterHotKeyModifiers.Alt;
            if (Keyboard.Modifiers.HasFlag(ModifierKeys.Shift)) keyModifiers |= RegisterHotKeyModifiers.Shift;
            if (Keyboard.Modifiers.HasFlag(ModifierKeys.Windows)) keyModifiers |= RegisterHotKeyModifiers.Win;

            _hotkey = new WindowsHotKey(key, keyModifiers);

            if (_hotkey.RegisterHotKey())
            {
                HotkeyTextBox.Text = HotkeyText;
                this.PreviewKeyDown -= HotkeyControl_PreviewKeyDown;
                HotkeyChanged?.Invoke(this, _hotkey);
            }
            else
            {
                MessageBox.Show("Failed to register hotkey. Please try again.");
            }
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            if (DataContext is HotkeyService hotkeyService)
            {
                IsHotKeyActive = hotkeyService.IsActive;
                hotkeyService.OnActiveChanged += HotkeyService_OnActiveChanged;
                if (hotkeyService.GetHotkey() is WindowsHotKey windowsHotKey && _hotkey is null)
                {
                    SetHotkey(windowsHotKey);
                }
            }
        }

        private void HotkeyService_OnActiveChanged(object? sender, bool e)
        {
            IsHotKeyActive = e;
        }
    }
}