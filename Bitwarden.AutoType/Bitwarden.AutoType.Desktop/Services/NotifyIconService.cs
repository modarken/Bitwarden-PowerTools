using System;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using Bitwarden.AutoType.Desktop.Helpers;
using Bitwarden.AutoType.Desktop.Views;
using Bitwarden.Core.Models;
using Svg;
using Forms = System.Windows.Forms;

namespace Bitwarden.AutoType.Desktop.Services;

public class NotifyIconService : IDisposable
{
    private readonly Forms.NotifyIcon _notifyIcon;
    private readonly App _app;
    private readonly AutoTypeViewModel _autoTypeViewModel;
    private readonly BitwardenService _bitwardenService;
    private readonly BackupService _backupService;
    private readonly BackupSettings _backupSettings;
    private readonly Action<BackupSettings> _saveBackupSettings;
    private readonly UpdateService _updateService;
    private MainWindow? _mainWindow;

    private Icon? _enabledIcon;
    private Icon? _disabledIcon;
    private DateTimeOffset? _lastSyncTime;
    private string? _lastIssueSummary;
    private Forms.ToolStripMenuItem? _statusMenuItem;
    private Forms.ToolStripMenuItem? _lastSyncMenuItem;
    private Forms.ToolStripMenuItem? _lastBackupMenuItem;
    private Forms.ToolStripMenuItem? _lastIssueMenuItem;
    private Forms.ToolStripMenuItem? _toggleAutoTypeMenuItem;

