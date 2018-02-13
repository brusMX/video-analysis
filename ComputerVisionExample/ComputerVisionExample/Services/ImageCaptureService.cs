using ComputerVisionExample.Models;
using Microsoft.ProjectOxford.Common.Contract;
using Microsoft.ProjectOxford.Emotion;
using Microsoft.ProjectOxford.Face;
using Microsoft.ProjectOxford.Vision;
using Microsoft.ProjectOxford.Vision.Contract;
using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Face = Microsoft.ProjectOxford.Face.Contract.Face;
using Rectangle = Microsoft.ProjectOxford.Common.Rectangle;

namespace ComputerVisionExample
{
    public interface IImageCaptureService
    {
        Task FetchVideoFromCameraAsync(int cameraIndex = 0, double overrideFps = 0);
    }

    public class ImageCaptureService
        : IImageCaptureService
    {
        private readonly ImageEncodingParam[] jpegParameters = {
            new ImageEncodingParam(ImwriteFlags.JpegQuality, 90)
        };

        private readonly IEmotionServiceClient emotionServiceClient;
        private readonly IFaceServiceClient faceServiceClient;
        private readonly IVisionServiceClient visionServiceClient;

        public ImageCaptureService()
        {
            emotionServiceClient = ServiceLocator.GetRequiredService<IEmotionServiceClient>();
            faceServiceClient = ServiceLocator.GetRequiredService<IFaceServiceClient>();
            visionServiceClient = ServiceLocator.GetRequiredService<IVisionServiceClient>();
        }

        public IEnumerable<Emotion> Emotions { get; set; }
        public IEnumerable<Face> Faces { get; set; }
        public AnalysisResult AnalysisResult { get; set; }

        public async Task FetchVideoFromCameraAsync(int cameraIndex = 0, double overrideFps = 0)
        {
            var videoCapture = new VideoCapture(cameraIndex);
            var fps = overrideFps;
            if (fps == 0)
            {
                fps = 30;
            }

            await Task.Factory.StartNew(() => StartProcessing(videoCapture, 0, TimeSpan.FromSeconds(1 / fps), () => DateTime.Now));
        }

        protected void StartProcessing(VideoCapture videoCapture, int frameCount, TimeSpan delay, Func<DateTime> timestampFunction)
        {
            var timer = new Timer(async state =>
            {
                await Task.Factory.StartNew(async () =>
                {
                    var capture = state as VideoCapture;
                    var timestamp = timestampFunction();
                    var image = new Mat();

                    VideoFrameMetadata videoFrameMetadata;
                    videoFrameMetadata.Index = frameCount;
                    videoFrameMetadata.Timestamp = timestamp;
                    capture.Read(image);

                    var videoFrame = new VideoFrame(image, videoFrameMetadata);
                    await AnalyzeEmotionAsync(videoFrame);
                });
            }, videoCapture, TimeSpan.Zero, delay);
        }

        protected async Task AnalyzeFaceAsync(VideoFrame videoFrame)
        {
            var imageStream = videoFrame.Image.ToMemoryStream(".jpg", jpegParameters);
            var faceAttributes = new List<FaceAttributeType>()
            {
                FaceAttributeType.Age,
                FaceAttributeType.Gender,
                FaceAttributeType.HeadPose
            };
            Faces = await faceServiceClient.DetectAsync(imageStream, returnFaceAttributes: faceAttributes);
        }

        protected async Task AnalyzeVisionAsync(VideoFrame videoFrame)
        {
            var imageStream = videoFrame.Image.ToMemoryStream(".jpg", jpegParameters);
            var visualFeatures = new List<VisualFeature>()
            {
                VisualFeature.Adult,
                VisualFeature.Categories,
                VisualFeature.Description,
                VisualFeature.Tags,
                VisualFeature.Color,
                VisualFeature.ImageType,
            };
            var analysisResult = await visionServiceClient.AnalyzeImageAsync(imageStream, visualFeatures);
        }

        protected async Task AnalyzeEmotionAsync(VideoFrame videoFrame)
        {
            var imageStream = videoFrame.Image.ToMemoryStream(".jpg", jpegParameters);
            var localFaces = (IEnumerable<Rect>)videoFrame.UserData;
            if (localFaces == null)
            {
                // If localFaces is null, we're not performing local face detection.
                // Use Cognigitve Services to do the face detection.
                Emotions = await emotionServiceClient.RecognizeAsync(imageStream);
            }
            else if (localFaces.Count() > 0)
            {
                // If we have local face detections, we can call the API with them. 
                // First, convert the OpenCvSharp rectangles. 
                var rectangles = localFaces.Select(face => new Rectangle
                {
                    Left = face.Left,
                    Top = face.Top,
                    Width = face.Width,
                    Height = face.Height
                });

                Emotions = await emotionServiceClient.RecognizeAsync(imageStream, rectangles.ToArray());
            }
            else
            {
                // Local face detection found no faces; don't call Cognitive Services.
                Emotions = new List<Emotion>();
            }
        }
    }
}
