using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Bitwarden.AutoType.Desktop.Helpers;

namespace Bitwarden.AutoType.Desktop.Views
{
    /// <summary>
    /// Interaction logic for TargetFinderSequenceComposerControl.xaml
    /// </summary>
    public partial class TargetFinderSequenceComposerControl : UserControl
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

        public TargetFinderSequenceComposerControl()
        {
            InitializeComponent();
            EditableTextBoxSequence.Text = Constants.BitwardenDefaultSequence;
            UpdateCustomField();
        }

        private void FindWindowIcon_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            FindWindowIcon.CaptureMouse();

            Mouse.OverrideCursor = Cursors.Cross;
            Mouse.Captured.MouseLeftButtonUp += FindWindowIcon_MouseLeftButtonUp;
            Mouse.Captured.MouseMove += FindWindowIcon_MouseMove;
        }

        private void FindWindowIcon_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (Mouse.Captured != null)
            {
                Mouse.Captured.MouseLeftButtonUp -= FindWindowIcon_MouseLeftButtonUp;
                Mouse.Captured.MouseMove -= FindWindowIcon_MouseMove;
            }

            Mouse.OverrideCursor = null;
            FindWindowIcon.ReleaseMouseCapture();

            var position = e.GetPosition(this);
            var screenPosition = PointToScreen(position);
            var point = new POINT((int)screenPosition.X, (int)screenPosition.Y);
            var hWnd = WindowFromPoint(point);

            UpdateTargetInfo(hWnd);
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
        }

        private void UpdateCustomField()
        {
            var customField = new AutoTypeCustomField
            {
                Title = Normalize(IncludeTitleRegexTextBox.Text),
                Process = Normalize(IncludeProcessRegexTextBox.Text),
                Class = Normalize(IncludeClassRegexTextBox.Text),
                ExcludeTitle = Normalize(ExcludeTitleRegexTextBox.Text),
                ExcludeProcess = Normalize(ExcludeProcessRegexTextBox.Text),
                ExcludeClass = Normalize(ExcludeClassRegexTextBox.Text),
                Sequence = EditableTextBoxSequence.Text
            };

            EditableTextBoxCustomFieldValue.Text = SerializeAutoTypeCustomField(customField);
        }

        private static string? Normalize(string? value)
        {
            return string.IsNullOrWhiteSpace(value) ? null : value;
        }

        private static string CreateExactRegex(string value)
        {
            return $"^{Regex.Escape(value)}$";
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
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            };

            return JsonSerializer.Serialize(obj, options);
        }

        #endregion Helpers

        private void ComposerInput_TextChanged(object sender, TextChangedEventArgs e)
        {
            UpdateCustomField();
        }

        private void UseDetectedValueButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button button || button.Tag is not string target)
            {
                return;
            }

            switch (target)
            {
                case "Title" when !string.IsNullOrWhiteSpace(ProcessTitleTextBox.Text):
                    IncludeTitleRegexTextBox.Text = CreateExactRegex(ProcessTitleTextBox.Text);
                    break;
                case "Process" when !string.IsNullOrWhiteSpace(ProcessNameTextBox.Text):
                    IncludeProcessRegexTextBox.Text = CreateExactRegex(ProcessNameTextBox.Text);
                    break;
                case "Class" when !string.IsNullOrWhiteSpace(ProcessClassNameTextBox.Text):
                    IncludeClassRegexTextBox.Text = CreateExactRegex(ProcessClassNameTextBox.Text);
                    break;
                default:
                    return;
            }

            UpdateCustomField();
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