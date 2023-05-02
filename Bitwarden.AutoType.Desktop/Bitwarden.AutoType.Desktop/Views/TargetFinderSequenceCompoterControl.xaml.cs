using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Bitwarden.AutoType.Desktop.Helpers;

namespace Bitwarden.AutoType.Desktop.Views
{
    /// <summary>
    /// Interaction logic for TargetFinderSequenceCompoterControl.xaml
    /// </summary>
    public partial class TargetFinderSequenceCompoterControl : UserControl
    {
        [DllImport("user32.dll", SetLastError = true)]
        public static extern IntPtr WindowFromPoint(POINT Point);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern uint GetWindowThreadProcessId(IntPtr hWnd, out int lpdwProcessId);

        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        public static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        public static extern int GetClassName(IntPtr hWnd, StringBuilder lpClassName, int nMaxCount);

        public struct POINT
        {
            public int X;
            public int Y;

            public POINT(int x, int y)
            {
                X = x;
                Y = y;
            }

            public POINT(System.Drawing.Point pt) : this(pt.X, pt.Y)
            {
            }

            public static implicit operator System.Drawing.Point(POINT p)
            {
                return new System.Drawing.Point(p.X, p.Y);
            }

            public static implicit operator POINT(System.Drawing.Point p)
            {
                return new POINT(p.X, p.Y);
            }
        }

        public TargetFinderSequenceCompoterControl()
        {
            InitializeComponent();
            EditableTextBoxSequence.Text = Constants.BitwardenDefaultSequence;
        }

        #region EditableComboBoxTargetType

        private void EditableComboBoxTargetType_Loaded(object sender, RoutedEventArgs e)
        {
            foreach (TargetTypes targetType in Enum.GetValues(typeof(TargetTypes)))
            {
                EditableComboBoxTargetType.Items.Add(targetType);
            }

            EditableComboBoxTargetType.SelectedIndex = 0;
        }

        private void EditableComboBoxTargetType_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var selectedType = EditableComboBoxTargetType.SelectedItem;

            if (selectedType is TargetTypes targetType)
            {
                var target = targetType switch
                {
                    TargetTypes.Title => ProcessTitleTextBox.Text,
                    TargetTypes.Process => ProcessNameTextBox.Text,
                    TargetTypes.Class => ProcessClassNameTextBox.Text,
                    _ => string.Empty
                };

                if (EditableTextBoxTargetRegex is null)
                {
                    return;
                }

                EditableTextBoxTargetRegex.Text = $"^{target}$";
                UpdateCustomField(EditableTextBoxTargetRegex.Text, targetType, EditableTextBoxSequence.Text);
            }
        }

        #endregion EditableComboBoxTargetType

        private void FindWindowIcon_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            FindWindowIcon.CaptureMouse();

