using Microsoft.UI.Xaml.Controls;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Processing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.Formats;
using System.IO;
using System.Diagnostics;

namespace SCPP_WinUI_CS.Helpers
{
    /* Comprime una imagen usando imageSharp
     * 
     */
    public class CompressImg
    {
        public async Task<string> Compress(StorageFile file)
        {
            var image = SixLabors.ImageSharp.Image.Load(file.Path);
            // Calculate the new height while maintaining the aspect ratio
            int newWidth = 1080;
            int newHeight = (int)((float)image.Height / image.Width * newWidth);

            // Resize the image
            image.Mutate(x => x.Resize(newWidth, newHeight));

            // Create a temporary file for the compressed image
            StorageFolder tempFolder = await StorageFolder.GetFolderFromPathAsync(Path.GetTempPath());
            string compressedFileName = Path.ChangeExtension(Path.GetRandomFileName(), Path.GetExtension(file.Path));
            StorageFile compressedTempFile = await tempFolder.CreateFileAsync(compressedFileName, CreationCollisionOption.GenerateUniqueName);

            // Automatically detect the encoder based on the file extension
            var encoder = GetEncoder(file.Path);
            // Save the compressed image to the temporary file
            image.Save(compressedTempFile.Path, encoder);
            return compressedTempFile.Path;
        }
        // Helper method to get an image encoder based on the file extension
        public IImageEncoder GetEncoder(string filePath)
        {
            string extension = System.IO.Path.GetExtension(filePath).TrimStart('.');

            return extension switch
            {
                "png" => new PngEncoder(),
                "jpg" => new JpegEncoder { Quality = 75 },
                "jpeg" => new JpegEncoder { Quality = 75 },
                // Add more cases for other supported formats if needed
                _ => new PngEncoder() // Default to PNG if extension is not recognized
            };
        }
    }
}
