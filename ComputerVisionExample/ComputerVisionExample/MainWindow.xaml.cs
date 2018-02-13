using ComputerVisionExample.Properties;
using Microsoft.ProjectOxford.Emotion;
using Microsoft.ProjectOxford.Face;
using Microsoft.ProjectOxford.Vision;
using System;
using Window = System.Windows.Window;

namespace ComputerVisionExample
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow
        : Window
    {
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
    }
}
