using OpenAs.Models;
using OpenAs.Services;
using System.Windows;

namespace OpenAs;

public partial class App : Application
{
    private async void Application_Startup(object sender, StartupEventArgs e)
    {
        if (FileOpenRequest.TryParse(e.Args, out var request))
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
