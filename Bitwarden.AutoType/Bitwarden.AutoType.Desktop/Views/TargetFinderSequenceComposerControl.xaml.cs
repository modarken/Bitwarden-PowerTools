using System;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Bitwarden.AutoType.Desktop.Helpers;

namespace Bitwarden.AutoType.Desktop.Views
{
    /// <summary>
    /// Interaction logic for TargetFinderSequenceComposerControl.xaml
    /// </summary>
    public partial class TargetFinderSequenceComposerControl : UserControl
    {
        private static readonly Brush ErrorBrush = Brushes.IndianRed;
        private static readonly Brush WarningBrush = Brushes.DarkOrange;
        private static readonly Brush SuccessBrush = Brushes.ForestGreen;
        private static readonly Brush NeutralBrush = Brushes.DimGray;

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

            RefreshValidationState(BuildCustomField());
        }

        private void UpdateCustomField()
        {
            var customField = BuildCustomField();

            EditableTextBoxCustomFieldValue.Text = SerializeAutoTypeCustomField(customField);
            RefreshValidationState(customField);
        }

        private AutoTypeCustomField BuildCustomField()
        {
            return new AutoTypeCustomField
            {
                Title = Normalize(IncludeTitleRegexTextBox.Text),
                Process = Normalize(IncludeProcessRegexTextBox.Text),
                Class = Normalize(IncludeClassRegexTextBox.Text),
                ExcludeTitle = Normalize(ExcludeTitleRegexTextBox.Text),
                ExcludeProcess = Normalize(ExcludeProcessRegexTextBox.Text),
                ExcludeClass = Normalize(ExcludeClassRegexTextBox.Text),
                Sequence = EditableTextBoxSequence.Text
            };
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

        private void TestRuleButton_Click(object sender, RoutedEventArgs e)
        {
            var result = AutoTypeAuthoringValidator.TestDetectedWindow(
                BuildCustomField(),
                ProcessTitleTextBox.Text,
                ProcessNameTextBox.Text,
                ProcessClassNameTextBox.Text);

            SetValidationText(TestResultTextBlock, result.Message, result.CanTest ? (result.IsMatch ? SuccessBrush : ErrorBrush) : NeutralBrush);
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

        private void RefreshValidationState(AutoTypeCustomField customField)
        {
            ApplyRegexValidation(IncludeTitleRegexTextBox, IncludeTitleValidationTextBlock, AutoTypeAuthoringValidator.ValidateRegexPattern(customField.Title));
            ApplyRegexValidation(IncludeProcessRegexTextBox, IncludeProcessValidationTextBlock, AutoTypeAuthoringValidator.ValidateRegexPattern(customField.Process));
            ApplyRegexValidation(IncludeClassRegexTextBox, IncludeClassValidationTextBlock, AutoTypeAuthoringValidator.ValidateRegexPattern(customField.Class));
            ApplyRegexValidation(ExcludeTitleRegexTextBox, ExcludeTitleValidationTextBlock, AutoTypeAuthoringValidator.ValidateRegexPattern(customField.ExcludeTitle));
            ApplyRegexValidation(ExcludeProcessRegexTextBox, ExcludeProcessValidationTextBlock, AutoTypeAuthoringValidator.ValidateRegexPattern(customField.ExcludeProcess));
            ApplyRegexValidation(ExcludeClassRegexTextBox, ExcludeClassValidationTextBlock, AutoTypeAuthoringValidator.ValidateRegexPattern(customField.ExcludeClass));

            var sequenceResult = AutoTypeAuthoringValidator.ValidateSequence(customField.Sequence);
            ApplySequenceValidation(sequenceResult);

            var ruleError = AutoTypeAuthoringValidator.ValidateRule(customField);
            var hasRegexErrors = new[]
            {
                customField.Title,
                customField.Process,
                customField.Class,
                customField.ExcludeTitle,
                customField.ExcludeProcess,
                customField.ExcludeClass
            }.Any(value => AutoTypeAuthoringValidator.ValidateRegexPattern(value) is not null);

            var hasBlockingIssues = ruleError is not null || hasRegexErrors || !sequenceResult.IsValid;

            if (ruleError is not null)
            {
                SetValidationText(ValidationSummaryTextBlock, ruleError, ErrorBrush);
            }
            else if (hasBlockingIssues)
            {
                SetValidationText(ValidationSummaryTextBlock, "Fix the highlighted issues before copying this rule into Bitwarden.", ErrorBrush);
            }
            else if (sequenceResult.HasWarnings)
            {
                SetValidationText(ValidationSummaryTextBlock, "Rule is usable. Review the sequence warning before copying if the token was intentional.", WarningBrush);
            }
            else
            {
                SetValidationText(ValidationSummaryTextBlock, "Rule is ready to copy into Bitwarden.", SuccessBrush);
            }

            CopyButton4.IsEnabled = !hasBlockingIssues;
            TestRuleButton.IsEnabled = !hasBlockingIssues;

            if (string.IsNullOrWhiteSpace(ProcessTitleTextBox.Text)
                && string.IsNullOrWhiteSpace(ProcessNameTextBox.Text)
                && string.IsNullOrWhiteSpace(ProcessClassNameTextBox.Text))
            {
                SetValidationText(TestResultTextBlock, "Drag the finder over a window to test this rule.", NeutralBrush);
            }
            else
            {
                SetValidationText(TestResultTextBlock, "Use the test button to compare this rule against the detected target.", NeutralBrush);
            }
        }

        private static void ApplyRegexValidation(TextBox textBox, TextBlock textBlock, string? error)
        {
            if (string.IsNullOrWhiteSpace(textBox.Text))
            {
                textBlock.Text = string.Empty;
                textBox.ClearValue(BorderBrushProperty);
                textBox.ToolTip = null;
                return;
            }

            if (error is null)
            {
                textBlock.Text = string.Empty;
                textBox.ClearValue(BorderBrushProperty);
                textBox.ToolTip = null;
                return;
            }

            SetValidationText(textBlock, error, ErrorBrush);
            textBox.BorderBrush = ErrorBrush;
            textBox.ToolTip = error;
        }

        private void ApplySequenceValidation(AutoTypeSequenceValidationResult result)
        {
            if (!result.IsValid)
            {
                SetValidationText(SequenceValidationTextBlock, result.Summary, ErrorBrush);
                EditableTextBoxSequence.BorderBrush = ErrorBrush;
                EditableTextBoxSequence.ToolTip = result.Summary;
                return;
            }

            EditableTextBoxSequence.ToolTip = result.HasWarnings ? string.Join(Environment.NewLine, result.Warnings) : null;
            EditableTextBoxSequence.BorderBrush = result.HasWarnings ? WarningBrush : SuccessBrush;
            SetValidationText(SequenceValidationTextBlock, result.Summary, result.HasWarnings ? WarningBrush : SuccessBrush);
        }

        private static void SetValidationText(TextBlock textBlock, string message, Brush brush)
        {
            textBlock.Text = message;
            textBlock.Foreground = brush;
        }
    }
}