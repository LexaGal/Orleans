using System.Collections.Generic;
using GrainsLib.Grains;

namespace GrainsLib.StateObjects
{
    public class DirProcessState
    {
        public int TryOpenFileAttempts { get; } = 3;
        public int TryOpenFileDelay { get; } = 3000;
        public int WaitDelay { get; } = 1000;
        public string Pattern { get; } = "IMG_[0-9]+.(PNG|JPEG|BMP)";
        public string BarcodeText { get; set; } = "SEPARATOR";
        public DirServiceState DirState { get; set; }
        public string InDir { get; set; }
        public readonly List<string> Messages = new List<string>();
        public readonly List<byte[]> Files = new List<byte[]>();
    }
}        