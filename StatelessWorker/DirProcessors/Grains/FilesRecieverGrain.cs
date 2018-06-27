using System.Threading.Tasks;
using GrainsLib.StateObjects;
using Orleans;
using Orleans.Providers;
using Orleans.Streams;

namespace GrainsLib.Grains
{
    [ImplicitStreamSubscription(Consts.FilesStreamNameSpace)]
    [StorageProvider(ProviderName = "DevStore")]
    public class FilesRecieverGrain : Grain<FilesReciever>, IFilesReciever
    {
        private IAsyncStream<string> _streamIds;
        private IAsyncStream<byte[]> _streamFiles;
        
        public override Task OnActivateAsync()
        {
            var streamProvider = GetStreamProvider(Consts.FilesStreamProvider);
            _streamIds = streamProvider.GetStream<string>(Consts.StreamIdsGuid, Consts.FilesStreamNameSpace);
            _streamFiles = streamProvider.GetStream<byte[]>(Consts.StreamFilesGuid, Consts.FilesStreamNameSpace);
            _streamIds.SubscribeAsync(async (id, token) => { await GetFileId(id); });
            _streamFiles.SubscribeAsync(async (file, token) => { await GetFile(file); });
            return base.OnActivateAsync();
        }

        public async Task<bool> GetFileId(string fileId)
        {
            State.Messages.Add(fileId);
            await WriteStateAsync();            
            return true;
        }

        public async Task<bool> GetFile(byte[] file)
        {
            State.Files.Add(file);
            await WriteStateAsync();
            return true;
        }

        public Task SetupFilesReciever(string outDir, string settingsDir)
        {
            State.SetupFilesReciever(outDir, settingsDir);
            return Task.CompletedTask;
        }

        public Task StartProcessFiles()
        {
            State.StartProcessFiles();
            return Task.CompletedTask;
        }

        public Task Stop()
        {
            State.Stop();
            return Task.CompletedTask;
        }
    }
}
    
