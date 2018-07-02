using System.Threading.Tasks;

namespace GrainsLib.Grains
{
    public interface IDirProcessor : Orleans.IGrainWithGuidKey
    {
        Task SetupDirProcessor(string inDir);
        Task StartProcessDir();
        Task Stop();      
    }
}
