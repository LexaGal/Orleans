using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using GrainsLib.StateObjects;
using Orleans;
using Orleans.Providers;
using Orleans.Streams;

namespace GrainsLib.Grains
{
    [StorageProvider(ProviderName = Consts.DevStore)]
    public class DirProcessorGrain : Grain<DirProcessor>, IDirProcessor
    {
        private IAsyncStream<string> _streamIds;
        private IAsyncStream<byte[]> _streamFiles;
        
        public override Task OnActivateAsync()
        {
            var streamProvider = GetStreamProvider(Consts.FilesStreamProvider);
            _streamIds = streamProvider.GetStream<string>(Consts.StreamIdsGuid, Consts.FilesStreamNameSpace);
            _streamFiles = streamProvider.GetStream<byte[]>(Consts.StreamFilesGuid, Consts.FilesStreamNameSpace);
            return base.OnActivateAsync();
        }

        public async Task<bool> SaveFileId(string fileId)
        {
            State.Messages.Add(fileId);
            await WriteStateAsync();
            await _streamIds.OnNextAsync(fileId);
            return true;
        }

        public async Task<bool> SaveFile(byte[] file)
        {
            State.Files.Add(file);
            await WriteStateAsync();
            await _streamFiles.OnNextAsync(file);
            return true;
        }

        public Task SetupDirProcessor(string inDir)
        {
            State.SetupDirProcessor(inDir);
            return Task.CompletedTask;
        }

        public Task StartProcessDir()
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
    
