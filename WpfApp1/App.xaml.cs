using System.Configuration;
using System.Data;
using System.Text;
using System.Windows;
using Amazon.Runtime;
using Amazon.TranscribeStreamingService;
using Amazon.TranscribeStreamingService.Models;
using CSCore.Codecs.WAV;
using CSCore;
using CSCore.SoundIn;
using CSCore.Streams;
using Microsoft.Extensions.Configuration;

namespace WpfApp1;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    public static AwsSettings? AwsSettings { get; private set; }
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        var config = new ConfigurationBuilder()
            .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .Build();

        AwsSettings = config.GetSection("AWS").Get<AwsSettings>();
    }
}

