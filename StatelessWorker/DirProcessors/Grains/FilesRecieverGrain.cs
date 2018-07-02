using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using GrainsLib.StateObjects;
using MigraDoc.DocumentObjectModel;
using MigraDoc.DocumentObjectModel.Shapes;
using MigraDoc.Rendering;
using Orleans;
using Orleans.Providers;
using Orleans.Streams;
using PdfSharp.Pdf;

namespace GrainsLib.Grains
{
    [ImplicitStreamSubscription(Consts.FilesStreamNameSpace)]
    [StorageProvider(ProviderName = "DevStore")]
    public class FilesRecieverGrain : Grain<FilesRecieverState>, IFilesReciever
    {
        private IAsyncStream<string> _streamIds;
        private IAsyncStream<byte[]> _streamFiles;
        Document _document;

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
            AddFileToPdfDocument(file);
            var doc =RenderPdfDocument();
            State.Files.Add(doc);
            await WriteStateAsync();
            return true;
        }

        public Task SetupFilesReciever(string outDir, string settingsDir)
        {
            State.OutDir = outDir;

            _document = new Document();
            _document.AddSection();

            return Task.CompletedTask;
        }

        private void AddFileToPdfDocument(byte[] file)
        {
            ((Section)_document.Sections.First).AddPageBreak();
            var f = "base64:" + Convert.ToBase64String(file);
            ((Section)_document.Sections.First).Add(new Image(f));
        }

        private PdfDocument RenderPdfDocument()
        {
            var render = new PdfDocumentRenderer { Document = _document };
            render.RenderDocument();
            _document = new Document();
            _document.AddSection();
            return render.PdfDocument;
        }
    }

}
    
