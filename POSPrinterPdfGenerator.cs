using Docnet.Core.Models;
using Docnet.Core;
using System.Security.Cryptography;
using PdfSharp.Pdf;
using PdfSharp.Pdf.IO;
using System.Runtime.InteropServices;
using PdfSharp.Drawing;
using PdfSharp;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Text;
using POSPrinterPdfGenerator;

namespace IDCT.Html2Pdf
{
    /// <summary>
    /// Allows generation of long and narrow PDFs out of HTML 4 for printing of receipts on POS printers.
    /// </summary>
    public class POSPrinterPdfGenerator
    {
        /// <summary>
        /// Path to htmldoc (or htmldoc.exe on Windows).
        /// </summary>
        private readonly string HtmldocPath;

        /// <summary>
        /// Indicates that unlicensed library is running. Changed to true if proper license is loaded.
        /// </summary>
        private readonly bool Unlicensed;

        /// <summary>
        /// Tries to find an executable in paths indicated by PATH env variable.
        /// </summary>
        /// <param name="exe">Name of the executable.</param>
        /// <returns>Full path to the executable.</returns>
        /// <exception cref="FileNotFoundException">When file is not found.</exception>
        private static string FindExePath(string exe)
        {
            exe = Environment.ExpandEnvironmentVariables(exe);
            if (!File.Exists(exe))
            {
                if (Path.GetDirectoryName(exe) == String.Empty)
                {
                    foreach (string test in (Environment.GetEnvironmentVariable("PATH") ?? "").Split(';'))
                    {
                        string path = test.Trim();
                        if (!String.IsNullOrEmpty(path) && File.Exists(path = Path.Combine(path, exe)))
                            return Path.GetFullPath(path);
                    }
                }
                throw new FileNotFoundException(new FileNotFoundException().Message, exe);
            }

            return Path.GetFullPath(exe);
        }

        /// <summary>
        /// Creates a new instance. Tries to find htmldoc if path is not provided.
        /// Looks for it in same PATH and same directory.
        /// </summary>
        /// <param name="htmldocPath">Full path to htmldoc executable.</param>
        /// <exception cref="DependencyNotFoundException">When htmldoc was not found or is in wrong version.</exception>
        /// <exception cref="InvalidLicenseException">If license is provided but is invalid.</exception>
        public POSPrinterPdfGenerator(string? htmldocPath = null)
        {
            string? htmldocExecutablePath = null;
            //try to locate htmldocPath
            if (htmldocPath != null)
            {
                if (!File.Exists(htmldocPath))
                {
                    throw new DependencyNotFoundException("Provided htmldoc path is invalid.");
                } else
                {
                    htmldocExecutablePath = htmldocPath;
                }
            }

            //revert to PATH
            if (htmldocExecutablePath == null)
            {
                string htmldocExecutable = "htmldoc";
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    htmldocExecutable = "htmldoc.exe";
                }

                try
                {
                    htmldocExecutablePath = FindExePath(htmldocExecutable);
                } catch
                {
                    throw new DependencyNotFoundException("htmldoc path not provided and not present in PATH.");
                }
            }

            //found, let us try to execute it to check version
            var process = new Process();
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.Arguments = "--version";
            process.StartInfo.FileName = htmldocExecutablePath;
            process.StartInfo.RedirectStandardOutput = true;
            process.Start();
            process.WaitForExit();

            var version = Version.Parse(process.StandardOutput.ReadToEnd());
            
            if (version.Major != 1 || version.Minor < 9)
            {
                throw new DependencyNotFoundException("Invalid htmldoc version, ^1.9 is supported.");
            }

            if (htmldocExecutablePath == null)
            {
                throw new DependencyNotFoundException("Unexpected situation. Htmldoc path is still null.");
            }