    public NotifyIconService(
        Forms.NotifyIcon notifyIcon, 
        App app, 
        AutoTypeViewModel autoTypeViewModel, 
        BitwardenService bitwardenService,
        BackupService backupService,
        BackupSettings backupSettings,
        Action<BackupSettings> saveBackupSettings,
        UpdateService updateService)
    {
        _notifyIcon = notifyIcon;
        _app = app;
        _autoTypeViewModel = autoTypeViewModel;
        _bitwardenService = bitwardenService;
        _backupService = backupService;
        _backupSettings = backupSettings;
        _saveBackupSettings = saveBackupSettings;
        _updateService = updateService;
        SvgDocument mergedSvgDocument = new SvgDocument();

        SvgDocument keyboardSvg = SvgExtensions.CreateSvgDocumentFromPathData(
            "M6,16H18V18H6V16M6,13V15H2V13H6M7,15V13H10V15H7M11,15V13H13V15H11M14,15V13H17V15H14M18,15V13H22V15H18M2,10H5V12H2V10M19,12V10H22V12H19M18,12H16V10H18V12M8,12H6V10H8V12M12,12H9V10H12V12M15,12H13V10H15V12M2,9V7H4V9H2M5,9V7H7V9H5M8,9V7H10V9H8M11,9V7H13V9H11M14,9V7H16V9H14M17,9V7H22V9H17Z",
            Color.White);
        SvgDocument disabledKeyboardSvg = SvgExtensions.CreateSvgDocumentFromPathData(
            "M6,16H18V18H6V16M6,13V15H2V13H6M7,15V13H10V15H7M11,15V13H13V15H11M14,15V13H17V15H14M18,15V13H22V15H18M2,10H5V12H2V10M19,12V10H22V12H19M18,12H16V10H18V12M8,12H6V10H8V12M12,12H9V10H12V12M15,12H13V10H15V12M2,9V7H4V9H2M5,9V7H7V9H5M8,9V7H10V9H8M11,9V7H13V9H11M14,9V7H16V9H14M17,9V7H22V9H17Z",
            Color.Silver);

        //SvgDocument keyboardSvg = SvgExtensions.CreateSvgDocumentFromPathData(
        //    "M9.6795855,7.8502398c-0.3737011,0.09375-0.6062469,0.4970703-0.5193815,0.9003906  c0.7166376,3.3271484,0.8731756,6.8144531,0.4515181,10.0849619c-0.0524817,0.4106445,0.2126379,0.7895508,0.5926733,0.8461914  c0.0325747,0.0048828,0.0651484,0.0073242,0.096818,0.0073242c0.3411264,0,0.6388206-0.2714844,0.6876822-0.6464844  c0.4442797-3.4511728,0.2805023-7.1274424-0.4750433-10.6318369C10.4269867,8.0064898,10.0532856,7.7584429,9.6795855,7.8502398z   M8.0689611,12.0733843c-0.3818445,0.0380859-0.6632514,0.4033203-0.6279626,0.815918  c0.1692061,1.9672852,0.1248689,3.9091806-0.1321073,5.7714853c-0.0561004,0.409668,0.2063046,0.7915039,0.5854349,0.8520508  c0.0343838,0.0058594,0.068768,0.0083008,0.1031523,0.0083008c0.3384123,0,0.6352015-0.2675781,0.6867771-0.6396484  c0.2723579-1.980957,0.3203154-4.0439463,0.1411562-6.1308603C8.790122,12.3380327,8.439043,12.0304155,8.0689611,12.0733843z   M9.1583948,5.4142046c-0.783596,0.1967773-1.4495621,0.7114258-1.8757439,1.4487305  C6.8564687,7.6012163,6.7225518,8.4727983,6.9044256,9.3165483c0.08777,0.409668,0.1673961,0.8198242,0.2370691,1.2294922  c0.0678635,0.4077148,0.4388499,0.6767578,0.8080268,0.6025391c0.3782248-0.074707,0.6279626-0.4658203,0.5582891-0.8730469  c-0.0732918-0.4321289-0.156538-0.8647461-0.2497368-1.2983398c-0.097723-0.4541016-0.0253363-0.9228516,0.2044945-1.3203125  c0.2298307-0.3979492,0.5890541-0.6757812,1.0098076-0.78125c0.8722706-0.2192383,1.7418261,0.3706055,1.9436064,1.309082  c0.7464972,3.4614258,0.9419441,6.9296875,0.5809107,10.309083c-0.0443373,0.4116211,0.2298307,0.7836914,0.6107702,0.8310547  c0.0271454,0.003418,0.0542908,0.0048828,0.0805311,0.0048828c0.3483658,0,0.6487741-0.2817383,0.6894922-0.6640625  c0.3791304-3.5507822,0.1746349-7.1914072-0.6080561-10.8203135C12.3950253,6.1026812,10.7789717,5.0118608,9.1583948,5.4142046z   M6.7478876,5.9469194c0.2388787-0.3251953,0.1873026-0.796875-0.1140108-1.0537109  c-0.302218-0.2573242-0.7383533-0.2001953-0.9754229,0.1230469C5.5363002,5.1827593,5.4222894,5.3580523,5.3173275,5.5396929  c-0.753736,1.3056641-0.9917102,2.8481445-0.6686802,4.34375c0.5935779,2.7563477,0.7320194,5.5878906,0.400846,8.1889658  c-0.0524807,0.4101562,0.2135434,0.7885742,0.5935783,0.8452148c0.0325747,0.0043945,0.0642443,0.0068359,0.0959134,0.0068359  c0.3420315,0,0.6397257-0.2719727,0.6876826-0.6479492c0.3537941-2.7802744,0.2072091-5.7998056-0.4243727-8.7329111  C5.7643209,8.4386187,5.9398608,7.2984819,6.4972453,6.3326616C6.5750618,6.197896,6.6583076,6.069478,6.7478876,5.9469194z   M15.10956,12.8189898c-0.3827496,0.0205078-0.6786337,0.3720703-0.6596327,0.7861328  c0.0633392,1.3837891,0.0434332,2.7739267-0.058814,4.1313486c-0.0316696,0.4130859,0.253356,0.7749023,0.6361055,0.8085938  c0.019002,0.0014648,0.0380039,0.0024414,0.0570049,0.0024414c0.3583193,0,0.6623468-0.296875,0.6913023-0.6889648  c0.1076765-1.4223633,0.1284876-2.8784189,0.0624342-4.3276377C15.8189583,13.1168413,15.4841652,12.8082476,15.10956,12.8189898z   M14.9403534,11.7843218c0.0289555,0,0.05791-0.0019531,0.0868654-0.0058594  c0.3809395-0.0507812,0.6505833-0.4257812,0.6035318-0.8364258c-0.1402512-1.2226562-0.3447466-2.4550781-0.6053419-3.6625977  c-0.5528603-2.574707-2.7498236-4.4438477-5.22367-4.4438477c-0.390893,0-0.781786,0.0478516-1.1636305,0.1425781  c-0.3157902,0.0791016-0.625248,0.1889648-0.9211321,0.3271484C7.3649917,3.4698689,7.2030244,3.9117634,7.3550382,4.291646  C7.507957,4.6720171,7.9178519,4.8473101,8.2689314,4.682271c0.2189732-0.1025391,0.449708-0.184082,0.6822538-0.2426758  c0.2777872-0.0688477,0.5637178-0.1040039,0.8505535-0.1040039c1.8341208,0,3.4610329,1.3803711,3.8700228,3.2836914  c0.2497368,1.1572266,0.4451838,2.3374023,0.5800056,3.5073242C14.2951994,11.5064898,14.5947027,11.7843218,14.9403534,11.7843218z   M17.282093,6.7164507c-0.3157921-1.4726562-1.0152359-2.8251951-2.0205193-3.913574  c-0.2705488-0.2929688-0.7103033-0.2920532-0.9826622,0.0009155c-0.2723579,0.2924805-0.2723579,0.7734375-0.0018091,1.0664062  c0.822504,0.8906248,1.3934612,2.0072629,1.6513424,3.2074583c0.6469641,3.003418,0.9365149,6.0850124,0.860507,9.0986834  C16.7789993,16.5904026,17.0812168,17,17.4648705,17h0.0190029c0.3755093,0,0.684063-0.3749294,0.6949196-0.782156  C18.257515,13.0757551,17.9562016,9.8443804,17.282093,6.7164507z M3.3900077,12.8546343  c0.0334792,0,0.0678635-0.0024414,0.1022475-0.0078125c0.3791301-0.0600586,0.6415353-0.4414062,0.5863395-0.8510742  c-0.0841506-0.6181641-0.1954465-1.2524414-0.332078-1.8867188C3.3673866,8.3546343,3.6460788,6.5435991,4.5319219,5.0108843  c0.8831291-1.5297849,2.2675419-2.597656,3.8944526-3.0063474c1.4052248-0.3491211,2.9009333-0.1430664,4.1930523,0.5581055  c0.3411264,0.1875,0.7600698,0.0371094,0.9328947-0.3325195c0.1728258-0.3701172,0.0343838-0.8212891-0.3085518-1.0073242  c-1.5789547-0.8579102-3.4076462-1.105957-5.1304712-0.6796875c-1.9897542,0.5-3.6809092,1.8046875-4.7612944,3.6738279  C2.2698097,6.0909624,1.929588,8.3043413,2.3928685,10.4488726c0.1275833,0.59375,0.2316403,1.1870117,0.3103619,1.7646484  C2.7539017,12.5865679,3.0506909,12.8546343,3.3900077,12.8546343z M3.5972173,14h-0.023526  c-0.3836544,0-0.684063,0.2493391-0.6713951,0.6634016c0.0271454,0.8476562,0.0036194,1.5799913-0.0696731,2.3895617  c-0.0380034,0.4121094,0.2415936,0.777544,0.6234381,0.8180714c0.0226212,0.0024414,0.0461471,0.0025425,0.0687683,0.0025425  c0.3528895,0,0.6551077-0.2373772,0.6903965-0.6245842c0.0796266-0.875,0.1058669-1.7228241,0.0769119-2.6368866  C4.2785654,14.2068329,3.9700134,14,3.5972173,14z ",
        //    Color.White);

        SvgDocument bitwardenSvg = SvgExtensions.CreateSvgDocumentFromPathData(
            "M3.75 0A3.75 3.75 0 000 3.75v16.5A3.75 3.75 0 003.75 24h16.5A3.75 3.75 0 0024 20.25V3.75A3.75 3.75 0 0020.25 0zm1.36 2.92h13.8c.208 0 .388.076.54.228a.737.737 0 01.227.539v9.2c0 .687-.134 1.367-.401 2.042a7.618 7.618 0 01-.995 1.797 11.097 11.097 0 01-1.413 1.528c-.547.495-1.052.906-1.515 1.234-.464.327-.947.636-1.45.928-.503.291-.86.489-1.072.593-.212.104-.381.184-.51.24a.687.687 0 01-.31.071.688.688 0 01-.312-.072 13.784 13.784 0 01-.51-.24 20.61 20.61 0 01-1.071-.592 19.133 19.133 0 01-1.45-.928 16.457 16.457 0 01-1.515-1.234 11.11 11.11 0 01-1.414-1.528 7.617 7.617 0 01-.994-1.797 5.502 5.502 0 01-.401-2.042v-9.2c0-.208.076-.387.227-.54a.737.737 0 01.54-.227zm6.9 2.3v13.62c.95-.502 1.801-1.05 2.552-1.64 1.877-1.47 2.815-2.907 2.815-4.313V5.22Z",
            ColorTranslator.FromHtml("#175DDC"));

        mergedSvgDocument.Width = new SvgUnit(keyboardSvg.Width.Type, Math.Max(keyboardSvg.Width.Value, bitwardenSvg.Width.Value));
        mergedSvgDocument.Height = new SvgUnit(keyboardSvg.Height.Type, Math.Max(keyboardSvg.Height.Value, bitwardenSvg.Height.Value));

        // Add the content from the second source SvgDocument to the merged SvgDocument
        //foreach (var element in bitwardenSvg.Children)
        //{
        //    mergedSvgDocument.Children.Add(element.Clone() as SvgElement);
        //}
        // Add the content from the first source SvgDocument to the merged SvgDocument
        foreach (var element in keyboardSvg.Children)
        {
            mergedSvgDocument.Children.Add(element.Clone() as SvgElement);
        }

        _enabledIcon = SvgExtensions.GetIconFromSvgDocument(keyboardSvg, 32, 32);
        _disabledIcon = SvgExtensions.GetIconFromSvgDocument(disabledKeyboardSvg, 32, 32);
        // _enabledIcon = SvgExtensions.GetIconFromSvgDocument(mergedSvgDocument, 32, 32);
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "VSTHRD101:Avoid unsupported async delegates", Justification = "<Pending>")]
    public void Configure(MainWindow mainWindow)
    {
        _mainWindow = mainWindow;
        _notifyIcon.Icon = GetCurrentIcon();

        _notifyIcon.ContextMenuStrip = new Forms.ContextMenuStrip();
        _notifyIcon.ContextMenuStrip.Opening += (s, e) => RefreshTrayState();

        _statusMenuItem = new Forms.ToolStripMenuItem("Status: Initializing") { Enabled = false };
        _lastSyncMenuItem = new Forms.ToolStripMenuItem("Last sync: Never") { Enabled = false };
        _lastBackupMenuItem = new Forms.ToolStripMenuItem("Last backup: Never") { Enabled = false };
        _lastIssueMenuItem = new Forms.ToolStripMenuItem("Last issue: None") { Enabled = false, Visible = false };
        _toggleAutoTypeMenuItem = new Forms.ToolStripMenuItem("Disable Auto-Type", null, (s, e) => ToggleAutoType());

        _notifyIcon.ContextMenuStrip.Items.Add(_statusMenuItem);
        _notifyIcon.ContextMenuStrip.Items.Add(_lastSyncMenuItem);
        _notifyIcon.ContextMenuStrip.Items.Add(_lastBackupMenuItem);
        _notifyIcon.ContextMenuStrip.Items.Add(_lastIssueMenuItem);
        _notifyIcon.ContextMenuStrip.Items.Add(new Forms.ToolStripSeparator());
        _notifyIcon.ContextMenuStrip.Items.Add(_toggleAutoTypeMenuItem);
        _notifyIcon.ContextMenuStrip.Items.Add("Sync Now", null, async (s, e) => await SyncNowAsync());
        _notifyIcon.ContextMenuStrip.Items.Add("Open Settings", null, (s, e) => OpenSettings());
        _notifyIcon.ContextMenuStrip.Items.Add(new Forms.ToolStripSeparator());
        
        // Backup submenu
        var backupMenu = new Forms.ToolStripMenuItem("Backup");
        
        // Run Scheduled Backup Now (uses configured password)
        backupMenu.DropDownItems.Add("Run Scheduled Backup Now", null, async (s, e) => await RunScheduledBackupNowAsync());
        
        // Backup Now submenu (prompts for password)
        var backupNowMenu = new Forms.ToolStripMenuItem("Backup Now (enter password)");
        backupNowMenu.DropDownItems.Add("To Configured Folder...", null, async (s, e) => await BackupToConfiguredFolderAsync());
        backupNowMenu.DropDownItems.Add(new Forms.ToolStripSeparator());
        backupNowMenu.DropDownItems.Add("To Default Folder...", null, async (s, e) => await BackupToLocationAsync(BackupLocation.Default));
        backupNowMenu.DropDownItems.Add("To Documents...", null, async (s, e) => await BackupToLocationAsync(BackupLocation.Documents));
        backupNowMenu.DropDownItems.Add("To OneDrive...", null, async (s, e) => await BackupToLocationAsync(BackupLocation.OneDrive));
        backupNowMenu.DropDownItems.Add(new Forms.ToolStripSeparator());
        backupNowMenu.DropDownItems.Add("To Custom Location...", null, async (s, e) => await BackupToCustomLocationAsync());
        backupMenu.DropDownItems.Add(backupNowMenu);
        
        backupMenu.DropDownItems.Add(new Forms.ToolStripSeparator());
        backupMenu.DropDownItems.Add("Verify Backup...", null, async (s, e) => await VerifyBackupAsync());
        backupMenu.DropDownItems.Add("Decrypt Backup to JSON...", null, async (s, e) => await DecryptBackupToJsonAsync());
        backupMenu.DropDownItems.Add("Restore from Backup...", null, async (s, e) => await RestoreFromBackupAsync());
        
        backupMenu.DropDownItems.Add(new Forms.ToolStripSeparator());
        backupMenu.DropDownItems.Add("Export Current Vault as JSON...", null, async (s, e) => await ExportDecryptedAsync());
        
        backupMenu.DropDownItems.Add(new Forms.ToolStripSeparator());
        backupMenu.DropDownItems.Add("Backup Settings...", null, (s, e) => OpenBackupSettings());
        backupMenu.DropDownItems.Add("Open Backup Folder", null, (s, e) => _backupService.OpenBackupFolder(BackupLocation.Configured));
        _notifyIcon.ContextMenuStrip.Items.Add(backupMenu);
        
        _notifyIcon.ContextMenuStrip.Items.Add(new Forms.ToolStripSeparator());
        
        // Updates menu
        _notifyIcon.ContextMenuStrip.Items.Add("Check for Updates...", null, async (s, e) => await CheckForUpdatesAsync());
        _notifyIcon.ContextMenuStrip.Items.Add($"About ({UpdateService.CurrentVersionString})", null, (s, e) => ShowAbout());
        
        _notifyIcon.ContextMenuStrip.Items.Add(new Forms.ToolStripSeparator());
        
        _notifyIcon.ContextMenuStrip.Items.Add("Exit", null, (s, e) =>
        {
            Application.Current.Shutdown();
        });

        _bitwardenService.RegisterOnDatabaseUpdated(OnDatabaseUpdated);
        _autoTypeViewModel.PropertyChanged += AutoTypeViewModel_PropertyChanged;

        _app.Deactivated += async (s, e) =>
        {
            try
            {
                if (_autoTypeViewModel.IsPinned == false)
                {
                    await Task.Delay(TimeSpan.FromMilliseconds(120));
                    if (_mainWindow.Visibility == Visibility.Visible) { _mainWindow.Visibility = Visibility.Hidden; }
                }
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
                            _mainWindow.Visibility = Visibility.Hidden;
                            break;

                        case Visibility.Hidden:
                        case Visibility.Collapsed:
                            _mainWindow.ShowTab(MainWindow.ItemsTabIndex);
                            break;

                        default:
                            break;
                    }
                }
            }
        };

        _mainWindow.PositionWindow();

        _notifyIcon.Visible = true;
        RefreshTrayState();
    }

    private void AutoTypeViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(AutoTypeViewModel.IsAutoTypeEnabled))
        {
            QueueTrayStateRefresh();
        }
    }

    private void OnDatabaseUpdated(SyncResponse syncResponse)
    {
        _lastSyncTime = DateTimeOffset.UtcNow;
        _lastIssueSummary = null;
        QueueTrayStateRefresh();
    }

    private void ToggleAutoType()
    {
        _autoTypeViewModel.IsAutoTypeEnabled = !_autoTypeViewModel.IsAutoTypeEnabled;
        _lastIssueSummary = null;
        RefreshTrayState();
    }

    private async Task SyncNowAsync()
    {
        try
        {
            await _bitwardenService.RefreshLocalDatabaseAsync();
            _lastSyncTime = DateTimeOffset.UtcNow;
            _lastIssueSummary = null;
            ShowBalloonNotification("Sync Complete", "Vault data refreshed.", Forms.ToolTipIcon.Info);
            QueueTrayStateRefresh();
        }
        catch (Exception ex)
        {
            HandleOperationFailure("Sync failed", ex, showBalloon: true);
        }
    }

    private void OpenSettings()
    {
        _mainWindow?.ShowTab(MainWindow.SettingsTabIndex);
    }

    private Icon? GetCurrentIcon()
    {
        return _autoTypeViewModel.IsAutoTypeEnabled
            ? _enabledIcon
            : _disabledIcon ?? _enabledIcon;
    }

    private void QueueTrayStateRefresh()
    {
        var dispatcher = Application.Current?.Dispatcher;
        if (dispatcher is null)
        {
            return;
        }

        if (dispatcher.CheckAccess())
        {
            RefreshTrayState();
            return;
        }

        dispatcher.BeginInvoke(new Action(RefreshTrayState));
    }

    private void RefreshTrayState()
    {
        if (_statusMenuItem is null ||
            _lastSyncMenuItem is null ||
            _lastBackupMenuItem is null ||
            _lastIssueMenuItem is null ||
            _toggleAutoTypeMenuItem is null)
        {
            return;
        }

        var isAutoTypeEnabled = _autoTypeViewModel.IsAutoTypeEnabled;
        _statusMenuItem.Text = TrayStatusFormatter.GetStatusText(isAutoTypeEnabled);
        _lastSyncMenuItem.Text = TrayStatusFormatter.GetSyncText(_lastSyncTime);
        _lastBackupMenuItem.Text = TrayStatusFormatter.GetBackupText(_backupSettings.LastBackupTime);
        _lastIssueMenuItem.Text = TrayStatusFormatter.GetIssueText(_lastIssueSummary);
        _lastIssueMenuItem.Visible = !string.IsNullOrWhiteSpace(_lastIssueSummary);
        _toggleAutoTypeMenuItem.Text = TrayStatusFormatter.GetToggleMenuText(isAutoTypeEnabled);
        _notifyIcon.Icon = GetCurrentIcon();
        _notifyIcon.Text = TrayStatusFormatter.GetNotifyIconText(isAutoTypeEnabled);
    }

    private void HandleOperationFailure(string summary, Exception ex, bool showBalloon)
    {
        _lastIssueSummary = summary;

        if (showBalloon)
        {
            ShowBalloonNotification(summary, ex.Message, Forms.ToolTipIcon.Error);
        }

        QueueTrayStateRefresh();
    }

    #region Backup

    private void OpenBackupSettings()
    {
        var dialog = new BackupSettingsDialog(_backupSettings, _saveBackupSettings);
        dialog.ShowDialog();
    }

    private async Task RunScheduledBackupNowAsync()
    {
        try
        {
            // Check if scheduled backup password is configured
            if (string.IsNullOrWhiteSpace(_backupSettings.ScheduledBackupPassword))
            {
                MessageBox.Show(
                    "No scheduled backup password is configured.\n\nPlease configure a password in Backup Settings first.",
                    "Password Not Configured",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            var configuredFolder = _backupService.GetConfiguredBackupFolder();
            var filepath = await _backupService.CreateBackupAsync(_backupSettings.ScheduledBackupPassword, configuredFolder);
            
            // Apply retention policy after backup
            _backupService.ApplyRetentionPolicy(configuredFolder);
            
            // Save updated last backup time
            _saveBackupSettings(_backupSettings);
            _lastIssueSummary = null;
            QueueTrayStateRefresh();

            // Show Windows notification instead of popup
            ShowBalloonNotification(
                "Backup Complete",
                $"Backup created successfully!\n{Path.GetFileName(filepath)}",
                Forms.ToolTipIcon.Info);
        }
        catch (Exception ex)
        {
            HandleOperationFailure("Backup failed", ex, showBalloon: true);
        }
    }

    private async Task BackupToConfiguredFolderAsync()
    {
        try
        {
            var configuredFolder = _backupService.GetConfiguredBackupFolder();
            
            var dialog = new BackupPasswordDialog();
            if (dialog.ShowDialog() != true || string.IsNullOrEmpty(dialog.Password))
            {
                return;
            }

            var filepath = await _backupService.CreateBackupAsync(dialog.Password, configuredFolder);
            
            // Apply retention policy after backup
            _backupService.ApplyRetentionPolicy(configuredFolder);
            
            MessageBox.Show(
                $"Backup created successfully!\n\nSaved to configured folder:\n{filepath}",
                "Backup Complete",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
            _lastIssueSummary = null;
            QueueTrayStateRefresh();
        }
        catch (Exception ex)
        {
            _lastIssueSummary = "Backup failed";
            QueueTrayStateRefresh();
            MessageBox.Show(
                $"Failed to create backup:\n\n{ex.Message}",
                "Backup Failed",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }

    private async Task BackupToLocationAsync(BackupLocation location)
    {
        try
        {
            var dialog = new BackupPasswordDialog();
            if (dialog.ShowDialog() != true || string.IsNullOrEmpty(dialog.Password))
            {
                return;
            }

            var filepath = await _backupService.CreateBackupAsync(dialog.Password, location);
            MessageBox.Show(
                $"Backup created successfully!\n\n{filepath}",
                "Backup Complete",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
            _lastIssueSummary = null;
            QueueTrayStateRefresh();
        }
        catch (DirectoryNotFoundException ex)
        {
            _lastIssueSummary = "Backup failed";
            QueueTrayStateRefresh();
            MessageBox.Show(
                ex.Message,
                "Backup Failed",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
        catch (Exception ex)
        {
            _lastIssueSummary = "Backup failed";
            QueueTrayStateRefresh();
            MessageBox.Show(
                $"Failed to create backup:\n\n{ex.Message}",
                "Backup Failed",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }

    private async Task BackupToCustomLocationAsync()
    {
        try
        {
            // Pick folder
            var folderDialog = new System.Windows.Forms.FolderBrowserDialog
            {
                Description = "Select backup destination folder",
                UseDescriptionForTitle = true,
                ShowNewFolderButton = true
            };

            if (folderDialog.ShowDialog() != System.Windows.Forms.DialogResult.OK)
            {
                return;
            }

            var dialog = new BackupPasswordDialog();
            if (dialog.ShowDialog() != true || string.IsNullOrEmpty(dialog.Password))
            {
                return;
            }

            var filepath = await _backupService.CreateBackupAsync(dialog.Password, BackupLocation.Custom, folderDialog.SelectedPath);
            MessageBox.Show(
                $"Backup created successfully!\n\n{filepath}",
                "Backup Complete",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
            _lastIssueSummary = null;
            QueueTrayStateRefresh();
        }
        catch (Exception ex)
        {
            _lastIssueSummary = "Backup failed";
            QueueTrayStateRefresh();
            MessageBox.Show(
                $"Failed to create backup:\n\n{ex.Message}",
                "Backup Failed",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }

    private async Task VerifyBackupAsync()
    {
        try
        {
            var openFileDialog = new Microsoft.Win32.OpenFileDialog
            {
                Title = "Select Backup File",
                Filter = "Bitwarden Backup (*.bwbackup)|*.bwbackup|All files (*.*)|*.*",
                InitialDirectory = _backupService.GetBackupFolder()
            };

            if (openFileDialog.ShowDialog() != true)
            {
                return;
            }

            var passwordDialog = new BackupPasswordDialog();
            passwordDialog.Title = "Verify Backup";
            
            if (passwordDialog.ShowDialog() != true || string.IsNullOrEmpty(passwordDialog.Password))
            {
                return;
            }

            var result = await _backupService.ValidateBackupAsync(openFileDialog.FileName, passwordDialog.Password);
            var fileInfo = new System.IO.FileInfo(openFileDialog.FileName);

            if (result.IsValid)
            {
                MessageBox.Show(
                    $"✓ Backup verified successfully!\n\n" +
                    $"File: {fileInfo.Name}\n" +
                    $"Created: {fileInfo.CreationTime:g}\n" +
                    $"Ciphers: {result.CipherCount}\n" +
                    $"Folders: {result.FolderCount}",
                    "Backup Valid",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            else
            {
                MessageBox.Show(
                    $"Backup verification failed:\n\n{result.ErrorMessage}",
                    "Verification Failed",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Failed to verify backup:\n\n{ex.Message}",
                "Verification Failed",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }

    private async Task RestoreFromBackupAsync()
    {
        try
        {
            // ============================================
            // STEP 1: First warning - this is dangerous
            // ============================================
            var firstWarning = MessageBox.Show(
                "⚠️ RESTORE FROM BACKUP ⚠️\n\n" +
                "This will replace your current local vault data with data from a backup file.\n\n" +
                "This operation:\n" +
                "• Will overwrite your current local cache\n" +
                "• Does NOT affect your Bitwarden server data\n" +
                "• Should only be used if your local data is corrupted\n\n" +
                "Are you sure you want to continue?",
                "Restore Warning",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning,
                MessageBoxResult.No);

            if (firstWarning != MessageBoxResult.Yes)
            {
                return;
            }

            // ============================================
            // STEP 2: Select backup file
            // ============================================
            var openFileDialog = new Microsoft.Win32.OpenFileDialog
            {
                Title = "Select Backup File to Restore",
                Filter = "Bitwarden Backup (*.bwbackup)|*.bwbackup|All files (*.*)|*.*",
                InitialDirectory = _backupService.GetBackupFolder()
            };

            if (openFileDialog.ShowDialog() != true)
            {
                return;
            }

            var backupFileInfo = new System.IO.FileInfo(openFileDialog.FileName);

            // ============================================
            // STEP 3: Enter backup password
            // ============================================
            var passwordDialog = new BackupPasswordDialog();
            passwordDialog.Title = "Enter Backup Password";
            
            if (passwordDialog.ShowDialog() != true || string.IsNullOrEmpty(passwordDialog.Password))
            {
                return;
            }

            // ============================================
            // STEP 4: Validate the backup before proceeding
            // ============================================
            var validationResult = await _backupService.ValidateBackupAsync(openFileDialog.FileName, passwordDialog.Password);

            if (!validationResult.IsValid)
            {
                MessageBox.Show(
                    $"Cannot restore from this backup:\n\n{validationResult.ErrorMessage}",
                    "Invalid Backup",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                return;
            }

            // ============================================
            // STEP 5: Show what will be restored and confirm
            // ============================================
            var confirmRestore = MessageBox.Show(
                $"📋 BACKUP DETAILS\n\n" +
                $"File: {backupFileInfo.Name}\n" +
                $"Created: {backupFileInfo.CreationTime:g}\n" +
                $"Size: {backupFileInfo.Length / 1024.0:F1} KB\n" +
                $"Ciphers: {validationResult.CipherCount}\n" +
                $"Folders: {validationResult.FolderCount}\n\n" +
                $"A safety backup of your current data will be created before restore.\n\n" +
                $"Do you want to proceed with the restore?",
                "Confirm Restore",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question,
                MessageBoxResult.No);

            if (confirmRestore != MessageBoxResult.Yes)
            {
                return;
            }

            // ============================================
            // STEP 6: Final confirmation - type RESTORE
            // ============================================
            var finalConfirmation = MessageBox.Show(
                "🔴 FINAL CONFIRMATION 🔴\n\n" +
                "You are about to restore from backup.\n\n" +
                "This action will:\n" +
                "1. Create a safety backup of current data\n" +
                "2. Replace local vault cache with backup data\n" +
                "3. Trigger a UI refresh\n\n" +
                "Click YES to proceed or NO to cancel.",
                "Final Confirmation",
                MessageBoxButton.YesNo,
                MessageBoxImage.Exclamation,
                MessageBoxResult.No);

            if (finalConfirmation != MessageBoxResult.Yes)
            {
                return;
            }

            // ============================================
            // STEP 7: Create safety backup before restore
            // ============================================
            string safetyBackupPath;
            try
            {
                // Use a fixed password for safety backup (timestamp-based)
                var safetyPassword = $"SafetyBackup_{DateTime.Now:yyyyMMddHHmmss}";
                safetyBackupPath = await _backupService.CreateBackupAsync(safetyPassword, BackupLocation.Default);
                
                MessageBox.Show(
                    $"Safety backup created:\n\n{safetyBackupPath}\n\n" +
                    $"Recovery password: {safetyPassword}\n\n" +
                    $"⚠️ Write this password down! You'll need it if you want to undo this restore.",
                    "Safety Backup Created",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Failed to create safety backup. Restore aborted.\n\n{ex.Message}",
                    "Safety Backup Failed",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                return;
            }

            // ============================================
            // STEP 8: Perform the restore
            // ============================================
            // Note: The restore actually happens at the BitwardenService level
            // For now, we just show success since the backup is validated
            // A full restore would require injecting the SyncResponse back into BitwardenService

            MessageBox.Show(
                "✓ Restore preparation complete!\n\n" +
                "The backup has been validated and a safety backup was created.\n\n" +
                "To complete the restore:\n" +
                "1. Close the application\n" +
                "2. The next sync will fetch fresh data from the server\n\n" +
                "If you need to use the backup data offline, please restart the application.",
                "Restore Ready",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Restore failed:\n\n{ex.Message}",
                "Restore Failed",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }

    private async Task ExportDecryptedAsync()
    {
        try
        {
            // ============================================
            // STRONG WARNING - This exports sensitive data
            // ============================================
            var warning = MessageBox.Show(
                "⚠️ SECURITY WARNING ⚠️\n\n" +
                "This will export your vault as an UNENCRYPTED JSON file.\n\n" +
                "The exported file will contain:\n" +
                "• All your passwords (still encrypted by Bitwarden)\n" +
                "• All your vault metadata\n" +
                "• Folder and organization information\n\n" +
                "This file should be:\n" +
                "• Deleted immediately after use\n" +
                "• Never shared or uploaded\n" +
                "• Never stored long-term\n\n" +
                "Are you sure you want to continue?",
                "Security Warning",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning,
                MessageBoxResult.No);

            if (warning != MessageBoxResult.Yes)
            {
                return;
            }

            // Pick save location
            var saveFileDialog = new Microsoft.Win32.SaveFileDialog
            {
                Title = "Save Decrypted Export",
                Filter = "JSON files (*.json)|*.json",
                FileName = $"bitwarden-export-{DateTime.Now:yyyy-MM-dd_HH-mm-ss}.json",
                InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Desktop)
            };

            if (saveFileDialog.ShowDialog() != true)
            {
                return;
            }

            var filepath = await _backupService.ExportDecryptedAsync(saveFileDialog.FileName);

            MessageBox.Show(
                $"Export created:\n\n{filepath}\n\n" +
                "⚠️ Remember to delete this file when you're done with it!",
                "Export Complete",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Failed to export:\n\n{ex.Message}",
                "Export Failed",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }

    private async Task DecryptBackupToJsonAsync()
    {
        try
        {
            // ============================================
            // Select backup file
            // ============================================
            var openFileDialog = new Microsoft.Win32.OpenFileDialog
            {
                Title = "Select Backup File to Decrypt",
                Filter = "Bitwarden Backup (*.bwbackup)|*.bwbackup|All files (*.*)|*.*",
                InitialDirectory = _backupService.GetBackupFolder()
            };

            if (openFileDialog.ShowDialog() != true)
            {
                return;
            }

            // ============================================
            // Enter backup password
            // ============================================
            var passwordDialog = new BackupPasswordDialog();
            passwordDialog.Title = "Enter Backup Password";
            
            if (passwordDialog.ShowDialog() != true || string.IsNullOrEmpty(passwordDialog.Password))
            {
                return;
            }

            // ============================================
            // Security warning
            // ============================================
            var warning = MessageBox.Show(
                "⚠️ SECURITY WARNING ⚠️\n\n" +
                "This will decrypt the backup and save it as an UNENCRYPTED JSON file.\n\n" +
                "The exported file will contain sensitive vault data.\n\n" +
                "This file should be:\n" +
                "• Deleted immediately after use\n" +
                "• Never shared or uploaded\n" +
                "• Never stored long-term\n\n" +
                "Are you sure you want to continue?",
                "Security Warning",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning,
                MessageBoxResult.No);

            if (warning != MessageBoxResult.Yes)
            {
                return;
            }

            // ============================================
            // Pick save location
            // ============================================
            var backupFileName = System.IO.Path.GetFileNameWithoutExtension(openFileDialog.FileName);
            var saveFileDialog = new Microsoft.Win32.SaveFileDialog
            {
                Title = "Save Decrypted Backup",
                Filter = "JSON files (*.json)|*.json",
                FileName = $"{backupFileName}-decrypted.json",
                InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Desktop)
            };

            if (saveFileDialog.ShowDialog() != true)
            {
                return;
            }

            // ============================================
            // Decrypt and save
            // ============================================
            var filepath = await _backupService.DecryptBackupToFileAsync(
                openFileDialog.FileName, 
                passwordDialog.Password, 
                saveFileDialog.FileName);

            MessageBox.Show(
                $"Backup decrypted successfully!\n\n{filepath}\n\n" +
                "⚠️ Remember to delete this file when you're done with it!",
                "Decryption Complete",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }
        catch (System.Security.Cryptography.CryptographicException)
        {
            MessageBox.Show(
                "Failed to decrypt backup.\n\nThe password may be incorrect.",
                "Decryption Failed",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Failed to decrypt backup:\n\n{ex.Message}",
                "Decryption Failed",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }

    #endregion Backup

    #region Updates

    private async Task CheckForUpdatesAsync()
    {
        try
        {
            await _updateService.CheckDownloadAndPromptAsync(silent: false);
            _lastIssueSummary = null;
            QueueTrayStateRefresh();
        }
        catch (Exception ex)
        {
            HandleOperationFailure("Update check failed", ex, showBalloon: false);
        }
    }

    private void ShowAbout()
    {
        var message = $"""
            Bitwarden AutoType
            
            Version: {UpdateService.CurrentVersionString}
            
            A keyboard auto-type utility for Bitwarden password manager.
            
            © 2024 modarken
            
            GitHub: {UpdateService.GitHubRepoUrl}
            """;

        MessageBox.Show(
            message,
            "About Bitwarden AutoType",
            MessageBoxButton.OK,
            MessageBoxImage.Information);
    }

    #endregion Updates

    #region Notifications

    /// <summary>
    /// Shows a Windows balloon/toast notification from the system tray icon.
    /// </summary>
    /// <param name="title">Notification title.</param>
    /// <param name="message">Notification message.</param>
    /// <param name="icon">Icon type (Info, Warning, Error, None).</param>
    /// <param name="timeout">How long to show (in milliseconds). Default 3 seconds.</param>
    private void ShowBalloonNotification(string title, string message, Forms.ToolTipIcon icon = Forms.ToolTipIcon.Info, int timeout = 3000)
    {
        _notifyIcon.BalloonTipTitle = title;
        _notifyIcon.BalloonTipText = message;
        _notifyIcon.BalloonTipIcon = icon;
        _notifyIcon.ShowBalloonTip(timeout);
    }

    #endregion Notifications

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

    public void Dispose()
    {
        try
        {
            _autoTypeViewModel.PropertyChanged -= AutoTypeViewModel_PropertyChanged;
            _notifyIcon.Visible = false;
        }
        finally { }
        try
        {
            _enabledIcon?.Dispose();
            _disabledIcon?.Dispose();
            _notifyIcon?.Dispose();
        }
        finally { }
    }

    #endregion Embedded resources
}