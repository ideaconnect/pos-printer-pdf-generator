using System.Runtime.Serialization;

namespace IDCT.Html2Pdf
{    
    /// <summary>
    /// Enum of supported fonts in the PDF.
    /// </summary>
    public enum PdfSupportedFont : sbyte
    {
        [EnumMember(Value = "Arial")]
        Arial = 0,

        [EnumMember(Value = "Courier")]
        Courier = 1,

        [EnumMember(Value = "Helvetica")]
        Helvetica = 2,

        [EnumMember(Value = "Monospace")]
        Monospace = 3,

        [EnumMember(Value = "Sans")]
        Sans = 4,

        [EnumMember(Value = "Serif")]
        Serif = 5,

        [EnumMember(Value = "Times")]
        Times = 6
    }
}
