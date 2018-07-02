using System.Threading.Tasks;

namespace GrainsLib.Grains
{
    public interface IFilesReciever : Orleans.IGrainWithGuidKey
    {
        Task SetupFilesReciever(string outDir, string settingsDir);
        Task<bool> GetFileId(string fileId);
        Task<bool> GetFile(byte[] file);
    }
}
