using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using MigraDoc.DocumentObjectModel;
using MigraDoc.Rendering;
using PdfSharp.Pdf;
using SixLabors.ImageSharp;
using ZXing;

//using FileProcessingService;
//using Microsoft.ServiceBus.Messaging;
//using Microsoft.WindowsAzure.Storage;
//using Microsoft.WindowsAzure.Storage.Blob;

namespace GrainsLib.StateObjects
{
    public class DirProcessor
    {
        Guid _id = Guid.NewGuid();

        string _inDir;
        ManualResetEvent _stopWorkEvent;
        
        AutoResetEvent _newFileEvent;
        List<string> _files;
        Document _document;

        //CloudBlobContainer _cloudBlobContainer;

        DirServiceState _state;

        int _tryOpenFileAttempts = 3;
        int _tryOpenFileDelay = 3000;
        int _waitDelay = 1000;

        string _pattern = "IMG_[0-9]+.(PNG|JPEG|BMP)";
        string _barcodeText = "SEPARATOR";
        FileSystemWatcher _watcher;
        Thread _workThread;
        Thread _sendInfoThread;
        Thread _recieveInfoThread;

        public readonly List<string> Messages = new List<string>();
        public readonly List<byte[]> Files = new List<byte[]>();

        public void SetupDirProcessor(string inDir)
        {
            _newFileEvent = new AutoResetEvent(false);
            _files = new List<string>();
            _document = new Document();
            _document.AddSection();

            _inDir = inDir;
            _stopWorkEvent = new ManualResetEvent(false);

            _watcher = new FileSystemWatcher(inDir);
            _watcher.Created += Watcher_Created;
            _workThread = new Thread(ProcessFiles);
            _sendInfoThread = new Thread(SendInfo);
            _recieveInfoThread = new Thread(RecieveInfo);

            //CloudStorageAccount storageAccount;
            //var storageConnectionString = ConfigurationManager.AppSettings["Storage"];
            //if (!CloudStorageAccount.TryParse(storageConnectionString, out storageAccount)) return;
            //var cloudBlobClient = storageAccount.CreateCloudBlobClient();
            //_cloudBlobContainer =
            //    cloudBlobClient.GetContainerReference(ConfigurationManager.AppSettings["Container"]);
        }

        public void StartProcessFiles()
        {
            _workThread.Start();
            _sendInfoThread.Start();
            _recieveInfoThread.Start();
            _watcher.EnableRaisingEvents = true;
        }

        public void Stop()
        {
            _watcher.EnableRaisingEvents = false;
            _workThread.Join();
            _sendInfoThread.Join();
            _recieveInfoThread.Join();

            _stopWorkEvent.Set();
        }

        private void ProcessFiles(object obj)
        {
            do
            {
                foreach (var file in Directory.EnumerateFiles(_inDir).ToList())
                {
                    _state = DirServiceState.Processing;

                    if (_stopWorkEvent.WaitOne(TimeSpan.Zero)) return;
                    var fileName = Path.GetFileName(file);
                    if (fileName == null) continue;
                    if (!Regex.IsMatch(fileName.ToUpper(), _pattern)) continue;

                    if (TryOpenFile(file, _tryOpenFileAttempts))
                    {
                        try
                        {
                            if (FileIsBarcode(file))
                            {
                                SendFiles(_files).GetAwaiter().GetResult();
                                _files.Clear();
                                if (TryOpenFile(file, _tryOpenFileAttempts)) File.Delete(file);
                            }
                            else
                            {
                                if (!_files.Contains(file))
                                {
                                    _files.Add(file);
                                    AddFileToPdfDocument(file);
                                }
                            }
                        }
                        catch (OutOfMemoryException)
                        {
                            if (!_files.Contains(file)) _files.Add(file);
                        }
                    }
                }
                _state = DirServiceState.Waiting;
            } while (WaitHandle.WaitAny(new WaitHandle[] { _stopWorkEvent, _newFileEvent }, _waitDelay) != 0);
        }

