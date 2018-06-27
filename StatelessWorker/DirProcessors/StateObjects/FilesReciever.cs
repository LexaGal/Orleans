using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

//using Microsoft.ServiceBus.Messaging;
//using Microsoft.WindowsAzure.Storage;
//using Microsoft.WindowsAzure.Storage.Blob;

namespace GrainsLib.StateObjects
{
    public class FilesReciever
    {
        ManualResetEvent _stopWorkEvent;
        string _outDir;
        string _settingsFile;
        //CloudBlobContainer _cloudBlobContainer;

        int _waitDelay = 1000;

        FileSystemWatcher _watcher;
        Thread _workThread;
        Thread _recieveInfoThread;

        public readonly List<string> Messages = new List<string>();
        public readonly List<byte[]> Files = new List<byte[]>();

        public void SetupFilesReciever(string outDir, string settingsDir)
        {
            _stopWorkEvent = new ManualResetEvent(false);

            _outDir = outDir;
            _settingsFile = Path.Combine(settingsDir, "settings.txt");

            _watcher = new FileSystemWatcher(settingsDir);
            _watcher.Changed += Watcher_Changed;
            _workThread = new Thread(ProcessFiles);
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
            _recieveInfoThread.Start();
            _watcher.EnableRaisingEvents = true;
        }

        public void Stop()
        {
            _watcher.EnableRaisingEvents = false;
            _workThread.Join();
            _recieveInfoThread.Join();
        }

        private void ProcessFiles(object obj)
        {
            do
            {
                if (_stopWorkEvent.WaitOne(TimeSpan.Zero)) return;
                RecieveFiles().GetAwaiter().GetResult();
            } while (WaitHandle.WaitAny(new WaitHandle[] { _stopWorkEvent }, _waitDelay) != 0);
        }

        private async Task RecieveFiles()
        {
            //var queueClient =
            //    QueueClient.Create(ConfigurationManager.AppSettings["FilesQueue"], ReceiveMode.ReceiveAndDelete);
            //var message = await queueClient.ReceiveAsync(new TimeSpan(1000));
            //if (message == null) return;
            //var id = message.GetBody<string>();
            //await queueClient.CloseAsync();

            //var cloudBlockBlob = _cloudBlobContainer.GetBlockBlobReference(id);
            //await cloudBlockBlob.DownloadToFileAsync($@"{_outDir}\{id}.pdf", FileMode.Create);
            //await cloudBlockBlob.DeleteAsync();
        }

        private void SendInfo()
        {
            SendState().GetAwaiter().GetResult();
            SendBarcode().GetAwaiter().GetResult();
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
            //var queueClient = QueueClient.Create(ConfigurationManager.AppSettings["SendStatesQueue"]);

            ////for 2 services
            //var message = new BrokeredMessage("Ask for state");
            //await queueClient.SendAsync(message);
            //message = new BrokeredMessage("Ask for state");
            //await queueClient.SendAsync(message);

            //await queueClient.CloseAsync();

            //Console.WriteLine("Send: Ask for state");
        }

        private async Task SendBarcode()
        {
            //var queueClient = QueueClient.Create(ConfigurationManager.AppSettings["UpdateBarcodesQueue"]);
            //if (TryOpenFile(_settingsFile, 3))
            //{
            //    var newBarcodeText = File.ReadAllLines(_settingsFile)[0].Split(' ')[1];

            //    //for 2 services
            //    var message = new BrokeredMessage(newBarcodeText);
            //    await queueClient.SendAsync(message);
            //    message = new BrokeredMessage(newBarcodeText);
            //    await queueClient.SendAsync(message);

            //    await queueClient.CloseAsync();

            //    Console.WriteLine($"Send: {newBarcodeText}");
            //}
        }

        private async Task RecieveState()
        {
            //var queueClient =
            //    QueueClient.Create(ConfigurationManager.AppSettings["StatesQueue"], ReceiveMode.ReceiveAndDelete);
            //var message = await queueClient.ReceiveAsync(new TimeSpan(1000));
            //if (message == null) return;
            //var state = message.GetBody<string>();
            //await queueClient.CloseAsync();
            //Console.WriteLine($"Recieve: {state}");
        }

        private async Task RecieveBarcode()
        {
            //var queueClient =
            //    QueueClient.Create(ConfigurationManager.AppSettings["BarcodesQueue"], ReceiveMode.ReceiveAndDelete);
            //var message = await queueClient.ReceiveAsync(new TimeSpan(1000));
            //if (message == null) return;
            //var code = message.GetBody<string>();
            //var barcodeText = code;
            //await queueClient.CloseAsync();
            //Console.WriteLine($"Recieve: {barcodeText}");
        }

        private void Watcher_Changed(object sender, FileSystemEventArgs e)
        {
            SendInfo();
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
                    Thread.Sleep(3000);
                }
            }
            return false;
        }
    }

    public enum DirServiceState
    {
        Waiting,
        Processing
    }
}