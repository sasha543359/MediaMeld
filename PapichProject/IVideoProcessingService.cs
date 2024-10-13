using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VideoProcessing;

public interface IVideoProcessingService
{
    Task<string> GetTikTokDownloadUrl(string videoUrl);
    Task DownloadVideo(string downloadUrl, string savePath);
    void ProcessVideo(string inputVideoPath, string outputVideoPath);
    void AddImageToVideo(string videoPath, string imagePath, string outputPath, int x, int y);
    void AddVideoWithChromaKey(string backgroundVideoPath, string overlayVideoPath, string outputPath);
    TimeSpan GetVideoDuration(string videoPath);
}