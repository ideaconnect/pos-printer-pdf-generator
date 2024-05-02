namespace POSPrinterPdfGenerator
{
    /// <summary>
    /// Options passed to the Htmldoc Parser.
    /// </summary>
    public class HtmldocOptions
    {
        /// <summary>
        /// Width in milimeters. Defaults to 48mm which is a popular width of a paper roll.
        /// </summary>
        public int Width { get; set; } = 48;

        /// <summary>
        /// Height of a page in mm. Best if long enough to cover whole potential receipt, but if output file is split into pages library will combine it.
        /// </summary>
        public int Height { get; set; } = 1000;

        /// <summary>
        /// Bottom margin in pixels.
        /// </summary>
        public int BottomMargin { get; set; } = 0;

        /// <summary>
        /// Default font size in points.
        /// </summary>
        public int FontSize { get; set; } = 10;

        /// <summary>
        /// Default font.
        /// </summary>
        public PdfSupportedFont PdfSupportedFont { get; set; } = PdfSupportedFont.Arial;

        /// <summary>
        /// Should the output file be generated in grayscale?
        /// </summary>
        public bool Gray { get; set; } = true;

    }
}
