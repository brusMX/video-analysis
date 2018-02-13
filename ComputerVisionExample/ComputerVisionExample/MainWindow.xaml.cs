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
        public MainWindow()
        {
            ServiceLocator.RegisterService<IEmotionServiceClient, EmotionServiceClient>();
            ServiceLocator.RegisterService<IFaceServiceClient, FaceServiceClient>();
            ServiceLocator.RegisterService<IVisionServiceClient, VisionServiceClient>();
            ServiceLocator.RegisterService<IImageCaptureService, ImageCaptureService>();

            InitializeComponent();
        }

        protected override void OnActivated(EventArgs e)
        {
            base.OnActivated(e);
        }
    }
}
