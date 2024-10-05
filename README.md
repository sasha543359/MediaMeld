# # MediaMed

### Description
**InstaVid Pro** is a C# application that enables users to download videos, clean their metadata, and customize them by adding a specified PNG image and video footage to the bottom of the screen. The app then exports the edited video and automates the process of uploading it to Instagram.

### Features
- **Download Videos**: Download videos directly from TikTok or other sources.
- **Remove Metadata**: Clean all metadata from the video for privacy and optimization.
- **Custom Overlay**: Add a PNG image and video overlay to the bottom part of the screen.
- **Export**: Export the customized video in the specified format.
- **Instagram Upload**: Upload the edited video directly to Instagram, making it ready for sharing.

### Technology Stack
- **C#**
- **FFmpeg** for video processing
- **Instagram API** (or third-party library) for uploading
- **RapidAPI** for video downloading

### Prerequisites
To run this application, you need to install **FFmpeg** for video processing.

- Download FFmpeg from [Gyan's FFmpeg Builds](https://www.gyan.dev/ffmpeg/builds/).
- Add the path to `ffmpeg.exe` in your system's environment variables, or place it in a folder in your project and configure the binary path in the code:

```csharp
GlobalFFOptions.Configure(options => options.BinaryFolder = @"C:\path\to\ffmpeg\bin");
```
