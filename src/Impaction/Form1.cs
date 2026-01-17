using Guna.UI2.WinForms;
using Microsoft.Win32;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Net.Http;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Impaction
{
    public partial class Form1 : Form
    {
        private Guna2Button activeButton = null;
        private List<Guna2Button> menuButtons;
        public Form1()
        {
            InitializeComponent();
            Load += async (s, e) => await LoadClientVersionUpload();
            this.Load += Form1_Load;
            panel11.Hide();
            panel12.Hide();
            panel15.Hide();
            RefreshListBox();

            menuButtons = new List<Guna2Button>
        {
            guna2Button1,
            guna2Button2,
            guna2Button3,
            guna2Button17
        };

            foreach (var btn in menuButtons)
            {
                btn.Click += MenuButton_Click;
                btn.FillColor = Color.FromArgb(15, 15, 15);
                btn.CustomBorderColor = Color.Transparent;
            }
            CreateShortcut();
        }
        private void CreateShortcut()
        {
            string exePath = Application.ExecutablePath;
            string appName = Path.GetFileNameWithoutExtension(exePath);

            string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
            string startMenuPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.StartMenu),
                "Programs"
            );

            CreateLnk(Path.Combine(desktopPath, appName + ".lnk"), exePath);
            CreateLnk(Path.Combine(startMenuPath, appName + ".lnk"), exePath);
        }

        private void CreateLnk(string shortcutPath, string exePath)
        {
            IShellLinkW link = (IShellLinkW)new ShellLink();
            link.SetPath(exePath);
            link.SetWorkingDirectory(Path.GetDirectoryName(exePath));
            link.SetIconLocation(exePath, 0);

            IPersistFile file = (IPersistFile)link;
            file.Save(shortcutPath, true);
        }
        [ComImport]
        [Guid("00021401-0000-0000-C000-000000000046")]
        class ShellLink { }

        [ComImport]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        [Guid("000214F9-0000-0000-C000-000000000046")]
        interface IShellLinkW
        {
            void GetPath(IntPtr pszFile, int cch, IntPtr pfd, int fFlags);
            void GetIDList(IntPtr ppidl);
            void SetIDList(IntPtr pidl);
            void GetDescription(IntPtr pszName, int cch);
            void SetDescription(string pszName);
            void GetWorkingDirectory(IntPtr pszDir, int cch);
            void SetWorkingDirectory(string pszDir);
            void GetArguments(IntPtr pszArgs, int cch);
            void SetArguments(string pszArgs);
            void GetHotkey(out short pwHotkey);
            void SetHotkey(short wHotkey);
            void GetShowCmd(out int piShowCmd);
            void SetShowCmd(int iShowCmd);
            void GetIconLocation(IntPtr pszIconPath, int cch, out int piIcon);
            void SetIconLocation(string pszIconPath, int iIcon);
            void SetRelativePath(string pszPathRel, int dwReserved);
            void Resolve(IntPtr hwnd, int fFlags);
            void SetPath(string pszFile);
        }

        [ComImport]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        [Guid("0000010B-0000-0000-C000-000000000046")]
        interface IPersistFile
        {
            void GetClassID(out Guid pClassID);
            void IsDirty();
            void Load(string pszFileName, int dwMode);
            void Save(string pszFileName, bool fRemember);
            void SaveCompleted(string pszFileName);
            void GetCurFile(out string ppszFileName);
        }

        private void MenuButton_Click(object sender, EventArgs e)
        {
            var clickedButton = sender as Guna2Button;
            if (clickedButton == null || !menuButtons.Contains(clickedButton))
                return;

            if (activeButton != null)
            {
                SetNormal(activeButton);
            }

            SetActive(clickedButton);
            activeButton = clickedButton;
        }

        private void SetActive(Guna2Button btn)
        {
            btn.FillColor = Color.FromArgb(25, 25, 25);
            btn.ForeColor = Color.White;
            btn.CustomBorderColor = Color.FromArgb(0, 200, 102);
        }

        private void SetNormal(Guna2Button btn)
        {
            btn.FillColor = Color.FromArgb(15, 15, 15);
            btn.ForeColor = Color.White;
            btn.CustomBorderColor = Color.Transparent;
        }
        string versionFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "version.txt");
        bool needUpdate = false;
        RobloxVersionFile latestVersion;
        private async Task LoadClientVersionUpload()
        {
            using (HttpClient client = new HttpClient())
            {
                string url = "https://clientsettings.roblox.com/v2/client-version/WindowsPlayer/channel/LIVE";
                string json = await client.GetStringAsync(url);

                JObject obj = JObject.Parse(json);
                string clientVersionUpload = obj["clientVersionUpload"]?.ToString();

                guna2Button7.Text = clientVersionUpload;
            }
        }
        private async void Form1_Load(object sender, EventArgs e)
        {
            await CheckRobloxVersionAsync();
            string folderPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "MainRoblox"
            );

            if (!Directory.Exists(folderPath))
            {
                Directory.CreateDirectory(folderPath);
            }

            string launcherExe = Application.ExecutablePath;

            string robloxExe = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "MainRoblox",
                "RobloxPlayerBeta.exe"
            );

            if (!File.Exists(launcherExe) || !File.Exists(robloxExe))
            {
                UnregisterProtocol();
                return;
            }

            if (!IsProtocolCorrect(launcherExe))
            {
                RegisterProtocol(launcherExe);
            }

            string[] args = Environment.GetCommandLineArgs();

            if (args.Length > 1)
            {
                LaunchRoblox(robloxExe, args[1]);

                Timer t = new Timer();
                t.Interval = 800;
                t.Tick += (s, _) =>
                {
                    t.Stop();
                    Application.Exit();
                };
                t.Start();
            }
            RefreshListBox();
            string indexFilePath = Path.Combine(
Application.StartupPath,
"file_index.json"
);
        }
        void LaunchRoblox(string robloxExe, string url)
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = robloxExe,
                Arguments = url,
                UseShellExecute = true
            });
        }

        bool IsProtocolCorrect(string launcherExe)
        {
            using (RegistryKey key = Registry.ClassesRoot.OpenSubKey(@"roblox-player\shell\open\command"))
            {
                if (key == null) return false;
                string v = key.GetValue("") as string;
                return v != null && v.Contains(launcherExe);
            }
        }

        void RegisterProtocol(string launcherExe)
        {
            using (RegistryKey key = Registry.ClassesRoot.CreateSubKey("roblox-player"))
            {
                key.SetValue("", "URL:Open Impaction");
                key.SetValue("URL Protocol", "");

                using (RegistryKey cmd = key.CreateSubKey(@"shell\open\command"))
                {
                    cmd.SetValue("", $"\"{launcherExe}\" \"%1\"");
                }
            }
        }

        void UnregisterProtocol()
        {
            try
            {
                Registry.ClassesRoot.DeleteSubKeyTree("roblox-player", false);
            }
            catch { }
        }
        private async Task CheckRobloxVersionAsync()
        {
            using (HttpClient client = new HttpClient())
            {
                string url = "https://clientsettings.roblox.com/v2/client-version/WindowsPlayer/channel/LIVE";
                string webJson = await client.GetStringAsync(url);

                RobloxWebResponse webData = JsonConvert.DeserializeObject<RobloxWebResponse>(webJson);

                latestVersion = new RobloxVersionFile
                {
                    version = webData.version,
                    clientVersionUpload = webData.clientVersionUpload,
                    bootstrapperVersion = webData.bootstrapperVersion
                };

                if (!File.Exists(versionFilePath))
                {
                    SaveVersionFile(latestVersion);
                    guna2Button5.Text = "Install Roblox";
                    return;
                }

                string localJson = File.ReadAllText(versionFilePath);
                RobloxVersionFile localData = JsonConvert.DeserializeObject<RobloxVersionFile>(localJson);

                if (localData.version != latestVersion.version ||
                    localData.clientVersionUpload != latestVersion.clientVersionUpload)
                {
                    needUpdate = true;
                    guna2Button5.Text = "Update Roblox";
                }
                else
                {
                    guna2Button5.Text = "Install Roblox";
                }
            }
        }

        private void SaveVersionFile(RobloxVersionFile data)
        {
            string json = JsonConvert.SerializeObject(data, Formatting.None);
            File.WriteAllText(versionFilePath, json);
        }
        public class RobloxVersionFile
        {
            public string version { get; set; }
            public string clientVersionUpload { get; set; }
            public string bootstrapperVersion { get; set; }
        }

        public class RobloxWebResponse
        {
            public string version { get; set; }
            public string clientVersionUpload { get; set; }
            public string bootstrapperVersion { get; set; }
        }
        void CreateDesktopShortcut(string exePath)
        {
            string desktop = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
            string shortcutPath = Path.Combine(desktop, "Roblox Player.lnk");

            Type shellType = Type.GetTypeFromProgID("WScript.Shell");
            dynamic shell = Activator.CreateInstance(shellType);
            dynamic shortcut = shell.CreateShortcut(shortcutPath);

            shortcut.TargetPath = exePath;
            shortcut.WorkingDirectory = Path.GetDirectoryName(exePath);
            shortcut.IconLocation = exePath;
            shortcut.Save();
        }

        private void guna2Button7_Click(object sender, EventArgs e)
        {
            string url = "https://clientsettings.roblox.com/v2/client-version/WindowsPlayer/channel/LIVE";
            Process.Start(new ProcessStartInfo
            {
                FileName = url,
                UseShellExecute = true
            });
        }

        private void guna2Button9_Click(object sender, EventArgs e)
        {
            string url = "https://impactionlab.vercel.app/";
            Process.Start(new ProcessStartInfo
            {
                FileName = url,
                UseShellExecute = true
            });
        }

        private async void guna2Button5_Click(object sender, EventArgs e)
        {
            string folderPath = Path.Combine(
       Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
       "MainRoblox"
   );

            if (Directory.Exists(folderPath))
            {
                try
                {
                    foreach (string file in Directory.GetFiles(folderPath))
                        File.Delete(file);

                    foreach (string dir in Directory.GetDirectories(folderPath))
                        Directory.Delete(dir, true);
                }
                catch
                {
                }
            }

            System.ComponentModel.BackgroundWorker worker = new System.ComponentModel.BackgroundWorker();
            worker.WorkerReportsProgress = true;

            worker.DoWork += (s, args) =>
            {
                try
                {
                    bool success = RDDAPI.Download("LIVE", (message) =>
                    {
                        worker.ReportProgress(0, message);
                    }).Result;
                    args.Result = success;
                }
                catch (Exception ex)
                {
                    args.Result = ex;
                }
            };

            worker.ProgressChanged += (s, args) =>
            {
                if (guna2Button8 != null)
                    guna2Button8.Text = args.UserState.ToString();
            };

            worker.RunWorkerCompleted += (s, args) =>
            {
                if (args.Result is bool success)
                {
                    if (success)
                    {
                        string installPath = RDDAPI.GetInstallationPath();
                        string robloxExe = Path.Combine(installPath, "RobloxPlayerBeta.exe");

                        if (File.Exists(robloxExe))
                            CreateDesktopShortcut(robloxExe);

                        MessageBox.Show(
                            $"✅ Roblox Player installed successfully!\n\nLocation: {installPath}",
                            "Success",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Information
                        );
                    }
                    else
                    {
                        MessageBox.Show(
                            "❌ Installation failed.",
                            "Error",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Error
                        );
                    }
                }
                else if (args.Result is Exception ex)
                {
                    MessageBox.Show(
                        $"❌ Error: {ex.Message}",
                        "Error",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error
                    );
                }
            };
            worker.RunWorkerAsync();
            if (!needUpdate) return;
            SaveVersionFile(latestVersion);
            needUpdate = false;
        }

        private void guna2Button4_Click(object sender, EventArgs e)
        {
            string robloxPath = Path.Combine(
           Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
           @"MainRoblox\RobloxPlayerBeta.exe"
           );

            if (!File.Exists(robloxPath))
            {
                MessageBox.Show(
                    "Roblox is not installed.",
                    "Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error
                );
                return;
            }

            Process.Start(robloxPath);
        }

        private void guna2Button6_Click(object sender, EventArgs e)
        {
            string folderPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "MainRoblox"
        );

            if (Directory.Exists(folderPath))
            {
                try
                {
                    foreach (string file in Directory.GetFiles(folderPath))
                        File.Delete(file);

                    foreach (string dir in Directory.GetDirectories(folderPath))
                        Directory.Delete(dir, true);
                }
                catch
                {
                }
            }
            string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            string robloxShortcut = Path.Combine(desktopPath, "Roblox Player.lnk");

            if (File.Exists(robloxShortcut))
            {
                File.Delete(robloxShortcut);
            }
            MessageBox.Show(
                   $"✅ Roblox Player removed successfully!",
                   "Success",
                   MessageBoxButtons.OK,
                   MessageBoxIcon.Information
               );
        }

        private void guna2Button2_Click(object sender, EventArgs e)
        {
            panel11.Show();
            panel7.Hide();
            panel12.Hide();
            panel15.Hide();
        }

        private void guna2Button1_Click(object sender, EventArgs e)
        {
            panel11.Hide();
            panel7.Show();
            panel12.Hide();
            panel15.Hide();
        }

        private void guna2Button3_Click(object sender, EventArgs e)
        {
            panel12.Show();
            panel11.Hide();
            panel7.Hide();
            panel15.Hide();
        }
        private void guna2Button17_Click(object sender, EventArgs e)
        {
            panel15.Show();
            panel11.Hide();
            panel7.Hide();
            panel12.Hide();
        }
        public class SavedFile
        {
            public string name { get; set; }
            public string path { get; set; }
        }
        string indexFilePath = Path.Combine(Application.StartupPath, "file_index.json");
        void RefreshListBox()
        {
            listBox1.Items.Clear();

            if (!File.Exists(indexFilePath))
                return;

            var files = JsonConvert.DeserializeObject<List<SavedFile>>(
                File.ReadAllText(indexFilePath)
            );

            if (files == null) return;

            foreach (var f in files)
            {
                listBox1.Items.Add(f.name);
            }
        }
        private void guna2Button10_Click(object sender, EventArgs e)
        {
            try
            {
                string baseDir = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "MainRoblox"
                );

                string clientSettingsDir = Path.Combine(baseDir, "ClientSettings");
                string configFile = Path.Combine(clientSettingsDir, "ClientAppSettings.json");

                if (!Directory.Exists(clientSettingsDir))
                    Directory.CreateDirectory(clientSettingsDir);

                File.WriteAllText(configFile, richTextBox1.Text);

                MessageBox.Show(
                    "Config saved successfully.",
                    "Success",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information
                );
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    ex.Message,
                    "Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error
                );
            }
        }

        private void guna2Button11_Click(object sender, EventArgs e)
        {
            richTextBox1.Text = "";
        }

        private async void guna2Button12_Click(object sender, EventArgs e)
        {
            try
            {
                using (System.Windows.Forms.SaveFileDialog sfd = new System.Windows.Forms.SaveFileDialog())
                {
                    sfd.Filter = "JSON (*.json)|*.json|Text (*.txt)|*.txt";
                    sfd.Title = "Simpan file";

                    if (sfd.ShowDialog() != System.Windows.Forms.DialogResult.OK)
                        return;

                    File.WriteAllText(sfd.FileName, richTextBox1.Text);

                    List<SavedFile> files = new List<SavedFile>();
                    if (File.Exists(indexFilePath))
                    {
                        files = JsonConvert.DeserializeObject<List<SavedFile>>(
                            File.ReadAllText(indexFilePath)
                        ) ?? new List<SavedFile>();
                    }

                    files.RemoveAll(f => f.path == sfd.FileName);
                    files.Add(new SavedFile
                    {
                        name = Path.GetFileName(sfd.FileName),
                        path = sfd.FileName
                    });

                    File.WriteAllText(
                        indexFilePath,
                        JsonConvert.SerializeObject(files, Formatting.Indented)
                    );

                    RefreshListBox();
                }
            }
            catch (Exception ex)
            {
            }
        }

        private void guna2Button13_Click(object sender, EventArgs e)
        {
            try
            {
                using (System.Windows.Forms.OpenFileDialog ofd = new System.Windows.Forms.OpenFileDialog())
                {
                    ofd.Title = "Pilih file JSON / TXT";
                    ofd.Filter = "JSON & TXT Files (*.json;*.txt)|*.json;*.txt|All Files (*.*)|*.*";
                    ofd.Multiselect = false;

                    if (ofd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                    {
                        richTextBox1.Text = File.ReadAllText(ofd.FileName);
                    }
                }
            }
            catch (Exception ex)
            {
            }
        }

        private void listBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            try
            {
                if (listBox1.SelectedIndex == -1)
                    return;

                var files = JsonConvert.DeserializeObject<List<SavedFile>>(
                    File.ReadAllText(indexFilePath)
                );

                if (files == null) return;

                var selected = files[listBox1.SelectedIndex];

                if (!File.Exists(selected.path))
                {
                }

                richTextBox1.Text = File.ReadAllText(selected.path);
            }
            catch (Exception ex)
            {
            }
        }

        private void guna2Button14_Click(object sender, EventArgs e)
        {
            try
            {
                string baseDir = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "MainRoblox"
                );

                string clientSettingsDir = Path.Combine(baseDir, "ClientSettings");
                string configFile = Path.Combine(clientSettingsDir, "ClientAppSettings.json");

                if (!Directory.Exists(clientSettingsDir))
                    Directory.CreateDirectory(clientSettingsDir);

                File.WriteAllText(configFile, "{\r\n\"DFIntTaskSchedulerTargetFps\":\"0\",\r\n\"FFlagGameBasicSettingsFramerateCap5\":\"False\",\r\n\"DFIntRenderQualityLevelOverride\":\"21\",\r\n\"DFIntDebugFRMQualityLevelOverride\":\"21\",\r\n\"FFlagFixGraphicsQuality\":\"True\",\r\n\"DFFlagDebugRenderForceTechnologyVoxel\":\"False\",\r\n\"FFlagRenderUnifiedLighting\":\"True\",\r\n\"FFlagRenderLightingFuture\":\"True\",\r\n\"FFlagDisablePostFx\":\"False\",\r\n\"FFlagDisablePostFxV2\":\"False\",\r\n\"FFlagEnableBloom\":\"True\",\r\n\"FFlagEnableSunRays\":\"True\",\r\n\"FFlagEnableDepthOfField\":\"True\",\r\n\"FFlagEnableMotionBlur\":\"True\",\r\n\"FFlagEnableColorCorrection\":\"True\",\r\n\"FIntRenderShadowIntensity\":\"100\",\r\n\"FFlagRenderShadows\":\"True\",\r\n\"FFlagShadowMapEnable\":\"True\",\r\n\"FFlagShadowMapHighQuality\":\"True\",\r\n\"DFIntShadowMapResolution\":\"8192\",\r\n\"FIntDebugForceMSAASamples\":\"8\",\r\n\"FFlagMSAAEnabled\":\"True\",\r\n\"FFlagEnableVolumetricLighting\":\"True\",\r\n\"FFlagVolumetricFog\":\"True\",\r\n\"DFIntVolumetricFogQuality\":\"4\",\r\n\"FFlagEnableGlobalIllumination\":\"True\",\r\n\"FFlagFutureIsBrightPhase2\":\"True\",\r\n\"FFlagEnableSurfaceAppearance\":\"True\",\r\n\"FFlagEnableHighQualityMaterials\":\"True\",\r\n\"FFlagEnableEnvironmentMap\":\"True\",\r\n\"DFIntEnvironmentMapResolution\":\"2048\",\r\n\"FFlagEnableReflectionProbe\":\"True\",\r\n\"DFIntReflectionProbeResolution\":\"2048\",\r\n\"FFlagEnableTerrainDetail\":\"True\",\r\n\"DFIntTerrainQualityOverride\":\"5\",\r\n\"FFlagEnableWaterReflections\":\"True\",\r\n\"FFlagEnableWaterTransparency\":\"True\",\r\n\"DFIntWaterDetailLevel\":\"5\",\r\n\"FFlagEnableHighQualityParticles\":\"True\",\r\n\"DFIntParticleQualityLevel\":\"5\",\r\n\"FFlagEnableTextureStreaming\":\"False\",\r\n\"DFIntTextureQualityOverride\":\"5\",\r\n\"FFlagDebugGraphicsPreferD3D11\":\"True\",\r\n\"FFlagDebugGraphicsDisableMetal\":\"True\"\r\n}");

                MessageBox.Show(
                    "Config saved successfully.",
                    "Success",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information
                );
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    ex.Message,
                    "Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error
                );
            }
        }

        private void guna2Button15_Click(object sender, EventArgs e)
        {
            try
            {
                string baseDir = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "MainRoblox"
                );

                string clientSettingsDir = Path.Combine(baseDir, "ClientSettings");
                string configFile = Path.Combine(clientSettingsDir, "ClientAppSettings.json");

                if (!Directory.Exists(clientSettingsDir))
                    Directory.CreateDirectory(clientSettingsDir);

                File.WriteAllText(configFile, "{\r\n\"FFlagOptimizeServerTickRate\":\"True\",\r\n\"DFIntServerTickRate\":\"60\",\r\n\"DFIntServerPhysicsUpdateRate\":\"60\",\r\n\"DFIntTaskSchedulerTargetFps\":\"60\",\r\n\"FFlagGameBasicSettingsFramerateCap5\":\"True\",\r\n\"FFlagTaskSchedulerLimitTargetFpsTo2402\":\"False\",\r\n\"FFlagDebugSkyGray\":\"True\",\r\n\"FIntPGSAngularDampingMultiplier\":\"1000\",\r\n\"FIntFontSizePadding\":\"3\",\r\n\"FIntFullscreenTitleBarTriggerDelayMillis\":\"3600000\",\r\n\"DFIntCanHideGuiGroupId\":\"32380007\",\r\n\"FFlagEnableChromePinnedChat\":\"True\",\r\n\"FFlagDebugDisplayFPS\":\"True\",\r\n\"DFFlagDebugPauseVoxelizer\":\"True\",\r\n\"DFIntNumFramesAllowedToBeAboveError\":\"0\",\r\n\"FIntNumFramesToCaptureCallStack\":\"1\",\r\n\"FFlagCommitToGraphicsQualityFix\":\"True\",\r\n\"DFIntDebugFRMQualityLevelOverride\":\"1\",\r\n\"FFlagFixGraphicsQuality\":\"True\",\r\n\"FFlagMSRefactor5\":\"False\",\r\n\"FFlagHandleAltEnterFullscreenManually\":\"False\",\r\n\"FFlagEnableQuickGameLaunch\":\"True\",\r\n\"FFlagGraphicsCheckComputeSupport\":\"False\",\r\n\"DFStringTelemetryV2Url\":\"null\",\r\n\"FFlagDebugDisableTelemetryV2Counter\":\"True\",\r\n\"DFStringRobloxAnalyticsURL\":\"null\",\r\n\"DFStringAltHttpPointsReporterUrl\":\"null\",\r\n\"DFStringCrashUploadToBacktraceWindowsPlayerToken\":\"null\",\r\n\"FFlagDebugDisableTelemetryPoint\":\"True\",\r\n\"FFlagDebugDisableTelemetryV2Stat\":\"True\",\r\n\"DFFlagDisableFastLogTelemetry\":\"True\",\r\n\"DFFlagBrowserTrackerIdTelemetryEnabled\":\"False\",\r\n\"DFStringLightstepHTTPTransportUrlPath\":\"null\",\r\n\"DFStringCrashUploadToBacktraceMacPlayerToken\":\"null\",\r\n\"FStringCoreScriptBacktraceErrorUploadToken\":\"null\",\r\n\"FFlagDebugDisableTelemetryEphemeralStat\":\"True\",\r\n\"FFlagDebugDisableTelemetryEphemeralCounter\":\"True\",\r\n\"DFStringLightstepToken\":\"null\",\r\n\"FFlagDebugDisableTelemetryV2Event\":\"True\",\r\n\"FFlagDebugDisableTelemetryEventIngest\":\"True\",\r\n\"DFStringLightstepHTTPTransportUrlHost\":\"null\",\r\n\"DFStringCrashUploadToBacktraceBaseUrl\":\"null\",\r\n\"DFStringHttpPointsReporterUrl\":\"null\",\r\n\"DFIntRakNetMinAckGrowthPercent\":\"100\",\r\n\"DFIntRaknetBandwidthInfluxHundredthsPercentageV2\":\"10000\",\r\n\"DFIntCodecMaxOutgoingFrames\":\"2139999999\",\r\n\"DFIntWaitOnUpdateNetworkLoopEndedMS\":\"100\",\r\n\"DFIntRakNetResendRttMultiple\":\"1\",\r\n\"DFIntClientPacketMaxFrameMicroseconds\":\"1\",\r\n\"DFIntRakNetMtuValue1InBytes\":\"1472\",\r\n\"DFIntMegaReplicatorNetworkQualityProcessorUnit\":\"-1\",\r\n\"DFIntClientPacketHealthyAllocationPercent\":\"50\",\r\n\"DFIntMaxProcessPacketsStepsAccumulated\":\"0\",\r\n\"DFIntRakNetClockDriftAdjustmentPerPingMillisecond\":\"2139999999\",\r\n\"DFIntRakNetMtuValue3InBytes\":\"1472\",\r\n\"DFIntInterpolationMinAssemblyCount\":\"1\",\r\n\"DFFlagRakNetEnablePoll\":\"True\",\r\n\"DFIntMaxProcessPacketsStepsPerCyclic\":\"2139999999\",\r\n\"DFIntRakNetSelectUnblockSocketWriteDurationMs\":\"10\",\r\n\"DFIntClientPacketMaxDelayMs\":\"0\",\r\n\"DFIntRakNetMtuValue2InBytes\":\"1472\",\r\n\"DFIntRakNetLoopMs\":\"0\",\r\n\"DFIntMaxFrameBufferSize\":\"1\",\r\n\"DFIntTargetTimeDelayFacctorTenths\":\"0\",\r\n\"DFIntMaxProcessPacketsJobScaling\":\"2139999999\",\r\n\"DFIntRaknetBandwidthPingSendEveryXSeconds\":\"-1\",\r\n\"DFIntConnectionMTUSize\":\"1472\",\r\n\"DFIntCodecMaxIncomingPackets\":\"2139999999\",\r\n\"DFFlagRakNetUnblockSelectOnShutdownByWritingToSocket\":\"True\",\r\n\"DFFlagDebugLargeReplicatorDisableCompression\":\"True\",\r\n\"DFFlagDebugLargeReplicatorDisableDelta\":\"True\",\r\n\"DFFlagReplicateCreateToPlayer\":\"True\",\r\n\"DFFlagFastEndUpdateLoop\":\"True\",\r\n\"DFFlagRakNetCalculateApplicationFeedback2\":\"False\",\r\n\"DFFlagHttpApplyDecompressionMultiplier\":\"False\",\r\n\"DFFlagHttpPointsReporterUseCompression\":\"False\",\r\n\"DFFlagNetworkUseZstdWrapper\":\"False\",\r\n\"FFlagDebugLargeReplicatorEnabled\":\"True\",\r\n\"FFlagDebugLargeReplicatorWrite\":\"True\",\r\n\"FFlagDebugLargeReplicatorRead\":\"True\",\r\n\"FFlagSimCSGV3IncrementalTriangulationStreamingCompression\":\"False\",\r\n\"FFlagEnableZstdDictionaryForClientSettings\":\"False\",\r\n\"FFlagCreationDBCompressRequest\":\"False\",\r\n\"FFlagEnableZstdForClientSettings\":\"False\",\r\n\"DFIntServerBandwidthPlayerSampleRateFacsOverride\":\"2139999999\",\r\n\"DFIntRakNetApplicationFeedbackScaleUpThresholdPercent\":\"0\",\r\n\"DFIntJoinDataItemEstimatedCompressionRatioHundreths\":\"0\",\r\n\"DFIntServerRakNetBandwidthPlayerSampleRate\":\"2139999999\",\r\n\"DFIntClusterSenderMaxUpdateBandwidthBps\":\"2139999999\",\r\n\"DFIntGameNetCompressionLodByteBudgetThresholdPct\":\"0\",\r\n\"DFIntClusterEstimatedCompressionRatioHundredths\":\"0\",\r\n\"DFIntClusterSenderMaxJoinBandwidthBps\":\"2139999999\",\r\n\"DFIntServerBandwidthPlayerSampleRate\":\"2139999999\",\r\n\"DFIntClientNetworkInfluxHundredthsPercentage\":\"0\",\r\n\"DFIntSendGameServerDataMaxLen\":\"2139999999\",\r\n\"DFIntTouchSenderMaxBandwidthBpsScaling\":\"2\",\r\n\"DFIntSendRakNetStatsInterval\":\"2139999999\",\r\n\"DFIntLargePacketQueueSizeCutoffMB\":\"1000\",\r\n\"DFIntNetworkSchemaCompressionRatio\":\"0\",\r\n\"DFIntTouchSenderMaxBandwidthBps\":\"1050\",\r\n\"DFIntNetworkQualityResponderUnit\":\"10\",\r\n\"DFIntRakNetNakResendDelayMsMax\":\"1\",\r\n\"DFIntJoinDataCompressionLevel\":\"0\",\r\n\"DFIntServerFramesBetweenJoins\":\"1\",\r\n\"DFIntClusterCompressionLevel\":\"0\",\r\n\"DFIntRakNetNakResendDelayMs\":\"1\",\r\n\"DFIntRakNetSelectTimeoutMs\":\"1\",\r\n\"DFIntSendItemLimit\":\"5\",\r\n\"DFIntMaxFramesToSend\":\"1\",\r\n\"DFIntDebugDefaultTargetWorldStepsPerFrame\":\"7500\",\r\n\"DFIntAnimatorThrottleMaxFramesToSkip\":\"1\",\r\n\"FIntRuntimeMaxNumOfThreads\":\"20000\",\r\n\"FIntRuntimeMaxNumOfMutexes\":\"20000\",\r\n\"FIntSimSolverResponsiveness\":\"2139999999\",\r\n\"FIntRuntimeMaxNumOfLatches\":\"20000\",\r\n\"FIntInterpolationMaxDelayMSec\":\"0\",\r\n\"FIntInterpolationAwareTargetTimeLerpHundredth\":\"100\",\r\n\"DFIntRuntimeConcurrency\":\"2139999999\",\r\n\"DFIntParallelAdaptiveInterpolationBatchCount\":\"1\",\r\n\"DFIntInterpolationDtLimitForLod\":\"1\",\r\n\"FIntRuntimeMaxNumOfConditions\":\"20000\",\r\n\"FIntRuntimeMaxNumOfSchedulers\":\"20000\",\r\n\"DFFlagDebugPerfMode\":\"True\",\r\n\"FStringVoiceBetaBadgeLearnMoreLink\":\"null\",\r\n\"FFlagSortKeyOptimization\":\"True\",\r\n\"DFIntAssetPreloading\":\"9999999\",\r\n\"FFlagEnablePerformanceControlService\":\"True\",\r\n\"DFFlagDisableDPIScale\":\"True\",\r\n\"FIntTerrainArraySliceSize\":\"4\",\r\n\"FIntMaquettesFrameRateBufferPercentage\":\"1\",\r\n\"FIntPerformanceControlFrameTimeMax\":\"1\",\r\n\"FFlagControlBetaBadgeWithGuac\":\"False\",\r\n\"FFlagAdServiceEnabled\":\"False\",\r\n\"FFlagBetaBadgeLearnMoreLinkFormview\":\"False\",\r\n\"FFlagGuiHidingApiSupport2\":\"True\",\r\n\"FFlagFasterPreciseTime4\":\"True\",\r\n\"FFlagPreloadAllFonts\":\"False\",\r\n\"FIntGrassMovementReducedMotionFactor\":\"0\",\r\n\"FFlagDebugRenderingSetDeterministic\":\"True\",\r\n\"FFlagDebugGraphicsPreferD3D11\":\"True\",\r\n\"FFlagDebugGraphicsPreferVulkan\":\"True\",\r\n\"FFlagEnableInGameMenuModernization\":\"True\",\r\n\"FFlagUserShowGuiHideToggles\":\"True\",\r\n\"DFFlagTextureQualityOverrideEnabled\":\"True\",\r\n\"FFlagVoiceBetaBadge\":\"False\",\r\n\"FFlagDebugForceFutureIsBrightPhase2\":\"True\",\r\n\"FIntActivatedCountTimerMSMouse\":\"0\",\r\n\"FIntActivatedCountTimerMSKeyboard\":\"0\",\r\n\"DFFlagDebugOverrideDPIScale\":\"True\",\r\n\"FIntRakNetDatagramMessageIdArrayLength\":\"8192\",\r\n\"FLogNetwork\":\"7\",\r\n\"DFIntMaxAverageFrameDelayExceedFactor\":\"0\",\r\n\"DFIntClientRecvFromRaknet\":\"255\",\r\n\"FFlagMouseGetPartOptimization\":\"True\",\r\n\"FFlagEnableInGameMenuChrome\":\"True\",\r\n\"FFlagDebugDisableDynamicLighting\":\"True\",\r\n\"FFlagDebugDisableShadows\":\"True\",\r\n\"FFlagDebugDisablePostEffects\":\"True\",\r\n\"FFlagDebugDisableBloom\":\"True\",\r\n\"FFlagDebugDisableDepthOfField\":\"True\",\r\n\"FFlagDebugDisableSunRays\":\"True\",\r\n\"FFlagDebugDisableColorCorrection\":\"True\",\r\n\"FFlagDebugDisableMotionBlur\":\"True\",\r\n\"FFlagDebugDisableSSAO\":\"True\",\r\n\"FFlagDebugDisableFXAA\":\"True\",\r\n\"FFlagDebugDisableTAA\":\"True\",\r\n\"FFlagDebugDisableMSAA\":\"True\",\r\n\"FFlagDebugDisableTextureFiltering\":\"True\",\r\n\"FFlagDebugDisableAnisotropicFiltering\":\"True\",\r\n\"FFlagDebugDisableVSync\":\"True\",\r\n\"FFlagDebugDisableTripleBuffering\":\"True\",\r\n\"FFlagDebugDisableGrass\":\"True\",\r\n\"FFlagDebugDisableWater\":\"True\",\r\n\"FFlagDebugDisableParticles\":\"True\",\r\n\"FFlagDebugDisableDecals\":\"True\",\r\n\"FFlagDebugDisableMeshCache\":\"True\",\r\n\"FFlagDebugDisableMaterialCache\":\"True\",\r\n\"FFlagDebugDisableTextureCache\":\"True\",\r\n\"FFlagDebugDisableShaderCache\":\"True\",\r\n\"FFlagDebugDisableGeometryCache\":\"True\",\r\n\"FFlagDebugDisablePhysics\":\"True\",\r\n\"FFlagDebugDisableAnimations\":\"True\",\r\n\"FFlagDebugDisableAudio\":\"True\",\r\n\"FFlagDebugDisableVoice\":\"True\",\r\n\"FFlagDebugDisableChat\":\"True\",\r\n\"FFlagDebugDisableNotifications\":\"True\",\r\n\"FFlagDebugDisableAds\":\"True\",\r\n\"FFlagDebugDisableTelemetry\":\"True\",\r\n\"FFlagDebugDisableAnalytics\":\"True\",\r\n\"FFlagDebugDisableCrashReporting\":\"True\",\r\n\"FFlagDebugDisableErrorReporting\":\"True\",\r\n\"FFlagDebugDisableLogging\":\"True\",\r\n\"FFlagDebugDisableProfiling\":\"True\",\r\n\"FFlagDebugDisableDebugging\":\"True\",\r\n\"FFlagDebugDisableValidation\":\"True\",\r\n\"FFlagDebugDisableAsserts\":\"True\",\r\n\"FFlagDebugDisableWarnings\":\"True\",\r\n\"FFlagDebugDisableInfos\":\"True\",\r\n\"FFlagDebugDisableVerbose\":\"True\",\r\n\"FFlagDebugDisableTrace\":\"True\",\r\n\"FFlagDebugDisableAllLogs\":\"True\",\r\n\"FFlagDebugDisableAllTelemetry\":\"True\",\r\n\"FFlagDebugDisableAllAnalytics\":\"True\",\r\n\"FFlagDebugDisableAllCrashReporting\":\"True\",\r\n\"FFlagDebugDisableAllErrorReporting\":\"True\",\r\n\"FFlagDebugDisableAllProfiling\":\"True\",\r\n\"FFlagDebugDisableAllDebugging\":\"True\",\r\n\"FFlagDebugDisableAllValidation\":\"True\",\r\n\"FFlagDebugDisableAllAsserts\":\"True\",\r\n\"FFlagDebugDisableAllWarnings\":\"True\",\r\n\"FFlagDebugDisableAllInfos\":\"True\",\r\n\"FFlagDebugDisableAllVerbose\":\"True\",\r\n\"FFlagDebugDisableAllTrace\":\"True\",\r\n\"FFlagDebugDisableAll\":\"True\",\r\n\"DFIntMaxTextureSize\":\"512\",\r\n\"DFIntMaxTextureMemoryMB\":\"256\",\r\n\"DFIntMaxMeshMemoryMB\":\"128\",\r\n\"DFIntMaxSoundMemoryMB\":\"64\",\r\n\"DFIntMaxScriptMemoryMB\":\"128\",\r\n\"DFIntMaxPhysicsMemoryMB\":\"128\",\r\n\"DFIntMaxAnimationMemoryMB\":\"64\",\r\n\"DFIntMaxDecalMemoryMB\":\"32\",\r\n\"DFIntMaxParticleMemoryMB\":\"32\",\r\n\"DFIntMaxWaterMemoryMB\":\"32\",\r\n\"DFIntMaxTerrainMemoryMB\":\"64\",\r\n\"DFIntMaxSkyMemoryMB\":\"32\",\r\n\"DFIntMaxLightingMemoryMB\":\"32\",\r\n\"DFIntMaxPostEffectMemoryMB\":\"32\",\r\n\"DFIntMaxUIMemoryMB\":\"64\",\r\n\"DFIntMaxFontMemoryMB\":\"16\",\r\n\"DFIntMaxCursorMemoryMB\":\"8\",\r\n\"DFIntMaxIconMemoryMB\":\"8\",\r\n\"DFIntMaxLogoMemoryMB\":\"8\",\r\n\"DFIntMaxBackgroundMemoryMB\":\"16\",\r\n\"DFIntMaxForegroundMemoryMB\":\"16\",\r\n\"DFIntMaxHUDMemoryMB\":\"32\",\r\n\"DFIntMaxChatMemoryMB\":\"16\",\r\n\"DFIntMaxNotificationMemoryMB\":\"16\",\r\n\"DFIntMaxAdMemoryMB\":\"16\",\r\n\"DFIntMaxTelemetryMemoryMB\":\"8\",\r\n\"DFIntMaxAnalyticsMemoryMB\":\"8\",\r\n\"DFIntMaxCrashReportingMemoryMB\":\"8\",\r\n\"DFIntMaxErrorReportingMemoryMB\":\"8\",\r\n\"DFIntMaxLoggingMemoryMB\":\"8\",\r\n\"DFIntMaxProfilingMemoryMB\":\"8\",\r\n\"DFIntMaxDebuggingMemoryMB\":\"8\",\r\n\"DFIntMaxValidationMemoryMB\":\"8\",\r\n\"DFIntMaxAssertMemoryMB\":\"8\",\r\n\"DFIntMaxWarningMemoryMB\":\"8\",\r\n\"DFIntMaxInfoMemoryMB\":\"8\",\r\n\"DFIntMaxVerboseMemoryMB\":\"8\",\r\n\"DFIntMaxTraceMemoryMB\":\"8\",\r\n\"DFIntMaxTotalMemoryMB\":\"2048\",\r\n\"DFIntRenderThreadCount\":\"1\",\r\n\"DFIntRenderQueueSize\":\"1\",\r\n\"DFIntRenderBatchSize\":\"1\",\r\n\"DFIntRenderSortBatchSize\":\"1\",\r\n\"DFIntRenderDrawCallBatchSize\":\"1\",\r\n\"DFIntRenderTriangleBatchSize\":\"1\",\r\n\"DFIntRenderVertexBatchSize\":\"1\",\r\n\"DFIntRenderIndexBatchSize\":\"1\",\r\n\"DFIntRenderTextureBatchSize\":\"1\",\r\n\"DFIntRenderShaderBatchSize\":\"1\",\r\n\"DFIntRenderMaterialBatchSize\":\"1\",\r\n\"DFIntRenderMeshBatchSize\":\"1\",\r\n\"DFIntRenderDecalBatchSize\":\"1\",\r\n\"DFIntRenderParticleBatchSize\":\"1\",\r\n\"DFIntRenderWaterBatchSize\":\"1\",\r\n\"DFIntRenderTerrainBatchSize\":\"1\",\r\n\"DFIntRenderSkyBatchSize\":\"1\",\r\n\"DFIntRenderLightingBatchSize\":\"1\",\r\n\"DFIntRenderPostEffectBatchSize\":\"1\",\r\n\"DFIntRenderUIBatchSize\":\"1\",\r\n\"DFIntRenderFontBatchSize\":\"1\",\r\n\"DFIntRenderCursorBatchSize\":\"1\",\r\n\"DFIntRenderIconBatchSize\":\"1\",\r\n\"DFIntRenderLogoBatchSize\":\"1\",\r\n\"DFIntRenderBackgroundBatchSize\":\"1\",\r\n\"DFIntRenderForegroundBatchSize\":\"1\",\r\n\"DFIntRenderHUDBatchSize\":\"1\",\r\n\"DFIntRenderChatBatchSize\":\"1\",\r\n\"DFIntRenderNotificationBatchSize\":\"1\",\r\n\"DFIntRenderAdBatchSize\":\"1\",\r\n\"DFIntNetworkThreadCount\":\"1\",\r\n\"DFIntNetworkQueueSize\":\"1\",\r\n\"DFIntNetworkPacketSize\":\"512\",\r\n\"DFIntNetworkPacketCount\":\"1\",\r\n\"DFIntNetworkBufferSize\":\"1024\",\r\n\"DFIntNetworkBufferCount\":\"1\",\r\n\"DFIntNetworkSendRate\":\"30\",\r\n\"DFIntNetworkReceiveRate\":\"30\",\r\n\"DFIntNetworkUpdateRate\":\"30\",\r\n\"DFIntNetworkPhysicsRate\":\"30\",\r\n\"DFIntNetworkAnimationRate\":\"30\",\r\n\"DFIntNetworkSoundRate\":\"30\",\r\n\"DFIntNetworkVoiceRate\":\"30\",\r\n\"DFIntNetworkChatRate\":\"30\",\r\n\"DFIntNetworkNotificationRate\":\"30\",\r\n\"DFIntNetworkAdRate\":\"30\",\r\n\"DFIntNetworkTelemetryRate\":\"30\",\r\n\"DFIntNetworkAnalyticsRate\":\"30\",\r\n\"DFIntNetworkCrashReportingRate\":\"30\",\r\n\"DFIntNetworkErrorReportingRate\":\"30\",\r\n\"DFIntNetworkLoggingRate\":\"30\",\r\n\"DFIntNetworkProfilingRate\":\"30\",\r\n\"DFIntNetworkDebuggingRate\":\"30\",\r\n\"DFIntNetworkValidationRate\":\"30\",\r\n\"DFIntNetworkAssertRate\":\"30\",\r\n\"DFIntNetworkWarningRate\":\"30\",\r\n\"DFIntNetworkInfoRate\":\"30\",\r\n\"DFIntNetworkVerboseRate\":\"30\",\r\n\"DFIntNetworkTraceRate\":\"30\",\r\n\"DFIntAudioThreadCount\":\"1\",\r\n\"DFIntAudioQueueSize\":\"1\",\r\n\"DFIntAudioBufferSize\":\"1024\",\r\n\"DFIntAudioBufferCount\":\"1\",\r\n\"DFIntAudioSampleRate\":\"22050\",\r\n\"DFIntAudioBitRate\":\"64\",\r\n\"DFIntAudioChannelCount\":\"1\",\r\n\"DFIntAudioVolume\":\"50\",\r\n\"DFIntAudioMusicVolume\":\"50\",\r\n\"DFIntAudioSFXVolume\":\"50\",\r\n\"DFIntAudioVoiceVolume\":\"50\",\r\n\"DFIntAudioChatVolume\":\"50\",\r\n\"DFIntAudioNotificationVolume\":\"50\",\r\n\"DFIntAudioAdVolume\":\"0\",\r\n\"DFIntAudioTelemetryVolume\":\"0\",\r\n\"DFIntAudioAnalyticsVolume\":\"0\",\r\n\"DFIntAudioCrashReportingVolume\":\"0\",\r\n\"DFIntAudioErrorReportingVolume\":\"0\",\r\n\"DFIntAudioLoggingVolume\":\"0\",\r\n\"DFIntAudioProfilingVolume\":\"0\",\r\n\"DFIntAudioDebuggingVolume\":\"0\",\r\n\"DFIntAudioValidationVolume\":\"0\",\r\n\"DFIntAudioAssertVolume\":\"0\",\r\n\"DFIntAudioWarningVolume\":\"0\",\r\n\"DFIntAudioInfoVolume\":\"0\",\r\n\"DFIntAudioVerboseVolume\":\"0\",\r\n\"DFIntAudioTraceVolume\":\"0\",\r\n\"DFIntInputThreadCount\":\"1\",\r\n\"DFIntInputQueueSize\":\"1\",\r\n\"DFIntInputBufferSize\":\"1024\",\r\n\"DFIntInputBufferCount\":\"1\",\r\n\"DFIntInputPollRate\":\"60\",\r\n\"DFIntInputUpdateRate\":\"60\",\r\n\"DFIntInputMouseRate\":\"60\",\r\n\"DFIntInputKeyboardRate\":\"60\",\r\n\"DFIntInputGamepadRate\":\"60\",\r\n\"DFIntInputTouchRate\":\"60\",\r\n\"DFIntInputMotionRate\":\"60\",\r\n\"DFIntInputVoiceRate\":\"60\",\r\n\"DFIntInputChatRate\":\"60\",\r\n\"DFIntInputNotificationRate\":\"60\",\r\n\"DFIntInputAdRate\":\"60\",\r\n\"DFIntInputTelemetryRate\":\"60\",\r\n\"DFIntInputAnalyticsRate\":\"60\",\r\n\"DFIntInputCrashReportingRate\":\"60\",\r\n\"DFIntInputErrorReportingRate\":\"60\",\r\n\"DFIntInputLoggingRate\":\"60\",\r\n\"DFIntInputProfilingRate\":\"60\",\r\n\"DFIntInputDebuggingRate\":\"60\",\r\n\"DFIntInputValidationRate\":\"60\",\r\n\"DFIntInputAssertRate\":\"60\",\r\n\"DFIntInputWarningRate\":\"60\",\r\n\"DFIntInputInfoRate\":\"60\",\r\n\"DFIntInputVerboseRate\":\"60\",\r\n\"DFIntInputTraceRate\":\"60\"\r\n}");

                MessageBox.Show(
                    "Config saved successfully.",
                    "Success",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information
                );
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    ex.Message,
                    "Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error
                );
            }
        }

        private void guna2Button16_Click(object sender, EventArgs e)
        {
            try
            {
                string baseDir = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "MainRoblox"
                );

                string clientSettingsDir = Path.Combine(baseDir, "ClientSettings");
                string configFile = Path.Combine(clientSettingsDir, "ClientAppSettings.json");

                if (!Directory.Exists(clientSettingsDir))
                    Directory.CreateDirectory(clientSettingsDir);

                File.WriteAllText(configFile, "{}");

                MessageBox.Show(
                    "Config saved successfully.",
                    "Success",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information
                );
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    ex.Message,
                    "Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error
                );
            }
        }

        private void guna2Button18_Click(object sender, EventArgs e)
        {
            string input = guna2TextBox1.Text.Trim();

            if (!int.TryParse(input, out int value))
            {
                MessageBox.Show("The value must be a number.", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            string path = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                @"Roblox\GlobalBasicSettings_13.xml"
            );

            if (!File.Exists(path))
            {
                MessageBox.Show("The configuration file was not found.", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            string xml = File.ReadAllText(path);

            xml = Regex.Replace(
                xml,
                @"<int name=""FramerateCap"">.*?</int>",
                $@"<int name=""FramerateCap"">{value}</int>"
            );

            File.WriteAllText(path, xml);

            MessageBox.Show("Engine has been updated.", "Success",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void guna2Button19_Click(object sender, EventArgs e)
        {
            string input = guna2TextBox2.Text.Trim().ToLower();

            if (input != "true" && input != "false")
            {
                MessageBox.Show("The value must be 'true' or 'false'.", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            string path = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                @"Roblox\GlobalBasicSettings_13.xml"
            );

            if (!File.Exists(path))
            {
                MessageBox.Show("The configuration file was not found.", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            string xml = File.ReadAllText(path);

            xml = Regex.Replace(
                xml,
                @"<bool name=""ReducedMotion"">.*?</bool>",
                $@"<bool name=""ReducedMotion"">{input}</bool>"
            );

            File.WriteAllText(path, xml);

            MessageBox.Show("Engine has been updated.", "Success",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void guna2Button20_Click(object sender, EventArgs e)
        {
            guna2TextBox2.Text = "true";
        }

        private void guna2Button21_Click(object sender, EventArgs e)
        {
            guna2TextBox2.Text = "false";
        }

        private void guna2Button24_Click(object sender, EventArgs e)
        {
            string input = guna2TextBox3.Text.Trim().ToLower();

            if (input != "true" && input != "false")
            {
                MessageBox.Show("The value must be 'true' or 'false'.", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            string path = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                @"Roblox\GlobalBasicSettings_13.xml"
            );

            if (!File.Exists(path))
            {
                MessageBox.Show("The configuration file was not found.", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            string xml = File.ReadAllText(path);

            xml = Regex.Replace(
                xml,
                @"<bool name=""PlayerNamesEnabled"">.*?</bool>",
                $@"<bool name=""PlayerNamesEnabled"">{input}</bool>"
            );

            File.WriteAllText(path, xml);

            MessageBox.Show("Engine has been updated.", "Success",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void guna2Button23_Click(object sender, EventArgs e)
        {
            guna2TextBox3.Text = "true";
        }

        private void guna2Button22_Click(object sender, EventArgs e)
        {
            guna2TextBox3.Text = "false";
        }

        private void guna2Button27_Click(object sender, EventArgs e)
        {
            string input = guna2TextBox4.Text.Trim().ToLower();

            int value;

            switch (input)
            {
                case "performance":
                    value = 0;
                    break;
                case "balanced":
                    value = 1;
                    break;
                case "quality":
                    value = 2;
                    break;
                default:
                    MessageBox.Show(
                        "Invalid value. Use Performance, Balanced, or Quality.",
                        "Error",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error
                    );
                    return;
            }

            string path = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                @"Roblox\GlobalBasicSettings_13.xml"
            );

            if (!File.Exists(path))
            {
                MessageBox.Show("The configuration file was not found.", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            string xml = File.ReadAllText(path);

            xml = Regex.Replace(
                xml,
                @"<token name=""GraphicsOptimizationMode"">.*?</token>",
                $@"<token name=""GraphicsOptimizationMode"">{value}</token>"
            );

            File.WriteAllText(path, xml);

            MessageBox.Show("Engine has been updated.", "Success",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private readonly string[] modes = { "performance", "balanced", "quality" };
        private int currentIndex = 0;
        private int GetModeValue(string mode)
        {
            switch (mode)
            {
                case "performance":
                    return 0;
                case "balanced":
                    return 1;
                case "quality":
                    return 2;
                default:
                    return 1;
            }
        }
        private void guna2Button26_Click(object sender, EventArgs e)
        {
            currentIndex--;

            if (currentIndex < 0)
                currentIndex = modes.Length - 1;

            string currentMode = modes[currentIndex];
            int value = GetModeValue(currentMode);
            guna2TextBox4.Text = modes[currentIndex];
        }

        private void guna2Button25_Click(object sender, EventArgs e)
        {
            currentIndex++;

            if (currentIndex >= modes.Length)
                currentIndex = 0;

            string currentMode = modes[currentIndex];
            int value = GetModeValue(currentMode);
            guna2TextBox4.Text = modes[currentIndex];
        }

        private void guna2Button30_Click(object sender, EventArgs e)
        {
            try
            {
                if (!int.TryParse(guna2TextBox5.Text, out int level) ||
                    level < 1 || level > 8)
                {
                    MessageBox.Show(
                        "MSAA level must be 1, 2, 3, 4, or 8.",
                        "Invalid Input",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning
                    );
                    return;
                }

                string baseDir = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "MainRoblox"
                );

                string clientSettingsDir = Path.Combine(baseDir, "ClientSettings");
                string configFile = Path.Combine(clientSettingsDir, "ClientAppSettings.json");

                if (!Directory.Exists(clientSettingsDir))
                    Directory.CreateDirectory(clientSettingsDir);

                JObject json;
                if (File.Exists(configFile))
                {
                    string text = File.ReadAllText(configFile).Trim();
                    json = string.IsNullOrWhiteSpace(text) ? new JObject() : JObject.Parse(text);
                }
                else
                {
                    json = new JObject();
                }

                if (json.ContainsKey("FIntDebugForceMSAASamples"))
                {
                    json["FIntDebugForceMSAASamples"] = level.ToString();
                }
                else
                {
                    JObject newJson = new JObject
                    {
                        ["FIntDebugForceMSAASamples"] = level.ToString()
                    };

                    foreach (var prop in json.Properties())
                        newJson.Add(prop.Name, prop.Value);

                    json = newJson;
                }
                File.WriteAllText(configFile, json.ToString());

                MessageBox.Show(
                    "MSAA level has been updated successfully.",
                    "Success",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information
                );
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    ex.Message,
                    "Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error
                );
            }
        }
        private void guna2Button29_Click(object sender, EventArgs e)
        {
            string[] items = { "1", "2", "3", "4" };
            int value = 0;

            currentIndex--;
            if (currentIndex < 0)
                currentIndex = items.Length - 1;

            string current = items[currentIndex];

            switch (current)
            {
                case "1":
                    value = 1;
                    break;
                case "2":
                    value = 2;
                    break;
                case "3":
                    value = 3;
                    break;
                case "4":
                    value = 4;
                    break;
            }

            guna2TextBox5.Text = current;
        }

        private void guna2Button28_Click(object sender, EventArgs e)
        {
            string[] items = { "1", "2", "3", "4" };
            int value = 0;

            currentIndex++;
            if (currentIndex >= items.Length)
                currentIndex = 0;

            string current = items[currentIndex];

            switch (current)
            {
                case "1":
                    value = 1;
                    break;
                case "2":
                    value = 2;
                    break;
                case "3":
                    value = 3;
                    break;
                case "4":
                    value = 4;
                    break;
            }

            guna2TextBox5.Text = current;
        }

        private void guna2Button33_Click(object sender, EventArgs e)
        {
            try
            {
                if (!int.TryParse(guna2TextBox6.Text, out int level) ||
                    level < 0 || level > 3)
                {
                    MessageBox.Show(
                        "Texture quality level must be 0, 1, 2, or 3.",
                        "Invalid Input",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning
                    );
                    return;
                }

                string baseDir = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "MainRoblox"
                );

                string clientSettingsDir = Path.Combine(baseDir, "ClientSettings");
                string configFile = Path.Combine(clientSettingsDir, "ClientAppSettings.json");

                if (!Directory.Exists(clientSettingsDir))
                    Directory.CreateDirectory(clientSettingsDir);

                JObject json;

                if (File.Exists(configFile))
                {
                    string text = File.ReadAllText(configFile).Trim();
                    json = string.IsNullOrWhiteSpace(text)
                        ? new JObject()
                        : JObject.Parse(text);
                }
                else
                {
                    json = new JObject();
                }

                if (json.ContainsKey("DFIntTextureQualityOverride"))
                {
                    json["DFIntTextureQualityOverride"] = level.ToString();
                }
                else
                {
                    JObject newJson = new JObject
                    {
                        ["DFIntTextureQualityOverride"] = level.ToString()
                    };

                    foreach (var prop in json.Properties())
                        newJson.Add(prop.Name, prop.Value);

                    json = newJson;
                }

                File.WriteAllText(configFile, json.ToString());

                MessageBox.Show(
                    "Texture quality level has been updated successfully.",
                    "Success",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information
                );
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    ex.Message,
                    "Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error
                );
            }
        }

        private void guna2Button32_Click(object sender, EventArgs e)
        {
            string[] items = { "0", "1", "2", "3" };
            int value = 0;

            currentIndex--;
            if (currentIndex < 0)
                currentIndex = items.Length - 1;

            string current = items[currentIndex];

            switch (current)
            {
                case "0":
                    value = 0;
                    break;
                case "1":
                    value = 1;
                    break;
                case "2":
                    value = 2;
                    break;
                case "3":
                    value = 3;
                    break;
            }

            guna2TextBox6.Text = current;
        }

        private void guna2Button31_Click(object sender, EventArgs e)
        {
            string[] items = { "0", "1", "2", "3" };
            int value = 0;

            currentIndex++;
            if (currentIndex >= items.Length)
                currentIndex = 0;

            string current = items[currentIndex];

            switch (current)
            {
                case "0":
                    value = 0;
                    break;
                case "1":
                    value = 1;
                    break;
                case "2":
                    value = 2;
                    break;
                case "3":
                    value = 3;
                    break;
            }

            guna2TextBox6.Text = current;
        }

        private void guna2Button36_Click(object sender, EventArgs e)
        {
            try
            {
                string mode = guna2TextBox7.Text.Trim();

                if (mode != "DirectX11" && mode != "Vulkan")
                {
                    MessageBox.Show(
                        "Graphics API must be DirectX11 or Vulkan.",
                        "Invalid Input",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning
                    );
                    return;
                }

                string baseDir = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "MainRoblox"
                );

                string clientSettingsDir = Path.Combine(baseDir, "ClientSettings");
                string configFile = Path.Combine(clientSettingsDir, "ClientAppSettings.json");

                if (!Directory.Exists(clientSettingsDir))
                    Directory.CreateDirectory(clientSettingsDir);

                JObject json;

                if (File.Exists(configFile))
                {
                    string text = File.ReadAllText(configFile).Trim();
                    json = string.IsNullOrWhiteSpace(text)
                        ? new JObject()
                        : JObject.Parse(text);
                }
                else
                {
                    json = new JObject();
                }

                json.Remove("FFlagDebugGraphicsPreferD3D11FL10");
                json.Remove("FFlagDebugGraphicsPreferVulkan");

                JObject newJson = new JObject();

                if (mode == "DirectX11")
                {
                    newJson["FFlagDebugGraphicsPreferD3D11FL10"] = "True";
                }
                else
                {
                    newJson["FFlagDebugGraphicsPreferVulkan"] = "True";
                }
                foreach (var prop in json.Properties())
                    newJson.Add(prop.Name, prop.Value);
                File.WriteAllText(configFile, newJson.ToString());

                MessageBox.Show(
                    "Graphics API preference has been updated successfully.",
                    "Success",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information
                );
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    ex.Message,
                    "Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error
                );
            }
        }

        private void guna2Button35_Click(object sender, EventArgs e)
        {
            guna2TextBox7.Text = "DirectX11";
        }

        private void guna2Button34_Click(object sender, EventArgs e)
        {
            guna2TextBox7.Text = "Vulkan";
        }

        private void linkLabel1_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            string url = "https://github.com/HawkHackerF/Impaction/blob/main/LICENSE";
            Process.Start(new ProcessStartInfo
            {
                FileName = url,
                UseShellExecute = true
            });
        }

        private void linkLabel2_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            string url = "https://github.com/HawkHackerF/Impaction";
            Process.Start(new ProcessStartInfo
            {
                FileName = url,
                UseShellExecute = true
            });
        }

        private void guna2Button39_Click(object sender, EventArgs e)
        {
            try
            {
                string resolution = guna2TextBox8.Text.Trim();

                var map = new Dictionary<string, string>
    {
        { "144p", "256" },
        { "240p", "512" },
        { "360p", "1024" },
        { "480p", "2048" },
        { "720p", "4096" },
        { "1080p", "8294" },
        { "1440p", "12000" },
        { "2160p", "16500" }
    };

                if (!map.ContainsKey(resolution))
                {
                    MessageBox.Show(
                        "Resolution must be 144p, 240p, 360p, 480p, 720p, 1080p, 1440p, or 2160p.",
                        "Invalid Input",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning
                    );
                    return;
                }

                string value = map[resolution];

                string baseDir = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "MainRoblox"
                );

                string clientSettingsDir = Path.Combine(baseDir, "ClientSettings");
                string configFile = Path.Combine(clientSettingsDir, "ClientAppSettings.json");

                if (!Directory.Exists(clientSettingsDir))
                    Directory.CreateDirectory(clientSettingsDir);

                JObject json;

                if (File.Exists(configFile))
                {
                    string text = File.ReadAllText(configFile).Trim();
                    json = string.IsNullOrWhiteSpace(text)
                        ? new JObject()
                        : JObject.Parse(text);
                }
                else
                {
                    json = new JObject();
                }

                if (json.ContainsKey("DFIntDebugDynamicRenderKiloPixels"))
                {
                    json["DFIntDebugDynamicRenderKiloPixels"] = value;
                }
                else
                {
                    JObject newJson = new JObject
                    {
                        ["DFIntDebugDynamicRenderKiloPixels"] = value
                    };

                    foreach (var prop in json.Properties())
                        newJson.Add(prop.Name, prop.Value);

                    json = newJson;
                }

                File.WriteAllText(configFile, json.ToString());

                MessageBox.Show(
                    "Dynamic render resolution has been updated successfully.",
                    "Success",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information
                );
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    ex.Message,
                    "Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error
                );
            }
        }

        private void guna2Button38_Click(object sender, EventArgs e)
        {
            string[] resolutions =
    {
        "144p", "240p", "360p", "480p",
        "720p", "1080p", "1440p", "2160p"
    };

            int value = 0;

            currentIndex--;
            if (currentIndex < 0)
                currentIndex = resolutions.Length - 1;

            string current = resolutions[currentIndex];

            switch (current)
            {
                case "144p": value = 256; break;
                case "240p": value = 512; break;
                case "360p": value = 1024; break;
                case "480p": value = 2048; break;
                case "720p": value = 4096; break;
                case "1080p": value = 8294; break;
                case "1440p": value = 12000; break;
                case "2160p": value = 16500; break;
            }

            guna2TextBox8.Text = current;
        }

        private void guna2Button37_Click(object sender, EventArgs e)
        {
            string[] resolutions =
    {
        "144p", "240p", "360p", "480p",
        "720p", "1080p", "1440p", "2160p"
    };

            int value = 0;

            currentIndex++;
            if (currentIndex >= resolutions.Length)
                currentIndex = 0;

            string current = resolutions[currentIndex];

            switch (current)
            {
                case "144p": value = 256; break;
                case "240p": value = 512; break;
                case "360p": value = 1024; break;
                case "480p": value = 2048; break;
                case "720p": value = 4096; break;
                case "1080p": value = 8294; break;
                case "1440p": value = 12000; break;
                case "2160p": value = 16500; break;
            }

            guna2TextBox8.Text = current;
        }
    }
}