            HtmldocPath = htmldocExecutablePath;
            Console.WriteLine(Directory.GetCurrentDirectory());
            if (File.Exists("idct.posprinterpdfgenerator.license.key") && File.Exists("idct.posprinterpdfgenerator.license.sign")) {
                string license = "";

                using (StreamReader reader = new("idct.posprinterpdfgenerator.license.key"))
                {
                    license = reader.ReadToEnd().Trim();
                }

                string[] parts = license.Split(';');
                string email = parts[0];
                string key = parts[1];

                string hashingKey = "77d65cea919cdd2e7d00510251a41bbffc3c690a";

                var hash = Convert.ToHexString(SHA512.HashData(Encoding.UTF8.GetBytes(email + ";;" + hashingKey + ";;" + email))).ToUpper();

                if (hash != key)
                {
                    throw new InvalidLicenseException("Provided license key file is invalid. Please contact support@idct.tech or obtain a valid license file on https://idct.tech.");
                }

                RsaSignatureVerifier verify = new();
                if (verify.Verify("idct.posprinterpdfgenerator.license.key", "idct.posprinterpdfgenerator.license.sign") == false)
                {
                    throw new InvalidLicenseException("Provided license key signature is invalid. Please contact support@idct.tech or obtain a valid license file on https://idct.tech.");
                }

                Unlicensed = false;
            } else
            {
                Unlicensed = true;
            }
        }

        /// <summary>
        /// Converts HTML to Receipt PDF.
        /// </summary>
        /// <param name="html">HTML markup. Supported HTML tags supported by Htmldoc: https://www.msweet.org/htmldoc/htmldoc.html</param>
        /// <param name="outputFilePath">Destination, target PDF file.</param>
        /// <param name="htmldocOptions">Options for the Htmldoc parser.</param>
        public Box HtmlToReceipt(string html, string outputFilePath, HtmldocOptions? htmldocOptions = null)
        {
            htmldocOptions ??= new HtmldocOptions();

            //first lets we need to generate a file with pages
            var tempOutputFile = Path.GetTempFileName() + ".pdf";
            HtmlToPdf(html, tempOutputFile, htmldocOptions);
            var tempCombinedFile = Path.GetTempFileName() + ".pdf";
            CombineLongPdf(tempOutputFile, tempCombinedFile);
            var size = TrimPdf(tempCombinedFile, outputFilePath, htmldocOptions.BottomMargin);
            File.Delete(tempOutputFile);
            File.Delete(tempCombinedFile);

            return size;
        }

        /// <summary>
        /// Converts HTML to a PDF file with parameters defined in htmldocOptions. By default a very long and narrow PDF.
        /// </summary>
        /// <param name="html">HTML markup. Supported HTML tags supported by Htmldoc: https://www.msweet.org/htmldoc/htmldoc.html</param>
        /// <param name="outputFilePath">Destination, target PDF file.</param>
        /// <param name="htmldocOptions">Options for the Htmldoc parser.</param>
        /// <exception cref="InvalidPdfParametersException">When file would be too narrow or too short.</exception>
        public void HtmlToPdf(string html, string outputFilePath, HtmldocOptions? htmldocOptions = null)
        {
            //creates a new temporary file or throws an exception if cannot
            string tmpPath = Path.GetTempFileName() + ".pdf";

            RegexOptions options = RegexOptions.IgnoreCase | RegexOptions.Singleline;
            Regex regx = new("<body.*?>", options);
            Match match = regx.Match(html);

            if (!match.Success)
            {
                throw new InvalidPdfParametersException("<body> tag not present in the input html!");
            }

            if (Unlicensed)
            {
                html = html.Replace(match.Value, match.Value + "<center><b>UNLICENSED</b><br>GO TO: <b>https://idct.tech</b><br><b>To get a license!</b><br></center><hr>");
            }

            //tries to write the html contents to file
            using (StreamWriter writer = new(tmpPath))
            {
                writer.WriteLine(html);
            }

            htmldocOptions ??= new HtmldocOptions();

            if (htmldocOptions.Width < 10 || htmldocOptions.Height < 200)
            {
                throw new InvalidPdfParametersException("Width cannot be lower than 10mm and height should be long enough to cover contents of at least one page");
            }

            string strCmdText = String.Format("--continuous --header . --footer . --top 0mm --bottom 0mm --left 0mm --right 0mm --size {0}x{1}mm --pscommands --fontsize {2} --embedfonts --textfont {3}{4} --charset utf-8 -f {5} {6}", htmldocOptions.Width, htmldocOptions.Height, htmldocOptions.FontSize, htmldocOptions.PdfSupportedFont.ToEnumMember(), (htmldocOptions.Gray ? " --gray" : ""),  outputFilePath, tmpPath);
            var process = new Process();
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.Arguments = strCmdText;
            process.StartInfo.FileName = HtmldocPath;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.RedirectStandardError = true;
            process.Start();
            process.WaitForExit();
            process.Dispose();
            File.Delete(tmpPath);
        }

