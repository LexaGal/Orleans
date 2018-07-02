using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using GrainsLib.StateObjects;
using Orleans;
using Orleans.Providers;
using Orleans.Streams;
using SixLabors.ImageSharp;
using ZXing;

namespace GrainsLib.Grains
{
    [StorageProvider(ProviderName = Consts.DevStore)]
    public class DirProcessorGrain : Grain<DirProcessState>, IDirProcessor
    {
        private IAsyncStream<string> _streamIds;
        private IAsyncStream<byte[]> _streamFiles;

        ManualResetEvent _stopWorkEvent;
        AutoResetEvent _newFileEvent;
        List<string> _files;
        FileSystemWatcher _watcher;
        Thread _workThread;
        
        public override Task OnActivateAsync()
        {
            var streamProvider = GetStreamProvider(Consts.FilesStreamProvider);
            _streamIds = streamProvider.GetStream<string>(Consts.StreamIdsGuid, Consts.FilesStreamNameSpace);
            _streamFiles = streamProvider.GetStream<byte[]>(Consts.StreamFilesGuid, Consts.FilesStreamNameSpace);
            return base.OnActivateAsync();
        }

        public Task SetupDirProcessor(string inDir)
        {
            State.InDir = inDir;
            _newFileEvent = new AutoResetEvent(false);
            _stopWorkEvent = new ManualResetEvent(false);
            _files = new List<string>();
            _watcher = new FileSystemWatcher(inDir);
            _watcher.Created += Watcher_Created;
            _workThread = new Thread(ProcessFiles);
            return Task.CompletedTask;
        }

        public Task StartProcessDir()
        {
            _workThread.Start();
            _watcher.EnableRaisingEvents = true;
            return Task.CompletedTask;
        }
        
        public Task Stop()
        {
            _watcher.EnableRaisingEvents = false;
            _workThread.Join();
            _stopWorkEvent.Set();              
            return Task.CompletedTask;
        }

        private void ProcessFiles(object obj)
        {
            do
            {
                foreach (var file in Directory.EnumerateFiles(State.InDir).ToList())
                {
                    State.DirState = DirServiceState.Processing;

                    if (_stopWorkEvent.WaitOne(TimeSpan.Zero)) return;
                    var fileName = Path.GetFileName(file);
                    if (fileName == null) continue;
                    if (!Regex.IsMatch(fileName.ToUpper(), State.Pattern)) continue;

                    if (TryOpenFile(file, State.TryOpenFileAttempts))
                    {
                        try
                        {
                            if (FileIsBarcode(file))
                            {
                                SendFiles(_files).GetAwaiter().GetResult();
                                _files.Clear();
                                if (TryOpenFile(file, State.TryOpenFileAttempts)) File.Delete(file);
                            }
                            else
                            {
                                if (!_files.Contains(file))
                                {
                                    _files.Add(file);
                                }
                            }
                        }
                        catch (OutOfMemoryException)
                        {
                            if (!_files.Contains(file)) _files.Add(file);
                        }
                    }
                }
                State.DirState = DirServiceState.Waiting;
            } while (WaitHandle.WaitAny(new WaitHandle[] { _stopWorkEvent, _newFileEvent }, State.WaitDelay) != 0);
        }
        
        private async Task SendFiles(IEnumerable<string> files)
        {
            var id = Guid.NewGuid().ToString();
            await SaveFileId(id);

            foreach (var file in files)
            {
                using (var stream = File.OpenRead(file))
                {
                    var image = Image.Load(stream).SavePixelData();
                    await SaveFile(image);
                }
                if (!TryOpenFile(file, State.TryOpenFileAttempts)) continue;
                File.Delete(file);
            }
        }

        private async Task<bool> SaveFileId(string fileId)
        {
            State.Messages.Add(fileId);
            await WriteStateAsync();
            await _streamIds.OnNextAsync(fileId);
            return true;
        }

        private async Task<bool> SaveFile(byte[] file)
        {
            State.Files.Add(file);
            await WriteStateAsync();
            await _streamFiles.OnNextAsync(file);
            return true;
        }

        private void Watcher_Created(object sender, FileSystemEventArgs e)
        {
            _newFileEvent.Set();
        }

        private bool TryOpenFile(string fileName, int attempts)
        {
            for (var i = 0; i < attempts; i++)
            {
                try
                {
                    var file = File.Open(fileName, FileMode.Open, FileAccess.Read, FileShare.None);
                    file.Close();
                    return true;
                }
                catch (IOException)
                {
                    Thread.Sleep(State.TryOpenFileDelay);
                }
            }
            return false;
        }
        
        private bool FileIsBarcode(string file)
        {
            var reader = new BarcodeReader { AutoRotate = true };
            using (var stream = File.OpenRead(file))
            {
                var image = Image.Load(stream).SavePixelData();
                var result = reader.Decode(image);
                return result?.Text.ToUpper() == State.BarcodeText;
            }
        }
    }
}
    