        private void SendInfo(object obj)
        {
            do
            {
                SendState().GetAwaiter().GetResult();
                SendBarcode().GetAwaiter().GetResult();
            } while (WaitHandle.WaitAny(new WaitHandle[] { _stopWorkEvent }, _waitDelay) != 0);
        }

        private void RecieveInfo(object obj)
        {
            do
            {
                RecieveState().GetAwaiter().GetResult();
                RecieveBarcode().GetAwaiter().GetResult();
            } while (WaitHandle.WaitAny(new WaitHandle[] { _stopWorkEvent }, _waitDelay) != 0);
        }

        private async Task SendState()
        {
            //var queueClient = QueueClient.Create(ConfigurationManager.AppSettings["StatesQueue"]);
            //var state = $"{_id}: {_state}";
            //var message = new BrokeredMessage(state);
            //await queueClient.SendAsync(message);
            //await queueClient.CloseAsync();
            //Console.WriteLine($"Send {state}");
        }

        private async Task SendBarcode()
        {
            //var queueClient = QueueClient.Create(ConfigurationManager.AppSettings["BarcodesQueue"]);
            //var message = new BrokeredMessage(_barcodeText);
            //await queueClient.SendAsync(message);
            //await queueClient.CloseAsync();
            //Console.WriteLine($"Send {_id}: {_barcodeText}");
        }

        private async Task RecieveState()
        {
            //var queueClient =
            //    QueueClient.Create(ConfigurationManager.AppSettings["SendStatesQueue"], ReceiveMode.ReceiveAndDelete);
            //var message = await queueClient.ReceiveAsync(new TimeSpan(1000));
            //if (message == null) return;
            //await SendState();
            //await queueClient.CloseAsync();
            //Console.WriteLine($"Recieve {_id}: {message.GetBody<string>()}");
        }

        private async Task RecieveBarcode()
        {
            //var queueClient =
            //    QueueClient.Create(ConfigurationManager.AppSettings["UpdateBarcodesQueue"], ReceiveMode.ReceiveAndDelete);
            //var message = await queueClient.ReceiveAsync(new TimeSpan(1000));
            //if (message == null) return;
            //var code = message.GetBody<string>();
            //_barcodeText = code;
            //await queueClient.CloseAsync();
            //Console.WriteLine($"Recieve {_id}: {code}");
        }

        private void Watcher_Created(object sender, FileSystemEventArgs e)
        {
            _newFileEvent.Set();
        }

        private async Task SendFiles(IEnumerable<string> files)
        {
            //var id = Guid.NewGuid().ToString();
            //var cloudBlockBlob = _cloudBlobContainer.GetBlockBlobReference(id);
            //var ms = new MemoryStream();
            //var pdf = RenderPdfDocument();
            //pdf.Save(ms, false);
            //await cloudBlockBlob.UploadFromStreamAsync(ms);

            //var queueClient = QueueClient.Create(ConfigurationManager.AppSettings["FilesQueue"]);
            //var message = new BrokeredMessage(id);
            //await queueClient.SendAsync(message);
            //await queueClient.CloseAsync();

            //foreach (var file in files)
            //{
            //    if (!TryOpenFile(file, _tryOpenFileAttempts)) continue;
            //    File.Delete(file);
            //}
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
                    Thread.Sleep(_tryOpenFileDelay);
                }
            }
            return false;
        }

        private void AddFileToPdfDocument(string file)
        {
            ((Section)_document.Sections.First).AddPageBreak();
            ((Section)_document.Sections.First).AddImage(file);
        }

        private PdfDocument RenderPdfDocument()
        {
            var render = new PdfDocumentRenderer { Document = _document };
            render.RenderDocument();
            _document = new Document();
            _document.AddSection();
            return render.PdfDocument;
        }

        private bool FileIsBarcode(string file)
        {
            var reader = new BarcodeReader { AutoRotate = true };
            using (var stream = File.OpenRead(file))
            {
                var image = Image.Load(stream).SavePixelData();
                var result = reader.Decode(image);
                return result?.Text.ToUpper() == _barcodeText;
            }            
        }
    }
}