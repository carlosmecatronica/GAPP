﻿using GAPPSF.Commands;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Automation.Peers;
using System.Windows.Automation.Provider;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using System.Xml;

namespace GAPPSF
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        internal delegate void ProcessArgDelegate(String arg);
        internal static ProcessArgDelegate ProcessArg;

        private Core.Storage.Database _currentConnectedDatabase = null;

        private string _popUpText = "";
        public string PopUpText 
        {
            get { return _popUpText; }
            set { SetProperty(ref _popUpText, value); } 
        }

        private Visibility _dutchMenusVisibility = Visibility.Visible;
        public Visibility DutchMenusVisibility
        {
            get { return _dutchMenusVisibility; }
            set { SetProperty(ref _dutchMenusVisibility, value); }
        }

        public Visibility DebugMenusVisibility
        {
            get 
            {
#if DEBUG
                return Visibility.Visible;
#else
                return Visibility.Collapsed;
#endif
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public MainWindow()
        {
            ProcessArg = delegate(String arg)
            {
                //process arguments
            };

            this.Initialized += delegate(object sender, EventArgs e)
            {
                //process arguments
                //ArgsRun.Text = (String)Application.Current.Resources[WpfSingleInstance.StartArgKey];
                try
                {
                    Application.Current.Resources.Remove(WpfSingleInstance.StartArgKey);
                }
                catch
                {

                }
            };

            CurrentConnectedDatabase = Core.ApplicationData.Instance.ActiveDatabase;

            this.DataContext = this;

            GridLength rpgl = Core.Settings.Default.MainWindowRightPanelWidth;
            GridLength blgl = Core.Settings.Default.MainWindowBottomLeftPanelWidth;
            GridLength bpgl = Core.Settings.Default.MainWindowBottomPanelHeight;

            Core.ApplicationData.Instance.MainWindow = this;
            Dialogs.ProgessWindow prog = Dialogs.ProgessWindow.Instance;
            InitializeComponent();

            SetFeatureControl(leftPanelContent, Core.Settings.Default.MainWindowLeftPanelFeature, "GAPPSF.UIControls.ApplicationDataInfo");
            SetFeatureControl(topPanelContent, Core.Settings.Default.MainWindowTopPanelFeature, "GAPPSF.UIControls.CacheList");
            SetFeatureControl(bottomLeftPanelContent, Core.Settings.Default.MainWindowBottomLeftPanelFeature, "GAPPSF.UIControls.GeocacheViewer");
            SetFeatureControl(bottomRightPanelContent, Core.Settings.Default.MainWindowBottomRightPanelFeature, "");
            SetFeatureControl(rightPanelContent, Core.Settings.Default.MainWindowRightPanelFeature, "");
            SetFeatureControl(expandedPanelContent, Core.Settings.Default.MainWindowExpandedPanelFeature, "");

            leftPanelContent.PropertyChanged += leftPanelContent_PropertyChanged;
            topPanelContent.PropertyChanged += topPanelContent_PropertyChanged;
            bottomLeftPanelContent.PropertyChanged += bottomLeftPanelContent_PropertyChanged;
            bottomRightPanelContent.PropertyChanged += bottomRightPanelContent_PropertyChanged;
            rightPanelContent.PropertyChanged += rightPanelContent_PropertyChanged;
            expandedPanelContent.PropertyChanged += expandedPanelContent_PropertyChanged;

            leftPanelContent.WindowStateButtonClick += panelContent_WindowStateButtonClick;
            topPanelContent.WindowStateButtonClick += panelContent_WindowStateButtonClick;
            bottomLeftPanelContent.WindowStateButtonClick += panelContent_WindowStateButtonClick;
            bottomRightPanelContent.WindowStateButtonClick += panelContent_WindowStateButtonClick;
            rightPanelContent.WindowStateButtonClick += panelContent_WindowStateButtonClick;
            expandedPanelContent.WindowStateButtonClick += panelContent_WindowStateButtonClick;

            if (expandedPanelContent.FeatureControl!=null)
            {
                normalView.Visibility = System.Windows.Visibility.Collapsed;
                expandedView.Visibility = System.Windows.Visibility.Visible;
            }

            DutchMenusVisibility = Localization.TranslationManager.Instance.CurrentLanguage.TwoLetterISOLanguageName.ToLower() == "nl" ? Visibility.Visible : Visibility.Collapsed;

            Core.ApplicationData.Instance.PropertyChanged += Instance_PropertyChanged;
            Core.Settings.Default.PropertyChanged += Default_PropertyChanged;
            Core.ApplicationData.Instance.Logger.LogAdded += Logger_LogAdded;
            Localization.TranslationManager.Instance.LanguageChanged += Instance_LanguageChanged;

            rightPanelColumn.Width = rpgl;
            bottomLeftPanelColumn.Width = blgl;
            bottomPanelsRow.Height = bpgl;

            UIControls.ActionBuilder.Manager mng = UIControls.ActionBuilder.Manager.Instance;
            ActionSequence.Manager mng2 = ActionSequence.Manager.Instance;

            updateShortCutKeyAssignment();
            //popup.IsOpen = true;
        }

        void Instance_LanguageChanged(object sender, EventArgs e)
        {
            DutchMenusVisibility = Localization.TranslationManager.Instance.CurrentLanguage.TwoLetterISOLanguageName.ToLower() == "nl" ? Visibility.Visible : Visibility.Collapsed;
        }

        void Logger_LogAdded(object sender, Core.Logger.LogEventArgs e)
        {
            Dispatcher.BeginInvoke((Action)(() =>
            {
                if (e.Level == Core.Logger.Level.Error)
                {
                    PopUpText = e.Message;
                    popup.IsOpen = true;
                }
            }));
        }

        void Default_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "MainWindowShortCutKeyAssignment")
            {
                updateShortCutKeyAssignment();
            }
        }

        private void clearShortCutKey(MenuItem mi)
        {
            mi.InputGestureText = "";
            foreach (var m in mi.Items)
            {
                if (m is MenuItem) clearShortCutKey(m as MenuItem);
            }
        }
        private void updateShortCutKeyAssignment()
        {
            this.InputBindings.Clear();
            //Shift|Control|Alt|Windows|MenuName|Key

            foreach(var m in mainMenu.Items)
            {
                if (m is MenuItem) clearShortCutKey(m as MenuItem);
            }

            if (!string.IsNullOrEmpty(Core.Settings.Default.MainWindowShortCutKeyAssignment))
            {
                string[] lines = Core.Settings.Default.MainWindowShortCutKeyAssignment.Split(new char[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (string l in lines)
                {
                    string[] parts = l.Split(new char[] { '|' }, 6);
                    if (parts.Length == 6)
                    {
                        MenuItem mi = LogicalTreeHelper.FindLogicalNode(mainMenu, parts[4]) as MenuItem;
                        //MenuItem mi = FindName(parts[4]) as MenuItem;
                        if (mi != null)
                        {
                            string decoration = "";
                            ModifierKeys mk = ModifierKeys.None;
                            if (bool.Parse(parts[0]))
                            {
                                mk |= ModifierKeys.Shift;
                                decoration = string.Concat(decoration, "Shift");
                            }
                            if (bool.Parse(parts[1]))
                            {
                                mk |= ModifierKeys.Control;
                                if (decoration.Length > 0)
                                {
                                    decoration = string.Concat(decoration, "+");
                                }
                                decoration = string.Concat(decoration, "Ctrl");
                            }
                            if (bool.Parse(parts[2]))
                            {
                                mk |= ModifierKeys.Alt;
                                if (decoration.Length > 0)
                                {
                                    decoration = string.Concat(decoration, "+");
                                }
                                decoration = string.Concat(decoration, "Alt");
                            }
                            if (bool.Parse(parts[3]))
                            {
                                mk |= ModifierKeys.Windows;
                                if (decoration.Length > 0)
                                {
                                    decoration = string.Concat(decoration, "+");
                                }
                                decoration = string.Concat(decoration, "Win");
                            }
                            if (decoration.Length > 0)
                            {
                                decoration = string.Concat(decoration, "+");
                            }
                            decoration = string.Concat(decoration, parts[5]);
                            Key k = (Key)Enum.Parse(typeof(Key), parts[5]);
                            this.InputBindings.Add(new KeyBinding(ShortCutKeyCommand, new KeyGesture(k, mk)) { CommandParameter = mi });
                            mi.InputGestureText = decoration;
                        }
                    }
                }
            }
        }


        private RelayCommand _liveApiLogGeocachesCommand = null;
        public RelayCommand LiveAPILogGeocachesCommand
        {
            get
            {
                if (_liveApiLogGeocachesCommand == null)
                {
                    _liveApiLogGeocachesCommand = new RelayCommand(param => LiveAPILogGeocaches(null),
                        param => Core.Settings.Default.LiveAPIMemberTypeId > 0);
                }
                return _liveApiLogGeocachesCommand;
            }
        }
        private RelayCommand _liveApiLogGeocachesActiveCommand = null;
        public RelayCommand LiveAPILogGeocachesActiveCommand
        {
            get
            {
                if (_liveApiLogGeocachesActiveCommand == null)
                {
                    _liveApiLogGeocachesActiveCommand = new RelayCommand(param => LiveAPILogGeocachesActive(),
                        param => Core.ApplicationData.Instance.ActiveGeocache != null && Core.Settings.Default.LiveAPIMemberTypeId > 0);
                }
                return _liveApiLogGeocachesActiveCommand;
            }
        }
        public void LiveAPILogGeocachesActive()
        {
            LiveAPILogGeocaches(new Core.Data.Geocache[] { Core.ApplicationData.Instance.ActiveGeocache }.ToList());
        }
        private RelayCommand _liveApiLogGeocachesSelectedCommand = null;
        public RelayCommand LiveAPILogGeocachesSelectedCommand
        {
            get
            {
                if (_liveApiLogGeocachesSelectedCommand == null)
                {
                    _liveApiLogGeocachesSelectedCommand = new RelayCommand(param => LiveAPILogGeocachesSelected(),
                        param => Core.ApplicationData.Instance.ActiveDatabase!=null && GeocacheSelectionCount > 0 && Core.Settings.Default.LiveAPIMemberTypeId > 0);
                }
                return _liveApiLogGeocachesSelectedCommand;
            }
        }
        public void LiveAPILogGeocachesSelected()
        {
            if (Core.ApplicationData.Instance.ActiveDatabase != null)
            {
                LiveAPILogGeocaches((from a in Core.ApplicationData.Instance.ActiveDatabase.GeocacheCollection where a.Selected select a).ToList());
            }
        }
        public void LiveAPILogGeocaches(List<Core.Data.Geocache> gcList)
        {
            LiveAPILogGeocaches.LogWindow dlg = gcList == null ? new LiveAPILogGeocaches.LogWindow() : new LiveAPILogGeocaches.LogWindow(gcList);
            dlg.ShowDialog();
        }


        private RelayCommand _liveApiImportGeocachesCommand = null;
        public RelayCommand LiveAPIImportGeocachesCommand
        {
            get
            {
                if (_liveApiImportGeocachesCommand == null)
                {
                    _liveApiImportGeocachesCommand = new RelayCommand(param => LiveAPIImportGeocaches(),
                        param => Core.ApplicationData.Instance.ActiveDatabase!=null && Core.Settings.Default.LiveAPIMemberTypeId>0);
                }
                return _liveApiImportGeocachesCommand;
            }
        }
        public void LiveAPIImportGeocaches()
        {
            if (Core.ApplicationData.Instance.ActiveDatabase!=null)
            {
                LiveAPIGetGeocaches.ImportWindow dlg = new LiveAPIGetGeocaches.ImportWindow();
                dlg.ShowDialog();
            }
        }


        private RelayCommand _shortCutKeyCommand = null;
        public RelayCommand ShortCutKeyCommand
        {
            get
            {
                if (_shortCutKeyCommand==null)
                {
                    _shortCutKeyCommand = new RelayCommand(param => handleShortCutKey(param));
                }
                return _shortCutKeyCommand;
            }
        }
        private void handleShortCutKey(object e)
        {
            MenuItem mni = e as MenuItem;
            if (mni != null)
            {
                MenuItemAutomationPeer p = new MenuItemAutomationPeer(mni);
                IInvokeProvider ip = p.GetPattern(PatternInterface.Invoke) as IInvokeProvider;
                ip.Invoke();
            }
        }

        private string _statusBarBackgroundColor = "#FF007ACC";
        public string StatusBarBackgroundColor
        {
            get { return _statusBarBackgroundColor; }
            set { SetProperty(ref _statusBarBackgroundColor, value); }
        }

        void Instance_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "ActiveDatabase")
            {
                CurrentConnectedDatabase = Core.ApplicationData.Instance.ActiveDatabase;
            }
            else if (e.PropertyName == "UIIsIdle")
            {
                StatusBarBackgroundColor = Core.ApplicationData.Instance.UIIsIdle ? "#FF007ACC" : "#FFE9760E";
            }
        }

        private async Task initializeApplicationAsync()
        {
            //if (true)
            if (!Core.Settings.Default.IsStorageOK)
            {
                Core.ApplicationData.Instance.Logger.AddLog(this, Core.Logger.Level.Error, Localization.TranslationManager.Instance.Translate("SettingsCorruptDoRestore") as string);
            }
            else
            {
                if (Core.Settings.Default.SettingsBackupAtStartup)
                {
                    await Core.Settings.Default.BackupAsync();
                }
            }
            if (Core.Settings.Default.FirstStart)
            {
                Core.Settings.Default.FirstStart = false;
                SetupWizard.SetupWizardWindow dlg = new SetupWizard.SetupWizardWindow();
                dlg.ShowDialog();
            }
            else
            {
                Core.ApplicationData.Instance.BeginActiviy();

                bool autoLoad = Core.Settings.Default.AutoLoadDatabases;
                string dbs = Core.Settings.Default.OpenedDatabases;
                string actDb = Core.Settings.Default.ActiveDatabase;
                string actGc = Core.Settings.Default.ActiveGeocache;
                Core.Settings.Default.OpenedDatabases = "";
                if (autoLoad && !string.IsNullOrEmpty(dbs))
                {
                    string[] lines = dbs.Split(new char[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
                    int index = 0;
                    using (Utils.ProgressBlock prog = new Utils.ProgressBlock("LoadingDatabases", "LoadingDatabases", lines.Length, 0, true))
                    {
                        foreach (string s in lines)
                        {
                            prog.Update(s, lines.Length, index);

                            Core.Storage.Database db = new Core.Storage.Database(s);
                            bool success = await db.InitializeAsync();
                            if (success)
                            {
                                Core.ApplicationData.Instance.Databases.Add(db);
                            }
                            else
                            {
                                db.Dispose();
                            }

                            index++;
                            if (!prog.Update(s, lines.Length, index))
                            {
                                break;
                            }
                        }
                    }
                    if (!string.IsNullOrEmpty(actDb))
                    {
                        Core.ApplicationData.Instance.ActiveDatabase = (from a in Core.ApplicationData.Instance.Databases where a.FileName == actDb select a).FirstOrDefault();
                    }
                    if (Core.ApplicationData.Instance.ActiveDatabase != null && !string.IsNullOrEmpty(actGc))
                    {
                        Core.ApplicationData.Instance.ActiveGeocache = (from a in Core.ApplicationData.Instance.ActiveDatabase.GeocacheCollection where a.Code == actGc select a).FirstOrDefault();
                    }
                }

                Core.ApplicationData.Instance.EndActiviy();
            }

            Thread thrd = new Thread(new ThreadStart(this.checkForNewVersionThreadMethod));
            thrd.IsBackground = true;
            thrd.Start();
        }

        private void checkForNewVersionThreadMethod()
        {
            try
            {
                using (System.Net.WebClient wc = new System.Net.WebClient())
                {
                    try
                    {
                        string xmldoc = wc.DownloadString("https://www.4geocaching.eu/downloads/_files/gapp/version.xml");
                        XmlDocument doc = new XmlDocument();
                        doc.LoadXml(xmldoc);
                        var root = doc.DocumentElement;
                        var vs = root.SelectSingleNode("version").InnerText.Substring(1);
                        Version v = Version.Parse(vs);
#if DEBUG
                        if (v > System.Reflection.Assembly.GetEntryAssembly().GetName().Version)
#else
                        if (v > System.Reflection.Assembly.GetEntryAssembly().GetName().Version)
#endif
                        {
                            Core.ApplicationData.Instance.Logger.AddLog(this, Core.Logger.Level.Error, Localization.TranslationManager.Instance.Translate("NewVersionAvailable") as string);
                        }
                    }
                    catch
                    {
                    }
                }
            }
            catch
            {
                //no error message checking new version!
            }
        }

        private Core.Storage.Database CurrentConnectedDatabase
        {
            get { return _currentConnectedDatabase; }
            set
            {
                if (_currentConnectedDatabase != value)
                {
                    if (_currentConnectedDatabase != null)
                    {
                        _currentConnectedDatabase.GeocacheCollection.CollectionChanged -= GeocacheCollection_CollectionChanged;
                        _currentConnectedDatabase.GeocacheCollection.GeocacheDataChanged -= GeocacheCollection_GeocacheDataChanged;
                        _currentConnectedDatabase.GeocacheCollection.GeocachePropertyChanged -= GeocacheCollection_GeocachePropertyChanged;
                    }
                    _currentConnectedDatabase = value;
                    if (_currentConnectedDatabase != null)
                    {
                        _currentConnectedDatabase.GeocacheCollection.CollectionChanged += GeocacheCollection_CollectionChanged;
                        _currentConnectedDatabase.GeocacheCollection.GeocacheDataChanged += GeocacheCollection_GeocacheDataChanged;
                        _currentConnectedDatabase.GeocacheCollection.GeocachePropertyChanged += GeocacheCollection_GeocachePropertyChanged;
                    }
                    UpdateView();
                }
            }
        }
        void GeocacheCollection_GeocachePropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            Core.Data.Geocache gc = sender as Core.Data.Geocache;
            if (gc != null)
            {
                if (gc == Core.ApplicationData.Instance.ActiveGeocache || e.PropertyName == "Selected")
                {
                    if (e.PropertyName == "Name" ||
                        e.PropertyName == "Selected")
                    {
                        UpdateView();
                    }
                }
            }
        }
        void GeocacheCollection_GeocacheDataChanged(object sender, EventArgs e)
        {
            Core.Data.Geocache gc = sender as Core.Data.Geocache;
            if (gc != null)
            {
                if (gc == Core.ApplicationData.Instance.ActiveGeocache)
                {
                    UpdateView();
                }
            }
        }
        void GeocacheCollection_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            UpdateView();
        }

        public void UpdateView()
        {
            if (Core.ApplicationData.Instance.ActiveDatabase==null)
            {
                GeocacheSelectionCount = 0;
            }
            else
            {
                GeocacheSelectionCount = (from a in Core.ApplicationData.Instance.ActiveDatabase.GeocacheCollection where a.Selected select a).Count();
            }
        }

        private int _geocacheSelectionCount = 0;
        public int GeocacheSelectionCount
        {
            get { return _geocacheSelectionCount; }
            set { SetProperty(ref _geocacheSelectionCount, value); }
        }

        protected void SetProperty<T>(ref T field, T value, [CallerMemberName] string name = "")
        {
            if (!EqualityComparer<T>.Default.Equals(field, value))
            {
                field = value;
                var handler = PropertyChanged;
                if (handler != null)
                {
                    handler(this, new PropertyChangedEventArgs(name));
                }
            }
        }

        void panelContent_WindowStateButtonClick(object sender, EventArgs e)
        {
            UIControls.UIControlContainer ucc = sender as UIControls.UIControlContainer;
            if (ucc != null)
            {
                ucc.DisposeOnClear = false;
                if (ucc == expandedPanelContent)
                {
                    //minimize
                    UserControl uc = ucc.FeatureControl;
                    ucc.FeatureControl = null;
                    UIControls.UIControlContainer targetUc = normalView.FindName(Core.Settings.Default.MainWindowMiximizedPanelName) as UIControls.UIControlContainer;
                    if (targetUc != null)
                    {
                        targetUc.DisposeOnClear = true;
                        targetUc.FeatureControl = uc;
                        targetUc.DisposeOnClear = false;
                    }
                    expandedView.Visibility = System.Windows.Visibility.Collapsed;
                    normalView.Visibility = System.Windows.Visibility.Visible;

                    Core.Settings.Default.MainWindowMiximizedPanelName = "";
                }
                else
                {
                    //maximize
                    Core.Settings.Default.MainWindowMiximizedPanelName = ucc.Name;
                    UserControl uc = ucc.FeatureControl;
                    ucc.FeatureControl = null;
                    expandedPanelContent.DisposeOnClear = false;
                    expandedPanelContent.FeatureControl = uc;
                    expandedPanelContent.DisposeOnClear = true;
                    normalView.Visibility = System.Windows.Visibility.Collapsed;
                    expandedView.Visibility = System.Windows.Visibility.Visible;
                }
                ucc.DisposeOnClear = true;
            }
        }

        private bool _shown;
        protected override void OnContentRendered(EventArgs e)
        {
            base.OnContentRendered(e);

            if (_shown)
                return;

            _shown = true;

            string s = Core.Settings.Default.MainWindowWindowFeature;
            if (!string.IsNullOrEmpty(s))
            {
                string[] parts = s.Split(new char[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (string p in parts)
                {
                    UserControl uc = CreateFeatureControl(p);
                    if (uc!=null)
                    {
                        FeatureWindow w = new FeatureWindow(uc);
                        w.Owner = this;
                        w.Show();
                    }
                }
            }
        }

        private string getFeatureControlSetting(UserControl uc)
        {
            if (uc==null)
            {
                return "";
            }
            else if (uc is UIControls.Maps.Control)
            {
                return (uc as UIControls.Maps.Control).MapFactory.GetType().ToString();
            }
            else
            {
                return uc.GetType().ToString();
            }
        }

        void rightPanelContent_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "FeatureControl")
            {
                Core.Settings.Default.MainWindowRightPanelFeature = getFeatureControlSetting(rightPanelContent.FeatureControl);
            }
        }

        void expandedPanelContent_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "FeatureControl")
            {
                Core.Settings.Default.MainWindowExpandedPanelFeature = getFeatureControlSetting(expandedPanelContent.FeatureControl);
            }
        }


        void bottomRightPanelContent_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "FeatureControl")
            {
                Core.Settings.Default.MainWindowBottomRightPanelFeature = getFeatureControlSetting(bottomRightPanelContent.FeatureControl);
            }
        }

        void bottomLeftPanelContent_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "FeatureControl")
            {
                Core.Settings.Default.MainWindowBottomLeftPanelFeature = getFeatureControlSetting(bottomLeftPanelContent.FeatureControl);
            }
        }

        void topPanelContent_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "FeatureControl")
            {
                Core.Settings.Default.MainWindowTopPanelFeature = getFeatureControlSetting(topPanelContent.FeatureControl);
            }
        }

        void leftPanelContent_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "FeatureControl")
            {
                Core.Settings.Default.MainWindowLeftPanelFeature = getFeatureControlSetting(leftPanelContent.FeatureControl);
            }
        }

        public void SetFeatureControl(UIControls.UIControlContainer container, string setting, string defaultFeature)
        {
            if (setting == null)
            {
                setting = defaultFeature;
            }
            if (!string.IsNullOrEmpty(setting))
            {
                UserControl uc = CreateFeatureControl(setting);
                container.FeatureControl = uc;
            }

        }

        private UserControl CreateFeatureControl(string type)
        {
            UserControl result = null;
            Type t = Type.GetType(type);
            if (t != null)
            {
                if (type.StartsWith("GAPPSF.MapProviders."))
                {
                    ConstructorInfo constructor2 = t.GetConstructor(Type.EmptyTypes);
                    MapProviders.MapControlFactory mcf = (GAPPSF.MapProviders.MapControlFactory)constructor2.Invoke(Type.EmptyTypes);
                    result = new UIControls.Maps.Control(mcf);
                }
                else
                {
                    ConstructorInfo constructor = t.GetConstructor(Type.EmptyTypes);
                    result = (UserControl)constructor.Invoke(Type.EmptyTypes);
                }
                //result = null;
            }
            return result;
        }

        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            Core.Settings.Default.MainWindowRightPanelWidth = rightPanelColumn.Width;
            Core.Settings.Default.MainWindowBottomLeftPanelWidth = bottomLeftPanelColumn.Width;
            Core.Settings.Default.MainWindowBottomPanelHeight = bottomPanelsRow.Height;
            Dialogs.ProgessWindow.Instance.Close();
            base.OnClosing(e);
        }

        private void MenuItem_Click(object sender, RoutedEventArgs e)
        {
            Window w = new FeatureWindow(new UIControls.CacheList());
            w.Owner = this;
            w.Show();
        }

        private void MenuItem_Click_1(object sender, RoutedEventArgs e)
        {
            Window w = new FeatureWindow(new UIControls.GeocacheViewer());
            w.Owner = this;
            w.Show();
        }

        private void MenuItem_Click_2(object sender, RoutedEventArgs e)
        {
            Localization.TranslationManager.Instance.CurrentLanguage = CultureInfo.InvariantCulture;
        }

        private void MenuItem_Click_3(object sender, RoutedEventArgs e)
        {
            Localization.TranslationManager.Instance.CurrentLanguage = new CultureInfo("en-US");
        }

        private void MenuItem_Click_4(object sender, RoutedEventArgs e)
        {
            Localization.TranslationManager.Instance.CurrentLanguage = new CultureInfo("de-DE");
        }

        private void MenuItem_Click_5(object sender, RoutedEventArgs e)
        {
            Localization.TranslationManager.Instance.CurrentLanguage = new CultureInfo("fr-FR");
        }

        private void MenuItem_Click_6(object sender, RoutedEventArgs e)
        {
            Localization.TranslationManager.Instance.CurrentLanguage = new CultureInfo("nl-NL");
        }



        AsyncDelegateCommand _importGCComBotesCommand;
        public ICommand ImportGCComNotesCommand
        {
            get
            {
                if (_importGCComBotesCommand == null)
                {
                    _importGCComBotesCommand = new AsyncDelegateCommand(param => this.ImportGCComNotes(),
                        param => Core.ApplicationData.Instance.ActiveDatabase != null && Core.Settings.Default.LiveAPIMemberTypeId >= 1);
                }
                return _importGCComBotesCommand;
            }
        }
        public async Task ImportGCComNotes()
        {
            LiveAPI.Import imp = new LiveAPI.Import();
            await imp.ImportNotesAsync(Core.ApplicationData.Instance.ActiveDatabase, false);
        }

        AsyncDelegateCommand _importGCComBotesMissingCommand;
        public ICommand ImportGCComNotesMissingCommand
        {
            get
            {
                if (_importGCComBotesMissingCommand == null)
                {
                    _importGCComBotesMissingCommand = new AsyncDelegateCommand(param => this.ImportGCComNotesMissing(),
                        param => Core.ApplicationData.Instance.ActiveDatabase != null && Core.Settings.Default.LiveAPIMemberTypeId >= 1);
                }
                return _importGCComBotesMissingCommand;
            }
        }
        public async Task ImportGCComNotesMissing()
        {
            LiveAPI.Import imp = new LiveAPI.Import();
            await imp.ImportNotesAsync(Core.ApplicationData.Instance.ActiveDatabase, true);
        }


        AsyncDelegateCommand _importGAPPDECommand;
        public ICommand ImportGAPPDECommand
        {
            get
            {
                if (_importGAPPDECommand == null)
                {
                    _importGAPPDECommand = new AsyncDelegateCommand(param => this.ImportGAPPDE(true),
                        param => Core.ApplicationData.Instance.ActiveDatabase != null);
                }
                return _importGAPPDECommand;
            }
        }
        public async Task ImportGAPPDE(bool missing)
        {
            if (Core.ApplicationData.Instance.ActiveDatabase != null)
            {
                GAPPDataExchange.Import imp = new GAPPDataExchange.Import();
                await imp.ImportFile(Core.ApplicationData.Instance.ActiveDatabase);
            }
        }



        AsyncDelegateCommand _importGCCCommand;
        public ICommand ImportGCCCommand
        {
            get
            {
                if (_importGCCCommand == null)
                {
                    _importGCCCommand = new AsyncDelegateCommand(param => this.ImportGCC(false),
                        param => Core.ApplicationData.Instance.ActiveDatabase != null);
                }
                return _importGCCCommand;
            }
        }
        AsyncDelegateCommand _importGCCMissingCommand;
        public ICommand ImportGCCMissingCommand
        {
            get
            {
                if (_importGCCMissingCommand == null)
                {
                    _importGCCMissingCommand = new AsyncDelegateCommand(param => this.ImportGCC(true),
                        param => Core.ApplicationData.Instance.ActiveDatabase != null);
                }
                return _importGCCMissingCommand;
            }
        }
        public async Task ImportGCC(bool missing)
        {
            if (Core.ApplicationData.Instance.ActiveDatabase != null)
            {
                GCComments.Import imp = new GCComments.Import();
                await imp.ImportGCComments(Core.ApplicationData.Instance.ActiveDatabase, missing);
            }
        }



        AsyncDelegateCommand _deleteActiveCommand;
        public ICommand DeleteActiveCommand
        {
            get
            {
                if (_deleteActiveCommand == null)
                {
                    _deleteActiveCommand = new AsyncDelegateCommand(param => this.DeleteActiveGeocache(false),
                        param => Core.ApplicationData.Instance.ActiveGeocache != null);
                }
                return _deleteActiveCommand;
            }
        }

        AsyncDelegateCommand _deleteIgnoreActiveCommand;
        public ICommand DeleteIgnoreActiveCommand
        {
            get
            {
                if (_deleteIgnoreActiveCommand == null)
                {
                    _deleteIgnoreActiveCommand = new AsyncDelegateCommand(param => this.DeleteActiveGeocache(true),
                        param => Core.ApplicationData.Instance.ActiveGeocache != null);
                }
                return _deleteIgnoreActiveCommand;
            }
        }

        async public Task DeleteActiveGeocache(bool ignore)
        {
            if (Core.ApplicationData.Instance.ActiveGeocache != null)
            {
                Core.Data.Geocache gc = Core.ApplicationData.Instance.ActiveGeocache;
                Core.ApplicationData.Instance.ActiveGeocache = null;
                using (Utils.DataUpdater upd = new Utils.DataUpdater(gc.Database))
                {
                    await Task.Run(() =>
                    {
                        Utils.DataAccess.DeleteGeocache(gc);
                        if (ignore)
                        {
                            Core.Settings.Default.AddIgnoreGeocacheCodes((new string[] { gc.Code }).ToList());
                        }
                    });
                }
            }
        }

        AsyncDelegateCommand _deleteSelectionCommand;
        public ICommand DeleteSelectionCommand
        {
            get
            {
                if (_deleteSelectionCommand == null)
                {
                    _deleteSelectionCommand = new AsyncDelegateCommand(param => this.DeleteSelectionGeocache(false),
                        param => GeocacheSelectionCount>0);
                }
                return _deleteSelectionCommand;
            }
        }

        AsyncDelegateCommand _deleteIgnoreSelectionCommand;
        public ICommand DeleteIgnoreSelectionCommand
        {
            get
            {
                if (_deleteIgnoreSelectionCommand == null)
                {
                    _deleteIgnoreSelectionCommand = new AsyncDelegateCommand(param => this.DeleteSelectionGeocache(true),
                        param => GeocacheSelectionCount > 0);
                }
                return _deleteIgnoreSelectionCommand;
            }
        }

        async public Task DeleteSelectionGeocache(bool ignore)
        {
            if (Core.ApplicationData.Instance.ActiveDatabase != null)
            {
                if (Core.ApplicationData.Instance.ActiveGeocache != null && Core.ApplicationData.Instance.ActiveGeocache.Selected)
                {
                    Core.ApplicationData.Instance.ActiveGeocache = null;
                }
                using (Utils.DataUpdater upd = new Utils.DataUpdater(Core.ApplicationData.Instance.ActiveDatabase))
                {
                    await Task.Run(() =>
                    {
                        int index = 0;
                        DateTime nextUpdate = DateTime.Now.AddSeconds(1);
                        List<Core.Data.Geocache> gcList = (from a in Core.ApplicationData.Instance.ActiveDatabase.GeocacheCollection where a.Selected select a).ToList();
                        using (Utils.ProgressBlock prog = new Utils.ProgressBlock("DeletingGeocaches", "DeletingGeocaches", gcList.Count, 0, true))
                        {
                            foreach (var gc in gcList)
                            {
                                Utils.DataAccess.DeleteGeocache(gc);
                                if (ignore)
                                {
                                    Core.Settings.Default.AddIgnoreGeocacheCodes((new string[] { gc.Code }).ToList());
                                }
                                index++;

                                if (DateTime.Now >= nextUpdate)
                                {
                                    if (!prog.Update("Deleting geocaches...", gcList.Count, index))
                                    {
                                        break;
                                    }
                                    nextUpdate = DateTime.Now.AddSeconds(1);
                                }
                            }
                        }
                    });
                }
            }
        }


        AsyncDelegateCommand _restoreDatabaseCommand;
        public ICommand RestoreDatabaseCommand
        {
            get
            {
                if (_restoreDatabaseCommand == null)
                {
                    _restoreDatabaseCommand = new AsyncDelegateCommand(param => this.RestoreDatabase());
                }
                return _restoreDatabaseCommand;
            }
        }
        public async Task RestoreDatabase()
        {
            Microsoft.Win32.OpenFileDialog dlg = new Microsoft.Win32.OpenFileDialog();
            dlg.FileName = ""; // Default file name
            dlg.Filter = "GAPP SF backup (*.gsf.bak)|*.gsf.bak*"; // Filter files by extension 

            // Show open file dialog box
            Nullable<bool> result = dlg.ShowDialog();

            // Process open file dialog box results 
            if (result == true)
            {
                int pos = dlg.FileName.ToLower().LastIndexOf(".bak");
                string orgFn = dlg.FileName.Substring(0, pos);

                //if database is open at the moment, close it.
                var db = (from a in Core.ApplicationData.Instance.Databases where string.Compare(a.FileName, orgFn, true) == 0 select a).FirstOrDefault();
                if (db != null)
                {
                    if (Core.ApplicationData.Instance.ActiveDatabase == db)
                    {
                        Core.ApplicationData.Instance.ActiveDatabase = null;
                    }
                    Core.ApplicationData.Instance.Databases.Remove(db);
                }

                //now, delete index file
                string indexFile = string.Concat(orgFn, ".gsx");
                if (File.Exists(indexFile))
                {
                    File.Delete(indexFile);
                }

                if (File.Exists(orgFn))
                {
                    File.Delete(orgFn);
                }
                File.Move(dlg.FileName, orgFn);

                //load database
                bool success;
                db = new Core.Storage.Database(orgFn);
                using (Utils.DataUpdater upd = new Utils.DataUpdater(db, true))
                {
                    success = await db.InitializeAsync();
                }
                if (success)
                {
                    Core.ApplicationData.Instance.Databases.Add(db);
                    Core.ApplicationData.Instance.ActiveDatabase = db;
                }
                else
                {
                    db.Dispose();
                }
            }
        }


        AsyncDelegateCommand _backupDatabaseActiveCommand;
        public ICommand BackupDatabaseActiveCommand
        {
            get
            {
                if (_backupDatabaseActiveCommand == null)
                {
                    _backupDatabaseActiveCommand = new AsyncDelegateCommand(param => this.BackupDatabaseActive(),
                        param => Core.ApplicationData.Instance.ActiveDatabase != null);
                }
                return _backupDatabaseActiveCommand;
            }
        }
        public async Task BackupDatabaseActive()
        {
            if (Core.ApplicationData.Instance.ActiveDatabase != null)
            {
                await Core.ApplicationData.Instance.ActiveDatabase.BackupAsync();
            }
        }

        AsyncDelegateCommand _backupSettingsCommand;
        public ICommand BackupSettingsCommand
        {
            get
            {
                if (_backupSettingsCommand == null)
                {
                    _backupSettingsCommand = new AsyncDelegateCommand(param => this.BackupSettings(),
                        param => Core.Settings.Default.IsStorageOK);
                }
                return _backupSettingsCommand;
            }
        }
        public async Task BackupSettings()
        {
            await Core.Settings.Default.BackupAsync();
        }


        AsyncDelegateCommand _deleteAllCommand;
        public ICommand DeleteAllCommand
        {
            get
            {
                if (_deleteAllCommand == null)
                {
                    _deleteAllCommand = new AsyncDelegateCommand(param => this.DeleteAllGeocache(false),
                        param => Core.ApplicationData.Instance.ActiveDatabase != null);
                }
                return _deleteAllCommand;
            }
        }

        AsyncDelegateCommand _deleteIgnoreAllCommand;
        public ICommand DeleteIgnoreAllCommand
        {
            get
            {
                if (_deleteIgnoreAllCommand == null)
                {
                    _deleteIgnoreAllCommand = new AsyncDelegateCommand(param => this.DeleteAllGeocache(true),
                        param => Core.ApplicationData.Instance.ActiveDatabase != null);
                }
                return _deleteIgnoreAllCommand;
            }
        }

        async public Task DeleteAllGeocache(bool ignore)
        {
            if (Core.ApplicationData.Instance.ActiveDatabase != null)
            {
                Core.ApplicationData.Instance.ActiveGeocache = null;
                using (Utils.DataUpdater upd = new Utils.DataUpdater(Core.ApplicationData.Instance.ActiveDatabase))
                {
                    await Task.Run(() =>
                    {
                        int index = 0;
                        DateTime nextUpdate = DateTime.Now.AddSeconds(1);
                        List<Core.Data.Geocache> gcList = (from a in Core.ApplicationData.Instance.ActiveDatabase.GeocacheCollection select a).ToList();
                        using (Utils.ProgressBlock prog = new Utils.ProgressBlock("DeletingGeocaches", "DeletingGeocaches", gcList.Count, 0, true))
                        {
                            foreach (var gc in gcList)
                            {
                                Utils.DataAccess.DeleteGeocache(gc);
                                if (ignore)
                                {
                                    Core.Settings.Default.AddIgnoreGeocacheCodes((new string[] { gc.Code }).ToList());
                                }
                                index++;

                                if (DateTime.Now >= nextUpdate)
                                {
                                    if (!prog.Update("Deleting geocaches...", gcList.Count, index))
                                    {
                                        break;
                                    }
                                    nextUpdate = DateTime.Now.AddSeconds(1);
                                }
                            }
                        }
                    });
                }
            }
        }


        AsyncDelegateCommand _importGpxCommand;
        public ICommand ImportGPXCommand
        {
            get
            {
                if (_importGpxCommand == null)
                {
                    _importGpxCommand = new AsyncDelegateCommand(param => this.ImportGPXFile(),
                        param => Core.ApplicationData.Instance.ActiveDatabase != null);
                }
                return _importGpxCommand;
            }
        }

        async private Task ImportGPXFile()
        {

            Microsoft.Win32.OpenFileDialog dlg = new Microsoft.Win32.OpenFileDialog();
            dlg.FileName = ""; // Default file name
            dlg.DefaultExt = ".gpx"; // Default file extension
            dlg.Filter = "GPX files (.gpx)|*.gpx|Pocket Query files (.zip)|*.zip|Garmin GGZ files (.ggz)|*.ggz"; // Filter files by extension 

            // Show open file dialog box
            Nullable<bool> result = dlg.ShowDialog();

            // Process open file dialog box results 
            if (result == true)
            {
                // Open document 
                GPX.Import imp = new GPX.Import();
                await imp.ImportFilesAsync(dlg.FileNames);
            }
        }


        RelayCommand _importLogsOfUsersCommand;
        public ICommand ImportLogsOfUsersCommand
        {
            get
            {
                if (_importLogsOfUsersCommand == null)
                {
                    _importLogsOfUsersCommand = new RelayCommand(param => this.ImportLogsOfUsers(),
                        param => Core.ApplicationData.Instance.ActiveDatabase != null && Core.Settings.Default.LiveAPIMemberTypeId > 0);
                }
                return _importLogsOfUsersCommand;
            }
        }
        private void ImportLogsOfUsers()
        {
            if (Core.ApplicationData.Instance.ActiveDatabase != null)
            {
                FindsOfUser.ImportWindow dlg = new FindsOfUser.ImportWindow();
                dlg.ShowDialog();
            }
        }


        AsyncDelegateCommand _importMyFavsCodesCommand;
        public ICommand ImportFavsCodesCommand
        {
            get
            {
                if (_importMyFavsCodesCommand == null)
                {
                    _importMyFavsCodesCommand = new AsyncDelegateCommand(param => this.ImportMyFavsCodes(),
                        param => Core.ApplicationData.Instance.ActiveDatabase != null && Core.Settings.Default.LiveAPIMemberTypeId > 1);
                }
                return _importMyFavsCodesCommand;
            }
        }
        private async Task ImportMyFavsCodes()
        {
            if (Core.ApplicationData.Instance.ActiveDatabase != null)
            {
                Favorites.GCCom com = new Favorites.GCCom();
                await com.GetAllYourFavoriteGeocachesAsync(Core.ApplicationData.Instance.ActiveDatabase, false);
            }
        }
        AsyncDelegateCommand _importMyFavsGeocachesCommand;
        public ICommand ImportFavsGeocachesCommand
        {
            get
            {
                if (_importMyFavsGeocachesCommand == null)
                {
                    _importMyFavsGeocachesCommand = new AsyncDelegateCommand(param => this.ImportMyFavsGeocaches(),
                        param => Core.ApplicationData.Instance.ActiveDatabase != null && Core.Settings.Default.LiveAPIMemberTypeId > 1);
                }
                return _importMyFavsGeocachesCommand;
            }
        }
        private async Task ImportMyFavsGeocaches()
        {
            if (Core.ApplicationData.Instance.ActiveDatabase != null)
            {
                Favorites.GCCom com = new Favorites.GCCom();
                await com.GetAllYourFavoriteGeocachesAsync(Core.ApplicationData.Instance.ActiveDatabase, true);
            }
        }
        AsyncDelegateCommand _favAddActiveGeocacheCommand;
        public ICommand FavAddActiveGeocacheCommand
        {
            get
            {
                if (_favAddActiveGeocacheCommand == null)
                {
                    _favAddActiveGeocacheCommand = new AsyncDelegateCommand(param => this.FavAddActiveGeocache(),
                        param => Core.ApplicationData.Instance.ActiveGeocache != null 
                            && Core.Settings.Default.LiveAPIMemberTypeId > 1
                            && !Favorites.Manager.Instance.GeocacheFavorited(Core.ApplicationData.Instance.ActiveGeocache.Code));
                }
                return _favAddActiveGeocacheCommand;
            }
        }
        private async Task FavAddActiveGeocache()
        {
            if (Core.ApplicationData.Instance.ActiveGeocache != null)
            {
                Favorites.GCCom com = new Favorites.GCCom();
                await com.AddFavoriteGeocacheAsync(Core.ApplicationData.Instance.ActiveGeocache);
            }
        }
        AsyncDelegateCommand _favRemoveActiveGeocacheCommand;
        public ICommand FavRemoveActiveGeocacheCommand
        {
            get
            {
                if (_favRemoveActiveGeocacheCommand == null)
                {
                    _favRemoveActiveGeocacheCommand = new AsyncDelegateCommand(param => this.FavRemoveActiveGeocache(),
                        param => Core.ApplicationData.Instance.ActiveGeocache != null
                            && Core.Settings.Default.LiveAPIMemberTypeId > 1
                            && Favorites.Manager.Instance.GeocacheFavorited(Core.ApplicationData.Instance.ActiveGeocache.Code));
                }
                return _favRemoveActiveGeocacheCommand;
            }
        }
        private async Task FavRemoveActiveGeocache()
        {
            if (Core.ApplicationData.Instance.ActiveGeocache != null)
            {
                Favorites.GCCom com = new Favorites.GCCom();
                await com.RemoveFavoriteGeocacheAsync(Core.ApplicationData.Instance.ActiveGeocache);
            }
        }



        AsyncDelegateCommand _importMyFindsCommand;
        public ICommand ImportMyFindsCommand
        {
            get
            {
                if (_importMyFindsCommand == null)
                {
                    _importMyFindsCommand = new AsyncDelegateCommand(param => this.ImportMyFinds(),
                        param => Core.ApplicationData.Instance.ActiveDatabase != null && Core.Settings.Default.LiveAPIMemberTypeId > 0);
                }
                return _importMyFindsCommand;
            }
        }
        private async Task ImportMyFinds()
        {
            if (Core.ApplicationData.Instance.ActiveDatabase != null)
            {
                MyFinds.ImportLiveAPI mf = new MyFinds.ImportLiveAPI();
                await mf.ImportMyFindsAsync(Core.ApplicationData.Instance.ActiveDatabase);
            }
        }


        RelayCommand _resetGCVotesCommand;
        public RelayCommand ResetGCVotesCommand
        {
            get
            {
                if (_resetGCVotesCommand == null)
                {
                    _resetGCVotesCommand = new RelayCommand(param => this.ResetGCVotes(),
                        param => Core.ApplicationData.Instance.ActiveGeocache != null);
                }
                return _resetGCVotesCommand;
            }
        }
        public void ResetGCVotes()
        {
            try
            {
                Core.Settings.Default.ClearGCVotes();
                if (Core.ApplicationData.Instance.ActiveDatabase != null)
                {
                    Core.ApplicationData.Instance.ActiveDatabase.GeocacheCollection.OnCollectionChanged(new System.Collections.Specialized.NotifyCollectionChangedEventArgs(System.Collections.Specialized.NotifyCollectionChangedAction.Reset));
                }
            }
            catch(Exception e)
            {
                Core.ApplicationData.Instance.Logger.AddLog(this, e);
            }
        }

        AsyncDelegateCommand _importGcvoteActiveCommand;
        public ICommand ImportGcvoteActiveCommand
        {
            get
            {
                if (_importGcvoteActiveCommand == null)
                {
                    _importGcvoteActiveCommand = new AsyncDelegateCommand(param => this.ImportGcvoteActive(),
                        param => Core.ApplicationData.Instance.ActiveGeocache != null);
                }
                return _importGcvoteActiveCommand;
            }
        }
        private async Task ImportGcvoteActive()
        {
            if (Core.ApplicationData.Instance.ActiveGeocache != null)
            {
                await ImportGcvote(new Core.Data.Geocache[] { Core.ApplicationData.Instance.ActiveGeocache }.ToList());
            }
        }
        AsyncDelegateCommand _importGcvoteSelectedCommand;
        public ICommand ImportGcvoteSelectedCommand
        {
            get
            {
                if (_importGcvoteSelectedCommand == null)
                {
                    _importGcvoteSelectedCommand = new AsyncDelegateCommand(param => this.ImportGcvoteSelected(),
                        param => Core.ApplicationData.Instance.ActiveDatabase != null && this.GeocacheSelectionCount > 0);
                }
                return _importGcvoteSelectedCommand;
            }
        }
        private async Task ImportGcvoteSelected()
        {
            if (Core.ApplicationData.Instance.ActiveDatabase != null)
            {
                await ImportGcvote((from a in Core.ApplicationData.Instance.ActiveDatabase.GeocacheCollection where a.Selected select a).ToList());
            }
        }
        AsyncDelegateCommand _importGcvoteAllCommand;
        public ICommand ImportGcvoteAllCommand
        {
            get
            {
                if (_importGcvoteAllCommand == null)
                {
                    _importGcvoteAllCommand = new AsyncDelegateCommand(param => this.ImportGcvoteAll(),
                        param => Core.ApplicationData.Instance.ActiveDatabase != null);
                }
                return _importGcvoteAllCommand;
            }
        }
        private async Task ImportGcvoteAll()
        {
            if (Core.ApplicationData.Instance.ActiveDatabase != null)
            {
                await ImportGcvote((from a in Core.ApplicationData.Instance.ActiveDatabase.GeocacheCollection select a).ToList());
            }
        }
        private async Task ImportGcvote(List<Core.Data.Geocache> gcList)
        {
            GCVote.Import imp = new GCVote.Import();
            await imp.ImporGCVotesAsync(gcList);
            //refresh
            if (Core.ApplicationData.Instance.ActiveDatabase != null)
            {
                Core.ApplicationData.Instance.ActiveDatabase.GeocacheCollection.OnCollectionChanged(new System.Collections.Specialized.NotifyCollectionChangedEventArgs(System.Collections.Specialized.NotifyCollectionChangedAction.Reset));
            }
        }





        AsyncDelegateCommand _updateStatusActiveCommand;
        public ICommand UpdateStatusActiveCommand
        {
            get
            {
                if (_updateStatusActiveCommand == null)
                {
                    _updateStatusActiveCommand = new AsyncDelegateCommand(param => this.UpdateStatusActive(),
                        param => Core.ApplicationData.Instance.ActiveGeocache != null && Core.Settings.Default.LiveAPIMemberTypeId > 0);
                }
                return _updateStatusActiveCommand;
            }
        }
        private async Task UpdateStatusActive()
        {
            if (Core.ApplicationData.Instance.ActiveGeocache != null)
            {
                await UpdateStatusGeocaches(new Core.Data.Geocache[] { Core.ApplicationData.Instance.ActiveGeocache }.ToList());
            }
        }
        AsyncDelegateCommand _updateStatusSelectedCommand;
        public ICommand UpdateStatusSelectedCommand
        {
            get
            {
                if (_updateStatusSelectedCommand == null)
                {
                    _updateStatusSelectedCommand = new AsyncDelegateCommand(param => this.UpdateStatusSelected(),
                        param => Core.ApplicationData.Instance.ActiveDatabase != null && this.GeocacheSelectionCount > 0 && Core.Settings.Default.LiveAPIMemberTypeId > 0);
                }
                return _updateStatusSelectedCommand;
            }
        }
        private async Task UpdateStatusSelected()
        {
            if (Core.ApplicationData.Instance.ActiveDatabase != null)
            {
                await UpdateStatusGeocaches((from a in Core.ApplicationData.Instance.ActiveDatabase.GeocacheCollection where a.Selected select a).ToList());
            }
        }
        AsyncDelegateCommand _updateStatusAllCommand;
        public ICommand UpdateStatusAllCommand
        {
            get
            {
                if (_updateStatusAllCommand == null)
                {
                    _updateStatusAllCommand = new AsyncDelegateCommand(param => this.UpdateStatusAll(),
                        param => Core.ApplicationData.Instance.ActiveDatabase != null && Core.Settings.Default.LiveAPIMemberTypeId > 0);
                }
                return _updateStatusAllCommand;
            }
        }
        private async Task UpdateStatusAll()
        {
            if (Core.ApplicationData.Instance.ActiveDatabase != null)
            {
                await UpdateStatusGeocaches(Core.ApplicationData.Instance.ActiveDatabase.GeocacheCollection);
            }
        }
        private async Task UpdateStatusGeocaches(List<Core.Data.Geocache> gcList)
        {
            if (Core.ApplicationData.Instance.ActiveDatabase != null)
            {
                using (Utils.DataUpdater upd = new Utils.DataUpdater(Core.ApplicationData.Instance.ActiveDatabase))
                {
                    await Task.Run(() =>
                    {
                        LiveAPI.Import.ImportGeocacheStatus(Core.ApplicationData.Instance.ActiveDatabase, (from a in gcList select a.Code).ToList());
                    });
                }
            }
        }



        AsyncDelegateCommand _updateGCLogsActiveCommand;
        public ICommand UpdateGCLogsActiveCommand
        {
            get
            {
                if (_updateGCLogsActiveCommand == null)
                {
                    _updateGCLogsActiveCommand = new AsyncDelegateCommand(param => this.UpdateGCLogsActive(),
                        param => Core.ApplicationData.Instance.ActiveGeocache != null && Core.Settings.Default.LiveAPIMemberTypeId > 0);
                }
                return _updateGCLogsActiveCommand;
            }
        }
        private async Task UpdateGCLogsActive()
        {
            if (Core.ApplicationData.Instance.ActiveGeocache != null)
            {
                await UpdateGCLogsGeocaches(new Core.Data.Geocache[] { Core.ApplicationData.Instance.ActiveGeocache }.ToList());
            }
        }
        AsyncDelegateCommand _updateGCLogsSelectedCommand;
        public ICommand UpdateGCLogsSelectedCommand
        {
            get
            {
                if (_updateGCLogsSelectedCommand == null)
                {
                    _updateGCLogsSelectedCommand = new AsyncDelegateCommand(param => this.UpdateGCLogsSelected(),
                        param => Core.ApplicationData.Instance.ActiveDatabase != null && this.GeocacheSelectionCount > 0 && Core.Settings.Default.LiveAPIMemberTypeId > 0);
                }
                return _updateGCLogsSelectedCommand;
            }
        }
        private async Task UpdateGCLogsSelected()
        {
            if (Core.ApplicationData.Instance.ActiveDatabase != null)
            {
                await UpdateGCLogsGeocaches((from a in Core.ApplicationData.Instance.ActiveDatabase.GeocacheCollection where a.Selected select a).ToList());
            }
        }
        AsyncDelegateCommand _updateGCLogsAllCommand;
        public ICommand UpdateGCLogsAllCommand
        {
            get
            {
                if (_updateGCLogsAllCommand == null)
                {
                    _updateGCLogsAllCommand = new AsyncDelegateCommand(param => this.UpdateGCLogsAll(),
                        param => Core.ApplicationData.Instance.ActiveDatabase != null && Core.Settings.Default.LiveAPIMemberTypeId > 0);
                }
                return _updateGCLogsAllCommand;
            }
        }
        private async Task UpdateGCLogsAll()
        {
            if (Core.ApplicationData.Instance.ActiveDatabase != null)
            {
                await UpdateGCLogsGeocaches(Core.ApplicationData.Instance.ActiveDatabase.GeocacheCollection);
            }
        }
        private async Task UpdateGCLogsGeocaches(List<Core.Data.Geocache> gcList)
        {
            if (Core.ApplicationData.Instance.ActiveDatabase != null)
            {
                await LiveAPI.Import.ImportGeocacheLogsAsync(Core.ApplicationData.Instance.ActiveDatabase, gcList);
            }
        }



        AsyncDelegateCommand _updateGCImagesActiveCommand;
        public ICommand UpdateGCImagesActiveCommand
        {
            get
            {
                if (_updateGCImagesActiveCommand == null)
                {
                    _updateGCImagesActiveCommand = new AsyncDelegateCommand(param => this.UpdateGCImagesActive(),
                        param => Core.ApplicationData.Instance.ActiveGeocache != null && Core.Settings.Default.LiveAPIMemberTypeId > 0);
                }
                return _updateGCImagesActiveCommand;
            }
        }
        private async Task UpdateGCImagesActive()
        {
            if (Core.ApplicationData.Instance.ActiveGeocache != null)
            {
                await UpdateGCImagesGeocaches(new Core.Data.Geocache[] { Core.ApplicationData.Instance.ActiveGeocache }.ToList());
            }
        }
        AsyncDelegateCommand _updateGCImagesSelectedCommand;
        public ICommand UpdateGCImagesSelectedCommand
        {
            get
            {
                if (_updateGCImagesSelectedCommand == null)
                {
                    _updateGCImagesSelectedCommand = new AsyncDelegateCommand(param => this.UpdateGCImagesSelected(),
                        param => Core.ApplicationData.Instance.ActiveDatabase != null && this.GeocacheSelectionCount > 0 && Core.Settings.Default.LiveAPIMemberTypeId > 0);
                }
                return _updateGCImagesSelectedCommand;
            }
        }
        private async Task UpdateGCImagesSelected()
        {
            if (Core.ApplicationData.Instance.ActiveDatabase != null)
            {
                await UpdateGCImagesGeocaches((from a in Core.ApplicationData.Instance.ActiveDatabase.GeocacheCollection where a.Selected select a).ToList());
            }
        }
        AsyncDelegateCommand _updateGCImagesAllCommand;
        public ICommand UpdateGCImagesAllCommand
        {
            get
            {
                if (_updateGCImagesAllCommand == null)
                {
                    _updateGCImagesAllCommand = new AsyncDelegateCommand(param => this.UpdateGCImagesAll(),
                        param => Core.ApplicationData.Instance.ActiveDatabase != null && Core.Settings.Default.LiveAPIMemberTypeId > 0);
                }
                return _updateGCImagesAllCommand;
            }
        }
        private async Task UpdateGCImagesAll()
        {
            if (Core.ApplicationData.Instance.ActiveDatabase != null)
            {
                await UpdateGCImagesGeocaches(Core.ApplicationData.Instance.ActiveDatabase.GeocacheCollection);
            }
        }
        private async Task UpdateGCImagesGeocaches(List<Core.Data.Geocache> gcList)
        {
            if (Core.ApplicationData.Instance.ActiveDatabase != null)
            {
                await LiveAPI.Import.ImportGeocacheImagesAsync(Core.ApplicationData.Instance.ActiveDatabase, gcList);
            }
        }




        AsyncDelegateCommand _updateActiveCommand;
        public ICommand UpdateActiveCommand
        {
            get
            {
                if (_updateActiveCommand == null)
                {
                    _updateActiveCommand = new AsyncDelegateCommand(param => this.UpdateActive(),
                        param => Core.ApplicationData.Instance.ActiveGeocache != null && Core.Settings.Default.LiveAPIMemberTypeId>0);
                }
                return _updateActiveCommand;
            }
        }
        private async Task UpdateActive()
        {
            if (Core.ApplicationData.Instance.ActiveGeocache != null)
            {
                await UpdateGeocaches(new Core.Data.Geocache[] { Core.ApplicationData.Instance.ActiveGeocache }.ToList());
            }
        }
        AsyncDelegateCommand _updateSelectedCommand;
        public ICommand UpdateSelectedCommand
        {
            get
            {
                if (_updateSelectedCommand == null)
                {
                    _updateSelectedCommand = new AsyncDelegateCommand(param => this.UpdateSelected(),
                        param => Core.ApplicationData.Instance.ActiveDatabase != null && this.GeocacheSelectionCount > 0 && Core.Settings.Default.LiveAPIMemberTypeId > 0);
                }
                return _updateSelectedCommand;
            }
        }
        private async Task UpdateSelected()
        {
            if (Core.ApplicationData.Instance.ActiveDatabase != null)
            {
                await UpdateGeocaches((from a in Core.ApplicationData.Instance.ActiveDatabase.GeocacheCollection where a.Selected select a).ToList());
            }
        }
        AsyncDelegateCommand _updateAllCommand;
        public ICommand UpdateAllCommand
        {
            get
            {
                if (_updateAllCommand == null)
                {
                    _updateAllCommand = new AsyncDelegateCommand(param => this.UpdateAll(),
                        param => Core.ApplicationData.Instance.ActiveDatabase != null && Core.Settings.Default.LiveAPIMemberTypeId > 0);
                }
                return _updateAllCommand;
            }
        }
        private async Task UpdateAll()
        {
            if (Core.ApplicationData.Instance.ActiveDatabase != null)
            {
                await UpdateGeocaches(Core.ApplicationData.Instance.ActiveDatabase.GeocacheCollection);
            }
        }
        private async Task UpdateGeocaches(List<Core.Data.Geocache> gcList)
        {
            if (Core.ApplicationData.Instance.ActiveDatabase != null)
            {
                using (Utils.DataUpdater upd = new Utils.DataUpdater(Core.ApplicationData.Instance.ActiveDatabase))
                {
                    await Task.Run(() =>
                        {
                            LiveAPI.Import.ImportGeocaches(Core.ApplicationData.Instance.ActiveDatabase, (from a in gcList select a.Code).ToList());
                        });
                }
            }
        }


        AsyncDelegateCommand _exportGAPPDEActiveCommand;
        public ICommand ExportGAPPDEActiveCommand
        {
            get
            {
                if (_exportGAPPDEActiveCommand == null)
                {
                    _exportGAPPDEActiveCommand = new AsyncDelegateCommand(param => this.ExportGAPPDEActive(),
                        param => Core.ApplicationData.Instance.ActiveGeocache != null);
                }
                return _exportGAPPDEActiveCommand;
            }
        }
        private async Task ExportGAPPDEActive()
        {
            if (Core.ApplicationData.Instance.ActiveGeocache != null)
            {
                await ExportGAPPDE(new Core.Data.Geocache[] { Core.ApplicationData.Instance.ActiveGeocache }.ToList());
            }
        }
        AsyncDelegateCommand _exportGAPPDESelectedCommand;
        public ICommand ExportGAPPDESelectedCommand
        {
            get
            {
                if (_exportGAPPDESelectedCommand == null)
                {
                    _exportGAPPDESelectedCommand = new AsyncDelegateCommand(param => this.ExportGAPPDESelected(),
                        param => Core.ApplicationData.Instance.ActiveDatabase != null && this.GeocacheSelectionCount > 0);
                }
                return _exportGAPPDESelectedCommand;
            }
        }
        private async Task ExportGAPPDESelected()
        {
            if (Core.ApplicationData.Instance.ActiveDatabase != null)
            {
                await ExportGAPPDE((from a in Core.ApplicationData.Instance.ActiveDatabase.GeocacheCollection where a.Selected select a).ToList());
            }
        }
        AsyncDelegateCommand _exportGAPPDEAllCommand;
        public ICommand ExportGAPPDEAllCommand
        {
            get
            {
                if (_exportGAPPDEAllCommand == null)
                {
                    _exportGAPPDEAllCommand = new AsyncDelegateCommand(param => this.ExportGAPPDEAll(),
                        param => Core.ApplicationData.Instance.ActiveDatabase != null);
                }
                return _exportGAPPDEAllCommand;
            }
        }
        private async Task ExportGAPPDEAll()
        {
            if (Core.ApplicationData.Instance.ActiveDatabase != null)
            {
                await ExportGAPPDE(Core.ApplicationData.Instance.ActiveDatabase.GeocacheCollection);
            }
        }
        private async Task ExportGAPPDE(List<Core.Data.Geocache> gcList)
        {
            GAPPDataExchange.Export exp = new GAPPDataExchange.Export();
            await exp.ExportFile(gcList);
        }



        RelayCommand _exportHTMLActiveCommand;
        public ICommand ExportHTMLActiveCommand
        {
            get
            {
                if (_exportHTMLActiveCommand == null)
                {
                    _exportHTMLActiveCommand = new RelayCommand(param => this.ExportHTMLActive(),
                        param => Core.ApplicationData.Instance.ActiveGeocache != null);
                }
                return _exportHTMLActiveCommand;
            }
        }
        private void ExportHTMLActive()
        {
            if (Core.ApplicationData.Instance.ActiveGeocache != null)
            {
                ExportHTML(new Core.Data.Geocache[] { Core.ApplicationData.Instance.ActiveGeocache }.ToList());
            }
        }
        RelayCommand _exportHTMLSelectedCommand;
        public ICommand ExportHTMLSelectedCommand
        {
            get
            {
                if (_exportHTMLSelectedCommand == null)
                {
                    _exportHTMLSelectedCommand = new RelayCommand(param => this.ExportHTMLSelected(),
                        param => Core.ApplicationData.Instance.ActiveDatabase != null && this.GeocacheSelectionCount > 0);
                }
                return _exportHTMLSelectedCommand;
            }
        }
        private void ExportHTMLSelected()
        {
            if (Core.ApplicationData.Instance.ActiveDatabase != null)
            {
                ExportHTML((from a in Core.ApplicationData.Instance.ActiveDatabase.GeocacheCollection where a.Selected select a).ToList());
            }
        }
        RelayCommand _exportHTMLAllCommand;
        public ICommand ExportHTMLAllCommand
        {
            get
            {
                if (_exportHTMLAllCommand == null)
                {
                    _exportHTMLAllCommand = new RelayCommand(param => this.ExportHTMLAll(),
                        param => Core.ApplicationData.Instance.ActiveDatabase != null);
                }
                return _exportHTMLAllCommand;
            }
        }
        private void ExportHTMLAll()
        {
            if (Core.ApplicationData.Instance.ActiveDatabase != null)
            {
                ExportHTML(Core.ApplicationData.Instance.ActiveDatabase.GeocacheCollection);
            }
        }
        private void ExportHTML(List<Core.Data.Geocache> gcList)
        {
            HTML.ExportWindow dlg = new HTML.ExportWindow(gcList);
            dlg.ShowDialog();
        }




        RelayCommand _exportExcelActiveCommand;
        public ICommand ExportExcelActiveCommand
        {
            get
            {
                if (_exportExcelActiveCommand == null)
                {
                    _exportExcelActiveCommand = new RelayCommand(param => this.ExportExcelActive(),
                        param => Core.ApplicationData.Instance.ActiveGeocache != null);
                }
                return _exportExcelActiveCommand;
            }
        }
        private void ExportExcelActive()
        {
            if (Core.ApplicationData.Instance.ActiveGeocache != null)
            {
                ExportExcel(new Core.Data.Geocache[] { Core.ApplicationData.Instance.ActiveGeocache }.ToList());
            }
        }
        RelayCommand _exportExcelSelectedCommand;
        public ICommand ExportExcelSelectedCommand
        {
            get
            {
                if (_exportExcelSelectedCommand == null)
                {
                    _exportExcelSelectedCommand = new RelayCommand(param => this.ExportExcelSelected(),
                        param => Core.ApplicationData.Instance.ActiveDatabase != null && this.GeocacheSelectionCount > 0);
                }
                return _exportExcelSelectedCommand;
            }
        }
        private void ExportExcelSelected()
        {
            if (Core.ApplicationData.Instance.ActiveDatabase != null)
            {
                ExportExcel((from a in Core.ApplicationData.Instance.ActiveDatabase.GeocacheCollection where a.Selected select a).ToList());
            }
        }
        RelayCommand _exportExcelAllCommand;
        public ICommand ExportExcelAllCommand
        {
            get
            {
                if (_exportExcelAllCommand == null)
                {
                    _exportExcelAllCommand = new RelayCommand(param => this.ExportExcelAll(),
                        param => Core.ApplicationData.Instance.ActiveDatabase != null);
                }
                return _exportExcelAllCommand;
            }
        }
        private void ExportExcelAll()
        {
            if (Core.ApplicationData.Instance.ActiveDatabase != null)
            {
                ExportExcel(Core.ApplicationData.Instance.ActiveDatabase.GeocacheCollection);
            }
        }
        private void ExportExcel(List<Core.Data.Geocache> gcList)
        {
            Excel.ExportWindow dlg = new Excel.ExportWindow(gcList);
            dlg.ShowDialog();
        }






        RelayCommand _exportCacheboxActiveCommand;
        public ICommand ExportCacheboxActiveCommand
        {
            get
            {
                if (_exportCacheboxActiveCommand == null)
                {
                    _exportCacheboxActiveCommand = new RelayCommand(param => this.ExportCacheboxActive(),
                        param => Core.ApplicationData.Instance.ActiveGeocache != null);
                }
                return _exportCacheboxActiveCommand;
            }
        }
        private void ExportCacheboxActive()
        {
            if (Core.ApplicationData.Instance.ActiveGeocache != null)
            {
                ExportCachebox(new Core.Data.Geocache[] { Core.ApplicationData.Instance.ActiveGeocache }.ToList());
            }
        }
        RelayCommand _exportCacheboxSelectedCommand;
        public ICommand ExportCacheboxSelectedCommand
        {
            get
            {
                if (_exportCacheboxSelectedCommand == null)
                {
                    _exportCacheboxSelectedCommand = new RelayCommand(param => this.ExportCacheboxSelected(),
                        param => Core.ApplicationData.Instance.ActiveDatabase != null && this.GeocacheSelectionCount > 0);
                }
                return _exportCacheboxSelectedCommand;
            }
        }
        private void ExportCacheboxSelected()
        {
            if (Core.ApplicationData.Instance.ActiveDatabase != null)
            {
                ExportCachebox((from a in Core.ApplicationData.Instance.ActiveDatabase.GeocacheCollection where a.Selected select a).ToList());
            }
        }
        RelayCommand _exportCacheboxAllCommand;
        public ICommand ExportCacheboxAllCommand
        {
            get
            {
                if (_exportCacheboxAllCommand == null)
                {
                    _exportCacheboxAllCommand = new RelayCommand(param => this.ExportCacheboxAll(),
                        param => Core.ApplicationData.Instance.ActiveDatabase != null);
                }
                return _exportCacheboxAllCommand;
            }
        }
        private void ExportCacheboxAll()
        {
            if (Core.ApplicationData.Instance.ActiveDatabase != null)
            {
                ExportCachebox(Core.ApplicationData.Instance.ActiveDatabase.GeocacheCollection);
            }
        }
        private void ExportCachebox(List<Core.Data.Geocache> gcList)
        {
            Cachebox.ExportWindow dlg = new Cachebox.ExportWindow(gcList);
            dlg.ShowDialog();
        }


        AsyncDelegateCommand _deleteActiveFromImageFolderCommand;
        AsyncDelegateCommand DeleteActiveFromImageFolderCommand
        {
            get
            {
                if (_deleteActiveFromImageFolderCommand==null)
                {
                    _deleteActiveFromImageFolderCommand = new AsyncDelegateCommand(param => DeleteActiveFromImageFolder(),
                        param => Core.ApplicationData.Instance.ActiveGeocache != null);
                }
                return _deleteActiveFromImageFolderCommand;
            }
        }
        public async Task DeleteActiveFromImageFolder()
        {
            await DeleteFromImageFolder(new Core.Data.Geocache[] { Core.ApplicationData.Instance.ActiveGeocache }.ToList());
        }
        AsyncDelegateCommand _deleteSelectedFromImageFolderCommand;
        AsyncDelegateCommand DeleteSelectedFromImageFolderCommand
        {
            get
            {
                if (_deleteSelectedFromImageFolderCommand == null)
                {
                    _deleteSelectedFromImageFolderCommand = new AsyncDelegateCommand(param => DeleteSelectedFromImageFolder(),
                        param => Core.ApplicationData.Instance.ActiveDatabase!=null && this.GeocacheSelectionCount > 0);
                }
                return _deleteSelectedFromImageFolderCommand;
            }
        }
        public async Task DeleteSelectedFromImageFolder()
        {
            await DeleteFromImageFolder((from a in Core.ApplicationData.Instance.ActiveDatabase.GeocacheCollection where a.Selected select a).ToList());
        }
        public async Task DeleteFromImageFolder(List<Core.Data.Geocache> gcList)
        {
            var dlg = new GAPPSF.Dialogs.FolderPickerDialog();
            if (dlg.ShowDialog() == true)
            {
                var exp = new ImageGrabber.Export();
                await exp.DeleteImagesFromFolder(gcList, dlg.SelectedPath);
            }
        }


        RelayCommand _exportOfflineImgActiveCommand;
        public ICommand ExportOfflineImgActiveCommand
        {
            get
            {
                if (_exportOfflineImgActiveCommand == null)
                {
                    _exportOfflineImgActiveCommand = new RelayCommand(param => this.ExportOfflineImgActive(),
                        param => Core.ApplicationData.Instance.ActiveGeocache != null);
                }
                return _exportOfflineImgActiveCommand;
            }
        }
        private void ExportOfflineImgActive()
        {
            if (Core.ApplicationData.Instance.ActiveGeocache != null)
            {
                ExportOfflineImg(new Core.Data.Geocache[] { Core.ApplicationData.Instance.ActiveGeocache }.ToList());
            }
        }
        RelayCommand _exportOfflineImgSelectedCommand;
        public ICommand ExportOfflineImgSelectedCommand
        {
            get
            {
                if (_exportOfflineImgSelectedCommand == null)
                {
                    _exportOfflineImgSelectedCommand = new RelayCommand(param => this.ExportOfflineImgSelected(),
                        param => Core.ApplicationData.Instance.ActiveDatabase != null && this.GeocacheSelectionCount > 0);
                }
                return _exportOfflineImgSelectedCommand;
            }
        }
        private void ExportOfflineImgSelected()
        {
            if (Core.ApplicationData.Instance.ActiveDatabase != null)
            {
                ExportOfflineImg((from a in Core.ApplicationData.Instance.ActiveDatabase.GeocacheCollection where a.Selected select a).ToList());
            }
        }
        RelayCommand _exportOfflineImgAllCommand;
        public ICommand ExportOfflineImgAllCommand
        {
            get
            {
                if (_exportOfflineImgAllCommand == null)
                {
                    _exportOfflineImgAllCommand = new RelayCommand(param => this.ExportOfflineImgAll(),
                        param => Core.ApplicationData.Instance.ActiveDatabase != null);
                }
                return _exportOfflineImgAllCommand;
            }
        }
        private void ExportOfflineImgAll()
        {
            if (Core.ApplicationData.Instance.ActiveDatabase != null)
            {
                ExportOfflineImg(Core.ApplicationData.Instance.ActiveDatabase.GeocacheCollection);
            }
        }
        private void ExportOfflineImg(List<Core.Data.Geocache> gcList)
        {
            ImageGrabber.ExportWindow dlg = new ImageGrabber.ExportWindow(gcList);
            dlg.ShowDialog();
        }



        RelayCommand _exportLocusActiveCommand;
        public ICommand ExportLocusActiveCommand
        {
            get
            {
                if (_exportLocusActiveCommand == null)
                {
                    _exportLocusActiveCommand = new RelayCommand(param => this.ExportLocusActive(),
                        param => Core.ApplicationData.Instance.ActiveGeocache != null);
                }
                return _exportLocusActiveCommand;
            }
        }
        private void ExportLocusActive()
        {
            if (Core.ApplicationData.Instance.ActiveGeocache != null)
            {
                ExportLocus(new Core.Data.Geocache[] { Core.ApplicationData.Instance.ActiveGeocache }.ToList());
            }
        }
        RelayCommand _exportLocusSelectedCommand;
        public ICommand ExportLocusSelectedCommand
        {
            get
            {
                if (_exportLocusSelectedCommand == null)
                {
                    _exportLocusSelectedCommand = new RelayCommand(param => this.ExportLocusSelected(),
                        param => Core.ApplicationData.Instance.ActiveDatabase != null && this.GeocacheSelectionCount > 0);
                }
                return _exportLocusSelectedCommand;
            }
        }
        private void ExportLocusSelected()
        {
            if (Core.ApplicationData.Instance.ActiveDatabase != null)
            {
                ExportLocus((from a in Core.ApplicationData.Instance.ActiveDatabase.GeocacheCollection where a.Selected select a).ToList());
            }
        }
        RelayCommand _exportLocusAllCommand;
        public ICommand ExportLocusAllCommand
        {
            get
            {
                if (_exportLocusAllCommand == null)
                {
                    _exportLocusAllCommand = new RelayCommand(param => this.ExportLocusAll(),
                        param => Core.ApplicationData.Instance.ActiveDatabase != null);
                }
                return _exportLocusAllCommand;
            }
        }
        private void ExportLocusAll()
        {
            if (Core.ApplicationData.Instance.ActiveDatabase != null)
            {
                ExportLocus(Core.ApplicationData.Instance.ActiveDatabase.GeocacheCollection);
            }
        }
        private void ExportLocus(List<Core.Data.Geocache> gcList)
        {
            Locus.ExportWindow dlg = new Locus.ExportWindow(gcList);
            dlg.ShowDialog();
        }


        RelayCommand _exportGarminPOIActiveCommand;
        public ICommand ExportGarminPOIActiveCommand
        {
            get
            {
                if (_exportGarminPOIActiveCommand == null)
                {
                    _exportGarminPOIActiveCommand = new RelayCommand(param => this.ExportGarminPOIActive(),
                        param => Core.ApplicationData.Instance.ActiveGeocache != null);
                }
                return _exportGarminPOIActiveCommand;
            }
        }
        private void ExportGarminPOIActive()
        {
            if (Core.ApplicationData.Instance.ActiveGeocache != null)
            {
                ExportGarminPOI(new Core.Data.Geocache[] { Core.ApplicationData.Instance.ActiveGeocache }.ToList());
            }
        }
        RelayCommand _exportGarminPOISelectedCommand;
        public ICommand ExportGarminPOISelectedCommand
        {
            get
            {
                if (_exportGarminPOISelectedCommand == null)
                {
                    _exportGarminPOISelectedCommand = new RelayCommand(param => this.ExportGarminPOISelected(),
                        param => Core.ApplicationData.Instance.ActiveDatabase != null && this.GeocacheSelectionCount > 0);
                }
                return _exportGarminPOISelectedCommand;
            }
        }
        private void ExportGarminPOISelected()
        {
            if (Core.ApplicationData.Instance.ActiveDatabase != null)
            {
                ExportGarminPOI((from a in Core.ApplicationData.Instance.ActiveDatabase.GeocacheCollection where a.Selected select a).ToList());
            }
        }
        RelayCommand _exportGarminPOIAllCommand;
        public ICommand ExportGarminPOIAllCommand
        {
            get
            {
                if (_exportGarminPOIAllCommand == null)
                {
                    _exportGarminPOIAllCommand = new RelayCommand(param => this.ExportGarminPOIAll(),
                        param => Core.ApplicationData.Instance.ActiveDatabase != null);
                }
                return _exportGarminPOIAllCommand;
            }
        }
        private void ExportGarminPOIAll()
        {
            if (Core.ApplicationData.Instance.ActiveDatabase != null)
            {
                ExportGarminPOI(Core.ApplicationData.Instance.ActiveDatabase.GeocacheCollection);
            }
        }
        private void ExportGarminPOI(List<Core.Data.Geocache> gcList)
        {
            GarminPOI.ExportWindow dlg = new GarminPOI.ExportWindow(Core.ApplicationData.Instance.ActiveDatabase, gcList);
            dlg.ShowDialog();
        }


        AsyncDelegateCommand _exportGAPPActiveCommand;
        public ICommand ExportGAPPActiveCommand
        {
            get
            {
                if (_exportGAPPActiveCommand == null)
                {
                    _exportGAPPActiveCommand = new AsyncDelegateCommand(param => this.ExportGAPPActive(),
                        param => Core.ApplicationData.Instance.ActiveDatabase!=null && Core.ApplicationData.Instance.ActiveGeocache != null);
                }
                return _exportGAPPActiveCommand;
            }
        }
        private async Task ExportGAPPActive()
        {
            if (Core.ApplicationData.Instance.ActiveGeocache != null)
            {
                await ExportGAPP(new Core.Data.Geocache[] { Core.ApplicationData.Instance.ActiveGeocache }.ToList());
            }
        }
        AsyncDelegateCommand _exportGAPPSelectedCommand;
        public ICommand ExportGAPPSelectedCommand
        {
            get
            {
                if (_exportGAPPSelectedCommand == null)
                {
                    _exportGAPPSelectedCommand = new AsyncDelegateCommand(param => this.ExportGAPPSelected(),
                        param => Core.ApplicationData.Instance.ActiveDatabase != null && this.GeocacheSelectionCount>0);
                }
                return _exportGAPPSelectedCommand;
            }
        }
        private async Task ExportGAPPSelected()
        {
            if (Core.ApplicationData.Instance.ActiveGeocache != null)
            {
                await ExportGAPP((from a in Core.ApplicationData.Instance.ActiveDatabase.GeocacheCollection where a.Selected select a).ToList());
            }
        }
        AsyncDelegateCommand _exportGAPPAllCommand;
        public ICommand ExportGAPPAllCommand
        {
            get
            {
                if (_exportGAPPAllCommand == null)
                {
                    _exportGAPPAllCommand = new AsyncDelegateCommand(param => this.ExportGAPPAll(),
                        param => Core.ApplicationData.Instance.ActiveDatabase != null);
                }
                return _exportGAPPAllCommand;
            }
        }
        private async Task ExportGAPPAll()
        {
            if (Core.ApplicationData.Instance.ActiveGeocache != null)
            {
                await ExportGAPP(Core.ApplicationData.Instance.ActiveDatabase.GeocacheCollection.ToList());
            }
        }
        private async Task ExportGAPP(List<Core.Data.Geocache> gcList)
        {
            Microsoft.Win32.SaveFileDialog dlg = new Microsoft.Win32.SaveFileDialog();
            dlg.FileName = ""; // Default file name
            dlg.DefaultExt = ".gpp"; // Default file extension
            dlg.Filter = "GAPP (.gpp)|*.gpp"; // Filter files by extension 

            // Show open file dialog box
            Nullable<bool> result = dlg.ShowDialog();

            // Process open file dialog box results 
            if (result == true)
            {
                GAPPDataStorage.Exporter exp = new GAPPDataStorage.Exporter();
                await exp.ExportAsync(dlg.FileName, Core.ApplicationData.Instance.ActiveDatabase, gcList);
            }
        }


        RelayCommand _exportGDAKActiveCommand;
        public ICommand ExportGDAKActiveCommand
        {
            get
            {
                if (_exportGDAKActiveCommand == null)
                {
                    _exportGDAKActiveCommand = new RelayCommand(param => this.ExportGDAKActive(),
                        param => Core.ApplicationData.Instance.ActiveGeocache != null);
                }
                return _exportGDAKActiveCommand;
            }
        }
        private void ExportGDAKActive()
        {
            if (Core.ApplicationData.Instance.ActiveGeocache != null)
            {
                ExportGDAK(new Core.Data.Geocache[] { Core.ApplicationData.Instance.ActiveGeocache }.ToList());
            }
        }
        RelayCommand _exportGDAKSelectedCommand;
        public ICommand ExportGDAKSelectedCommand
        {
            get
            {
                if (_exportGDAKSelectedCommand == null)
                {
                    _exportGDAKSelectedCommand = new RelayCommand(param => this.ExportGDAKSelected(),
                        param => Core.ApplicationData.Instance.ActiveDatabase != null && this.GeocacheSelectionCount > 0);
                }
                return _exportGDAKSelectedCommand;
            }
        }
        private void ExportGDAKSelected()
        {
            if (Core.ApplicationData.Instance.ActiveDatabase != null)
            {
                ExportGDAK((from a in Core.ApplicationData.Instance.ActiveDatabase.GeocacheCollection where a.Selected select a).ToList());
            }
        }
        RelayCommand _exportGDAKAllCommand;
        public ICommand ExportGDAKAllCommand
        {
            get
            {
                if (_exportGDAKAllCommand == null)
                {
                    _exportGDAKAllCommand = new RelayCommand(param => this.ExportGDAKAll(),
                        param => Core.ApplicationData.Instance.ActiveDatabase != null);
                }
                return _exportGDAKAllCommand;
            }
        }
        private void ExportGDAKAll()
        {
            if (Core.ApplicationData.Instance.ActiveDatabase != null)
            {
                ExportGDAK(Core.ApplicationData.Instance.ActiveDatabase.GeocacheCollection);
            }
        }
        private void ExportGDAK(List<Core.Data.Geocache> gcList)
        {
            GDAK.ExportWindow dlg = new GDAK.ExportWindow(gcList);
            dlg.ShowDialog();
        }



        RelayCommand _exportIGKActiveCommand;
        public ICommand ExportIGKActiveCommand
        {
            get
            {
                if (_exportIGKActiveCommand == null)
                {
                    _exportIGKActiveCommand = new RelayCommand(param => this.ExportIGKActive(),
                        param => Core.ApplicationData.Instance.ActiveGeocache != null);
                }
                return _exportIGKActiveCommand;
            }
        }
        private void ExportIGKActive()
        {
            if (Core.ApplicationData.Instance.ActiveGeocache != null)
            {
                ExportIGK(new Core.Data.Geocache[] { Core.ApplicationData.Instance.ActiveGeocache }.ToList());
            }
        }
        RelayCommand _exportIGKSelectedCommand;
        public ICommand ExportIGKSelectedCommand
        {
            get
            {
                if (_exportIGKSelectedCommand == null)
                {
                    _exportIGKSelectedCommand = new RelayCommand(param => this.ExportIGKSelected(),
                        param => Core.ApplicationData.Instance.ActiveDatabase != null && this.GeocacheSelectionCount > 0);
                }
                return _exportIGKSelectedCommand;
            }
        }
        private void ExportIGKSelected()
        {
            if (Core.ApplicationData.Instance.ActiveDatabase != null)
            {
                ExportIGK((from a in Core.ApplicationData.Instance.ActiveDatabase.GeocacheCollection where a.Selected select a).ToList());
            }
        }
        RelayCommand _exportIGKAllCommand;
        public ICommand ExportIGKAllCommand
        {
            get
            {
                if (_exportIGKAllCommand == null)
                {
                    _exportIGKAllCommand = new RelayCommand(param => this.ExportIGKAll(),
                        param => Core.ApplicationData.Instance.ActiveDatabase != null);
                }
                return _exportIGKAllCommand;
            }
        }
        private void ExportIGKAll()
        {
            if (Core.ApplicationData.Instance.ActiveDatabase != null)
            {
                ExportIGK(Core.ApplicationData.Instance.ActiveDatabase.GeocacheCollection);
            }
        }
        private void ExportIGK(List<Core.Data.Geocache> gcList)
        {
            iGeoKnife.ExportWindow dlg = new iGeoKnife.ExportWindow(gcList);
            dlg.ShowDialog();
        }




        RelayCommand _exportOV2ActiveCommand;
        public ICommand ExportOV2ActiveCommand
        {
            get
            {
                if (_exportOV2ActiveCommand == null)
                {
                    _exportOV2ActiveCommand = new RelayCommand(param => this.ExportOV2Active(),
                        param => Core.ApplicationData.Instance.ActiveGeocache != null);
                }
                return _exportOV2ActiveCommand;
            }
        }
        private void ExportOV2Active()
        {
            if (Core.ApplicationData.Instance.ActiveGeocache != null)
            {
                ExportOV2(new Core.Data.Geocache[] { Core.ApplicationData.Instance.ActiveGeocache }.ToList());
            }
        }
        RelayCommand _exportOV2SelectedCommand;
        public ICommand ExportOV2SelectedCommand
        {
            get
            {
                if (_exportOV2SelectedCommand == null)
                {
                    _exportOV2SelectedCommand = new RelayCommand(param => this.ExportOV2Selected(),
                        param => Core.ApplicationData.Instance.ActiveDatabase != null && this.GeocacheSelectionCount > 0);
                }
                return _exportOV2SelectedCommand;
            }
        }
        private void ExportOV2Selected()
        {
            if (Core.ApplicationData.Instance.ActiveDatabase != null)
            {
                ExportOV2((from a in Core.ApplicationData.Instance.ActiveDatabase.GeocacheCollection where a.Selected select a).ToList());
            }
        }
        RelayCommand _exportOV2AllCommand;
        public ICommand ExportOV2AllCommand
        {
            get
            {
                if (_exportOV2AllCommand == null)
                {
                    _exportOV2AllCommand = new RelayCommand(param => this.ExportOV2All(),
                        param => Core.ApplicationData.Instance.ActiveDatabase != null);
                }
                return _exportOV2AllCommand;
            }
        }
        private void ExportOV2All()
        {
            if (Core.ApplicationData.Instance.ActiveDatabase != null)
            {
                ExportOV2(Core.ApplicationData.Instance.ActiveDatabase.GeocacheCollection);
            }
        }
        private void ExportOV2(List<Core.Data.Geocache> gcList)
        {
            OV2.ExportWindow dlg = new OV2.ExportWindow(gcList);
            dlg.ShowDialog();
        }



        RelayCommand _exportGPXActiveCommand;
        public ICommand ExportGPXActiveCommand
        {
            get
            {
                if (_exportGPXActiveCommand == null)
                {
                    _exportGPXActiveCommand = new RelayCommand(param => this.ExportGPXActive(),
                        param => Core.ApplicationData.Instance.ActiveGeocache != null);
                }
                return _exportGPXActiveCommand;
            }
        }
        private void ExportGPXActive()
        {
            if (Core.ApplicationData.Instance.ActiveGeocache != null)
            {
                ExportGPX(new Core.Data.Geocache[] { Core.ApplicationData.Instance.ActiveGeocache }.ToList());
            }
        }
        RelayCommand _exportGPXSelectedCommand;
        public ICommand ExportGPXSelectedCommand
        {
            get
            {
                if (_exportGPXSelectedCommand == null)
                {
                    _exportGPXSelectedCommand = new RelayCommand(param => this.ExportGPXSelected(),
                        param => Core.ApplicationData.Instance.ActiveDatabase != null && this.GeocacheSelectionCount > 0);
                }
                return _exportGPXSelectedCommand;
            }
        }
        private void ExportGPXSelected()
        {
            if (Core.ApplicationData.Instance.ActiveDatabase != null)
            {
                ExportGPX((from a in Core.ApplicationData.Instance.ActiveDatabase.GeocacheCollection where a.Selected select a).ToList());
            }
        }
        RelayCommand _exportGPXAllCommand;
        public ICommand ExportGPXAllCommand
        {
            get
            {
                if (_exportGPXAllCommand == null)
                {
                    _exportGPXAllCommand = new RelayCommand(param => this.ExportGPXAll(),
                        param => Core.ApplicationData.Instance.ActiveDatabase != null);
                }
                return _exportGPXAllCommand;
            }
        }
        private void ExportGPXAll()
        {
            if (Core.ApplicationData.Instance.ActiveDatabase != null)
            {
                ExportGPX(Core.ApplicationData.Instance.ActiveDatabase.GeocacheCollection);
            }
        }
        private void ExportGPX(List<Core.Data.Geocache> gcList)
        {
            GPX.ExportWindow dlg = new GPX.ExportWindow(gcList);
            dlg.ShowDialog();
        }


        AsyncDelegateCommand _exportKMLActiveCommand;
        public ICommand ExportKMLActiveCommand
        {
            get
            {
                if (_exportKMLActiveCommand == null)
                {
                    _exportKMLActiveCommand = new AsyncDelegateCommand(param => this.ExportKMLActive(),
                        param => Core.ApplicationData.Instance.ActiveGeocache != null);
                }
                return _exportKMLActiveCommand;
            }
        }
        async private Task ExportKMLActive()
        {
            if (Core.ApplicationData.Instance.ActiveGeocache != null)
            {
                await ExportKML(new Core.Data.Geocache[] { Core.ApplicationData.Instance.ActiveGeocache }.ToList());
            }
        }
        AsyncDelegateCommand _exportKMLSelectedCommand;
        public ICommand ExportKMLSelectedCommand
        {
            get
            {
                if (_exportKMLSelectedCommand == null)
                {
                    _exportKMLSelectedCommand = new AsyncDelegateCommand(param => this.ExportKMLSelected(),
                        param => Core.ApplicationData.Instance.ActiveDatabase != null && this.GeocacheSelectionCount>0);
                }
                return _exportKMLSelectedCommand;
            }
        }
        async private Task ExportKMLSelected()
        {
            if (Core.ApplicationData.Instance.ActiveDatabase != null)
            {
                await ExportKML((from a in Core.ApplicationData.Instance.ActiveDatabase.GeocacheCollection where a.Selected select a).ToList());
            }
        }
        AsyncDelegateCommand _exportKMLAllCommand;
        public ICommand ExportKMLAllCommand
        {
            get
            {
                if (_exportKMLAllCommand == null)
                {
                    _exportKMLAllCommand = new AsyncDelegateCommand(param => this.ExportKMLAll(),
                        param => Core.ApplicationData.Instance.ActiveDatabase != null);
                }
                return _exportKMLAllCommand;
            }
        }
        async private Task ExportKMLAll()
        {
            if (Core.ApplicationData.Instance.ActiveDatabase != null)
            {
                await ExportKML(Core.ApplicationData.Instance.ActiveDatabase.GeocacheCollection);
            }
        }
        async private Task ExportKML(List<Core.Data.Geocache> gcList)
        {
            KML.Export p = new KML.Export();
            await p.PerformExportAsync(gcList);
        }


        RelayCommand _importMunzeeAfxCommand;
        public ICommand ImportMunzeeAfxCommand
        {
            get
            {
                if (_importMunzeeAfxCommand == null)
                {
                    _importMunzeeAfxCommand = new RelayCommand(param => this.ImportMunzeeAfx(),
                        param => Core.ApplicationData.Instance.ActiveDatabase != null);
                }
                return _importMunzeeAfxCommand;
            }
        }
        public void ImportMunzeeAfx()
        {
            Munzee.DfxAtImportWindow dlg = new Munzee.DfxAtImportWindow();
            dlg.ShowDialog();
        }


        AsyncDelegateCommand _importGSAKCommand;
        public ICommand ImportGSAKCommand
        {
            get
            {
                if (_importGSAKCommand == null)
                {
                    _importGSAKCommand = new AsyncDelegateCommand(param => this.ImportGSAKDatabase(),
                        param => Core.ApplicationData.Instance.ActiveDatabase != null);
                }
                return _importGSAKCommand;
            }
        }

        async private Task ImportGSAKDatabase()
        {
            if (Core.ApplicationData.Instance.ActiveDatabase != null)
            {
                Microsoft.Win32.OpenFileDialog dlg = new Microsoft.Win32.OpenFileDialog();
                dlg.FileName = ""; // Default file name
                dlg.DefaultExt = ".db3"; // Default file extension
                dlg.Filter = "GSAK database (sqlite.db3)|sqlite.db3"; // Filter files by extension 

                // Show open file dialog box
                Nullable<bool> result = dlg.ShowDialog();

                // Process open file dialog box results 
                if (result == true)
                {
                    // Open document 
                    await GSAK.Importer.PerformAction(Core.ApplicationData.Instance.ActiveDatabase, dlg.FileName);
                }
            }
        }


        AsyncDelegateCommand _importGappCommand;
        public ICommand ImportGAPPCommand
        {
            get
            {
                if (_importGappCommand == null)
                {
                    _importGappCommand = new AsyncDelegateCommand(param => this.ImportGAPPDatabase(),
                        param => Core.ApplicationData.Instance.ActiveDatabase!=null);
                }
                return _importGappCommand;
            }
        }

        async private Task ImportGAPPDatabase()
        {
            if (Core.ApplicationData.Instance.ActiveDatabase != null)
            {
                Microsoft.Win32.OpenFileDialog dlg = new Microsoft.Win32.OpenFileDialog();
                dlg.FileName = ""; // Default file name
                dlg.DefaultExt = ".gpp"; // Default file extension
                dlg.Filter = "GAPP Data Storage (.gpp)|*.gpp"; // Filter files by extension 

                // Show open file dialog box
                Nullable<bool> result = dlg.ShowDialog();

                // Process open file dialog box results 
                if (result == true)
                {
                    // Open document 
                    await GAPPDataStorage.Importer.PerformAction(Core.ApplicationData.Instance.ActiveDatabase, dlg.FileName);
                }
            }
        }

        AsyncDelegateCommand _newCommand;
        public ICommand NewCommand
        {
            get
            {
                if (_newCommand == null)
                {
                    _newCommand = new AsyncDelegateCommand(param => this.NewDatabase());
                }
                return _newCommand;
            }
        }

        async private Task NewDatabase()
        {
            try
            {
                Microsoft.Win32.SaveFileDialog dlg = new Microsoft.Win32.SaveFileDialog();
                dlg.FileName = ""; // Default file name
                dlg.DefaultExt = ".gsf"; // Default file extension
                dlg.Filter = "GAPP SF (.gsf)|*.gsf"; // Filter files by extension 

                // Show open file dialog box
                Nullable<bool> result = dlg.ShowDialog();

                // Process open file dialog box results 
                if (result == true)
                {
                    // Open document 
                    string filename = dlg.FileName;
                    if (File.Exists(filename))
                    {
                        File.Delete(filename);
                    }
                    Core.Storage.Database db = new Core.Storage.Database(filename);
                    bool success = await db.InitializeAsync();
                    if (success)
                    {
                        Core.ApplicationData.Instance.Databases.Add(db);
                        Core.ApplicationData.Instance.ActiveDatabase = db;
                    }
                    else
                    {
                        db.Dispose();
                    }
                }
            }
            catch(Exception e)
            {
                Core.ApplicationData.Instance.Logger.AddLog(this, e);
            }
        }

        RelayCommand _liveApiAuthorizeCommand;
        public ICommand LiveApiAuthorizeCommand
        {
            get
            {
                if (_liveApiAuthorizeCommand == null)
                {
                    _liveApiAuthorizeCommand = new RelayCommand(param => this.LiveAPIAuthorize());
                }
                return _liveApiAuthorizeCommand;
            }
        }
        public void LiveAPIAuthorize()
        {
            LiveAPI.GeocachingLiveV6.Authorize(true);
        }

        RelayCommand _liveApiAuthorizeTestSiteCommand;
        public ICommand LiveApiAuthorizeTestSiteCommand
        {
            get
            {
                if (_liveApiAuthorizeTestSiteCommand == null)
                {
                    _liveApiAuthorizeTestSiteCommand = new RelayCommand(param => this.LiveAPIAuthorizeTestSite());
                }
                return _liveApiAuthorizeTestSiteCommand;
            }
        }
        public void LiveAPIAuthorizeTestSite()
        {
            LiveAPI.GeocachingLiveV6.Authorize(true, true);
        }


        private RelayCommand _removeDatabaseCommand = null;
        public RelayCommand RemoveDatabaseCommand
        {
            get
            {
                if (_removeDatabaseCommand == null)
                {
                    _removeDatabaseCommand = new RelayCommand(param => RemoveDatabase(),
                        param => Core.ApplicationData.Instance.ActiveDatabase!=null);
                }
                return _removeDatabaseCommand;
            }
        }
        public void RemoveDatabase()
        {
            Core.Storage.Database db = Core.ApplicationData.Instance.ActiveDatabase;
            if (db != null)
            {
                Core.ApplicationData.Instance.ActiveDatabase = null;
                Core.ApplicationData.Instance.Databases.Remove(db);
            }
        }


        AsyncDelegateCommand _openCommand;
        public ICommand OpenCommand
        {
            get
            {
                if (_openCommand == null)
                {
                    _openCommand = new AsyncDelegateCommand(param => this.OpenDatabase());
                }
                return _openCommand;
            }
        }

        AsyncDelegateCommand _importGeocacheDistanceCommand;
        public ICommand ImportGeocacheDistanceCommand
        {
            get
            {
                if (_importGeocacheDistanceCommand == null)
                {
                    _importGeocacheDistanceCommand = new AsyncDelegateCommand(param => this.ImportGeocacheDistance(), 
                        param => Core.ApplicationData.Instance.ActiveDatabase!=null);
                }
                return _importGeocacheDistanceCommand;
            }
        }
        public async Task ImportGeocacheDistance()
        {
            GlobalcachingEU.Import imp = new GlobalcachingEU.Import();
            await imp.ImportGeocacheDistanceAsync(Core.ApplicationData.Instance.ActiveDatabase);
        }


        AsyncDelegateCommand _importGeocacheFavoritesCommand;
        public ICommand ImportGeocacheFavoritesCommand
        {
            get
            {
                if (_importGeocacheFavoritesCommand == null)
                {
                    _importGeocacheFavoritesCommand = new AsyncDelegateCommand(param => this.ImportGeocacheFavorites(),
                        param => Core.ApplicationData.Instance.ActiveDatabase != null);
                }
                return _importGeocacheFavoritesCommand;
            }
        }
        public async Task ImportGeocacheFavorites()
        {
            GlobalcachingEU.Import imp = new GlobalcachingEU.Import();
            await imp.ImportFavoritesAsync(Core.ApplicationData.Instance.ActiveDatabase);
        }


        AsyncDelegateCommand _importOKAPIMyFindsCommand;
        public ICommand ImportOKAPIMyFindsCommand
        {
            get
            {
                if (_importOKAPIMyFindsCommand == null)
                {
                    _importOKAPIMyFindsCommand = new AsyncDelegateCommand(param => this.ImportOKAPIMyFinds(),
                        param => Core.ApplicationData.Instance.ActiveDatabase != null && OKAPI.SiteManager.Instance.ActiveSite != null);
                }
                return _importOKAPIMyFindsCommand;
            }
        }
        public async Task ImportOKAPIMyFinds()
        {
            if (OKAPI.SiteManager.Instance.CheckAPIAccess())
            {
                OKAPI.Import imp = new OKAPI.Import();
                await imp.ImportMyLogsWithCachesAsync(Core.ApplicationData.Instance.ActiveDatabase, OKAPI.SiteManager.Instance.ActiveSite);
            }
        }


        RelayCommand _purgeLogsCommand;
        public ICommand PurgeLogsCommand
        {
            get
            {
                if (_purgeLogsCommand == null)
                {
                    _purgeLogsCommand = new RelayCommand(param => this.PerformPurgeLogs(), param => Core.ApplicationData.Instance.ActiveDatabase != null);
                }
                return _purgeLogsCommand;
            }
        }
        public void PerformPurgeLogs()
        {
            PurgeLogs.PurgerWindow dlg = new PurgeLogs.PurgerWindow();
            dlg.ShowDialog();
        }


        async private Task OpenDatabase()
        {
            Microsoft.Win32.OpenFileDialog dlg = new Microsoft.Win32.OpenFileDialog();
            dlg.FileName = ""; // Default file name
            dlg.DefaultExt = ".gsf"; // Default file extension
            dlg.Filter = "GAPP SF (.gsf)|*.gsf"; // Filter files by extension 

            // Show open file dialog box
            Nullable<bool> result = dlg.ShowDialog();

            // Process open file dialog box results 
            if (result == true)
            {
                // Open document 
                string filename = dlg.FileName;
                Core.Storage.Database db = new Core.Storage.Database(filename);
                bool success = await db.InitializeAsync();
                if (success)
                {
                    Core.ApplicationData.Instance.Databases.Add(db);
                    Core.ApplicationData.Instance.ActiveDatabase = db;
                }
                else
                {
                    db.Dispose();
                }
            }
        }


        private void MenuItem_Click_10(object sender, RoutedEventArgs e)
        {
            Window w = new FeatureWindow(new UIControls.ApplicationDataInfo());
            w.Owner = this;
            w.Show();
        }


        ForAllGeocachesCommand _assignCityNameActiveCommand;
        public ICommand AssignCityNameActiveCommand
        {
            get
            {
                if (_assignCityNameActiveCommand == null)
                {
                    _assignCityNameActiveCommand = new ForAllGeocachesCommand(param => this.assignCityNameActive(param), true);
                }
                return _assignCityNameActiveCommand;
            }
        }
        private void assignCityNameActive(Core.Data.Geocache gc)
        {
            if (gc==Core.ApplicationData.Instance.ActiveGeocache)
            {
                gc.City = Utils.Geocoder.GetCityName(gc.Lat, gc.Lon);
            }
        }

        ForSelectedGeocachesCommand _assignCityNameSelectedCommand;
        public ICommand AssignCityNameSelectedCommand
        {
            get
            {
                if (_assignCityNameSelectedCommand == null)
                {
                    _assignCityNameSelectedCommand = new ForSelectedGeocachesCommand(param => this.assignCityNameSelected(param), true);
                }
                return _assignCityNameSelectedCommand;
            }
        }
        private void assignCityNameSelected(Core.Data.Geocache gc)
        {
            System.Threading.Thread.Sleep(1000); //OSM user policy
            gc.City = Utils.Geocoder.GetCityName(gc.Lat, gc.Lon);
        }

        ForAllGeocachesCommand _assignCityNameAllCommand;
        public ICommand AssignCityNameAllCommand
        {
            get
            {
                if (_assignCityNameAllCommand == null)
                {
                    _assignCityNameAllCommand = new ForAllGeocachesCommand(param => this.assignCityNameAll(param));
                }
                return _assignCityNameAllCommand;
            }
        }
        private void assignCityNameAll(Core.Data.Geocache gc)
        {
            System.Threading.Thread.Sleep(1000); //OSM user policy
            gc.City = Utils.Geocoder.GetCityName(gc.Lat, gc.Lon);
        }


        ForAllGeocachesCommand _selectWithUserWPCommand;
        public ICommand SelectWithUserWPCommand
        {
            get
            {
                if (_selectWithUserWPCommand == null)
                {
                    _selectWithUserWPCommand = new ForAllGeocachesCommand(param => this.selectWithUserWP(param));
                }
                return _selectWithUserWPCommand;
            }
        }
        private void selectWithUserWP(Core.Data.Geocache gc)
        {
           gc.Selected = Utils.DataAccess.GetUserWaypointsFromGeocache(gc.Database,gc.Code).Count() > 0;
        }


        ForAllGeocachesCommand _selectMultipleFoundsCommand;
        public ICommand SelectMultipleFoundsCommand
        {
            get
            {
                if (_selectMultipleFoundsCommand == null)
                {
                    _selectMultipleFoundsCommand = new ForAllGeocachesCommand(param => this.selectMultipleFounds(param));
                }
                return _selectMultipleFoundsCommand;
            }
        }
        private void selectMultipleFounds(Core.Data.Geocache gc)
        {
            if (gc.Found)
            {
                Core.Data.AccountInfo ai = Core.ApplicationData.Instance.AccountInfos.GetAccountInfo(gc.Code.Substring(0, 2));
                if (ai != null)
                {
                    string me = ai.AccountName;
                    gc.Selected = (from a in Utils.DataAccess.GetLogs(gc.Database, gc.Code) where a.LogType.AsFound && a.Finder == me select a).Count() > 1;
                }
            }
            else
            {
                gc.Selected = false;
            }
        }


        ForAllGeocachesCommand _selectOwnCommand;
        public ICommand SelectOwnCommand
        {
            get
            {
                if (_selectOwnCommand == null)
                {
                    _selectOwnCommand = new ForAllGeocachesCommand(param => this.selectOwn(param));
                }
                return _selectOwnCommand;
            }
        }
        private void selectOwn(Core.Data.Geocache gc)
        {
            gc.Selected = gc.IsOwn;
        }


        ForAllGeocachesCommand _selectCorrectCoordsCommand;
        public ICommand SelectCorrectCoordsCommand
        {
            get
            {
                if (_selectCorrectCoordsCommand == null)
                {
                    _selectCorrectCoordsCommand = new ForAllGeocachesCommand(param => this.selectCorrectCoords(param));
                }
                return _selectCorrectCoordsCommand;
            }
        }
        private void selectCorrectCoords(Core.Data.Geocache gc)
        {
            gc.Selected = gc.ContainsCustomLatLon;
        }



        ForAllGeocachesCommand _selectNotesCommand;
        public ICommand SelectNotesCommand
        {
            get
            {
                if (_selectNotesCommand == null)
                {
                    _selectNotesCommand = new ForAllGeocachesCommand(param => this.selectNotes(param));
                }
                return _selectNotesCommand;
            }
        }
        private void selectNotes(Core.Data.Geocache gc)
        {
            gc.Selected = gc.ContainsNote;
        }


        ForAllGeocachesCommand _selectFlaggedCommand;
        public ICommand SelectFlaggedCommand
        {
            get
            {
                if (_selectFlaggedCommand == null)
                {
                    _selectFlaggedCommand = new ForAllGeocachesCommand(param => this.selectFlagged(param));
                }
                return _selectFlaggedCommand;
            }
        }
        private void selectFlagged(Core.Data.Geocache gc)
        {
            gc.Selected = gc.Flagged;
        }

        ForAllGeocachesCommand _selectAvailableCommand;
        public ICommand SelectAvailableCommand
        {
            get
            {
                if (_selectAvailableCommand == null)
                {
                    _selectAvailableCommand = new ForAllGeocachesCommand(param => this.selectAvailable(param));
                }
                return _selectAvailableCommand;
            }
        }
        private void selectAvailable(Core.Data.Geocache gc)
        {
            gc.Selected = gc.Available;
        }

        ForAllGeocachesCommand _selectFoundCommand;
        public ICommand SelectFoundCommand
        {
            get
            {
                if (_selectFoundCommand == null)
                {
                    _selectFoundCommand = new ForAllGeocachesCommand(param => this.selectFound(param));
                }
                return _selectFoundCommand;
            }
        }
        private void selectFound(Core.Data.Geocache gc)
        {
            gc.Selected = gc.Found;
        }

        ForAllGeocachesCommand _selectNotFoundCommand;
        public ICommand SelectNotFoundCommand
        {
            get
            {
                if (_selectNotFoundCommand == null)
                {
                    _selectNotFoundCommand = new ForAllGeocachesCommand(param => this.selectNotFound(param));
                }
                return _selectNotFoundCommand;
            }
        }
        private void selectNotFound(Core.Data.Geocache gc)
        {
            gc.Selected = !gc.Found;
        }


        ForAllGeocachesCommand _selectInvertCommand;
        public ICommand SelectInvertCommand
        {
            get
            {
                if (_selectInvertCommand == null)
                {
                    _selectInvertCommand = new ForAllGeocachesCommand(param => this.selectInvert(param));
                }
                return _selectInvertCommand;
            }
        }
        private void selectInvert(Core.Data.Geocache gc)
        {
            gc.Selected = !gc.Selected;
        }

        ForAllGeocachesCommand _selectArchivedCommand;
        public ICommand SelectArchivedCommand
        {
            get
            {
                if (_selectArchivedCommand == null)
                {
                    _selectArchivedCommand = new ForAllGeocachesCommand(param => this.selectIfArchived(param));
                }
                return _selectArchivedCommand;
            }
        }
        private void selectIfArchived(Core.Data.Geocache gc)
        {
            gc.Selected = gc.Archived;
        }

        ForAllGeocachesCommand _selectAllCommand;
        public ICommand SelectAllCommand
        {
            get
            {
                if (_selectAllCommand == null)
                {
                    _selectAllCommand = new ForAllGeocachesCommand(param => this.selectAll(param));
                }
                return _selectAllCommand;
            }
        }
        private void selectAll(Core.Data.Geocache gc)
        {
            gc.Selected = true;
        }

        ForAllGeocachesCommand _selectNoneCommand;
        public ICommand SelectNoneCommand
        {
            get
            {
                if (_selectNoneCommand == null)
                {
                    _selectNoneCommand = new ForAllGeocachesCommand(param => this.selectNone(param));
                }
                return _selectNoneCommand;
            }
        }
        private void selectNone(Core.Data.Geocache gc)
        {
            gc.Selected = false;
        }


        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (Core.ApplicationData.Instance.UIIsIdle)
            {
                StringBuilder sb = new StringBuilder();
                foreach (var w in this.OwnedWindows)
                {
                    if (w is FeatureWindow)
                    {
                        sb.AppendLine(getFeatureControlSetting((w as FeatureWindow).featureContainer.FeatureControl));
                    }
                }
                Core.Settings.Default.MainWindowWindowFeature = sb.ToString();

                if (leftPanelContent.FeatureControl is IDisposable) (leftPanelContent.FeatureControl as IDisposable).Dispose();
                if (topPanelContent.FeatureControl is IDisposable) (topPanelContent.FeatureControl as IDisposable).Dispose();
                if (bottomLeftPanelContent.FeatureControl is IDisposable) (topPanelContent.FeatureControl as IDisposable).Dispose();
                if (bottomRightPanelContent.FeatureControl is IDisposable) (topPanelContent.FeatureControl as IDisposable).Dispose();
                if (expandedPanelContent.FeatureControl is IDisposable) (topPanelContent.FeatureControl as IDisposable).Dispose();
            }
            else
            {
                e.Cancel = true;
            }
        }


        private void MenuItem_Click_7(object sender, RoutedEventArgs e)
        {
            Window w = new FeatureWindow(new UIControls.GMap.GoogleMap());
            w.Owner = this;
            w.Show();
        }

        private void MenuItem_Click_8(object sender, RoutedEventArgs e)
        {
            Shapefiles.SettingsWindow dlg = new Shapefiles.SettingsWindow();
            dlg.ShowDialog();
        }

        private void MenuItem_Click_9(object sender, RoutedEventArgs e)
        {
            Window w = new FeatureWindow(new UIControls.OfflineImages.Control());
            w.Owner = this;
            w.Show();
        }

        private void Window_ContentRendered(object sender, EventArgs e)
        {
            Dispatcher.BeginInvoke(new Action(async () => await initializeApplicationAsync()), DispatcherPriority.ContextIdle, null);
        }

        private void TextBlock_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (Core.ApplicationData.Instance.ActiveGeocache!=null && !string.IsNullOrEmpty(Core.ApplicationData.Instance.ActiveGeocache.Url))
            {
                System.Diagnostics.Process.Start(Core.ApplicationData.Instance.ActiveGeocache.Url);
            }
        }

        private void MenuItem_Click_11(object sender, RoutedEventArgs e)
        {
            Window w = new FeatureWindow(new UIControls.GeocacheFilter.Control());
            w.Owner = this;
            w.Show();
        }

        private void MenuItem_Click_12(object sender, RoutedEventArgs e)
        {
            Dialogs.ShortCutAssignmentWindow dlg = new Dialogs.ShortCutAssignmentWindow();
            if (dlg.ShowDialog()==true)
            {
                updateShortCutKeyAssignment();
            }
        }

        private void menu27_Click(object sender, RoutedEventArgs e)
        {
            Window w = new FeatureWindow(new UIControls.IgnoreGeocaches.Control());
            w.Owner = this;
            w.Show();
        }

        private void menua27_Click(object sender, RoutedEventArgs e)
        {
            Window w = new FeatureWindow(new UIControls.DebugLogView.Control());
            w.Owner = this;
            w.Show();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            popup.IsOpen = false;
        }

        private void menub27_Click(object sender, RoutedEventArgs e)
        {
            Window w = new FeatureWindow(new UIControls.ActionBuilder.Control());
            w.Owner = this;
            w.Show();
        }

        private void menud27_Click(object sender, RoutedEventArgs e)
        {
            Dialogs.ActionSequenceWindow dlg = new Dialogs.ActionSequenceWindow();
            dlg.ShowDialog();
        }

        private void menud37_Click(object sender, RoutedEventArgs e)
        {
            Dialogs.GCComBookmarkWindow dlg = new Dialogs.GCComBookmarkWindow();
            dlg.ShowDialog();
        }

        private void menux27_Click(object sender, RoutedEventArgs e)
        {
            Dialogs.AboutWindow dlg = new Dialogs.AboutWindow(this);
            dlg.ShowDialog();
        }

        private void menuexit_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void menux25_Click(object sender, RoutedEventArgs e)
        {
            Window w = new FeatureWindow(new UIControls.Maps.Control(new MapProviders.MapControlFactoryGoogle()));
            w.Owner = this;
            w.Show();
        }

        private void menuy25_Click(object sender, RoutedEventArgs e)
        {
            Window w = new FeatureWindow(new UIControls.Maps.Control(new MapProviders.MapControlFactoryOSMOnline()));
            w.Owner = this;
            w.Show();
        }

        private void menuz25_Click(object sender, RoutedEventArgs e)
        {
            Window w = new FeatureWindow(new UIControls.Maps.Control(new MapProviders.MapControlFactoryOSMOffline()));
            w.Owner = this;
            w.Show();
        }

        private void menut37_Click(object sender, RoutedEventArgs e)
        {
            GPX.ImportPQWindow dlg = new GPX.ImportPQWindow();
            dlg.ShowDialog();
        }

        private void menuf25_Click(object sender, RoutedEventArgs e)
        {
            Window w = new FeatureWindow(new UIControls.GoogleEarth.Control());
            w.Owner = this;
            w.Show();
        }

        private void menua37_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                System.Diagnostics.Process.Start("http://gapp.globalcaching.eu/Forum.aspx");
            }
            catch(Exception ex)
            {
                Core.ApplicationData.Instance.Logger.AddLog(this, ex);
            }
        }

        private void menua47_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                System.Diagnostics.Process.Start("http://gapp.globalcaching.eu/Instructionvideos.aspx");
            }
            catch (Exception ex)
            {
                Core.ApplicationData.Instance.Logger.AddLog(this, ex);
            }
        }

        private void menua57_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                System.Diagnostics.Process.Start("http://gapp.globalcaching.eu/Help/Nederlandstaligehelp.aspx");
            }
            catch (Exception ex)
            {
                Core.ApplicationData.Instance.Logger.AddLog(this, ex);
            }
        }

        private void menud87_Click(object sender, RoutedEventArgs e)
        {
            if (Core.ApplicationData.Instance.ActiveDatabase!=null)
            {
                using (Utils.DataUpdater upd = new Utils.DataUpdater(Core.ApplicationData.Instance.ActiveDatabase))
                {
                    foreach(var gc in Core.ApplicationData.Instance.ActiveDatabase.GeocacheCollection)
                    {
                        gc.Selected = Favorites.Manager.Instance.GeocacheFavorited(gc.Code);
                    }
                }
            }
        }

        private void menub07_Click(object sender, RoutedEventArgs e)
        {
            Window w = new FeatureWindow(new UIControls.Attachement.Control());
            w.Owner = this;
            w.Show();
        }

        private void menubi7_Click(object sender, RoutedEventArgs e)
        {
            Window w = new FeatureWindow(new UIControls.CAR.Control());
            w.Owner = this;
            w.Show();
        }

        private void menubk7_Click(object sender, RoutedEventArgs e)
        {
            Window w = new FeatureWindow(new UIControls.FormulaSolver.Control());
            w.Owner = this;
            w.Show();
        }

        private void menubkb7_Click(object sender, RoutedEventArgs e)
        {
            Window w = new FeatureWindow(new UIControls.GCEditor.Control());
            w.Owner = this;
            w.Show();
        }

        private void menubkb8_Click(object sender, RoutedEventArgs e)
        {
            Window w = new FeatureWindow(new UIControls.WPEditor.Control());
            w.Owner = this;
            w.Show();
        }

        private void menubbb8_Click(object sender, RoutedEventArgs e)
        {
            Window w = new FeatureWindow(new UIControls.LogViewer.Control());
            w.Owner = this;
            w.Show();
        }

        private void MenuItem_Click_1c2(object sender, RoutedEventArgs e)
        {
            Dialogs.BackupSettingsWindow dlg = new Dialogs.BackupSettingsWindow();
            dlg.ShowDialog();
        }

        private void menuqqb8_Click(object sender, RoutedEventArgs e)
        {
            Window w = new FeatureWindow(new UIControls.NotesEditor.Control());
            w.Owner = this;
            w.Show();
        }

        private void menuzzb8_Click(object sender, RoutedEventArgs e)
        {
            Window w = new FeatureWindow(new UIControls.GeocacheCollection.Control());
            w.Owner = this;
            w.Show();
        }

        private void menum27_Click(object sender, RoutedEventArgs e)
        {
            GlobalcachingEU.AutoUpdaterWindow dlg = new GlobalcachingEU.AutoUpdaterWindow();
            dlg.ShowDialog();
        }

        private void MenuItem_Click_1v0(object sender, RoutedEventArgs e)
        {
            LiveAPI.SettingsWindow dlg = new LiveAPI.SettingsWindow();
            dlg.ShowDialog();
        }

        private void MenuItem_Click_2v4(object sender, RoutedEventArgs e)
        {
            Munzee.SettingsWindow dlg = new Munzee.SettingsWindow();
            dlg.ShowDialog();
        }

        private void MenuItem_Click_2v5(object sender, RoutedEventArgs e)
        {
            GeoSpy.SettingsWindow dlg = new GeoSpy.SettingsWindow();
            dlg.ShowDialog();
        }

        private void menudb7_Click(object sender, RoutedEventArgs e)
        {
            Dialogs.SelectAreaWindow dlg = new Dialogs.SelectAreaWindow();
            dlg.ShowDialog();
        }

        private void MenuItem_Click_2v8(object sender, RoutedEventArgs e)
        {
            Dialogs.RestoreSettingsWindow dlg = new Dialogs.RestoreSettingsWindow();
            if (dlg.ShowDialog()==true)
            {
                RestartApplication();
            }
        }

        public void RestartApplication()
        {
            //automatic restart is not working unless:
            //- including WinForms and perform application.restart
            //- create batch file with delay and execute before shutdown
            try
            {
                StringBuilder sb = new StringBuilder();
                sb.AppendLine("sleep 5");
                sb.AppendLine(string.Format("start \"\" \"{0}\"", Application.ResourceAssembly.Location));
                string fn = System.IO.Path.Combine(System.Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "restart.bat");
                File.WriteAllText(fn, sb.ToString());
                Process.Start(fn);
            }
            catch
            {

            }
            Application.Current.Shutdown();
        }

        private void menud50h_Click(object sender, RoutedEventArgs e)
        {
            OKAPI.SettingsWindow dlg = new OKAPI.SettingsWindow();
            dlg.ShowDialog();
        }

        private void menudf0h_Click(object sender, RoutedEventArgs e)
        {
            if (OKAPI.SiteManager.Instance.CheckAPIAccess())
            {
                OKAPI.ImportByBBoxWindow dlg = new OKAPI.ImportByBBoxWindow();
                dlg.ShowDialog();
            }
        }

        private void menudf1h_Click(object sender, RoutedEventArgs e)
        {
            if (OKAPI.SiteManager.Instance.CheckAPIAccess())
            {
                OKAPI.ImportByRadiusWindow dlg = new OKAPI.ImportByRadiusWindow();
                dlg.ShowDialog();
            }
        }

        private void menudv2v7_Click(object sender, RoutedEventArgs e)
        {
            Regions.AssignRegionWindow dlg = new Regions.AssignRegionWindow();
            dlg.ShowDialog();
        }

        private void menudb8_Click(object sender, RoutedEventArgs e)
        {
            Regions.SelectRegionWindow dlg = new Regions.SelectRegionWindow();
            dlg.ShowDialog();
        }

        private void MenuItem_Click_19x(object sender, RoutedEventArgs e)
        {
            Localization.SettingsWindow dlg = new Localization.SettingsWindow();
            dlg.ShowDialog();
            Localization.TranslationManager.Instance.ReloadUserTranslation();
        }

        private void menulib8_Click(object sender, RoutedEventArgs e)
        {
            Window w = new FeatureWindow(new UIControls.LogImageViewer.Control());
            w.Owner = this;
            w.Show();
        }

        private void menufc55_Click(object sender, RoutedEventArgs e)
        {
            LiveAPILogGeocaches.GarminGeocacheVisits imp = new LiveAPILogGeocaches.GarminGeocacheVisits();
            imp.SelectAndLog();
        }

        private void menufc56_Click(object sender, RoutedEventArgs e)
        {
            LiveAPILogGeocaches.CGeoGeocacheVisits imp = new LiveAPILogGeocaches.CGeoGeocacheVisits();
            imp.SelectAndLog();
        }

        private void menulbb8_Click(object sender, RoutedEventArgs e)
        {
            Window w = new FeatureWindow(new UIControls.Chat.Control());
            w.Owner = this;
            w.Show();
        }

        private void menulbb9_Click(object sender, RoutedEventArgs e)
        {
            Window w = new FeatureWindow(new UIControls.InternalWebBrowser.Control());
            w.Owner = this;
            w.Show();
        }

        private void MenuItem_Click_2v88(object sender, RoutedEventArgs e)
        {
            Dialogs.SettingsFolderWindow dlg = new Dialogs.SettingsFolderWindow();
            if (dlg.ShowDialog()==true)
            {
                GAPPSF.Properties.Settings.Default.SettingsFolder = dlg.SelectedSettingsPath;
                GAPPSF.Properties.Settings.Default.Save();
                RestartApplication();
            }
        }

        private void menulbm9_Click(object sender, RoutedEventArgs e)
        {
            Window w = new FeatureWindow(new UIControls.Trackables.Control());
            w.Owner = this;
            w.Show();
        }

        private void menu13x_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new LiveAPI.ManualAuthorizationWindow();
            dlg.ShowDialog();
        }

        private void menus25_Click(object sender, RoutedEventArgs e)
        {
            Window w = new FeatureWindow(new UIControls.OpenAreas.Control());
            w.Owner = this;
            w.Show();
        }

    }
}
