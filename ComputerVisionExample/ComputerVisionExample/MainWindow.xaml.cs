using ComputerVisionExample.Properties;
using Microsoft.ProjectOxford.Emotion;
using Microsoft.ProjectOxford.Face;
using Microsoft.ProjectOxford.Vision;
using Microsoft.Win32;
using Newtonsoft.Json;
using System;
using System.ComponentModel;
using System.IO;
using System.Threading;
using System.Windows;
using ClientException = Microsoft.ProjectOxford.Common.ClientException;
using Window = System.Windows.Window;

namespace ComputerVisionExample
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow
        : Window, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        private readonly IImageCaptureService imageCaptureService;

        public MainWindow()
        {
            ServiceLocator.RegisterService<IEmotionServiceClient, EmotionServiceClient>((sp) =>
            {
                return new EmotionServiceClient(Settings.Default.EmotionAPIKey, Settings.Default.EmotionAPIHost);
            });

            ServiceLocator.RegisterService<IFaceServiceClient, FaceServiceClient>((sp) =>
            {
                return new FaceServiceClient(Settings.Default.FaceAPIKey, Settings.Default.FaceAPIHost);
            });

            ServiceLocator.RegisterService<IVisionServiceClient, VisionServiceClient>((sp) =>
            {
                return new VisionServiceClient(Settings.Default.VisionAPIKey, Settings.Default.VisionAPIHost);
            });

            ServiceLocator.RegisterService<IImageCaptureService, ImageCaptureService>();

            imageCaptureService = ServiceLocator.GetRequiredService<IImageCaptureService>();
            InitializeComponent();
        }

        protected override void OnActivated(EventArgs e)
        {
            base.OnActivated(e);
        }

        private string ResultText { get; set; }
        public string Result
        {
            get
            {
                return ResultText;
            }
            set
            {
                ResultText = value;
                OnPropertyChanged("Result");
            }
        }

        private async void StartCamera_Click(object sender, RoutedEventArgs e)
        {
            await imageCaptureService.FetchVideoFromCameraAsync(0, 60);
        }

        private async void OpenFile_Click(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new OpenFileDialog();
            if (openFileDialog.ShowDialog() == true)
            {
                try
                {
                    await imageCaptureService.FetchVideoFromFileAsync(new FileInfo(openFileDialog.FileName), 60);
                    new Timer((state) =>
                    {
                        var emotions = imageCaptureService.Emotions;
                        var faces = imageCaptureService.Faces;
                        var analysisResults = imageCaptureService.Analysis;
                        ResultText += JsonConvert.SerializeObject(emotions);
                        ResultText += JsonConvert.SerializeObject(faces);
                        ResultText += JsonConvert.SerializeObject(analysisResults);
                    }, null, 1000, 0);
                }
                catch (ClientException clientException)
                {
                    ErrorText.Text = clientException.Error.Message;
                }
            }
        }

        private async void OpenImage_Click(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new OpenFileDialog();
            if (openFileDialog.ShowDialog() == true)
            {
                try
                {
                    await imageCaptureService.FetchImageFromFileAsync(new FileInfo(openFileDialog.FileName));
                    new Timer((state) =>
                    {
                        var analysisResults = imageCaptureService.Analysis;
                        ResultText += JsonConvert.SerializeObject(analysisResults);
                    }, null, 1000, 0);
                }
                catch (ClientException clientException)
                {
                    ErrorText.Text = clientException.Error.Message;
                }
            }
        }

        protected void OnPropertyChanged(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
