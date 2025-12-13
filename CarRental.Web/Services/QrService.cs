using QRCoder;
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;

namespace CarRental.Web.Services
{
    public static class QrService
    {
        public static string GenerateQrBase64(string content)
        {
            QRCodeGenerator qr = new QRCodeGenerator();
            QRCodeData data = qr.CreateQrCode(content, QRCodeGenerator.ECCLevel.Q);
            QRCode code = new QRCode(data);

            using (Bitmap bitmap = code.GetGraphic(20))
            using (MemoryStream ms = new MemoryStream())
            {
                bitmap.Save(ms, ImageFormat.Png);
                string base64 = Convert.ToBase64String(ms.ToArray());
                return $"data:image/png;base64,{base64}";
            }
        }
    }
}
