using System;
using System.IO;
using System.IO.Compression;
using System.Diagnostics;
using System.Windows;
using System.Collections.Generic;

namespace SteamFreeGamesTool
{
    public partial class MainWindow : Window
    {
        private readonly string steamPath = @"C:\Program Files (x86)\Steam";
        private readonly string depotCachePath = @"C:\Program Files (x86)\Steam\depotcache";
        private readonly string luaPluginPath = @"C:\Program Files (x86)\Steam\config\stplug-in";

        public MainWindow()
        {
            InitializeComponent();
            LoadNovaInstalledGames();
        }

    

        private void DropZone_DragEnter(object sender, DragEventArgs e) => e.Effects = DragDropEffects.Copy;

        private async void DropZone_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
                string zipPath = files[0];

                try
                {
                    StatusText.Text = "جاري معالجة وتوزيع ملفات الترخيص...";

                    string tempPath = Path.Combine(Path.GetTempPath(), "Nova_Temp");
                    if (Directory.Exists(tempPath)) Directory.Delete(tempPath, true);
                    Directory.CreateDirectory(tempPath);

                    await System.Threading.Tasks.Task.Run(() => {
                        ZipFile.ExtractToDirectory(zipPath, tempPath, true);
                    });

                    if (!Directory.Exists(depotCachePath)) Directory.CreateDirectory(depotCachePath);
                    if (!Directory.Exists(luaPluginPath)) Directory.CreateDirectory(luaPluginPath);

                    var manifestFiles = Directory.GetFiles(tempPath, "*.manifest", SearchOption.AllDirectories);
                    foreach (var file in manifestFiles)
                    {
                        File.Copy(file, Path.Combine(depotCachePath, Path.GetFileName(file)), true);
                    }

                    var luaFiles = Directory.GetFiles(tempPath, "*.lua", SearchOption.AllDirectories);
                    foreach (var file in luaFiles)
                    {
                        File.Copy(file, Path.Combine(luaPluginPath, Path.GetFileName(file)), true);
                    }

                    string sourceNovaDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "nova");
                    if (Directory.Exists(sourceNovaDir))
                    {
                        foreach (var dll in Directory.GetFiles(sourceNovaDir, "*.*", SearchOption.AllDirectories))
                        {
                            string relativePath = dll.Substring(sourceNovaDir.Length).TrimStart(Path.DirectorySeparatorChar);
                            string destDllPath = Path.Combine(steamPath, relativePath);
                            Directory.CreateDirectory(Path.GetDirectoryName(destDllPath) ?? steamPath);
                            File.Copy(dll, destDllPath, true);
                        }
                    }

                    StatusText.Text = "تم التوزيع بنجاح!";
                    RestartSteamProcess();
                    MessageBox.Show("تمت إضافة اللعبة وتفعيل الترخيص عبر البرنامج بنجاح!", "Nova System", MessageBoxButton.OK, MessageBoxImage.Information);
                    
                    LoadNovaInstalledGames();
                }
                catch (Exception ex) 
                { 
                    MessageBox.Show("خطأ أثناء التنفيذ: " + ex.Message, "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
                    StatusText.Text = "حدث خطأ في المعالجة.";
                }
            }
        }

        private void BtnRestartSteam_Click(object sender, RoutedEventArgs e) => RestartSteamProcess();

        private void BtnOpenGithub_Click(object sender, RoutedEventArgs e)
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = "https://github.com/abushama1-ar/Nova_Premium",
                UseShellExecute = true
            });
        }

        private void RestartSteamProcess()
        {
            try
            {
                foreach (var p in Process.GetProcessesByName("steam")) { p.Kill(); p.WaitForExit(3000); }
                string steamExe = Path.Combine(steamPath, "steam.exe");
                if (File.Exists(steamExe)) Process.Start(steamExe);
            }
            catch (Exception ex) { MessageBox.Show("خطأ في إعادة تشغيل ستيم: " + ex.Message); }
        }

        private void LoadNovaInstalledGames()
        {
            try
            {
                var novaGames = new List<string>();

                if (Directory.Exists(luaPluginPath))
                {
                    foreach (var file in Directory.GetFiles(luaPluginPath, "*.lua"))
                    {
                        novaGames.Add("Lua Script: " + Path.GetFileNameWithoutExtension(file));
                    }
                }

                if (Directory.Exists(depotCachePath))
                {
                    foreach (var file in Directory.GetFiles(depotCachePath, "*.manifest"))
                    {
                        novaGames.Add("Manifest ID: " + Path.GetFileNameWithoutExtension(file));
                    }
                }

                NovaGamesListBox.ItemsSource = novaGames;
            }
            catch (Exception) { }
        }

        private void BtnDeleteGameByInput_Click(object sender, RoutedEventArgs e)
        {
            string query = DeleteGameInput.Text.Trim();
            if (string.IsNullOrEmpty(query)) { MessageBox.Show("الرجاء إدخال اسم اللعبة أو الـ AppID للحذف!"); return; }

            try
            {
                int deletedCount = 0;

                if (Directory.Exists(depotCachePath))
                {
                    foreach (var file in Directory.GetFiles(depotCachePath, $"*{query}*"))
                    {
                        File.Delete(file);
                        deletedCount++;
                    }
                }

                if (Directory.Exists(luaPluginPath))
                {
                    foreach (var file in Directory.GetFiles(luaPluginPath, $"*{query}*"))
                    {
                        File.Delete(file);
                        deletedCount++;
                    }
                }

                if (deletedCount > 0)
                {
                    MessageBox.Show($"تم حذف ملفات الترخيص المرتبطة بـ ({query}) بنجاح!", "حذف ناجح", MessageBoxButton.OK, MessageBoxImage.Information);
                    LoadNovaInstalledGames();
                }
                else
                {
                    MessageBox.Show("لم يتم العثور على ملفات تطابق المدخلات في مجلدات البرنامج!", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("خطأ أثناء الحذف: " + ex.Message, "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}