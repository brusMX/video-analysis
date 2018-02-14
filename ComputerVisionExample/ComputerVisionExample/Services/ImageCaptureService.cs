using ComputerVisionExample.Models;
using Microsoft.ProjectOxford.Common.Contract;
using Microsoft.ProjectOxford.Emotion;
using Microsoft.ProjectOxford.Face;
using Microsoft.ProjectOxford.Vision;
using Microsoft.ProjectOxford.Vision.Contract;
using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.IO;
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
        Task FetchVideoFromFileAsync(FileInfo fileInfo, double overrideFps = 0);
        Task FetchImageFromFileAsync(FileInfo fileInfo);

        IEnumerable<Emotion> Emotions { get; set; }
        IEnumerable<Face> Faces { get; set; }
        AnalysisResult Analysis { get; set; }
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
        public AnalysisResult Analysis { get; set; }
        public bool Stopping { get; set; }

        public async Task FetchVideoFromCameraAsync(int cameraIndex = 0, double overrideFps = 0)
        {
            var videoCapture = new VideoCapture(cameraIndex);
            var fps = overrideFps;
            if (fps == 0)
            {
                fps = 30;
            }

            await Task.Factory.StartNew(() => StartProcessing(videoCapture, 0, TimeSpan.FromSeconds(1 / fps)));
        }

        public async Task FetchVideoFromFileAsync(FileInfo fileInfo, double overrideFps = 0)
        {
            var fileCapture = new VideoCapture(fileInfo.FullName);
            var fps = overrideFps;
            if (fps == 0)
            {
                fps = 30;
            }

            await Task.Factory.StartNew(() => StartProcessing(fileCapture, 0, TimeSpan.FromSeconds(1 / fps)));
        }

        public async Task FetchImageFromFileAsync(FileInfo fileInfo)
        {
            using (FileStream fileStream = File.OpenRead(fileInfo.FullName))
            {
                var memoryStream = new MemoryStream();
                memoryStream.SetLength(fileStream.Length);
                fileStream.Read(memoryStream.GetBuffer(), 0, (int)fileStream.Length);

                await AnalyzeEmotionAsync(memoryStream, null);
                await AnalyzeFaceAsync(memoryStream);
                await AnalyzeVisionAsync(memoryStream);
            }
        }

        protected void StartProcessing(VideoCapture videoCapture, int frameCount, TimeSpan delay)
        {
            var timer = new Timer(state =>
            {
                var passedValues = new InsideState()
                {
                    VideoCapture = videoCapture,
                    FrameCount = frameCount++,
                };

                new Timer(async insideState => await ProcessStream(insideState), passedValues, 1000, delay.Milliseconds);
            }, videoCapture, 1000, delay.Milliseconds);
        }

        protected async Task ProcessStream(object state)
        {
            await Task.Factory.StartNew(async () =>
            {
                var insideState = state as InsideState;
                DateTime timestamp() => DateTime.Now;
                var image = new Mat();

                VideoFrameMetadata videoFrameMetadata;
                videoFrameMetadata.Index = insideState.FrameCount;
                videoFrameMetadata.Timestamp = timestamp();
                insideState.VideoCapture.Read(image);

                var videoFrame = new VideoFrame(image, videoFrameMetadata);
                await AnalyzeEmotionAsync(videoFrame.Image.ToMemoryStream(".jpg", jpegParameters), (IEnumerable<Rect>)videoFrame.UserData);
                await AnalyzeFaceAsync(videoFrame.Image.ToMemoryStream(".jpg", jpegParameters));
                await AnalyzeVisionAsync(videoFrame.Image.ToMemoryStream(".jpg", jpegParameters));
            });
        }

        protected async Task AnalyzeFaceAsync(Stream imageStream)
        {
            var faceAttributes = new List<FaceAttributeType>()
            {
                FaceAttributeType.Age,
                FaceAttributeType.Gender,
                FaceAttributeType.HeadPose
            };
            Faces = await faceServiceClient.DetectAsync(imageStream, returnFaceAttributes: faceAttributes);
        }

        protected async Task AnalyzeVisionAsync(Stream imageStream)
        {
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

        protected async Task AnalyzeEmotionAsync(Stream imageStream, IEnumerable<Rect> localFaces)
        {
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
