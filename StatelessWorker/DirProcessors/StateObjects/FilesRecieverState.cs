using System.Collections.Generic;
using PdfSharp.Pdf;

namespace GrainsLib.StateObjects
{
    public class FilesRecieverState
    {
        public string OutDir { get; set; }
        public readonly List<string> Messages = new List<string>();
        public readonly List<PdfDocument> Files = new List<PdfDocument>();
    }
}