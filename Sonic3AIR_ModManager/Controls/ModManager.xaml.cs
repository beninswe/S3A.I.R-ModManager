﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using Microsoft.Win32;
using System.IO;
using Newtonsoft.Json.Linq;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.IO.Compression;
using System.Net.Http;
using System.Net;
using System.Security.Permissions;
using Microsoft.VisualBasic;
using SharpCompress.Readers;
using SharpCompress.Writers;
using SharpCompress.Archives;
using SharpCompress.Common;
using SharpCompress.Compressors;
using SharpCompress.IO;
using SharpCompress.Archives.Rar;
using SharpCompress.Archives.SevenZip;
using SharpCompress.Archives.Zip;
using System.Dynamic;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System.Reflection;
using System.Threading;
using System.Resources;
using Path = System.IO.Path;
using OpenFileDialog = System.Windows.Forms.OpenFileDialog;
using DialogResult = System.Windows.Forms.DialogResult;
using MessageBoxIcon = System.Windows.Forms.MessageBoxIcon;
using MessageBoxButtons = System.Windows.Forms.MessageBoxButtons;
using MessageBox = System.Windows.Forms.MessageBox;

namespace Sonic3AIR_ModManager
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    /// 


    // TODO: Implement Version Checking to Prevent More Stupid Bug Reports
    // TODO: Fix Unable to Install Version After Download is Complete
    // TODO: Backup/Restore A.I.R. Save Data Functionality


    public partial class ModManager : Window
    {
        #region Variables

        #region Winforms
        private System.Windows.Forms.Timer ApiInstallChecker;
        #endregion

        #region Tooltips

        ToolTip AddModTooltip = new ToolTip();
        ToolTip RemoveSelectedModTooltip = new ToolTip();
        ToolTip MoveModUpTooltip = new ToolTip();
        ToolTip MoveModDownTooltip = new ToolTip();
        ToolTip MoveModToTopTooltip = new ToolTip();
        ToolTip MoveModToBottomTooltip = new ToolTip();

        #endregion

        public ModManagement ModManagement;
        public static Settings.ModManagerSettings Settings { get; set; }

        public static Dictionary<int, List<GenerationsLib.WPF.Controls.RecentsListMenuItem.RecentItem>> ModCollectionMenuItems { get; set; } = new Dictionary<int, List<GenerationsLib.WPF.Controls.RecentsListMenuItem.RecentItem>>();
        public static Dictionary<int, List<GenerationsLib.WPF.Controls.RecentsListMenuItem.RecentItem>> LaunchPresetMenuItems { get; set; } = new Dictionary<int, List<GenerationsLib.WPF.Controls.RecentsListMenuItem.RecentItem>>();

        public string nL = Environment.NewLine;
        public static AIR_API.Settings S3AIRSettings;
        public static ModManager Instance;
        public static AIR_API.GameConfig GameConfig { get; set; }
        public static AIR_API.VersionMetadata CurrentAIRVersion;

        bool AllowUpdate { get; set; } = true;
        bool HasInitilizationCompleted { get; set; } = false;


        #region Hosted Elements

        public System.Windows.Controls.ListView ModList { get => ModViewer.View; set => ModViewer.View = value; }
        public System.Windows.Controls.ListView VersionsListView { get => VersionsViewer.View; set => VersionsViewer.View = value; }
        public System.Windows.Controls.ListView GameRecordingList { get => RecordingsViewer.View; set => RecordingsViewer.View = value; }

        #endregion

        #endregion

        #region Initialize Methods
        public ModManager(bool autoBoot = false)
        {
            if (Properties.Settings.Default.AutoUpdates)
            {
                if (autoBoot == false && Program.AIRUpdaterState == Program.UpdateState.NeverStarted && Program.MMUpdaterState == Program.UpdateState.NeverStarted)
                {
                    new Updater();
                    new ModManagerUpdater();
                }
            }


            StartModloader(autoBoot);

        }

        public ModManager(string gamebanana_api)
        {
            StartModloader(false, gamebanana_api);
        }

        public void SetNonDesignerRules()
        {
            LaunchOptionsWarning.Visibility = Visibility.Visible;
        }

        #region WPF Definitions
        private void InitializeHostedComponents()
        {
            ModViewer.View.MouseUp += View_MouseUp;

            ModViewer.SelectionChanged += View_SelectionChanged;
            ModViewer.FolderView.SelectionChanged += FolderView_SelectionChanged;

            ApiInstallChecker = new System.Windows.Forms.Timer();
            ApiInstallChecker.Tick += apiInstallChecker_Tick;


        }





        #endregion

        private void StartModloader(bool autoBoot = false, string gamebanana_api = "")
        {
            AllowUpdate = false;
            InitializeComponent();
            InitializeHostedComponents();
            SetNonDesignerRules();
            AllowUpdate = true;

            ModManagement = new ModManagement(this);

            if (ValidateInstall() == true)
            {
                SetTooltips();
                ModManagement.UpdateModsList(true);
                UpdateUI();
                Instance = this;

                Settings = new Settings.ModManagerSettings(ProgramPaths.Sonic3AIR_MM_SettingsFile);

                if (Properties.Settings.Default.WindowSize != null)
                {
                    this.Width = Properties.Settings.Default.WindowSize.Width;
                    this.Height = Properties.Settings.Default.WindowSize.Height;
                }

                ApiInstallChecker.Enabled = true;
                ApiInstallChecker.Start();

                FileManagement.GBAPIWatcher.NotifyFilter = NotifyFilters.LastAccess | NotifyFilters.LastWrite | NotifyFilters.FileName;
                FileManagement.GBAPIWatcher.EnableRaisingEvents = true;
                FileManagement.GBAPIWatcher.Changed += FileManagement.GBAPIWatcher_Changed;

                UserLanguage.ApplyLanguage(ref Instance);
                if (autoBoot) GameHandler.LaunchSonic3AIR();
                if (gamebanana_api != "") FileManagement.GamebananaAPI_Install(gamebanana_api);
                HasInitilizationCompleted = true;
            }
            else
            {
                Environment.Exit(0);
            }

        }

        #endregion

        #region Events

        private void LegacyLoadingCheckbox_Click(object sender, RoutedEventArgs e)
        {
            ModManagement.S3AIRActiveMods.UseLegacyLoading = LegacyLoadingCheckbox.IsChecked.Value;
            ModManagement.Save();
        }

        private void addInputMethodButton_Click(object sender, RoutedEventArgs e)
        {
            AddInputDevice();
        }

        private void removeInputMethodButton_Click(object sender, RoutedEventArgs e)
        {
            RemoveInputDevice();
        }

        private void importConfigButton_Click(object sender, RoutedEventArgs e)
        {
            ImportInputDevice();
        }

        private void exportConfigButton_Click(object sender, RoutedEventArgs e)
        {
            ExportInputDevice();
        }

        private void useDarkModeCheckBox_Click(object sender, RoutedEventArgs e)
        {
            if (Properties.Settings.Default.UseDarkTheme)
            {
                App.ChangeSkin(Skin.Dark);
            }
            else
            {
                App.ChangeSkin(Skin.Light);
            }
            RefreshTheming();


            void RefreshTheming()
            {
                this.InvalidateVisual();
                foreach (UIElement element in Extensions.FindVisualChildren<UIElement>(this))
                {
                    element.InvalidateVisual();
                }
            }
        }

        private void LaunchOptionsUnderstandingButton_Click(object sender, RoutedEventArgs e)
        {
            LaunchOptionsWarning.Visibility = Visibility.Collapsed;
        }

        private void LaunchOptions_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (this.IsLoaded) UpdateAIRGameConfigLaunchOptions();
        }

        private void CurrentWindowComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (this.IsLoaded)
            {
                if (GameConfig != null)
                {
                    S3AIRSettings.Fullscreen = FullscreenTypeComboBox.SelectedIndex;
                    S3AIRSettings.Save();
                    UpdateAIRSettings();
                }

            }
        }

        private void sonic3AIRPathBox_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key != System.Windows.Input.Key.Enter) return;

            ProgramPaths.Sonic3AIRPath = sonic3AIRPathBox.Text;
            // your event handler here
            e.Handled = true;
            UpdateAIRSettings();
        }

        private void MenuItem_Click(object sender, RoutedEventArgs e)
        {
            var list = new List<GenerationsLib.WPF.Controls.RecentsListMenuItem.RecentItem>();
            //TODO: Gut unused Method
            string file = @"D:\Users\CarJem\Downloads\tails_tails_sprites.zip";

            list.Add(new GenerationsLib.WPF.Controls.RecentsListMenuItem.RecentItem("test1", "1"));
            list.Add(new GenerationsLib.WPF.Controls.RecentsListMenuItem.RecentItem("test2", "2"));
            list.Add(new GenerationsLib.WPF.Controls.RecentsListMenuItem.RecentItem("test3", "3"));
            list.Add(new GenerationsLib.WPF.Controls.RecentsListMenuItem.RecentItem("test4", "4"));
            list.Add(new GenerationsLib.WPF.Controls.RecentsListMenuItem.RecentItem("test5", "5"));
            list.Add(new GenerationsLib.WPF.Controls.RecentsListMenuItem.RecentItem("test6", "6"));


            RecentsMenu.RecentItemsSource = list;
        }

        private void RecentsMenu_RecentItemSelected(object sender, GenerationsLib.WPF.Controls.RecentsListMenuItem.RecentItem e)
        {
            //TODO: Gut unused Method
            MessageBox.Show($"{e.Header}{nL}{e.Content}{nL}", "Results");
        }

        private void MenuItem_Click_1(object sender, RoutedEventArgs e)
        {
            //TODO: Gut unused Method
            RecentsMenu.RecentItemsSource = null;
        }

        private void AboutMenuItem_Click(object sender, RoutedEventArgs e)
        {
            AboutWindow about = new AboutWindow();
            about.Owner = this;
            about.ShowDialog();
        }

        private void ForceQuitGame_Click(object sender, RoutedEventArgs e)
        {
            GameHandler.ForceQuitSonic3AIR();
        }

        private void apiInstallChecker_Tick(object sender, EventArgs e)
        {
            FileManagement.GBAPIInstallTrigger();
        }

        public static void UpdateUIFromInvoke()
        {
            Instance.ModManagement.UpdateModsList(true);
        }

        public void DownloadButtonTest_Click(object sender, RoutedEventArgs e)
        {
            FileManagement.AddModFromURLLink();
        }

        private void LanguageComboBox_SelectionChangeCommitted(object sender, SelectionChangedEventArgs e)
        {
            if (AllowUpdate) UpdateCurrentLanguage();
        }
        private void OpenDownloadsFolderToolStripMenuItem_Click(object sender, RoutedEventArgs e)
        {
            Process.Start(ProgramPaths.Sonic3AIR_MM_DownloadsFolder);
        }

        private void OpenVersionsFolderToolStripMenuItem_Click(object sender, RoutedEventArgs e)
        {
            Process.Start(ProgramPaths.Sonic3AIR_MM_VersionsFolder);
        }

        private void airModManagerPlacesButton_Click(object sender, RoutedEventArgs e)
        {
            airModManagerPlacesButton.ContextMenu.IsOpen = true;
        }

        private void OpenConfigFileToolStripMenuItem_Click(object sender, RoutedEventArgs e)
        {
            OpenConfigFile();
        }

        private void FromSettingsFileToolStripMenuItem_Click(object sender, RoutedEventArgs e)
        {
            ChangeAIRPathFromSettings();
        }

        private void moveDeviceNameToTopButton_Click(object sender, RoutedEventArgs e)
        {
            MoveDeviceName(FileManagement.MoveListItemDirection.MoveToTop);
        }

        private void moveDeviceNameUpButton_Click(object sender, RoutedEventArgs e)
        {
            MoveDeviceName(FileManagement.MoveListItemDirection.MoveUp);
        }

        private void moveDeviceNameDownButton_Click(object sender, RoutedEventArgs e)
        {
            MoveDeviceName(FileManagement.MoveListItemDirection.MoveDown);
        }

        private void moveDeviceNameToBottomButton_Click(object sender, RoutedEventArgs e)
        {
            MoveDeviceName(FileManagement.MoveListItemDirection.MoveToBottom);
        }

        private void SaveInputsButton_Click(object sender, RoutedEventArgs e)
        {
            InputDevicesHandler.SaveInputs();
        }

        private void ResetInputsButton_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show(Program.LanguageResource.GetString("ResetInputMappingsDefaultFormMessage"), Program.LanguageResource.GetString("ResetInputMappingsDefaultFormTitle"), MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
            if (result == System.Windows.Forms.DialogResult.Yes)
            {
                InputDevicesHandler.InputDevices.ResetDevicesToDefault();
                RefreshInputMappings();
            }

        }

        private void InputMethodsList_SelectedIndexChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateInputMappings();
        }

        private void InputButton_Click(object sender, RoutedEventArgs e)
        {
            if (inputMethodsList.SelectedItem != null) ChangeInputMappings(sender);
        }

        private void AirPlacesButton_Click(object sender, RoutedEventArgs e)
        {
            airPlacesButton.ContextMenu.IsOpen = true;

        }

        private void AirMediaButton_Click(object sender, RoutedEventArgs e)
        {
            airMediaButton.ContextMenu.IsOpen = true;

        }
        private void MoveToTopButton_Click(object sender, RoutedEventArgs e)
        {
            ModManagement.MoveModToTop();
        }

        private void MoveToBottomButton_Click(object sender, RoutedEventArgs e)
        {
            ModManagement.MoveModToBottom();
        }

        private void MoreModOptionsButton_Click(object sender, RoutedEventArgs e)
        {
            moreModOptionsButton.ContextMenu.IsOpen = true;
        }

        private void MoveDownButton_Click(object sender, RoutedEventArgs e)
        {
            ModManagement.MoveModDown();
        }

        private void MoveUpButton_Click(object sender, RoutedEventArgs e)
        {
            ModManagement.MoveModUp();
        }

        private void S3AIRWebsiteButton_Click(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Process.Start("http://sonic3air.org/");
        }

        private void GamebannaButton_Click(object sender, RoutedEventArgs e)
        {
            Process.Start("https://gamebanana.com/games/6878");
        }

        private void EukaTwitterButton_Click(object sender, RoutedEventArgs e)
        {
            Process.Start("https://twitter.com/eukaryot3k");
        }

        private void CarJemTwitterButton_Click(object sender, RoutedEventArgs e)
        {
            Process.Start("https://twitter.com/carter5467_99");
        }

        private void OpenModdingTemplatesFolder_Click(object sender, RoutedEventArgs e)
        {
            OpenModdingTemplatesFolder();
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            ModManagement.Save();
        }

        private void OpenSampleModsFolderButton_Click(object sender, RoutedEventArgs e)
        {
            OpenSampleModsFolder();

        }

        private void OpenUserManualButton_Click(object sender, RoutedEventArgs e)
        {
            OpenUserManual();
        }

        private void OpenModDocumentationButton_Click(object sender, RoutedEventArgs e)
        {
            OpenModDocumentation();
        }

        private void OpenModURLToolStripMenuItem_Click(object sender, RoutedEventArgs e)
        {
            OpenModURL((ModViewer.SelectedItem as ModViewerItem).Source.URL);
        }

        private void ShowLogFileButton_Click(object sender, RoutedEventArgs e)
        {
            OpenLogFile();
        }

        private void AutoRunCheckbox_CheckedChanged(object sender, RoutedEventArgs e)
        {
           if (AllowUpdate) UpdateUI();
        }

        private void ModManager_WindowClosing(object sender, CancelEventArgs e)
        {
            if (GameHandler.isGameRunning)
            {
                e.Cancel = true;
            }
            else
            {
                Properties.Settings.Default.WindowSize = new System.Drawing.Size((int)this.Width, (int)this.Height);
                Properties.Settings.Default.Save();
            }
        }

        private void Window_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            UpdateInGameButtons();
        }

        private void DeleteRecordingButton_Click(object sender, RoutedEventArgs e)
        {
            if (GameRecordingList.SelectedItem != null && GameRecordingList.SelectedItem is AIR_API.Recording)
            {
                if (FileManagement.DeleteRecording(GameRecordingList.SelectedItem as AIR_API.Recording) == true)
                {
                    CollectGameRecordings();
                    GameRecordingList_SelectedIndexChanged(null, null);
                }
            }
        }
        private void ExitButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void RemoveModToolStripMenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (ModViewer.SelectedItem != null)
            {
                FileManagement.RemoveMod((ModViewer.SelectedItem as ModViewerItem).Source);
            }
        }

        private void OpenModFolderToolStripMenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (ModViewer.SelectedItem != null)
            {
                OpenSelectedModFolder(ModViewer.SelectedItem as ModViewerItem);
            }
        }

        private void View_MouseUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {

        }

        private void View_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            RefreshSelectedModProperties();
        }

        private void FolderView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ModManagement.UpdateModsList();
        }

        private void AddMods_Click(object sender, RoutedEventArgs e)
        {
            FileManagement.AddMod();
        }

        private void RemoveButton_Click(object sender, RoutedEventArgs e)
        {
            if (ModViewer.SelectedItem != null)
            {
                FileManagement.RemoveMod((ModViewer.SelectedItem as ModViewerItem).Source);
            }
        }
        private void TabControl1_SelectedIndexChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.Source is TabControl)
            {
                if (PrimaryTabControl.SelectedItem == toolsPage)
                {
                    CollectGameRecordings();
                }
                else if (PrimaryTabControl.SelectedItem == optionsPage)
                {
                    RefreshInputMappings();
                }
            }
        }
        private void TabControl3_SelectedIndexChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.Source is TabControl)
            {
                if (tabControl3.SelectedItem == recordingsPage)
                {
                    CollectGameRecordings();
                }
            }
        }

        private void CopyRecordingFilePath_Click(object sender, RoutedEventArgs e)
        {
            if (GameRecordingList.SelectedItem != null)
            {
                FileManagement.CopyRecordingLocationToClipboard(GameRecordingList.SelectedItem as AIR_API.Recording);
            }
        }

        private void UploadButton_Click(object sender, RoutedEventArgs e)
        {
            if (GameRecordingList.SelectedItem != null)
            {
                FileManagement.UploadRecordingToFileDotIO(GameRecordingList.SelectedItem as AIR_API.Recording);
            }

        }

        private void GameRecordingList_SelectedIndexChanged(object sender, SelectionChangedEventArgs e)
        {
            if (GameRecordingList.SelectedItem != null)
            {
                openRecordingButton.IsEnabled = true;
                copyRecordingFilePath.IsEnabled = true;
                uploadButton.IsEnabled = true;
                deleteRecordingButton.IsEnabled = true;
                playbackRecordingButton.IsEnabled = true;

                openRecordingMenuItem.IsEnabled = true;
                copyRecordingFilePathMenuItem.IsEnabled = true;
                recordingUploadMenuItem.IsEnabled = true;
                deleteRecordingMenuItem.IsEnabled = true;
                playbackRecordingMenuItem.IsEnabled = true;
            }
            else
            {
                openRecordingButton.IsEnabled = false;
                copyRecordingFilePath.IsEnabled = false;
                uploadButton.IsEnabled = false;
                deleteRecordingButton.IsEnabled = false;
                playbackRecordingButton.IsEnabled = false;

                openRecordingMenuItem.IsEnabled = false;
                copyRecordingFilePathMenuItem.IsEnabled = false;
                recordingUploadMenuItem.IsEnabled = false;
                deleteRecordingMenuItem.IsEnabled = false;
                playbackRecordingMenuItem.IsEnabled = false;
            }
        }

        private void OpenRecordingButton_Click(object sender, RoutedEventArgs e)
        {
            OpenRecordingLocation();
        }

        private void RefreshDebugButton_Click(object sender, RoutedEventArgs e)
        {
            CollectGameRecordings();
            GameRecordingList_SelectedIndexChanged(null, null);
        }

        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            ModManagement.UpdateModsList(true);
        }

        private void UpdateSonic3AIRPath_Click(object sender, RoutedEventArgs e)
        {
            GameHandler.UpdateSonic3AIRLocation(true);
            UpdateAIRSettings();
        }

        private void moveInputMethodUpButton_Click(object sender, RoutedEventArgs e)
        {
            MoveInputMethod(FileManagement.MoveListItemDirection.MoveUp);
        }

        private void moveInputMethodDownButton_Click(object sender, RoutedEventArgs e)
        {
            MoveInputMethod(FileManagement.MoveListItemDirection.MoveDown);
        }

        private void moveInputMethodToBottomButton_Click(object sender, RoutedEventArgs e)
        {
            MoveInputMethod(FileManagement.MoveListItemDirection.MoveToBottom);
        }

        private void moveInputMethodToTopButton_Click(object sender, RoutedEventArgs e)
        {
            MoveInputMethod(FileManagement.MoveListItemDirection.MoveToTop);
        }

        private void ChangeSonic3AIRPathButton_Click(object sender, RoutedEventArgs e)
        {
            updateSonic3AIRPathButton.ContextMenu.IsOpen = true;
            UpdateAIRVersionsToolstrips();
            UpdateAIRSettings();
        }

        private void ChangeRomPathButton_Click(object sender, RoutedEventArgs e)
        {
            ChangeS3RomPath();
        }

        private void ModsList_ItemCheck()
        {
            ModManagement.UpdateModsList();
        }

        private void ModsList_SelectedValueChanged(object sender, EventArgs e)
        {
            RefreshSelectedModProperties();
        }

        private void RunButton_Click(object sender, RoutedEventArgs e)
        {
            ModManagement.Save();
            GameHandler.LaunchSonic3AIR();
            UpdateInGameButtons();
        }

        private void OpenModsFolder_Click(object sender, RoutedEventArgs e)
        {
            OpenModsFolder();
        }

        private void OpenEXEFolderButton_Click(object sender, RoutedEventArgs e)
        {
            OpenEXEFolder();
        }

        private void OpenAppDataFolderButton_Click(object sender, RoutedEventArgs e)
        {
            OpenAppDataFolder();
        }

        private void OpenConfigFile_Click(object sender, RoutedEventArgs e)
        {
            OpenSettingsFile();
        }

        private void ModsList_SelectedIndexChanged(object sender, EventArgs e)
        {
            RefreshSelectedModProperties();
        }

        private void FixGlitchesCheckbox_Click(object sender, RoutedEventArgs e)
        {
            UpdateBoolSettings(S3AIRSetting.FixGlitches, fixGlitchesCheckbox.IsChecked.Value);
        }

        private void FailSafeModeCheckbox_Click(object sender, RoutedEventArgs e)
        {
            UpdateBoolSettings(S3AIRSetting.FailSafeMode, failSafeModeCheckbox.IsChecked.Value);
        }

        private void devModeCheckbox_Click(object sender, RoutedEventArgs e)
        {
            UpdateBoolSettings(S3AIRSetting.EnableDevMode, devModeCheckbox.IsChecked.Value);
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            Properties.Settings.Default.Save();
            App.Instance.Shutdown();
        }

        private void OpenGamepadSettingsButton_Click(object sender, RoutedEventArgs e)
        {
            LaunchSystemGamepadSettings();
        }

        private void InputDeviceNamesList_SelectedIndexChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateInputDeviceNamesList();
        }

        private void AddDeviceNameButton_Click(object sender, RoutedEventArgs e)
        {
            AddInputDeviceName();
        }

        private void RemoveDeviceNameButton_Click(object sender, RoutedEventArgs e)
        {
            RemoveInputDeviceName();
        }

        #endregion

        #region Refreshing and Updating

        public void SetTooltips()
        {
            AddModTooltip.Content = Program.LanguageResource.GetString("AddAMod");
            RemoveSelectedModTooltip.Content = Program.LanguageResource.GetString("RemoveSelectedMod");
            MoveModUpTooltip.Content = Program.LanguageResource.GetString("IncreaseModPriority");
            MoveModDownTooltip.Content = Program.LanguageResource.GetString("DecreaseModPriority");
            MoveModToTopTooltip.Content = Program.LanguageResource.GetString("IncreaseModPriorityToMax");
            MoveModToBottomTooltip.Content = Program.LanguageResource.GetString("DecreaseModPriorityToMin");

            removeButton.ToolTip = RemoveSelectedModTooltip;
            addMods.ToolTip = AddModTooltip;
            moveUpButton.ToolTip = MoveModUpTooltip;
            moveDownButton.ToolTip = MoveModDownTooltip;
            moveToTopButton.ToolTip = MoveModToTopTooltip;
            moveToBottomButton.ToolTip = MoveModToBottomTooltip;

            this.Title = string.Format("{0} {1}", Program.LanguageResource.GetString("ApplicationTitle"), Program.Version);
        }

        public void UpdateInGameButtons()
        {
            bool enabled = !GameHandler.isGameRunning;
            ModManagerButtons.Visibility = (enabled ? Visibility.Visible : Visibility.Hidden);
            InGameButtons.Visibility = (enabled ? Visibility.Hidden : Visibility.Visible);
            saveAndLoadButton.IsEnabled = enabled;
            saveButton.IsEnabled = enabled;
            exitButton.IsEnabled = enabled;
            keepLoaderOpenCheckBox.IsEnabled = enabled;
            keepOpenOnQuitCheckBox.IsEnabled = enabled;
            sonic3AIRPathBox.IsEnabled = enabled;
            romPathBox.IsEnabled = enabled;
            fixGlitchesCheckbox.IsEnabled = enabled;
            failSafeModeCheckbox.IsEnabled = enabled;
            modPanel.IsEnabled = enabled;
            autoRunCheckbox.IsEnabled = enabled;
            inputPanel.IsEnabled = enabled;
            AboutMenuItem.IsEnabled = enabled;
            devModeCheckbox.IsEnabled = enabled;
            OptionsTabControl.IsEnabled = enabled;
        }

        private void UpdateUI()
        {
            UpdateAIRSettings();
            autoLaunchDelayLabel.IsEnabled = Properties.Settings.Default.AutoLaunch;
            AutoLaunchNUD.IsEnabled = Properties.Settings.Default.AutoLaunch;
        }

        private void ChangeS3RomPath()
        {
            OpenFileDialog fileDialog = new OpenFileDialog()
            {
                Filter = $"{Program.LanguageResource.GetString("Sonic3KRomFile")} (*.bin)|*.bin",
                InitialDirectory = Path.GetDirectoryName(S3AIRSettings.Sonic3KRomPath),
                Title = Program.LanguageResource.GetString("SelectSonic3KRomFile")

            };
            if (fileDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                S3AIRSettings.Sonic3KRomPath = fileDialog.FileName;
                S3AIRSettings.Save();
            }
            UpdateAIRSettings();
        }

        private enum S3AIRSetting : int
        {
            FailSafeMode = 0,
            FixGlitches = 1,
            EnableDevMode = 2
        }

        private void UpdateBoolSettings(S3AIRSetting setting, bool isChecked)
        {
            if (setting == S3AIRSetting.FailSafeMode)
            {
                S3AIRSettings.FailSafeMode = isChecked;
            }
            else if (setting == S3AIRSetting.FixGlitches)
            {
                S3AIRSettings.FixGlitches = isChecked;
            }
            else
            {
                S3AIRSettings.EnableDebugMode = isChecked;
            }
            S3AIRSettings.Save();
        }

        private void UpdateCurrentLanguage()
        {
            if (languageComboBox.SelectedItem != null)
            {
                switch ((languageComboBox.SelectedItem as ComboBoxItem).Tag.ToString())
                {
                    case "EN_US":
                        UserLanguage.CurrentLanguage = UserLanguage.Language.EN_US;
                        break;
                    case "GR":
                        UserLanguage.CurrentLanguage = UserLanguage.Language.GR;
                        break;
                    case "FR":
                        UserLanguage.CurrentLanguage = UserLanguage.Language.FR;
                        break;
                    case "NULL":
                        UserLanguage.CurrentLanguage = UserLanguage.Language.NULL;
                        break;
                    default:
                        UserLanguage.CurrentLanguage = UserLanguage.Language.NULL;
                        break;
                }

                UserLanguage.ApplyLanguage(ref Instance);
                ModManagement.UpdateModsList(true);
                UpdateAIRSettings();
            }



        }

        private void GetLanguageSelection()
        {
            if (languageComboBox != null)
            {
                languageComboBox.Items.Clear();

                Binding LangItemsWidth = new Binding("ActualWidth") { ElementName = "languageComboBox" };

                AllowUpdate = false;

                ComboBoxItem EN_US = new ComboBoxItem()
                {
                    Tag = "EN_US",
                    Content = "English (US)",
                };
                ComboBoxItem GR = new ComboBoxItem()
                {
                    Tag = "GR",
                    Content = "Deutsch"
                };
                ComboBoxItem FR = new ComboBoxItem()
                {
                    Tag = "FR",
                    Content = "Français"
                };
                ComboBoxItem NULL = new ComboBoxItem()
                {
                    Tag = "NULL",
                    Content = "NULL"
                };

                EN_US.SetBinding(FrameworkElement.WidthProperty, LangItemsWidth);
                GR.SetBinding(FrameworkElement.WidthProperty, LangItemsWidth);
                FR.SetBinding(FrameworkElement.WidthProperty, LangItemsWidth);
                NULL.SetBinding(FrameworkElement.WidthProperty, LangItemsWidth);

                languageComboBox.Items.Add(EN_US);
                languageComboBox.Items.Add(GR);
                languageComboBox.Items.Add(FR);
                if (Program.isDebug) languageComboBox.Items.Add(NULL);

                if (UserLanguage.CurrentLanguage == UserLanguage.Language.EN_US) languageComboBox.SelectedItem = EN_US;
                else if (UserLanguage.CurrentLanguage == UserLanguage.Language.GR) languageComboBox.SelectedItem = GR;
                else if (UserLanguage.CurrentLanguage == UserLanguage.Language.FR) languageComboBox.SelectedItem = FR;
                else if (UserLanguage.CurrentLanguage == UserLanguage.Language.NULL) languageComboBox.SelectedItem = NULL;
                else languageComboBox.SelectedItem = NULL;

                AllowUpdate = true;
            }

        }

        public void UpdateAIRSettings()
        {
            sonic3AIRPathBox.Text = ProgramPaths.Sonic3AIRPath;
            if (S3AIRSettings != null)
            {
                romPathBox.Text = S3AIRSettings.Sonic3KRomPath;
                fixGlitchesCheckbox.IsChecked = S3AIRSettings.FixGlitches;
                failSafeModeCheckbox.IsChecked = S3AIRSettings.FailSafeMode;
                devModeCheckbox.IsChecked = S3AIRSettings.EnableDebugMode;
                FullscreenTypeComboBox.SelectedIndex = S3AIRSettings.Fullscreen;
            }

            GetLanguageSelection();
            RetriveLaunchOptions();

            ModManagement.UpdateModsList(true);

            if (File.Exists(ProgramPaths.Sonic3AIRPath))
            {
                string metaDataFile = Directory.GetFiles(Path.GetDirectoryName(ProgramPaths.Sonic3AIRPath), "metadata.json", SearchOption.AllDirectories).FirstOrDefault();
                if (metaDataFile != null)
                {
                    try
                    {
                        CurrentAIRVersion = new AIR_API.VersionMetadata(new FileInfo(metaDataFile));
                        airVersionLabel.Text = $"{Program.LanguageResource.GetString("AIRVersion")}: {CurrentAIRVersion.VersionString}";
                        airVersionLabel.Text += Environment.NewLine + $"{Program.LanguageResource.GetString("SettingsVersionLabel")}: {S3AIRSettings.Version.ToString()}";
                    }
                    catch
                    {
                        NullSituation();

                    }

                }
                else
                {
                    NullSituation();
                }
            }
            else NullSituation();
            Properties.Settings.Default.Save();

            void NullSituation()
            {
                if (airVersionLabel != null)
                {
                    airVersionLabel.Text = $"{Program.LanguageResource.GetString("AIRVersion")}: NULL";
                    if (S3AIRSettings.Version != null)
                    {
                        airVersionLabel.Text += Environment.NewLine + $"{Program.LanguageResource.GetString("SettingsVersionLabel")}: {S3AIRSettings.Version.ToString()}";
                    }
                    else airVersionLabel.Text += Environment.NewLine + $"{Program.LanguageResource.GetString("SettingsVersionLabel")}: NULL";
                }
            }

        }

        public void UpdateAIRGameConfigLaunchOptions()
        {
            SaveLaunchOptions();
            RetriveLaunchOptions();
        }

        private void SaveLaunchOptions()
        {
            if (SceneComboBox != null && PlayerComboBox != null && StartPhaseComboBox != null)
            {
                if (GameConfig != null)
                {
                    SceneComboBox.SelectionChanged -= LaunchOptions_SelectionChanged;
                    PlayerComboBox.SelectionChanged -= LaunchOptions_SelectionChanged;
                    StartPhaseComboBox.SelectionChanged -= LaunchOptions_SelectionChanged;

                    if ((SceneComboBox.SelectedItem as ComboBoxItem).Tag.ToString() != "NONE")
                    {
                        GameConfig.LoadLevel = (SceneComboBox.SelectedItem as ComboBoxItem).Tag.ToString();
                    }
                    else GameConfig.LoadLevel = null;
                    if ((PlayerComboBox.SelectedItem as ComboBoxItem).Tag.ToString() != "NONE")
                    {
                        if (int.TryParse((PlayerComboBox.SelectedItem as ComboBoxItem).Tag.ToString(), out int result))
                        {
                            GameConfig.UseCharacters = result;
                        }
                    }
                    else GameConfig.UseCharacters = null;
                    if ((StartPhaseComboBox.SelectedItem as ComboBoxItem).Tag.ToString() != "NONE")
                    {
                        if (int.TryParse((StartPhaseComboBox.SelectedItem as ComboBoxItem).Tag.ToString(), out int result))
                        {
                            GameConfig.StartPhase = result;
                        }
                    }
                    else GameConfig.StartPhase = null;

                    GameConfig.Save();


                    SceneComboBox.SelectionChanged += LaunchOptions_SelectionChanged;
                    PlayerComboBox.SelectionChanged += LaunchOptions_SelectionChanged;
                    StartPhaseComboBox.SelectionChanged += LaunchOptions_SelectionChanged;
                }


            }
        }

        public void RetriveLaunchOptions()
        {
            if (SceneComboBox != null && PlayerComboBox != null && StartPhaseComboBox != null)
            {
                if (GameConfig != null)
                {
                    SceneComboBox.SelectionChanged -= LaunchOptions_SelectionChanged;
                    PlayerComboBox.SelectionChanged -= LaunchOptions_SelectionChanged;
                    StartPhaseComboBox.SelectionChanged -= LaunchOptions_SelectionChanged;

                    CollectGameConfig();
                    if (GameConfig != null)
                    {
                        if (GameConfig.LoadLevel != null)
                        {
                            ComboBoxItem item = SceneComboBox.Items.Cast<ComboBoxItem>().Where(x => x.Tag.ToString() == GameConfig.LoadLevel.ToString()).FirstOrDefault();
                            SceneComboBox.SelectedItem = item;
                        }
                        else SceneComboBox.SelectedIndex = 0;

                        if (GameConfig.UseCharacters != null)
                        {
                            ComboBoxItem item = PlayerComboBox.Items.Cast<ComboBoxItem>().Where(x => x.Tag.ToString() == GameConfig.UseCharacters.ToString()).FirstOrDefault();
                            PlayerComboBox.SelectedItem = item;
                        }
                        else PlayerComboBox.SelectedIndex = 0;

                        if (GameConfig.StartPhase != null)
                        {
                            string phase = GameConfig.StartPhase.ToString();
                            if (GameConfig.StartPhase.ToString() == "3" && !Program.isDeveloper) phase = "NONE";
                            ComboBoxItem item = StartPhaseComboBox.Items.Cast<ComboBoxItem>().Where(x => x.Tag.ToString() == phase).FirstOrDefault();
                            StartPhaseComboBox.SelectedItem = item;
                        }
                        else StartPhaseComboBox.SelectedIndex = 0;


                        if (SceneComboBox.SelectedIndex == 0) PlayerComboBox.IsEnabled = false;
                        else PlayerComboBox.IsEnabled = true;

                        if (!Program.isDeveloper)
                        {
                            DeveloperOnlyStartPhaseItem.Visibility = Visibility.Collapsed;
                            DeveloperOnlyStartPhaseItem.IsEnabled = false;

                            if (SceneComboBox.SelectedIndex != 0) StartPhaseComboBox.IsEnabled = false;
                            else StartPhaseComboBox.IsEnabled = true;
                        }
                        else
                        {
                            StartPhaseComboBox.IsEnabled = true;
                            DeveloperOnlyStartPhaseItem.Visibility = Visibility.Visible;
                            DeveloperOnlyStartPhaseItem.IsEnabled = true;
                        }


                    }

                    if (GameConfig == null) AIRGameConfigNullSituation(2);

                    SceneComboBox.SelectionChanged += LaunchOptions_SelectionChanged;
                    PlayerComboBox.SelectionChanged += LaunchOptions_SelectionChanged;
                    StartPhaseComboBox.SelectionChanged += LaunchOptions_SelectionChanged;
                }
            }
        }

        public void RefreshSelectedModProperties()
        {
            if (ModViewer.SelectedItem != null)
            {
                if (ModViewer.ActiveView.SelectedItem != null && (ModViewer.ActiveView.SelectedItem as ModViewerItem).IsEnabled && !ModManagement.S3AIRActiveMods.UseLegacyLoading)
                {
                    moveUpButton.IsEnabled = (ModViewer.ActiveView.Items.IndexOf((ModViewer.ActiveView.SelectedItem as ModViewerItem)) > 0);
                    moveDownButton.IsEnabled = (ModViewer.ActiveView.Items.IndexOf((ModViewer.ActiveView.SelectedItem as ModViewerItem)) < ModViewer.ActiveView.Items.Count - 1);
                    moveToTopButton.IsEnabled = (ModViewer.ActiveView.Items.IndexOf((ModViewer.ActiveView.SelectedItem as ModViewerItem)) > 0);
                    moveToBottomButton.IsEnabled = (ModViewer.ActiveView.Items.IndexOf((ModViewer.ActiveView.SelectedItem as ModViewerItem)) < ModViewer.ActiveView.Items.Count - 1);
                }
                else
                {
                    moveUpButton.IsEnabled = false;
                    moveDownButton.IsEnabled = false;
                    moveToTopButton.IsEnabled = false;
                    moveToBottomButton.IsEnabled = false;
                }
                removeButton.IsEnabled = true;
                removeModToolStripMenuItem.IsEnabled = true;
                editModFolderToolStripMenuItem.IsEnabled = true;
                openModFolderToolStripMenuItem.IsEnabled = true;
                openModURLToolStripMenuItem.IsEnabled = (ValidateURL((ModViewer.SelectedItem as ModViewerItem).Source.URL));
                if (ModViewer.ActiveView.Items.Contains(ModViewer.SelectedItem) && !ModManagement.S3AIRActiveMods.UseLegacyLoading) moveModToSubFolderMenuItem.IsEnabled = false;
                else moveModToSubFolderMenuItem.IsEnabled = true;
            }
            else
            {
                moveUpButton.IsEnabled = false;
                moveDownButton.IsEnabled = false;
                moveToTopButton.IsEnabled = false;
                moveToBottomButton.IsEnabled = false;
                removeButton.IsEnabled = false;
                editModFolderToolStripMenuItem.IsEnabled = false;
                removeModToolStripMenuItem.IsEnabled = false;
                openModFolderToolStripMenuItem.IsEnabled = false;
                moveModToSubFolderMenuItem.IsEnabled = false;
                openModURLToolStripMenuItem.IsEnabled = false;
            }

            if (ModManagement.S3AIRActiveMods.UseLegacyLoading)
            {
                moveUpButton.Visibility = Visibility.Collapsed;
                moveDownButton.Visibility = Visibility.Collapsed;
                moveToTopButton.Visibility = Visibility.Collapsed;
                moveToBottomButton.Visibility = Visibility.Collapsed;
            }
            else
            {
                moveUpButton.Visibility = Visibility.Visible;
                moveDownButton.Visibility = Visibility.Visible;
                moveToTopButton.Visibility = Visibility.Visible;
                moveToBottomButton.Visibility = Visibility.Visible;
            }



            if (ModViewer.SelectedItem != null)
            {
                AIR_API.Mod item = (ModViewer.SelectedItem as ModViewerItem).Source;
                if (item != null)
                {

                    string author = $"{Program.LanguageResource.GetString("By")}: {item.Author}";
                    string version = $"{Program.LanguageResource.GetString("Version")}: {item.ModVersion}";
                    string air_version = $"{Program.LanguageResource.GetString("AIRVersion")}: {item.GameVersion}";
                    string tech_name = $"{item.TechnicalName}";

                    string description = item.Description;
                    if (description == "No Description Provided.")
                    {
                        description = Program.LanguageResource.GetString("NoModDescript");
                    }

                    Paragraph author_p = new Paragraph(new Run(author));
                    Paragraph version_p = new Paragraph(new Run(version));
                    Paragraph air_version_p = new Paragraph(new Run(air_version));
                    Paragraph tech_name_p = new Paragraph(new Run(tech_name));
                    Paragraph description_p = new Paragraph(new Run($"{nL}{description}"));


                    author_p.FontWeight = FontWeights.Normal;                    
                    version_p.FontWeight = FontWeights.Normal;
                    air_version_p.FontWeight = FontWeights.Normal;
                    tech_name_p.FontWeight = FontWeights.Bold;
                    description_p.FontWeight = FontWeights.Normal;


                    var no_margin = new Thickness(0);
                    author_p.Margin = no_margin;
                    version_p.Margin = no_margin;
                    air_version_p.Margin = no_margin;
                    tech_name_p.Margin = no_margin;
                    description_p.Margin = no_margin;

                    modInfoTextBox.Document.Blocks.Clear();

                    modInfoTextBox.Document.Blocks.Add(author_p);
                    modInfoTextBox.Document.Blocks.Add(version_p);
                    modInfoTextBox.Document.Blocks.Add(air_version_p);
                    modInfoTextBox.Document.Blocks.Add(tech_name_p);
                    modInfoTextBox.Document.Blocks.Add(description_p);
                }
                else
                {
                    modInfoTextBox.Document.Blocks.Clear();
                }
            }
            else
            {
                modInfoTextBox.Document.Blocks.Clear();
            }


            bool ValidateURL(string value)
            {
                if (value == null) return false;
                else if (value == "" || value == "NULL") return false;
                else if (!Uri.TryCreate(value, UriKind.Absolute, out Uri uriResult) && ValidateURI(uriResult)) return false;
                else return true;

                bool ValidateURI(Uri uriResult)
                {
                    if (uriResult == null) return false;
                    else return (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps);
                }
            }
        }
        #endregion

        #region Information Retriving

        public void CollectInputMappings()
        {
            inputMethodsList.SelectionChanged -= InputMethodsList_SelectedIndexChanged;
            AIR_API.InputMappings.Device selectedItem = null;
            if (inputMethodsList.SelectedItem != null)
            {
                selectedItem = inputMethodsList.SelectedItem as AIR_API.InputMappings.Device;
            }
            inputMethodsList.ItemsSource = null;
            inputMethodsList.Items.Refresh();
            if (InputDevicesHandler.InputDevices != null)
            {
                if (inputMethodsList.Items.Count != 0 && inputMethodsList.ItemsSource == null) inputMethodsList.Items.Clear();
                inputMethodsList.ItemsSource = InputDevicesHandler.InputDevices.Items.Select(x => x.Value);
                if (selectedItem != null && inputMethodsList.Items.Contains(selectedItem)) inputMethodsList.SelectedItem = selectedItem;
            }
            else
            {
                CollectGameConfig();
                RecollectInputMappings();
            }
            inputMethodsList.SelectionChanged += InputMethodsList_SelectedIndexChanged;
        }

        private void RecollectInputMappings()
        {
            HideInputMappingErrorPanels();

            if (ProgramPaths.Sonic3AIRPath != null && ProgramPaths.Sonic3AIRPath != "" && File.Exists(ProgramPaths.Sonic3AIRPath))
            {
                if (InputDevicesHandler.InputDevices != null)
                {
                    try
                    {
                        foreach (var inputMethod in InputDevicesHandler.InputDevices.Items)
                        {
                            inputMethodsList.Items.Add(inputMethod);
                        }
                    }
                    catch
                    {
                        AIRInputMappingsNullSituation(1);
                    }
                }
                else AIRInputMappingsNullSituation(2);

            }
            else AIRInputMappingsNullSituation();



        }

        private void HideErrorGameConfigErrorPanels()
        {
            LaunchOptionsFailureMessageBackground.Visibility = Visibility.Collapsed;
            airLaunchPanel.IsEnabled = true;
        }

        private void ShowGameConfigErrorPanels()
        {
            airLaunchPanel.IsEnabled = false;
            LaunchOptionsFailureMessageBackground.Visibility = Visibility.Visible;
        }

        private void HideInputMappingErrorPanels()
        {
            inputPanel.IsEnabled = true;
            inputErrorMessage.Visibility = Visibility.Collapsed;
        }

        private void ShowInputMappingErrorPanels()
        {
            inputPanel.IsEnabled = false;
            inputErrorMessage.Visibility = Visibility.Visible;
        }

        private void CollectGameConfig()
        {
            HideErrorGameConfigErrorPanels();

            if (ProgramPaths.Sonic3AIRPath != null && ProgramPaths.Sonic3AIRPath != "" && File.Exists(ProgramPaths.Sonic3AIRPath))
            {
                string Sonic3AIREXEFolder = Path.GetDirectoryName(ProgramPaths.Sonic3AIRPath);
                FileInfo config = new FileInfo($"{Sonic3AIREXEFolder}//config.json");
                if (config.Exists)
                {
                    try
                    {
                        GameConfig = new AIR_API.GameConfig(config);
                    }
                    catch
                    {
                        AIRGameConfigNullSituation(1);
                    }

                }
                else AIRGameConfigNullSituation(2);
            }
            else AIRGameConfigNullSituation();

        }

        private void AIRGameConfigNullSituation(int situation = 0)
        {
            string hyperLink = nL + Program.LanguageResource.GetString("ErrorHyperlinkClickMessage");
            if (situation == 0) LaunchOptionsFailureMessage.Text = Program.LanguageResource.GetString("InputMappingError1") + hyperLink;
            else if (situation == 1) LaunchOptionsFailureMessage.Text = Program.LanguageResource.GetString("InputMappingError2") + hyperLink;
            else if (situation == 2) LaunchOptionsFailureMessage.Text = Program.LanguageResource.GetString("InputMappingError3") + hyperLink;


            ShowGameConfigErrorPanels();
        }

        private void AIRInputMappingsNullSituation(int situation = 0)
        {
            string hyperLink = nL + Program.LanguageResource.GetString("ErrorHyperlinkClickMessage");
            if (situation == 0) inputErrorMessage.Content = Program.LanguageResource.GetString("InputMappingError1") + hyperLink;
            else if (situation == 1) inputErrorMessage.Content = Program.LanguageResource.GetString("InputMappingError2") + hyperLink;
            else if (situation == 2) inputErrorMessage.Content = Program.LanguageResource.GetString("InputMappingError3") + hyperLink;

            ShowInputMappingErrorPanels();
        }

        private bool ValidateInstall()
        {
            return ProgramPaths.ValidateInstall(ref ModManagement.S3AIRActiveMods, ref S3AIRSettings);
        }

        #endregion

        #region Input Mapping


        public void UpdateInputDeviceButtons()
        {
            if (inputMethodsList.SelectedItem != null)
            {
                moveInputMethodUpButton.IsEnabled = inputMethodsList.Items.IndexOf(inputMethodsList.SelectedItem) > 0;
                moveInputMethodDownButton.IsEnabled = inputMethodsList.Items.IndexOf(inputMethodsList.SelectedItem) < inputMethodsList.Items.Count - 1;
                moveInputMethodToTopButton.IsEnabled = inputMethodsList.Items.IndexOf(inputMethodsList.SelectedItem) > 0;
                moveInputMethodToBottomButton.IsEnabled = inputMethodsList.Items.IndexOf(inputMethodsList.SelectedItem) < inputMethodsList.Items.Count - 1;

                removeInputMethodButton.IsEnabled = true;
                exportConfigButton.IsEnabled = true;
            }
            else
            {
                removeInputMethodButton.IsEnabled = false;
                exportConfigButton.IsEnabled = false;
                moveInputMethodUpButton.IsEnabled = false;
                moveInputMethodDownButton.IsEnabled = false;
                moveInputMethodToTopButton.IsEnabled = false;
                moveInputMethodToBottomButton.IsEnabled = false;
            }
        }

        public void UpdateInputMappings()
        {
            UpdateInputDeviceButtons();
            inputDeviceNamesList.Items.Clear();
            if (GameConfig != null)
            {
                if (inputMethodsList.SelectedItem != null)
                {
                    if (inputMethodsList.SelectedItem is AIR_API.InputMappings.Device)
                    {
                        AIR_API.InputMappings.Device device = inputMethodsList.SelectedItem as AIR_API.InputMappings.Device;
                        aInputButton.Content = (device.A.Count() > 1 ? Program.LanguageResource.GetString("Input_MULTI") : device.A.FirstOrDefault());
                        bInputButton.Content = (device.B.Count() > 1 ? Program.LanguageResource.GetString("Input_MULTI") : device.B.FirstOrDefault());
                        xInputButton.Content = (device.X.Count() > 1 ? Program.LanguageResource.GetString("Input_MULTI") : device.X.FirstOrDefault());
                        yInputButton.Content = (device.Y.Count() > 1 ? Program.LanguageResource.GetString("Input_MULTI") : device.Y.FirstOrDefault());
                        upInputButton.Content = (device.Up.Count() > 1 ? Program.LanguageResource.GetString("Input_MULTI") : device.Up.FirstOrDefault());
                        downInputButton.Content = (device.Down.Count() > 1 ? Program.LanguageResource.GetString("Input_MULTI") : device.Down.FirstOrDefault());
                        leftInputButton.Content = (device.Left.Count() > 1 ? Program.LanguageResource.GetString("Input_MULTI") : device.Left.FirstOrDefault());
                        rightInputButton.Content = (device.Right.Count() > 1 ? Program.LanguageResource.GetString("Input_MULTI") : device.Right.FirstOrDefault());
                        startInputButton.Content = (device.Start.Count() > 1 ? Program.LanguageResource.GetString("Input_MULTI") : device.Start.FirstOrDefault());
                        backInputButton.Content = (device.Back.Count() > 1 ? Program.LanguageResource.GetString("Input_MULTI") : device.Back.FirstOrDefault());

                        if (aInputButton.Content == null) aInputButton.Content = Program.LanguageResource.GetString("Input_NONE");
                        if (bInputButton.Content == null) bInputButton.Content = Program.LanguageResource.GetString("Input_NONE");
                        if (xInputButton.Content == null) xInputButton.Content = Program.LanguageResource.GetString("Input_NONE");
                        if (yInputButton.Content == null) yInputButton.Content = Program.LanguageResource.GetString("Input_NONE");
                        if (upInputButton.Content == null) upInputButton.Content = Program.LanguageResource.GetString("Input_NONE");
                        if (downInputButton.Content == null) downInputButton.Content = Program.LanguageResource.GetString("Input_NONE");
                        if (leftInputButton.Content == null) leftInputButton.Content = Program.LanguageResource.GetString("Input_NONE");
                        if (rightInputButton.Content == null) rightInputButton.Content = Program.LanguageResource.GetString("Input_NONE");
                        if (startInputButton.Content == null) startInputButton.Content = Program.LanguageResource.GetString("Input_NONE");
                        if (backInputButton.Content == null) backInputButton.Content = Program.LanguageResource.GetString("Input_NONE");

                        UpdateInputDeviceNamesList(true);



                    }
                }
                else
                {

                    DisableMappings();
                }
            }
        }

        private void ToggleDeviceNamesUI(bool enabled)
        {
            inputDeviceNamesList.IsEnabled = enabled;
            addDeviceNameButton.IsEnabled = enabled;
            removeDeviceNameButton.IsEnabled = (enabled == true ? inputDeviceNamesList.SelectedItem != null : enabled);

            if (inputDeviceNamesList.SelectedItem != null)
            {
                moveDeviceNameUpButton.IsEnabled = inputDeviceNamesList.Items.IndexOf(inputDeviceNamesList.SelectedItem) > 0 && enabled;
                moveDeviceNameDownButton.IsEnabled = inputDeviceNamesList.Items.IndexOf(inputDeviceNamesList.SelectedItem) < inputDeviceNamesList.Items.Count - 1 && enabled;
                moveDeviceNameToTopButton.IsEnabled = inputDeviceNamesList.Items.IndexOf(inputDeviceNamesList.SelectedItem) > 0 && enabled;
                moveDeviceNameToBottomButton.IsEnabled = inputDeviceNamesList.Items.IndexOf(inputDeviceNamesList.SelectedItem) < inputDeviceNamesList.Items.Count - 1 && enabled;
            }
            else
            {
                moveDeviceNameUpButton.IsEnabled = false;
                moveDeviceNameDownButton.IsEnabled = false;
                moveDeviceNameToTopButton.IsEnabled = false;
                moveDeviceNameToBottomButton.IsEnabled = false;
            }
        }

        private void DisableMappings()
        {
            inputDeviceNamesList.Items.Clear();
            aInputButton.Content = (Program.LanguageResource.GetString("Input_NULL") == null ? "" : Program.LanguageResource.GetString("Input_NULL"));
            bInputButton.Content = (Program.LanguageResource.GetString("Input_NULL") == null ? "" : Program.LanguageResource.GetString("Input_NULL"));
            xInputButton.Content = (Program.LanguageResource.GetString("Input_NULL") == null ? "" : Program.LanguageResource.GetString("Input_NULL"));
            yInputButton.Content = (Program.LanguageResource.GetString("Input_NULL") == null ? "" : Program.LanguageResource.GetString("Input_NULL"));
            upInputButton.Content = (Program.LanguageResource.GetString("Input_NULL") == null ? "" : Program.LanguageResource.GetString("Input_NULL"));
            downInputButton.Content = (Program.LanguageResource.GetString("Input_NULL") == null ? "" : Program.LanguageResource.GetString("Input_NULL"));
            leftInputButton.Content = (Program.LanguageResource.GetString("Input_NULL") == null ? "" : Program.LanguageResource.GetString("Input_NULL"));
            rightInputButton.Content = (Program.LanguageResource.GetString("Input_NULL") == null ? "" : Program.LanguageResource.GetString("Input_NULL"));
            startInputButton.Content = (Program.LanguageResource.GetString("Input_NULL") == null ? "" : Program.LanguageResource.GetString("Input_NULL"));
            backInputButton.Content = (Program.LanguageResource.GetString("Input_NULL") == null ? "" : Program.LanguageResource.GetString("Input_NULL"));
            inputDeviceNamesList.Items.Add((Program.LanguageResource.GetString("Input_NULL") == null ? "" : Program.LanguageResource.GetString("Input_NULL")));
        }

        private void ChangeInputMappings(object sender)
        {
            AIR_API.InputMappings.Device device = inputMethodsList.SelectedItem as AIR_API.InputMappings.Device;

            if (sender.Equals(aInputButton)) ChangeMappings(ref device, "A");
            else if (sender.Equals(bInputButton)) ChangeMappings(ref device, "B");
            else if (sender.Equals(xInputButton)) ChangeMappings(ref device, "X");
            else if (sender.Equals(yInputButton)) ChangeMappings(ref device, "Y");
            else if (sender.Equals(upInputButton)) ChangeMappings(ref device, "Up");
            else if (sender.Equals(downInputButton)) ChangeMappings(ref device, "Down");
            else if (sender.Equals(leftInputButton)) ChangeMappings(ref device, "Left");
            else if (sender.Equals(rightInputButton)) ChangeMappings(ref device, "Right");
            else if (sender.Equals(startInputButton)) ChangeMappings(ref device, "Start");
            else if (sender.Equals(backInputButton)) ChangeMappings(ref device, "Back");

            void ChangeMappings(ref AIR_API.InputMappings.Device button, string input)
            {
                switch (input)
                {
                    case "A":
                        MappingDialog(ref button.A);
                        break;
                    case "B":
                        MappingDialog(ref button.B);
                        break;
                    case "X":
                        MappingDialog(ref button.X);
                        break;
                    case "Y":
                        MappingDialog(ref button.Y);
                        break;
                    case "Up":
                        MappingDialog(ref button.Up);
                        break;
                    case "Down":
                        MappingDialog(ref button.Down);
                        break;
                    case "Left":
                        MappingDialog(ref button.Left);
                        break;
                    case "Right":
                        MappingDialog(ref button.Right);
                        break;
                    case "Start":
                        MappingDialog(ref button.Start);
                        break;
                    case "Back":
                        MappingDialog(ref button.Back);
                        break;
                }
                UpdateInputMappings();

                void MappingDialog(ref List<string> mappings)
                {
                    var mD = new KeybindingsListDialog(mappings);
                    mD.ShowDialog();
                }

            }
        }

        private void AddInputDeviceName()
        {
            if (inputMethodsList.SelectedItem != null)
            {
                if (FileManagement.AddInputDeviceName(inputMethodsList.SelectedIndex) == true)
                {
                    UpdateInputMappings();
                }
            }

        }

        private void AddInputDevice()
        {
            if (FileManagement.AddInputDevice() == true)
            {
                RefreshInputMappings();
            }
        }

        private void RemoveInputDevice()
        {
            if (inputMethodsList.SelectedItem != null && inputMethodsList.SelectedItem is AIR_API.InputMappings.Device)
            {
                FileManagement.RemoveInputDevice(inputMethodsList.SelectedItem as AIR_API.InputMappings.Device);
            }
        }

        public void RefreshInputMappings()
        {
            if (S3AIRSettings.RawSettings is AIR_API.Raw.Settings.Interfaces.AIRSettingsMK2) InputDevicesHandler.InputDevices = S3AIRSettings.InputDevices;
            else InputDevicesHandler.InputDevices = S3AIRSettings.InputDevices;

            DisableMappings();
            CollectInputMappings();
            UpdateInputDeviceButtons();
        }

        private void ImportInputDevice()
        {
            if (GameConfig != null)
            {
                FileManagement.ImportInputMappings();
                RefreshInputMappings();
            }
        }

        private void ExportInputDevice()
        {
            if (inputMethodsList.SelectedItem != null)
            {
                if (inputMethodsList.SelectedItem is AIR_API.InputMappings.Device)
                {
                    AIR_API.InputMappings.Device device = inputMethodsList.SelectedItem as AIR_API.InputMappings.Device;
                    FileManagement.ExportInputMappings(device);
                }
            }
        }

        private void RemoveInputDeviceName()
        {
            if (inputMethodsList.SelectedItem != null && inputDeviceNamesList.SelectedItem != null)
            {
                if (FileManagement.RemoveInputDeviceName(inputDeviceNamesList.SelectedItem.ToString(), inputMethodsList.SelectedIndex, inputDeviceNamesList.SelectedIndex) == true)
                {
                    UpdateInputMappings();
                }
            }
        }

        private void UpdateInputDeviceNamesList(bool refreshItems = false)
        {
            if (GameConfig != null)
            {
                if (inputMethodsList.SelectedItem != null)
                {
                    if (inputMethodsList.SelectedItem is AIR_API.InputMappings.Device)
                    {
                        AIR_API.InputMappings.Device device = inputMethodsList.SelectedItem as AIR_API.InputMappings.Device;
                        if (device.HasDeviceNames)
                        {
                            if (refreshItems)
                            {
                                foreach (var name in device.DeviceNames)
                                {
                                    inputDeviceNamesList.Items.Add(name);
                                }
                            }
                            ToggleDeviceNamesUI(true);
                        }
                        else
                        {
                            inputDeviceNamesList.Items.Add((Program.LanguageResource.GetString("Input_UNSUPPORTED") == null ? "" : Program.LanguageResource.GetString("Input_UNSUPPORTED")));
                            ToggleDeviceNamesUI(false);
                        }
                    }
                }
            }
        }

        private void LaunchSystemGamepadSettings()
        {
            Process.Start("joy.cpl");

        }

        private void MoveInputMethod(FileManagement.MoveListItemDirection direction)
        {
            var Instance = this;
            FileManagement.MoveInputDevice(ref Instance, direction);
        }

        private void MoveDeviceName(FileManagement.MoveListItemDirection direction)
        {
            var Instance = this;
            FileManagement.MoveInputDeviceIdentifier(ref Instance, direction);
        }

        #endregion

        #region Mod Management

        public void UpdateModListItemCheck(bool shouldEnd)
        {
            if (shouldEnd) ModViewer.ItemCheck = null;
            else ModViewer.ItemCheck = ModsList_ItemCheck;
        }

        #endregion

        #region Launching Events

        private void AddRemoveURLHandlerButton_Click(object sender, RoutedEventArgs e)
        {
            string ModLoaderPath = System.Reflection.Assembly.GetExecutingAssembly().Location;
            string InstallerPath = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) + "//GameBanana API Installer.exe";
            Process.Start($"\"{InstallerPath}\"", $"\"{ModLoaderPath}\"");
        }

        private void OpenEXEFolder()
        {
            if (ProgramPaths.Sonic3AIRPath != null || ProgramPaths.Sonic3AIRPath != "")
            {
                string filename = ProgramPaths.Sonic3AIRPath;
                Process.Start(Path.GetDirectoryName(filename));
            }
            else
            {
                if (GameHandler.UpdateSonic3AIRLocation())
                {
                    UpdateAIRSettings();
                    string filename = ProgramPaths.Sonic3AIRPath;
                    Process.Start(Path.GetDirectoryName(filename));
                }
            }
        }

        private void OpenAppDataFolder()
        {
            Process.Start(ProgramPaths.Sonic3AIRAppDataFolder);
        }

        private void OpenModsFolder()
        {
            Process.Start(ProgramPaths.Sonic3AIRModsFolder);
        }

        private void OpenSelectedModFolder(ModViewerItem mod)
        {
            Process.Start(mod.Source.FolderPath);
        }

        private void OpenConfigFile()
        {
            if (ProgramPaths.Sonic3AIRPath != null || ProgramPaths.Sonic3AIRPath != "")
            {
                string filename = ProgramPaths.Sonic3AIRPath;
                if (File.Exists(Path.GetDirectoryName(filename) + "//config.json"))
                {
                    Process.Start(Path.GetDirectoryName(filename) + "//config.json");
                }
            }
            else
            {
                if (GameHandler.UpdateSonic3AIRLocation())
                {
                    UpdateAIRSettings();
                    string filename = ProgramPaths.Sonic3AIRPath;
                    if (File.Exists(Path.GetDirectoryName(filename) + "//config.json"))
                    {
                        Process.Start(Path.GetDirectoryName(filename) + "//config.json");
                    }
                }
            }
        }

        private void OpenLogFile()
        {
            if (File.Exists(ProgramPaths.Sonic3AIRAppDataFolder + "//logfile.txt"))
            {
                Process.Start(ProgramPaths.Sonic3AIRAppDataFolder + "//logfile.txt");
            }
            else
            {
                MessageBox.Show($"{Program.LanguageResource.GetString("LogFileNotFound")}: {nL}{ProgramPaths.Sonic3AIRAppDataFolder}\\logfile.txt");
            }

        }

        private void OpenModdingTemplatesFolder()
        {
            if (ProgramPaths.Sonic3AIRPath != null || ProgramPaths.Sonic3AIRPath != "")
            {
                if (ProgramPaths.ValidateSonic3AIRModdingTemplatesFolderPath()) Process.Start(ProgramPaths.Sonic3AIRModdingTemplatesFolder);
            }
            else
            {
                if (GameHandler.UpdateSonic3AIRLocation())
                {
                    UpdateAIRSettings();
                    if (ProgramPaths.ValidateSonic3AIRModdingTemplatesFolderPath()) Process.Start(ProgramPaths.Sonic3AIRModdingTemplatesFolder);
                }
            }
        }

        private void OpenSampleModsFolder()
        {
            if (ProgramPaths.Sonic3AIRPath != null || ProgramPaths.Sonic3AIRPath != "")
            {
                if (ProgramPaths.ValidateSonic3AIRSampleModsFolderPath()) Process.Start(ProgramPaths.Sonic3AIRSampleModsFolder);
            }
            else
            {
                if (GameHandler.UpdateSonic3AIRLocation())
                {
                    UpdateAIRSettings();
                    if (ProgramPaths.ValidateSonic3AIRSampleModsFolderPath()) Process.Start(ProgramPaths.Sonic3AIRSampleModsFolder);
                }
            }
        }

        private void OpenUserManual()
        {
            if (ProgramPaths.Sonic3AIRPath != null || ProgramPaths.Sonic3AIRPath != "")
            {
                if (ProgramPaths.ValidateSonic3AIRUserManualFilePath()) OpenPDFViewer(ProgramPaths.Sonic3AIRUserManualFile);
            }
            else
            {
                if (GameHandler.UpdateSonic3AIRLocation())
                {
                    UpdateAIRSettings();
                    if (ProgramPaths.ValidateSonic3AIRUserManualFilePath()) OpenPDFViewer(ProgramPaths.Sonic3AIRUserManualFile);
                }
            }
        }

        private void OpenModDocumentation()
        {
            if (ProgramPaths.Sonic3AIRPath != null || ProgramPaths.Sonic3AIRPath != "")
            {
                if (ProgramPaths.ValidateSonic3AIRModDocumentationFilePath()) OpenPDFViewer(ProgramPaths.Sonic3AIRModDocumentationFile);
            }
            else
            {
                if (GameHandler.UpdateSonic3AIRLocation())
                {
                    UpdateAIRSettings();
                    if (ProgramPaths.ValidateSonic3AIRModDocumentationFilePath()) OpenPDFViewer(ProgramPaths.Sonic3AIRModDocumentationFile);
                }
            }
        }

        private void OpenPDFViewer(string file)
        {
            DocumentationViewer viewer = new DocumentationViewer();
            viewer.ShowDialog(file);
        }

        private void OpenModURL(string url)
        {
            try
            {
                if (url != "")
                {
                    Process.Start(url);
                }

            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }

        }

        private void OpenSettingsFile()
        {
            Process.Start(ProgramPaths.Sonic3AIRAppDataFolder + "//settings.json");
        }

        private void OpenRecordingLocation()
        {
            if (GameRecordingList.SelectedItem != null)
            {
                AIR_API.Recording item = GameRecordingList.SelectedItem as AIR_API.Recording;
                if (File.Exists(item.FilePath))
                {
                    Process.Start("explorer.exe", "/select, " + item.FilePath);
                }
            }

        }

        #endregion

        #region Protocol Handler

        public void CreateGameBananaShortcut()
        {
            ProgramPaths.Sonic3AIRGBLinkPath = ProgramPaths.Sonic3AIRAppDataFolder + "//AIRModLoader.lnk";
            IWshRuntimeLibrary.WshShell shell = new IWshRuntimeLibrary.WshShell();
            IWshRuntimeLibrary.IWshShortcut shortcut = shell.CreateShortcut(ProgramPaths.Sonic3AIRGBLinkPath) as IWshRuntimeLibrary.IWshShortcut;
            shortcut.TargetPath = System.Reflection.Assembly.GetExecutingAssembly().Location;
            shortcut.WorkingDirectory = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
            shortcut.Save();
        }




        #endregion

        #region AIR EXE Version Handler Toolstrip / Path Management

        private void AIRVersionZIPToolStripMenuItem_Click(object sender, RoutedEventArgs e)
        {
            FileManagement.InstallVersionFromZIP();
        }

        private void AIRPathMenuStrip_ContextMenuOpening(object sender, ContextMenuEventArgs e)
        {
            UpdateAIRVersionsToolstrips();
        }

        private void ManageAIRVersionsMenuItem_Click(object sender, RoutedEventArgs e)
        {
            GoToAIRVersionManagement();
        }

        private void GoToAIRVersionManagement()
        {
            settingsPage.IsSelected = true;
            PrimaryTabControl.SelectedItem = settingsPage;
            versionsPage.IsSelected = true;
            OptionsTabControl.SelectedItem = versionsPage;
        }

        private void UpdateAIRVersionsToolstrips()
        {
            CleanUpInstalledVersionsToolStrip();
            if (Directory.Exists(ProgramPaths.Sonic3AIR_MM_VersionsFolder))
            {
                DirectoryInfo directoryInfo = new DirectoryInfo(ProgramPaths.Sonic3AIR_MM_VersionsFolder);
                var folders = directoryInfo.GetDirectories().ToList();
                if (folders.Count != 0)
                {
                    foreach (var folder in folders.VersionSort().Reverse())
                    {
                        string filePath = Path.Combine(folder.FullName, "sonic3air_game", "Sonic3AIR.exe");
                        if (File.Exists(filePath))
                        {
                            ChangeAIRVersionMenuItem.Items.Add(GenerateInstalledVersionsToolstripItem(folder.Name, filePath));
                            ChangeAIRVersionFileMenuItem.Items.Add(GenerateInstalledVersionsToolstripItem(folder.Name, filePath));
                        }


                    }
                }

            }

        }

        private void CleanUpInstalledVersionsToolStrip()
        {
            foreach (var item in ChangeAIRVersionMenuItem.Items.Cast<MenuItem>())
            {
                item.Click -= ChangeAIRPathByInstalls;
            }
            foreach (var item in ChangeAIRVersionFileMenuItem.Items.Cast<MenuItem>())
            {
                item.Click -= ChangeAIRPathByInstalls;
            }
            ChangeAIRVersionMenuItem.Items.Clear();
            ChangeAIRVersionFileMenuItem.Items.Clear();
        }

        private MenuItem GenerateInstalledVersionsToolstripItem(string name, string filepath)
        {
            MenuItem item = new MenuItem();
            item.Header = name;
            item.Tag = filepath;
            item.Click += ChangeAIRPathByInstalls;
            return item;
        }

        private void ChangeAIRPathByInstalls(object sender, EventArgs e)
        {
            MenuItem item = sender as MenuItem;
            ProgramPaths.Sonic3AIRPath = item.Tag.ToString();
            Properties.Settings.Default.Save();
            UpdateAIRSettings();
        }

        private void ChangeAIRPathFromSettings()
        {
            if (S3AIRSettings != null)
            {
                if (S3AIRSettings.HasEXEPath)
                {
                    if (File.Exists(S3AIRSettings.AIREXEPath))
                    {
                        ProgramPaths.Sonic3AIRPath = S3AIRSettings.AIREXEPath;
                        Properties.Settings.Default.Save();
                        UpdateAIRSettings();
                    }
                    else
                    {
                        MessageBox.Show(Program.LanguageResource.GetString("AIRChangePathNoLongerExists"));
                    }
                }
            }
        }
        #endregion

        #region A.I.R. Version Manager List

        public void RefreshVersionsList(bool fullRefresh = false)
        {
            if (fullRefresh)
            {
                VersionsListView.Items.Clear();
                DirectoryInfo directoryInfo = new DirectoryInfo(ProgramPaths.Sonic3AIR_MM_VersionsFolder);
                var folders = directoryInfo.GetDirectories().ToList();
                if (folders.Count != 0)
                {
                    foreach (var folder in folders.VersionSort().Reverse())
                    {
                        string filePath = Path.Combine(folder.FullName, "sonic3air_game", "Sonic3AIR.exe");
                        if (File.Exists(filePath))
                        {
                            VersionsListView.Items.Add(new FileManagement.AIRVersionListItem(folder.Name, folder.FullName));
                        }


                    }
                }
            }

            bool enabled = VersionsListView.SelectedItem != null;
            removeVersionButton.IsEnabled = enabled;
            openVersionLocationButton.IsEnabled = enabled;
        }
        private void VersionsListBox_SelectedValueChanged(object sender, SelectionChangedEventArgs e)
        {
            RefreshVersionsList();
        }

        private void TabControl2_SelectedIndexChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.Source is TabControl)
            {
                if (OptionsTabControl.SelectedItem == versionsPage)
                {
                    RefreshVersionsList(true);
                }
                else if (OptionsTabControl.SelectedItem == gameOptionsPage || OptionsTabControl.SelectedItem == inputPage)
                {
                    CollectGameConfig();
                    RetriveLaunchOptions();
                    if (OptionsTabControl.SelectedItem == inputPage)
                    {
                        RefreshInputMappings();
                    }
                }
            }
        }

        private void OpenVersionLocationButton_Click(object sender, RoutedEventArgs e)
        {
            if (VersionsListView.SelectedItem != null && VersionsListView.SelectedItem is FileManagement.AIRVersionListItem)
            {
                FileManagement.AIRVersionListItem item = VersionsListView.SelectedItem as FileManagement.AIRVersionListItem;
                Process.Start(item.FilePath);
            }
        }

        private void RemoveVersionButton_Click(object sender, RoutedEventArgs e)
        {
            FileManagement.RemoveVersion(VersionsListView.SelectedItem);
        }

        #endregion

        #region Selected Mod Modification

        private void editModFolderToolStripMenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (ModViewer.SelectedItem != null)
            {
                var item = (ModViewer.SelectedItem as ModViewerItem);
                var parent = this as Window;
                ConfigEditorDialog cfg = new ConfigEditorDialog(ref parent);
                if(cfg.ShowConfigEditDialog(item.Source).Value == true)
                {
                    (ModViewer.SelectedItem as ModViewerItem).Source.Name = cfg.EditorNameField.Text;
                    (ModViewer.SelectedItem as ModViewerItem).Source.Author = cfg.EditorAuthorField.Text;
                    (ModViewer.SelectedItem as ModViewerItem).Source.Description = cfg.EditorDescriptionField.Text;
                    (ModViewer.SelectedItem as ModViewerItem).Source.URL = cfg.EditorURLField.Text;
                    (ModViewer.SelectedItem as ModViewerItem).Source.GameVersion = cfg.EditorGameVersionField.Text;
                    (ModViewer.SelectedItem as ModViewerItem).Source.ModVersion = cfg.EditorModVersionField.Text;

                    (ModViewer.SelectedItem as ModViewerItem).Source.Save();
                    ModManagement.UpdateModsList(true);
                }
            }
        }

        #endregion

        #region Move Mod to Subfolder Methods

        private void addNewModSubfolderMenuItem_Click(object sender, RoutedEventArgs e)
        {
            FileManagement.AddNewModSubfolder(ModViewer.View.SelectedItem);
        }

        private void moveModToSubFolderMenuItem_ContextMenuOpening(object sender, RoutedEventArgs e)
        {
            List<MenuItem> ItemsToRemove = new List<MenuItem>();
            foreach (var item in moveModToSubFolderMenuItem.Items)
            {
                if (item is MenuItem && !item.Equals(addNewModSubfolderMenuItem))
                {
                    ItemsToRemove.Add(item as MenuItem);
                }
            }
            foreach (var item in ItemsToRemove)
            {
                int index = moveModToSubFolderMenuItem.Items.IndexOf(item);
                (moveModToSubFolderMenuItem.Items[index] as MenuItem).Click -= SubDirectoryMove_Click;
                moveModToSubFolderMenuItem.Items.Remove(item);
            }
            ItemsToRemove.Clear();


            foreach (var item in ModViewer.FolderView.Items)
            {
                SubDirectoryItem realItem;
                if (item is SubDirectoryItem) realItem = item as SubDirectoryItem;
                else realItem = null;

                if (realItem != null)
                {
                    var menuItem = GenerateSubDirectoryToolstripItem(realItem.FileName, realItem.FilePath);
                    moveModToSubFolderMenuItem.Items.Add(menuItem);
                }
            }

        }

        private MenuItem GenerateSubDirectoryToolstripItem(string name, string filepath)
        {
            MenuItem item = new MenuItem();
            item.Header = name;
            item.Tag = filepath;
            item.Click += SubDirectoryMove_Click;
            return item;
        }

        private void SubDirectoryMove_Click(object sender, RoutedEventArgs e)
        {
            if (ModViewer.View.SelectedItem != null && ModViewer.View.SelectedItem is ModViewerItem)
            {
                FileManagement.MoveMod((ModViewer.View.SelectedItem as ModViewerItem).Source, (sender as MenuItem).Tag.ToString());
            }
            else if (ModManagement.S3AIRActiveMods.UseLegacyLoading)
            {
                if (ModViewer.ActiveView.SelectedItem != null && ModViewer.ActiveView.SelectedItem is ModViewerItem)
                {
                    FileManagement.MoveMod((ModViewer.ActiveView.SelectedItem as ModViewerItem).Source, (sender as MenuItem).Tag.ToString());
                }
            }



        }

        #endregion

        #region Error Message Helpers

        private void HyperlinkToGeneralTabAIRPath()
        {
            settingsPage.IsSelected = true;
            PrimaryTabControl.SelectedItem = settingsPage;
            optionsPage.IsSelected = true;
            OptionsTabControl.SelectedItem = optionsPage;

        }

        private void OpenAIRPathSettings(object sender, MouseButtonEventArgs e)
        {
            if (sender.Equals(LaunchOptionsGroup) && LaunchOptionsFailureMessageBackground.Visibility == Visibility.Visible) HyperlinkToGeneralTabAIRPath();
            else if (!sender.Equals(LaunchOptionsGroup)) HyperlinkToGeneralTabAIRPath();

        }


        #endregion

        #region Game Recording Management

        private void CollectGameRecordings()
        {
            GameRecordingList.Items.Clear();

            if (ProgramPaths.GameRecordingsFolderDesiredPath == ProgramPaths.GameRecordingSearchLocation.S3AIR_Custom)
            {
                if (Directory.Exists(ProgramPaths.CustomGameRecordingsFolderPath))
                {
                    RecordingsLocationBrowse.Content = $"{UserLanguage.GetOutputString("CustomWordString")} ({ProgramPaths.CustomGameRecordingsFolderPath})"; 
                }
                else
                {
                    RecordingsLocationBrowse.Content = RecordingsLocationBrowse.Tag.ToString();
                }
                RecordingsSelectedLocationBrowseButton.IsEnabled = true;
            }
            else
            {
                RecordingsSelectedLocationBrowseButton.IsEnabled = false;
            }

            if (ProgramPaths.DoesSonic3AIRGameRecordingsFolderPathExist())
            {
                recordingsErrorMessagePanel.Visibility = Visibility.Collapsed;

                string baseDirectory = ProgramPaths.GetSonic3AIRGameRecordingsFolderPath();
                if (Directory.Exists(baseDirectory))
                {
                    Regex reg = new Regex(@"(gamerecording_)\d{6}(_)\d{6}");
                    DirectoryInfo directoryInfo = new DirectoryInfo(baseDirectory);
                    FileInfo[] fileInfo = directoryInfo.GetFiles("*.bin").Where(path => reg.IsMatch(path.Name)).ToArray();
                    foreach (var file in fileInfo)
                    {
                        AIR_API.Recording recording = new AIR_API.Recording(file);
                        GameRecordingList.Items.Add(recording);
                    }
                }
            }
            else
            {
                recordingsErrorMessage.Text = recordingsErrorMessage.Tag.ToString().Replace("{0}", UserLanguage.FolderOrFileDoesNotExist(ProgramPaths.GetSonic3AIRGameRecordingsFolderPath(), false));
                recordingsErrorMessagePanel.Visibility = Visibility.Visible;
            }

        }

        private void RecordingsSelectedLocationCombobox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (HasInitilizationCompleted)
            {
                if (RecordingsSelectedLocationCombobox.SelectedItem == RecordingsLocationDefault)
                {
                    ProgramPaths.GameRecordingsFolderDesiredPath = ProgramPaths.GameRecordingSearchLocation.S3AIR_Default;
                    CollectGameRecordings();
                }
                else if (RecordingsSelectedLocationCombobox.SelectedItem == RecordingsLocationAppData)
                {
                    ProgramPaths.GameRecordingsFolderDesiredPath = ProgramPaths.GameRecordingSearchLocation.S3AIR_AppData;
                    CollectGameRecordings();
                }
                else if (RecordingsSelectedLocationCombobox.SelectedItem == RecordingsLocationRecordingsFolder)
                {
                    ProgramPaths.GameRecordingsFolderDesiredPath = ProgramPaths.GameRecordingSearchLocation.S3AIR_RecordingsFolder;
                    CollectGameRecordings();
                }
                else if (RecordingsSelectedLocationCombobox.SelectedItem == RecordingsLocationEXEFolder)
                {
                    ProgramPaths.GameRecordingsFolderDesiredPath = ProgramPaths.GameRecordingSearchLocation.S3AIR_EXE_Folder;
                    CollectGameRecordings();
                }
                else if (RecordingsSelectedLocationCombobox.SelectedItem == RecordingsLocationBrowse)
                {
                    ProgramPaths.GameRecordingsFolderDesiredPath = ProgramPaths.GameRecordingSearchLocation.S3AIR_Custom;
                    if (!Directory.Exists(ProgramPaths.CustomGameRecordingsFolderPath)) SearchForCustomGameRecordingFolder();
                    else CollectGameRecordings();
                }
            }

        }

        private void RecordingsSelectedLocationBrowseButton_Click(object sender, RoutedEventArgs e)
        {
            SearchForCustomGameRecordingFolder();
        }

        private void SearchForCustomGameRecordingFolder()
        {
            ProgramPaths.SetCustomGameRecordingsFolderPath();
            CollectGameRecordings();
        }

        private Dictionary<string, string> RecordingVersions = new Dictionary<string, string>();

        #region AIR Gamerecording Playback Feature
        private void playbackRecordingButton_Click(object sender, RoutedEventArgs e)
        {
            playbackRecordingButton.ContextMenu.IsOpen = true;
            UpdateAIRVersionsForPlaybackToolstrips();
        }

        private void playUsingCurrentVersionMenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (GameRecordingList.SelectedItem != null && GameRecordingList.SelectedItem is AIR_API.Recording)
            {
                CollectGameConfig();
                var recordingFile = GameRecordingList.SelectedItem as AIR_API.Recording;
                GameHandler.LaunchGameRecording(recordingFile.FilePath, ProgramPaths.Sonic3AIRPath);
                UpdateInGameButtons();
            }
        }

        private void PlaybackContextMenu_ContextMenuOpening(object sender, ContextMenuEventArgs e)
        {
            UpdateAIRVersionsForPlaybackToolstrips();
        }

        private void UpdateAIRVersionsForPlaybackToolstrips()
        {
            GameRecordingList_SelectedIndexChanged(null, null);
            CleanUpInstalledVersionsForPlaybackToolStrip();
            PlayUsingOtherVersionMenuItem.IsEnabled = false;
            PlayUsingOtherVersionHoverMenuItem.IsEnabled = false;
            if (Directory.Exists(ProgramPaths.Sonic3AIR_MM_VersionsFolder))
            {
                DirectoryInfo directoryInfo = new DirectoryInfo(ProgramPaths.Sonic3AIR_MM_VersionsFolder);
                var folders = directoryInfo.GetDirectories().ToList();
                if (folders.Count != 0)
                {
                    foreach (var folder in folders.VersionSort().Reverse())
                    {
                        string filePath = Path.Combine(folder.FullName, "sonic3air_game", "Sonic3AIR.exe");
                        if (File.Exists(filePath))
                        {
                            PlayUsingOtherVersionMenuItem.Items.Add(GenerateInstalledVersionsForPlaybackToolstripItem(folder.Name, filePath));
                            PlayUsingOtherVersionHoverMenuItem.Items.Add(GenerateInstalledVersionsForPlaybackToolstripItem(folder.Name, filePath));
                            string versionID = folder.Name;
                            if (File.Exists(Path.Combine(folder.FullName, "sonic3air_game","data","metadata.json")))
                            {
                                AIR_API.VersionMetadata meta = new AIR_API.VersionMetadata(new FileInfo(Path.Combine(folder.FullName, "sonic3air_game", "data", "metadata.json")));
                                versionID = meta.VersionString;

                                PlayUsingOtherVersionMenuItem.IsEnabled = true;
                                PlayUsingOtherVersionHoverMenuItem.IsEnabled = true;
                            }
                            RecordingVersions.Add(versionID, filePath);
                        }


                    }
                }

            }

        }

        private void CleanUpInstalledVersionsForPlaybackToolStrip()
        {
            foreach (var item in PlayUsingOtherVersionMenuItem.Items.Cast<MenuItem>())
            {
                item.Click -= ChangeAIRPathByInstalls;
            }
            foreach (var item in PlayUsingOtherVersionHoverMenuItem.Items.Cast<MenuItem>())
            {
                item.Click -= ChangeAIRPathByInstalls;
            }
            PlayUsingOtherVersionMenuItem.Items.Clear();
            PlayUsingOtherVersionHoverMenuItem.Items.Clear();
            RecordingVersions.Clear();
        }

        private MenuItem GenerateInstalledVersionsForPlaybackToolstripItem(string name, string filepath)
        {
            MenuItem item = new MenuItem();
            item.Header = name;
            item.Tag = filepath;
            item.Click += LaunchPlaybackOnThisVersion;
            return item;
        }

        private void LaunchPlaybackOnThisVersion(object sender, EventArgs e)
        {
            MenuItem item = sender as MenuItem;
            if (GameRecordingList.SelectedItem != null && GameRecordingList.SelectedItem is AIR_API.Recording)
            {
                CollectGameConfig();
                var recordingFile = GameRecordingList.SelectedItem as AIR_API.Recording;
                GameHandler.LaunchGameRecording(recordingFile.FilePath, item.Tag.ToString());
                UpdateInGameButtons();
            }
        }

        private void playUsingMatchingVersionMenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (GameRecordingList.SelectedItem != null && GameRecordingList.SelectedItem is AIR_API.Recording)
            {
                CollectGameConfig();
                var recordingFile = GameRecordingList.SelectedItem as AIR_API.Recording;
                if (RecordingVersions.Keys.ToList().Contains(recordingFile.AIRVersion))
                {
                    var exe_path = RecordingVersions.Where(x => x.Key == recordingFile.AIRVersion).FirstOrDefault().Value;
                    GameHandler.LaunchGameRecording(recordingFile.FilePath, exe_path);
                    UpdateInGameButtons();
                }

            }
        }

        private void PlaybackContextMenu_ContextMenuOpening(object sender, RoutedEventArgs e)
        {
            UpdateAIRVersionsForPlaybackToolstrips();
        }

        private void RecordingsViewer_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Right)
            {
                if (GameRecordingList.SelectedItem != null && GameRecordingList.SelectedItem is AIR_API.Recording && GameRecordingList.IsMouseOver)
                {
                    recordingsPanel.ContextMenu.IsOpen = !GameHandler.isGameRunning;
                }
            }

        }




        #endregion

        #endregion

        #region Mod Manager Settings Management



        #region Mod Collections / Launch Presets Mananagement
        private void FileMenuItem_SubmenuOpened(object sender, RoutedEventArgs e)
        {
            UpdateAIRVersionsToolstrips();
            CollectModCollectionMenuItemsDictionary();
            CollectLaunchPresetsMenuItemsDictionary();
        }

        private void CollectModCollectionMenuItemsDictionary()
        {

            LoadModCollectionMenuItem.RecentItemsSource = null;
            RenameModCollectionMenuItem.RecentItemsSource = null;
            DeleteModCollectionMenuItem.RecentItemsSource = null;
            SaveModCollectonAsMenuItem.RecentItemsSource = null;

            if (ModCollectionMenuItems.ContainsKey(0)) ModCollectionMenuItems[0].Clear();
            if (ModCollectionMenuItems.ContainsKey(1)) ModCollectionMenuItems[1].Clear();
            if (ModCollectionMenuItems.ContainsKey(2)) ModCollectionMenuItems[2].Clear();
            if (ModCollectionMenuItems.ContainsKey(3)) ModCollectionMenuItems[3].Clear();

            ModCollectionMenuItems.Clear();
            for (int i = 0; i < 4; i++)
            {
                ModCollectionMenuItems.Add(i, CollectModCollectionsMenuItems());
            }

            LoadModCollectionMenuItem.RecentItemsSource = ModCollectionMenuItems[0];
            RenameModCollectionMenuItem.RecentItemsSource = ModCollectionMenuItems[1];
            DeleteModCollectionMenuItem.RecentItemsSource = ModCollectionMenuItems[2];
            SaveModCollectonAsMenuItem.RecentItemsSource = ModCollectionMenuItems[3];


        }

        private List<GenerationsLib.WPF.Controls.RecentsListMenuItem.RecentItem> CollectModCollectionsMenuItems()
        {
            List<GenerationsLib.WPF.Controls.RecentsListMenuItem.RecentItem> collections = new List<GenerationsLib.WPF.Controls.RecentsListMenuItem.RecentItem>();
            foreach (var collection in Settings.Options.ModCollections)
            {
                collections.Add(new GenerationsLib.WPF.Controls.RecentsListMenuItem.RecentItem(collection.Name, collection));
            }
            return collections;
        }

        private void CollectLaunchPresetsMenuItemsDictionary()
        {
            LoadLaunchPresetsMenuItem.RecentItemsSource.Clear();
            RenameLaunchPresetsMenuItem.RecentItemsSource.Clear();
            DeleteLaunchPresetsMenuItem.RecentItemsSource.Clear();
            SaveLaunchPresetAsMenuItem.RecentItemsSource.Clear();

            LoadLaunchPresetsMenuItem.RecentItemsSource = null;
            RenameLaunchPresetsMenuItem.RecentItemsSource = null;
            DeleteLaunchPresetsMenuItem.RecentItemsSource = null;
            SaveLaunchPresetAsMenuItem.RecentItemsSource = null;

            LoadLaunchPresetsMenuItem.RecentItemsSource = CollectLaunchPresetsMenuItems();
            RenameLaunchPresetsMenuItem.RecentItemsSource = CollectLaunchPresetsMenuItems();
            DeleteLaunchPresetsMenuItem.RecentItemsSource = CollectLaunchPresetsMenuItems();
            SaveLaunchPresetAsMenuItem.RecentItemsSource = CollectLaunchPresetsMenuItems();

        }

        private List<GenerationsLib.WPF.Controls.RecentsListMenuItem.RecentItem> CollectLaunchPresetsMenuItems()
        {
            List<GenerationsLib.WPF.Controls.RecentsListMenuItem.RecentItem> collections = new List<GenerationsLib.WPF.Controls.RecentsListMenuItem.RecentItem>();
            foreach (var collection in Settings.Options.LaunchPresets)
            {
                collections.Add(new GenerationsLib.WPF.Controls.RecentsListMenuItem.RecentItem(collection.Name, collection));
            }
            return collections;
        }

        private void LoadLaunchPresetsMenuItem_RecentItemSelected(object sender, GenerationsLib.WPF.Controls.RecentsListMenuItem.RecentItem e)
        {
            
        }

        private void RenameLaunchPresetsMenuItem_RecentItemSelected(object sender, GenerationsLib.WPF.Controls.RecentsListMenuItem.RecentItem e)
        {

        }

        private void DeleteLaunchPresetsMenuItem_RecentItemSelected(object sender, GenerationsLib.WPF.Controls.RecentsListMenuItem.RecentItem e)
        {

        }

        private void SaveLaunchPresetAsMenuItem_RecentItemSelected(object sender, GenerationsLib.WPF.Controls.RecentsListMenuItem.RecentItem e)
        {

        }

        private void DeleteAllLaunchPresetsMenuItem_Click(object sender, RoutedEventArgs e)
        {

        }

        private void SaveLaunchPresetMenuItem_Click(object sender, RoutedEventArgs e)
        {

        }

        private void LoadModCollectionMenuItem_RecentItemSelected(object sender, GenerationsLib.WPF.Controls.RecentsListMenuItem.RecentItem e)
        {
            var collection = e.Content as Settings.ModCollection;
            ModManagement.S3AIRActiveMods.Save(collection.Mods);
            ModManagement.UpdateModsList(true);
        }

        private void RenameModCollectionMenuItem_RecentItemSelected(object sender, GenerationsLib.WPF.Controls.RecentsListMenuItem.RecentItem e)
        {
            var collection = e.Content as Settings.ModCollection;
            string name = collection.Name;
            string caption = UserLanguage.GetOutputString("ModCollectionDialog_Caption_Rename");
            string message = UserLanguage.GetOutputString("ModCollectionDialog_Message_Rename");
            var result = ExtraDialog.ShowInputDialog(ref name, caption, message);
            if (result == System.Windows.Forms.DialogResult.OK)
            {
                ModManagement.Save();
                int collectionsIndex = Settings.Options.ModCollections.IndexOf(collection);
                Settings.Options.ModCollections[collectionsIndex].Name = name;
                SaveModManagerSettings();
            }
        }

        private void DeleteModCollectionMenuItem_RecentItemSelected(object sender, GenerationsLib.WPF.Controls.RecentsListMenuItem.RecentItem e)
        {
            var collection = e.Content as Settings.ModCollection;
            string caption = UserLanguage.GetOutputString("ModCollectionDialog_Caption_Delete");
            string message = string.Format(UserLanguage.GetOutputString("ModCollectionDialog_Message_Delete"), collection.Name);
            if (MessageBox.Show(message, caption, MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == System.Windows.Forms.DialogResult.Yes)
            {
                Settings.Options.ModCollections.Remove(collection);
                SaveModManagerSettings();
            }
        }

        private void SaveModCollectonAsMenuItem_RecentItemSelected(object sender, GenerationsLib.WPF.Controls.RecentsListMenuItem.RecentItem e)
        {
            string caption = UserLanguage.GetOutputString("ModCollectionDialog_Caption_Replace");
            string message = UserLanguage.GetOutputString("ModCollectionDialog_Message_Replace");
            if (MessageBox.Show(message, caption, MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == System.Windows.Forms.DialogResult.Yes)
            {
                ModManagement.Save();
                var collection = e.Content as Settings.ModCollection;
                int collectionsIndex = Settings.Options.ModCollections.IndexOf(collection);
                Settings.Options.ModCollections[collectionsIndex] = new Sonic3AIR_ModManager.Settings.ModCollection(ModManagement.S3AIRActiveMods.ActiveClass, collection.Name);
                SaveModManagerSettings();
            }
        }

        private void DeleteAllModCollectionsMenuItem_Click(object sender, RoutedEventArgs e)
        {
            string caption = UserLanguage.GetOutputString("ModCollectionDialog_Caption_DeleteAll");
            string message = UserLanguage.GetOutputString("ModCollectionDialog_Message_DeleteAll");
            if (MessageBox.Show(message, caption, MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == System.Windows.Forms.DialogResult.Yes)
            {
                Settings.Options.ModCollections.Clear();
                SaveModManagerSettings();
            }
        }

        private void SaveModCollectonMenuItem_Click(object sender, RoutedEventArgs e)
        {
            string name = UserLanguage.GetOutputString("ModCollectionDialog_Name_Save");
            string caption = UserLanguage.GetOutputString("ModCollectionDialog_Caption_Save");
            string message = UserLanguage.GetOutputString("ModCollectionDialog_Message_Save");
            var result = ExtraDialog.ShowInputDialog(ref name, caption, message);
            if (result == System.Windows.Forms.DialogResult.OK)
            {
                ModManagement.Save();
                Settings.Options.ModCollections.Add(new Sonic3AIR_ModManager.Settings.ModCollection(ModManagement.S3AIRActiveMods.ActiveClass, name));
                SaveModManagerSettings();
            }

        }

        #endregion

        private void SaveModManagerSettings()
        {
            Settings.Save();
        }

        private void LoadModManagerSettings()
        {
            Settings = null;
            Settings = new Settings.ModManagerSettings(ProgramPaths.Sonic3AIR_MM_SettingsFile);
        }



        #endregion

        private void FullDebugOutputCheckBox_Click(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.Save();
        }
    }
}