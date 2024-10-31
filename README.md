**Description**

This is a C# application that automates video processing and uploading for social media. This application receives a TikTok video link through a Telegram bot, downloads the video to the local PC, processes it using FFmpeg, and then uploads the edited video to Instagram via Selenium. Additionally, the application uses AutoIt to automate the file selection process for uploading.

**Features**

- **Video Download**: Downloads videos directly from TikTok through a Telegram bot link.
- **Video Processing**: Processes the downloaded video using FFmpeg, allowing for custom overlays and format adjustments.
- **Instagram Upload**: Automates the process of uploading the processed video to Instagram using Selenium.
- **Automated File Selection**: Uses AutoIt to select the correct video file during the Instagram upload process.

**Technology Stack**

- **C#**
- **FFmpeg** for video processing
- **Selenium** for automated Instagram uploads
- **AutoIt** for file selection
- **Telegram Bot** for receiving TikTok video links

**Prerequisites**

To run this application, you need to install the following:

1. **FFmpeg** for video processing:
   - Download FFmpeg from [Gyan's FFmpeg Builds](https://www.gyan.dev/ffmpeg/builds/).
   - Add the path to `ffmpeg.exe` in your system's environment variables, or place it in a folder within your project and configure the binary path in the code:
     ```csharp
     GlobalFFOptions.Configure(options => options.BinaryFolder = @"C:\path\to\ffmpeg\bin");
     ```

2. **AutoIt** for automated file selection:
   - Download AutoIt from [this link](https://www.autoitscript.com/site/autoit/downloads/).
   - This enables the application to interact with the file selection dialog on the PC for Instagram uploads.

---
