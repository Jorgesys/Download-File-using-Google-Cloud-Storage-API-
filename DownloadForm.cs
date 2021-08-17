using Google.Apis.Auth.OAuth2;
using Google.Apis.Download;
using Google.Apis.Services;
using Google.Apis.Storage.v1;
using Google.Cloud.Storage.V1;
using System;
using System.IO;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Google_Financial_Report
{
    public partial class DownloadForm : Form
    {

        
        //Get P12 or Json Key : https://console.cloud.google.com/iam-admin/serviceaccounts/
        public string LocalDownloadPath = @"C:\Data\Development Android\GoogleFinancialReport\";
        const string GCSBucketName = "JorgesysFBProject-12c.appspot.com";
        const string P12KeyFullPath = @"C:\Data\Development Android\GoogleFinancialReport\JorgesysFBProject-12c-d7b52c17507f.p12";
        const string P12KeySecret = "jorgesyssecret";
        const string GCSServiceAccountEmail = "google-financial-reports@JorgesysFBProject-12c.iam.gserviceaccount.com";
        const string GCSApplicationName = "JorgesysGCProject";
		public long CurrentFileSizeInByte = 0;

        public DownloadForm()
        {
            InitializeComponent();
        }

        private void btnProcess_Click(object sender, EventArgs e)
        {            
            callAPI();               
        }

        private async void callAPI()
        {
            Console.WriteLine("Jorgesys Google Cloud Storage API Sample");
            Console.WriteLine("====================");
            try
            {                 
                  await Run();
            }
            catch (AggregateException ex)
            {
                foreach (var e in ex.InnerExceptions)
                {
                    Console.WriteLine("ERROR: " + e.Message);
                }
            }
            Console.WriteLine("Press any key to continue...");
        }



        private async Task Run()
        {            
            Console.WriteLine($"*** \" async Task Run()()\" ....");
            //Get Credential.
            var credential = GoogleCredential.FromFile(@"C:\Data\Development Android\GoogleFinancialReport\JorgesysFBProject-b729ddb6e510.json");
            //var credential = GoogleCredential.GetApplicationDefault();
            var storage = StorageClient.Create(credential);
            //Make an authenticated API request getting buckets.
            var buckets = storage.ListBuckets("JorgesysFBProject-12c"/*ProjectId*/);
            foreach (var bucket in buckets)
            {
                Console.WriteLine("bucket: " + bucket.Name);
            }            
			      //File in bucket: JorgesysFBProject-12c.appspot.com  Project: JorgesysFBProject 
            var object_name = "GoogleEarnigsReport.json"; 

            try
            {
                Console.WriteLine("Downloading file: " + object_name);
                DownloadFile(object_name).Wait();

                Console.WriteLine("*** Succesfull Download!...");
            }
            catch (AggregateException ex)
            {
                foreach (var e in ex.InnerExceptions)
                {
                    Console.WriteLine("DownloadFile ERROR: " + e.Message);
                }                
            }            
        }

        private async Task DownloadFile(string filePathInGcs)
        {            
            //Loading the Key file.
            var certificate
                     = new X509Certificate2(P12KeyFullPath, P12KeySecret,
                           X509KeyStorageFlags.MachineKeySet |
                           X509KeyStorageFlags.PersistKeySet |
                           X509KeyStorageFlags.Exportable);
            //Authentication.
            var credential = new ServiceAccountCredential(
                new ServiceAccountCredential.Initializer(GCSServiceAccountEmail)
                {
                    Scopes = new[] { StorageService.Scope.DevstorageReadWrite }
                }.FromCertificate(certificate));
            //Initializing service for Google API!.
            var service = new StorageService(
                new BaseClientService.Initializer
                {
                    ApplicationName = GCSApplicationName,
                    HttpClientInitializer = credential
                }
                );            
            /*--------- FOR TESTING PURSPOSES ---------*/            
            Console.WriteLine("* service.ApiKey: " + service.ApiKey);
            Console.WriteLine("* service.ApplicationName: " + service.ApplicationName);
            Console.WriteLine("* service.BasePath: " + service.BasePath);
            Console.WriteLine("* service.BaseUri: " + service.BaseUri);
            Console.WriteLine("* service.BucketAccessControls: " + service.BucketAccessControls);
            Console.WriteLine("* service.DefaultObjectAccessControls: " + service.DefaultObjectAccessControls);
            Console.WriteLine("* service.Buckets: " + service.Buckets);
            Console.WriteLine("* service.Channels: " + service.Channels);
            Console.WriteLine("* service.Features: " + service.Features);
            foreach(String feature in service.Features){
                Console.WriteLine("* service.Feature : " + feature);
            }
            Console.WriteLine("* service.GZipEnabled: " + service.GZipEnabled);
            Console.WriteLine("* service.HttpClient: " + service.HttpClient);
            Console.WriteLine("* service.HttpClientInitializer: " + service.HttpClientInitializer);
            Console.WriteLine("* service.Name: " + service.Name);
            Console.WriteLine("* service.Notifications: " + service.Notifications);
            Console.WriteLine("* service.ObjectAccessControls: " + service.ObjectAccessControls);
            Console.WriteLine("* service.Objects: " + service.Objects);
            Console.WriteLine("* service.Projects: " + service.Projects);
            Console.WriteLine("* service.Serializer: " + service.Serializer);
            Console.WriteLine("*--------------------------------------*");
            /*--------- END FOR TESTING PURSPOSES ---------*/
            //Get file meta-data from Google Cloud Storage.
            var fileRequest = new ObjectsResource.GetRequest(service, GCSBucketName, filePathInGcs);
            Console.WriteLine("* fileRequest.Bucket: " + fileRequest.Bucket);
            Console.WriteLine("* fileRequest.Alt: " + fileRequest.Alt);
            Console.WriteLine("* fileRequest.Credential: " + fileRequest.Credential);
            Console.WriteLine("* fileRequest.ETagAction: " + fileRequest.ETagAction);
            Console.WriteLine("* fileRequest.Fields: " + fileRequest.Fields);
            Console.WriteLine("* fileRequest.Generation: " + fileRequest.Generation);
            Console.WriteLine("* fileRequest.HttpMethod: " + fileRequest.HttpMethod);
            Console.WriteLine("* fileRequest.Key: " + fileRequest.Key);
            Console.WriteLine("* fileRequest.MethodName: " + fileRequest.MethodName);
            var fileMetaDataObj = fileRequest.Execute();
            //Let's download the file!.
            fileRequest.MediaDownloader.ProgressChanged += DownloadProgress;
            //* Google cloud storage API support chunk size multiple of 256KB.
            fileRequest.MediaDownloader.ChunkSize = (256 * 1024);
            Console.WriteLine("*** Downloading file LocalDownloadPath : " + LocalDownloadPath + " :: " + filePathInGcs);
            if (fileMetaDataObj.Size.HasValue)
            {
                Console.WriteLine(DateTime.Now
                        + ": Downloading file of size "
                        + fileMetaDataObj.Size.Value + " Bytes.");
            }         
            var downloadStream =
                     new FileStream(Path.Combine(LocalDownloadPath, filePathInGcs),
                          FileMode.Create,
                          FileAccess.Write);
            await fileRequest.MediaDownloader.DownloadAsync(fileMetaDataObj.MediaLink,
                          downloadStream,
                          CancellationToken.None);
            service.Dispose();
        }

        private void DownloadProgress(IDownloadProgress progress)
        {
            switch (progress.Status)
            {
                case DownloadStatus.Completed:
                    Console.WriteLine("Download completed!");
                    break;
                case DownloadStatus.Downloading:
                    Console.WriteLine("Downloaded " +
                          progress.BytesDownloaded + " Bytes.");
                    break;
                case DownloadStatus.Failed:
                    Console.WriteLine("Download failed "
                          + Environment.NewLine
                          + progress.Exception.Message
                          + Environment.NewLine
                          + progress.Exception.StackTrace
                          + Environment.NewLine
                          + progress.Exception.Source
                          + Environment.NewLine
                          + progress.Exception.InnerException
                          + Environment.NewLine
                          + "HR-Result" + progress.Exception.HResult);
                    break;
            }
        }

    }

}
