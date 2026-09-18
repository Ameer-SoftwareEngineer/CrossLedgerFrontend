using QRCoder;

namespace CrossLedgerFrontend.Auth;

/// <summary>Renders the otpauth:// URI BeginTotpEnrollmentResponse already carries into an
/// actual scannable QR code - as a base64 PNG data: URI, so no JS interop or canvas
/// element is needed, the same technique already used for the KYC document preview.
/// Uses QRCoder's QRCodeGenerator (pure C#) + PngByteQRCode (writes PNG bytes by hand,
/// no System.Drawing.Bitmap/Graphics) specifically because Blazor WASM has no GDI+ backend
/// - QRCoder's other renderers (QRCode, ArtQRCode, Base64QRCode) would fail there.</summary>
public static class TotpQrCodeGenerator
{
    public static string GenerateDataUri(string otpauthUri)
    {
        using var generator = new QRCodeGenerator();
        using var data = generator.CreateQrCode(otpauthUri, QRCodeGenerator.ECCLevel.Q);
        using var pngQrCode = new PngByteQRCode(data);
        var bytes = pngQrCode.GetGraphic(10);
        return $"data:image/png;base64,{Convert.ToBase64String(bytes)}";
    }
}
