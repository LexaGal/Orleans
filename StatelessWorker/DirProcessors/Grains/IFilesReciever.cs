using System.Threading.Tasks;

namespace GrainsLib.Grains
{
    public interface IFilesReciever : Orleans.IGrainWithGuidKey
    {
        Task SetupFilesReciever(string outDir, string settingsDir);
        Task StartProcessFiles();
        Task Stop();
        Task<bool> GetFileId(string fileId);
        Task<bool> GetFile(byte[] file);
    }
}