            Mouse.OverrideCursor = Cursors.Cross;
            Mouse.Captured.MouseLeftButtonUp += FindWindowIcon_MouseLeftButtonUp;
            Mouse.Captured.MouseMove += FindWindowIcon_MouseMove;
        }

        private void FindWindowIcon_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            Mouse.OverrideCursor = null;
            FindWindowIcon.ReleaseMouseCapture();

            var position = e.GetPosition(this);
            var screenPosition = PointToScreen(position);
            var point = new POINT((int)screenPosition.X, (int)screenPosition.Y);
            var hWnd = WindowFromPoint(point);

            UpdateTargetInfo(hWnd);

            if (Mouse.Captured != null)
            {
                Mouse.Captured.MouseLeftButtonUp -= FindWindowIcon_MouseLeftButtonUp;
                Mouse.Captured.MouseMove -= FindWindowIcon_MouseMove;
            }
        }

        private void FindWindowIcon_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                var position = e.GetPosition(this);
                var screenPosition = PointToScreen(position);
                var point = new POINT((int)screenPosition.X, (int)screenPosition.Y);
                var hWnd = WindowFromPoint(point);

                int processId;
                GetWindowThreadProcessId(hWnd, out processId);

                var targetProcess = Process.GetProcessById(processId);
                ProcessNameTextBox.Text = targetProcess.ProcessName;

                var title = new StringBuilder(256);
                GetWindowText(hWnd, title, title.Capacity);
                ProcessTitleTextBox.Text = title.ToString();

                var className = new StringBuilder(256);
                GetClassName(hWnd, className, className.Capacity);
                ProcessClassNameTextBox.Text = className.ToString();
            }
        }

        private void UpdateTargetInfo(IntPtr hWnd)
        {
            int processId;
            GetWindowThreadProcessId(hWnd, out processId);

            var targetProcess = Process.GetProcessById(processId);
            ProcessNameTextBox.Text = targetProcess.ProcessName;

            var title = new StringBuilder(256);
            GetWindowText(hWnd, title, title.Capacity);
            ProcessTitleTextBox.Text = title.ToString();

            var className = new StringBuilder(256);
            GetClassName(hWnd, className, className.Capacity);
            ProcessClassNameTextBox.Text = className.ToString();

            var selectedType = EditableComboBoxTargetType.SelectedItem;

            if (selectedType is TargetTypes targetType)
            {
                var target = targetType switch
                {
                    TargetTypes.Title => title.ToString(),
                    TargetTypes.Process => targetProcess.ProcessName,
                    TargetTypes.Class => className.ToString(),
                    _ => string.Empty
                };

                EditableTextBoxTargetRegex.Text = $"^{target}$";
                // dont need to update custom field here because it will be updated by EditableComboBoxTargetType_SelectionChanged
                // leaving commented out for now in case we want to change this behavior
                // UpdateCustomField(EditableTextBoxTargetRegex.Text, targetType, EditableTextBoxSequence.Text);
            }
        }

        private void UpdateCustomField(string target, TargetTypes targetType, string sequence)
        {
            var customField = new AutoTypeCustomField
            {
                Target = target,
                Type = targetType,
                Sequence = sequence
            };
            EditableTextBoxCustomFieldValue.Text = SerializeAutoTypeCustomField(customField);
        }

        #region Helpers

        private void CopyButton_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            if (button is null) return;
            var textBox = button.Tag as TextBox;

            if (textBox != null)
            {
                Clipboard.SetText(textBox.Text);
            }
        }

        public static string SerializeAutoTypeCustomField(AutoTypeCustomField obj)
        {
            var options = new JsonSerializerOptions
            {
                WriteIndented = false,
            };

            return JsonSerializer.Serialize(obj, options);
        }

        #endregion Helpers

        private void EditableTextBoxTargetRegex_TextChanged(object sender, TextChangedEventArgs e)
        {
            var selectedType = EditableComboBoxTargetType.SelectedItem;

            if (selectedType is TargetTypes targetType)
            {
                var target = targetType switch
                {
                    TargetTypes.Title => ProcessTitleTextBox.Text,
                    TargetTypes.Process => ProcessNameTextBox.Text,
                    TargetTypes.Class => ProcessClassNameTextBox.Text,
                    _ => string.Empty
                };

                UpdateCustomField(EditableTextBoxTargetRegex.Text, targetType, EditableTextBoxSequence.Text);
            }
        }

        private void EditableTextBoxSequence_TextChanged(object sender, TextChangedEventArgs e)
        {
            var selectedType = EditableComboBoxTargetType.SelectedItem;

            if (selectedType is TargetTypes targetType)
            {
                var target = targetType switch
                {
                    TargetTypes.Title => ProcessTitleTextBox.Text,
                    TargetTypes.Process => ProcessNameTextBox.Text,
                    TargetTypes.Class => ProcessClassNameTextBox.Text,
                    _ => string.Empty
                };

                UpdateCustomField(EditableTextBoxTargetRegex.Text, targetType, EditableTextBoxSequence.Text);
            }
        }

        private void ShowSequenceButton_Click(object sender, RoutedEventArgs e)
        {
            // NotepadHelper.OpenNotepadAndTypeText(Constants.DefaultSequenceHelpText);
            TextFileHelper.OpenTextFileAndTypeText("AutoType_Sequences", Constants.DefaultSequenceHelpText);
        }

        private void QuestionButton_Click(object sender, RoutedEventArgs e)
        {
            // Your code to handle the question button click
            TextFileHelper.OpenTextFileAndTypeText("How_to_add_AutoType_sequence_to_bitwarden", Constants.DefaultAddFieldToBitwardenHelpText);
        }
    }
}