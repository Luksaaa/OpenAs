using OpenAs.Models;
using OpenAs.Services;
using System.Windows;

namespace OpenAs;

public partial class App : Application
{
    private async void Application_Startup(object sender, StartupEventArgs e)
    {
        var installedFileFormatService = new InstalledFileFormatService();
        if (FileOpenRequest.TryParse(e.Args, installedFileFormatService.Find, out var request))
        {
            var opener = new FileOpenService(new FileSignatureService());
            await opener.OpenAsync(request);
            Shutdown();
            return;
        }

        var window = new MainWindow();
        window.Show();
    }
}
