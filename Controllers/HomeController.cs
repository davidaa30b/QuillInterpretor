using iText.Html2pdf;
using iText.Html2pdf.Resolver.Font;
using iText.Kernel.Geom;
using iText.Kernel.Pdf;
using Microsoft.AspNetCore.Mvc;
using QuillTextEditor.Models;
using System.Diagnostics;
using System.Net;
using System.Text.RegularExpressions;

namespace QuillTextEditor.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;

        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
        }

        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }
        private async Task<string> SaveFontToTempFileAsync(string fileUri, string tempFileName)
        {
            using var client = new HttpClient();
            var fontData = await client.GetByteArrayAsync(fileUri);

            string tempPath = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"{tempFileName}_{Guid.NewGuid()}.ttf");
            await System.IO.File.WriteAllBytesAsync(tempPath, fontData);
            return tempPath;
        }

        private async Task<string> SaveTimesNewRomanFontToTempFileAsync()
        {
            string fileUri = "https://companymanagerdocuments.blob.core.windows.net/fonts/times.ttf";

            return await SaveFontToTempFileAsync(fileUri, "times");
        }
        private async Task<string> SaveGeorgiaFontToTempFileAsync()
        {
            string fileUri = "https://companymanagerdocuments.blob.core.windows.net/fonts/georgia.ttf";
            return await SaveFontToTempFileAsync(fileUri, "georgia");

        }
        private async Task<string> SaveGeorgiaBFontToTempFileAsync()
        {
            string fileUri = "https://companymanagerdocuments.blob.core.windows.net/fonts/georgiab.ttf";
            return await SaveFontToTempFileAsync(fileUri, "georgiab");
        }
        private async Task<string> SaveGeorgiaIFontToTempFileAsync()
        {
            string fileUri = "https://companymanagerdocuments.blob.core.windows.net/fonts/georgiai.ttf";
            return await SaveFontToTempFileAsync(fileUri, "georgiai");
        }
        private async Task<string> SaveGeorgiaZFontToTempFileAsync()
        {
            string fileUri = "https://companymanagerdocuments.blob.core.windows.net/fonts/georgiaz.ttf";
            return await SaveFontToTempFileAsync(fileUri, "georgiaz");
        }

        public string AdjustParagraphSpacingWithSubtraction(string htmlContent, int subtractPx = 15)
        {
            var doc = new HtmlAgilityPack.HtmlDocument();
            doc.LoadHtml(htmlContent);

            var paragraphs = doc.DocumentNode.SelectNodes("//p");
            if (paragraphs != null)
            {
                foreach (var p in paragraphs)
                {
                    var style = p.GetAttributeValue("style", "");

                    // Match margin-bottom: XXpx;
                    var match = Regex.Match(style, @"margin-bottom\s*:\s*(\d+)px", RegexOptions.IgnoreCase);

                    if (match.Success)
                    {
                        int currentMargin = int.Parse(match.Groups[1].Value);
                        int newMargin = currentMargin - subtractPx; // Ensure it doesn't go below 0

                        // Replace old value with new one
                        var newStyle = Regex.Replace(style, @"margin-bottom\s*:\s*\d+px", $"margin-bottom: {newMargin}px", RegexOptions.IgnoreCase);

                        p.SetAttributeValue("style", newStyle);
                    }
                }
            }

            return doc.DocumentNode.OuterHtml;
        }

        public async Task<IActionResult> Save(string htmlContent)
        {
            htmlContent = AdjustParagraphSpacingWithSubtraction(htmlContent);
            htmlContent = WebUtility.UrlDecode(htmlContent);
            htmlContent = WebUtility.HtmlDecode(htmlContent);

            var props = new ConverterProperties();
            props.SetCharset("utf-8");
            var fontProvider = new DefaultFontProvider(false, false, false);
            props.SetFontProvider(fontProvider);

            string timesfontPath = await SaveTimesNewRomanFontToTempFileAsync();
            string georgiafontPath = await SaveGeorgiaFontToTempFileAsync();
            string georgiaifontPath = await SaveGeorgiaIFontToTempFileAsync();
            string georgiabfontPath = await SaveGeorgiaBFontToTempFileAsync();
            string georgiazfontPath = await SaveGeorgiaZFontToTempFileAsync();

            fontProvider.AddFont(timesfontPath);
            fontProvider.AddFont(georgiafontPath);
            fontProvider.AddFont(georgiaifontPath);
            fontProvider.AddFont(georgiabfontPath);
            fontProvider.AddFont(georgiazfontPath);

            props.SetFontProvider(fontProvider);


            using var ms = new MemoryStream();
            using var writer = new PdfWriter(ms);

            // Set custom page size (e.g., custom width 500pt, height 842pt like A4)
            var customPageSize = new PageSize(595, 842);
            using var pdfDoc = new PdfDocument(writer);
            pdfDoc.SetDefaultPageSize(customPageSize);
            HtmlConverter.ConvertToPdf(htmlContent, pdfDoc, props);

            pdfDoc.Close();

            return File(ms.ToArray(), "application/pdf");
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
