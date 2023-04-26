using System;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using Bitwarden.AutoType.Desktop.Helpers;
using Svg;
using Forms = System.Windows.Forms;

namespace Bitwarden.AutoType.Desktop.Services;

public class NotifyIconService
{
    private readonly Forms.NotifyIcon _notifyIcon;
    private readonly App _app;

    //private IMainWindowViewModel? _mainWindowViewModel;
    private MainWindow? _mainWindow;

    //private Icon? _grayIcon;
    //private Icon? _yellowIcon;
    private Icon? _bitwardenIcon;

    public NotifyIconService(Forms.NotifyIcon notifyIcon, App app)
    {
        _notifyIcon = notifyIcon;
        _app = app;

        SvgDocument svgDocument = SvgExtensions.CreateSvgDocumentFromPathData(
            "M6,16H18V18H6V16M6,13V15H2V13H6M7,15V13H10V15H7M11,15V13H13V15H11M14,15V13H17V15H14M18,15V13H22V15H18M2,10H5V12H2V10M19,12V10H22V12H19M18,12H16V10H18V12M8,12H6V10H8V12M12,12H9V10H12V12M15,12H13V10H15V12M2,9V7H4V9H2M5,9V7H7V9H5M8,9V7H10V9H8M11,9V7H13V9H11M14,9V7H16V9H14M17,9V7H22V9H17Z",
             Color.WhiteSmoke);

        _bitwardenIcon = SvgExtensions.GetIconFromSvgDocument(svgDocument, 32, 32);

        //_bitwardenIcon = GetEmbededResourceIcon($"{nameof(Bitwarden)}.{nameof(AutoType)}.{nameof(Desktop)}.Resources.Bitwarden.AutoType.ico");
        //_grayIcon = GetEmbededResourceIcon("GhostMouse.Desktop.Resources.Material-Rodent.Gray.ico");
        //_yellowIcon = GetEmbededResourceIcon("GhostMouse.Desktop.Resources.Material-Rodent.Yellow.ico");
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "VSTHRD101:Avoid unsupported async delegates", Justification = "<Pending>")]
    //public void Configure(MainWindow mainWindow, IMainWindowViewModel mainWindowViewModel)
    public void Configure(MainWindow mainWindow)
    {
        _mainWindow = mainWindow;
        _notifyIcon.Icon = _bitwardenIcon;

        // _mainWindowViewModel = mainWindowViewModel;
        // _notifyIcon.Icon = _grayIcon// ;

        // // _mainWindowViewModel.OnIsMouseStartedChangedEvent += (s, e) =>
        // // {
        // //     if (e)
        // //     {
        // //         _notifyIcon.Icon = _yellowIcon;
        // //     }
        // //     else
        // //     {
        //         _notifyIcon.Icon = _grayIcon;
        //     }
        // };

        _notifyIcon.ContextMenuStrip = new Forms.ContextMenuStrip();
        _notifyIcon.ContextMenuStrip.Items.Add("Exit", null, (s, e) => { Application.Current.Shutdown(); });

        _app.Deactivated += async (s, e) =>
        {
            try
            {
                await Task.Delay(TimeSpan.FromMilliseconds(120));
                if (_mainWindow.Visibility == Visibility.Visible) { _mainWindow.Visibility = Visibility.Hidden; }
            }
            catch
            {
            }
        };

        _notifyIcon.Click += (s, e) =>
        {
            if (e is Forms.MouseEventArgs mouseEvent)
            {
                if (mouseEvent.Button == Forms.MouseButtons.Left)
                {
                    switch (_mainWindow.Visibility)
                    {
                        case Visibility.Visible:
                            //notifyIconLogger.LogTrace($"notifyIcon.Click: {Visibility.Collapsed}");
                            _mainWindow.Visibility = Visibility.Hidden;
                            break;

                        case Visibility.Hidden:
                        case Visibility.Collapsed:
                            //notifyIconLogger.LogTrace($"notifyIcon.Click: {Visibility.Visible}");
                            _mainWindow.Visibility = Visibility.Visible;
                            _mainWindow.Activate();
                            break;

                        default:
                            break;
                    }
                }
            }
        };

        _mainWindow.PositionWindow();

        _notifyIcon.Visible = true;
    }

    #region Embedded resources

    public static Icon? GetEmbededResourceIcon(string resourceName)
    {
        var assembly = Assembly.GetExecutingAssembly();
        var stream = assembly.GetManifestResourceStream(resourceName);
        if (stream == null) return null;
        using StreamReader reader = new StreamReader(stream);
        return new System.Drawing.Icon(stream);
    }

    public static Bitmap? GetEmbededResourceBitmap(string resourceName)
    {
        var assembly = Assembly.GetExecutingAssembly();
        var stream = assembly.GetManifestResourceStream(resourceName);
        if (stream == null) return default;
        using StreamReader reader = new StreamReader(stream);
        return new Bitmap(stream);
    }

    public static Stream GetEmbeddedResource(string resourceName)
    {
        Assembly assembly = Assembly.GetExecutingAssembly();
        Stream? resourceStream = assembly.GetManifestResourceStream(resourceName);

        if (resourceStream == null)
        {
            throw new ArgumentException($"Resource '{resourceName}' not found.");
        }

        return resourceStream;
    }

    #endregion Embedded resources
}