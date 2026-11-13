using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Security.Principal;

namespace Temp
{
    internal static class Program
    {
        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            AppLogger.Information("Uygulama başlatılıyor.");
            try
            {
                if (!IsRunningAsAdministrator())
                {
                    const string message =
                        "CPU sensörlerine erişebilmek için uygulamanın yönetici yetkisiyle çalışması gerekir.\n\n" +
                        "Uygulama yönetici olarak yeniden başlatılsın mı?";

                    DialogResult result = MessageBox.Show(
                        message,
                        "Yönetici yetkisi gerekli",
                        MessageBoxButtons.YesNo,
                        MessageBoxIcon.Warning);

                    if (result != DialogResult.Yes ||
                        !RestartAsAdministrator())
                    {
                        return;
                    }

                    return;
                }

                ApplicationConfiguration.Initialize();
                Application.ThreadException += application_ThreadException;
                Application.Run(new Form1());
            }
            catch (FileNotFoundException ex)
            {
                AppLogger.Critical("Bağımlılık bulunamadı.", ex);
                ShowDependencyError(ex);
            }
            catch (FileLoadException ex)
            {
                AppLogger.Critical("Assembly yüklenemedi.", ex);
                ShowDependencyError(ex);
            }
            catch (DllNotFoundException ex)
            {
                AppLogger.Critical("Native DLL yüklenemedi.", ex);
                ShowDependencyError(ex);
            }
            catch (BadImageFormatException ex)
            {
                AppLogger.Critical("Uyumsuz DLL biçimi.", ex);
                ShowDependencyError(ex);
            }
            catch (TypeInitializationException ex) when (ContainsDependencyException(ex))
            {
                ShowDependencyError(GetDependencyException(ex));
            }
            catch (Exception ex)
            {
                AppLogger.Critical("Başlangıç hatası.", ex);
                ShowStartupError(ex);
            }
        }

        private static void application_ThreadException(
            object? sender,
            ThreadExceptionEventArgs e)
        {
            if (IsDependencyException(e.Exception))
            {
                ShowDependencyError(GetDependencyException(e.Exception));
                return;
            }

            ShowStartupError(e.Exception);
        }

        private static bool IsDependencyException(Exception exception)
        {
            return exception is FileNotFoundException ||
                exception is FileLoadException ||
                exception is DllNotFoundException ||
                exception is BadImageFormatException ||
                exception is TypeInitializationException &&
                ContainsDependencyException(exception);
        }

        private static bool ContainsDependencyException(Exception exception)
        {
            for (Exception? current = exception; current is not null; current = current.InnerException)
            {
                if (current is FileNotFoundException ||
                    current is FileLoadException ||
                    current is DllNotFoundException ||
                    current is BadImageFormatException)
                {
                    return true;
                }
            }

            return false;
        }

        private static Exception GetDependencyException(Exception exception)
        {
            for (Exception? current = exception; current is not null; current = current.InnerException)
            {
                if (current is FileNotFoundException ||
                    current is FileLoadException ||
                    current is DllNotFoundException ||
                    current is BadImageFormatException)
                {
                    return current;
                }
            }

            return exception;
        }

        private static void ShowDependencyError(Exception exception)
        {
            string dependencyName = exception switch
            {
                FileNotFoundException fileNotFound when
                    !string.IsNullOrWhiteSpace(fileNotFound.FileName) =>
                    fileNotFound.FileName,
                FileLoadException => "Yüklenemeyen veya sürümü uyumsuz bir assembly",
                DllNotFoundException => "Gerekli native DLL",
                BadImageFormatException => "Uyumsuz veya bozuk bir DLL",
                _ => "Gerekli bir bağımlılık"
            };

            MessageBox.Show(
                $"Uygulama başlatılamadı çünkü şu bağımlılık yüklenemedi:\n\n" +
                $"{dependencyName}\n\n" +
                "NuGet paketlerinin geri yüklendiğini, uygulamanın doğru platformda " +
                "(x64/x86) derlendiğini ve gerekli DLL dosyalarının bin klasöründe " +
                "bulunduğunu kontrol edin.\n\n" +
                $"Ayrıntı: {exception.Message}",
                "Eksik uygulama bağımlılığı",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }

        private static void ShowStartupError(Exception exception)
        {
            MessageBox.Show(
                $"Uygulama başlatılırken beklenmeyen bir hata oluştu:\n\n" +
                $"{exception.Message}",
                "Başlatma hatası",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }

        private static bool IsRunningAsAdministrator()
        {
            using WindowsIdentity identity = WindowsIdentity.GetCurrent();
            WindowsPrincipal principal = new WindowsPrincipal(identity);

            return principal.IsInRole(WindowsBuiltInRole.Administrator);
        }

        private static bool RestartAsAdministrator()
        {
            string executablePath = Environment.ProcessPath
                ?? Application.ExecutablePath;

            var startInfo = new ProcessStartInfo
            {
                FileName = executablePath,
                UseShellExecute = true,
                Verb = "runas",
                WorkingDirectory = AppContext.BaseDirectory
            };

            string[] arguments = Environment.GetCommandLineArgs();
            for (int i = 1; i < arguments.Length; i++)
            {
                startInfo.ArgumentList.Add(arguments[i]);
            }

            try
            {
                Process.Start(startInfo);
                return true;
            }
            catch (Win32Exception ex) when (ex.NativeErrorCode == 1223)
            {
                MessageBox.Show(
                    "Yönetici izni verilmedi. Uygulama kapatılacak.",
                    "İşlem iptal edildi",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            }
            catch (Win32Exception ex)
            {
                MessageBox.Show(
                    $"Uygulama yönetici olarak yeniden başlatılamadı:\n{ex.Message}",
                    "Başlatma hatası",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
            catch (InvalidOperationException ex)
            {
                MessageBox.Show(
                    $"Uygulama başlatılamadı:\n{ex.Message}",
                    "Başlatma hatası",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }

            return false;
        }
    }
}