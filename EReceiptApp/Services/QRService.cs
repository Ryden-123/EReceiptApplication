using QRCoder;
using System;
using System.IO;
using System.Windows.Media.Imaging;
using EReceiptApp.Models;

namespace EReceiptApp.Services
{
    public class QRService
    {
        public BitmapImage GenerateQR(Receipt receipt)
        {
            string payload = BuildQRPayload(receipt);

            using var qrGenerator = new QRCodeGenerator();
            var qrData = qrGenerator.CreateQrCode(payload,
                QRCodeGenerator.ECCLevel.Q);

            // Use PngByteQRCode which works across all platforms
            var qrCode = new PngByteQRCode(qrData);
            byte[] qrBytes = qrCode.GetGraphic(10);

            return BytesToBitmapImage(qrBytes);
        }

        public void SaveQRAsPng(Receipt receipt, string filePath)
        {
            string payload = BuildQRPayload(receipt);

            using var qrGenerator = new QRCodeGenerator();
            var qrData = qrGenerator.CreateQrCode(payload,
                QRCodeGenerator.ECCLevel.Q);

            var qrCode = new PngByteQRCode(qrData);
            byte[] qrBytes = qrCode.GetGraphic(10);

            File.WriteAllBytes(filePath, qrBytes);
        }

        // Replace with
        private string BuildQRPayload(Receipt receipt)
        {
            return $"RECEIPT#{receipt.ReceiptNumber}\n" +
                   $"Issued To:{receipt.IssuedTo}\n" +
                   $"ID:{receipt.IdNumber}\n" +
                   $"Date:{receipt.DateIssued:yyyy-MM-dd}\n" +
                   $"Total:PHP {receipt.TotalAmount:F2}\n" +
                   $"Org:{receipt.OrganizationName}";
        }

        private BitmapImage BytesToBitmapImage(byte[] bytes)
        {
            using var stream = new MemoryStream(bytes);
            var image = new BitmapImage();
            image.BeginInit();
            image.CacheOption = BitmapCacheOption.OnLoad;
            image.StreamSource = stream;
            image.EndInit();
            image.Freeze();
            return image;
        }
    }
}