        /// <summary>
        /// Combines pages of a PDF file into a single long page. Takes width from first page.
        /// </summary>
        /// <param name="inputFilePath">Path to the input file.</param>
        /// <param name="outputFilePath">Path to the output file.</param>
        public static void CombineLongPdf(string inputFilePath, string outputFilePath)
        {
            PdfDocument pdfDocument = PdfReader.Open(inputFilePath, PdfDocumentOpenMode.Import);
            int pages = pdfDocument.Pages.Count;
            var width = pdfDocument.Pages[0].Width.Point;
            var height = pdfDocument.Pages[0].Height.Point;

            PdfDocument outputDocument = new();            
            XPdfForm form = XPdfForm.FromFile(inputFilePath);
            XGraphics gfx;
            XRect box;

            // Add a new page to the output document
            PdfPage page = outputDocument.AddPage();
            page.Orientation = PageOrientation.Portrait;
            page.Width = width;
            page.Height = height * pages;

            gfx = XGraphics.FromPdfPage(page);

            for (int i = 1; i <= pages; i++)
            {
                form.PageNumber = i;
                box = new XRect(0, height * (i -1), width, height);
                // Draw the page identified by the page number like an image
                gfx.DrawImage(form, box);
            }

            form.Dispose();
            // Set page number (which is one-based)            
            outputDocument.Save(outputFilePath);
            outputDocument.Close();
        }

        /// <summary>
        /// Trims a PDF file, removes remaining white space at the end, supports keeping a margin in pixels.
        /// </summary>
        /// <param name="inputFilePath">Path to the input file.</param>
        /// <param name="outputFilePath">Path to the output file.</param>
        /// <param name="bottomMargin">Bottom margin in pixels, defaults to 0.</param>
        public static Box TrimPdf(string inputFilePath, string outputFilePath, int bottomMargin = 0)
        {
            PdfDocument pdfDocument = PdfReader.Open(inputFilePath, PdfDocumentOpenMode.Import);
            DocLib DocNet = DocLib.Instance;

            //you are specifying the max resolution of image on any side, actual resolution will be limited by longer side, 
            //preserving the aspect ratio
            var docReader = DocNet.GetDocReader(
            inputFilePath,
            new PageDimensions(Convert.ToInt32(pdfDocument.Pages[0].Width.Point), Convert.ToInt32(pdfDocument.Pages[0].Height.Point)));

            //mamy tutaj juz wysokosc
            int finalHeight = 0;
            int baseWidth = 0;
            int height = Convert.ToInt32(pdfDocument.Pages[0].Height.Point);
            int width = Convert.ToInt32(pdfDocument.Pages[0].Width.Point);

            using (var pageReader = docReader.GetPageReader(0))
            {
                var rawBytes = pageReader.GetImage();
                baseWidth = width;
                var widthInPixels = width * 4; //BGRA
                height = pageReader.GetPageHeight();                
                var position = widthInPixels * height - widthInPixels; //last chunk
                var chunks = rawBytes.Length / widthInPixels;                
                var linesRemoved = 0;
                for (int currentChunkStart = position; currentChunkStart > 0; currentChunkStart -= widthInPixels)
                {                    
                    var chunk = rawBytes.Slice(currentChunkStart, widthInPixels);
                    var onlyZeroes = true;
                    foreach (byte b in chunk)
                    {
                        if (b != 0)
                        {
                            onlyZeroes = false;
                            break;
                        }
                    }

                    if (onlyZeroes)
                    {
                        linesRemoved++;
                    } else
                    {
                        break;
                    }
                }

                finalHeight = height - linesRemoved;
            }

            docReader.Dispose();
                
            //teraz potrzebujemy utworzyc nowy plik
            PdfDocument outputDocument = new();
            //zaladowac stary jako form
            XPdfForm form = XPdfForm.FromFile(inputFilePath);
            XGraphics gfx;
            XRect box;

            // Add a new page to the output document
            PdfPage page = outputDocument.AddPage();
            page.Orientation = PageOrientation.Portrait;
            page.Width = baseWidth;
            page.Height = finalHeight + bottomMargin;

            gfx = XGraphics.FromPdfPage(page);

            // Set page number (which is one-based)
            form.PageNumber = 1;

            box = new XRect(0, 0, baseWidth, height);
            // Draw the page identified by the page number like an image
            gfx.DrawImage(form, box);

            outputDocument.Save(outputFilePath);
            outputDocument.Close();

            return new Box(page.Width, page.Height);
        }
    }
}
