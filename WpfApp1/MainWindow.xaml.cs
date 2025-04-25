using Amazon.Runtime;
using Amazon.TranscribeStreamingService.Models;
using Amazon.TranscribeStreamingService;
using CSCore.Codecs.WAV;
using CSCore.SoundIn;
using CSCore.Streams;
using CSCore;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Text.RegularExpressions;

namespace WpfApp1;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    private WasapiCapture? waveIn;
    private AmazonTranscribeStreamingClient? client;
    public MainWindow()
    {
        InitializeComponent();
    }

    private async void StartTranscription_Click(object sender, RoutedEventArgs e)
    {
        await StartLiveTranscriptionAsync();
    }
    public async Task StartLiveTranscriptionAsync()
    {

        var aws = App.AwsSettings;
        Config config = new Config("pcm", "16000", "en-US");
        string? accessKey = aws.AccessKey;
        string? secretKey = aws.SecretKey;
        string? region = aws.Region;
        BasicAWSCredentials basicCreds = new BasicAWSCredentials(accessKey, secretKey);

        client = new AmazonTranscribeStreamingClient(region, config, basicCreds);
        client.TranscriptEvent += TranscriptEvent;
        client.TranscriptException += TranscribeException;
        await client.StartStreaming();
        StreamMicrophone(client);
    }
    private void StreamMicrophone(AmazonTranscribeStreamingClient client)
    {
        waveIn = new WasapiCapture();
        if (waveIn.Device == null)
        {
            try
            {
                waveIn.Dispose();
                waveIn = null;
            }
            catch { }
        }
        waveIn.Initialize();
        using (var source = new SoundInSource(waveIn) { FillWithZeros = false })
        using (var convertedSource = source.ChangeSampleRate(16000).ToSampleSource().ToWaveSource(16).ToMono())
        {
            byte[] buffer = new byte[3200];
            waveIn.Start();
            Console.WriteLine("Recording started .... please enter to stop...");
            while (client.IsConnected)
            {
                int bytesRead = convertedSource.Read(buffer, 0, buffer.Length);
                if (bytesRead > 0)
                {
                    //Console.WriteLine($"bytes - {buffer.Take(bytesRead).ToArray().Length}");
                    client.StreamBuffer(buffer.Take(bytesRead).ToArray());
                }
                Thread.Sleep(100);

                if (Console.KeyAvailable && Console.ReadKey(true).Key == ConsoleKey.Enter)
                    break;
            }

            waveIn.Stop();
            Console.WriteLine("Recording stopped.");
        }
    }
    private static void TranscriptEvent(object? sender, TranscriptEvent transcriptEvent)
    {
        var result = transcriptEvent.Transcript?.Results?.FirstOrDefault();

        if (result != null && !result.IsPartial)
        {
            string? finalTranscript = result.Alternatives?.FirstOrDefault()?.Transcript;
            if (!string.IsNullOrEmpty(finalTranscript))
            {
                Console.WriteLine($" Final Trancription -----------> {finalTranscript}");
            }
        }
    }

    private static void TranscribeException(object? sender, TranscribeException exception)
    {
        Console.WriteLine("Error: " + exception.ExceptionType);
        Console.WriteLine(exception.Message);
    }

    private void StopButton_Click(object sender, RoutedEventArgs e)
    {
        waveIn?.Stop();
        waveIn?.Dispose();
        waveIn = null;
        client?.StopStreaming();
        client = null;
        this.startBtn.IsEnabled = true;
        Console.WriteLine("Recording stopped.");
    }
}
