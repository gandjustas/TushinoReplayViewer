using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using System.Net;
using SharpCompress.Archives.SevenZip;
using PboTools;
using CoreFtp;
using System.IO;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using Microsoft.Extensions.Configuration;
using System.Net.Http;

namespace ReplayViewer.Controllers
{
    [Route("api/[controller]")]
    public class ReplayController : Controller
    {
        CloudStorageAccount storageAccount;

        public ReplayController(CloudStorageAccount storageAccount)
        {
            this.storageAccount = storageAccount;
        }

        [HttpGet]
        public async Task<ActionResult> Get(string p, string r)
        {
            var uri = new Uri(p);

            CloudBlobClient blobClient = storageAccount.CreateCloudBlobClient();
            CloudBlobContainer container = blobClient.GetContainerReference("replays");
            var blob = container.GetBlockBlobReference(uri.Segments[uri.Segments.Length - 1] + "/log.txt");

            if (r != null || !await blob.ExistsAsync())
            {
                var file = new MemoryStream();

                if (uri.Scheme == "ftp")
                {
                    var userInfo = uri.UserInfo.Split(':');
                    using (var ftpClient = new FtpClient(new FtpClientConfiguration
                    {
                        Host = uri.Host,
                        Port = uri.Port,
                        Username = userInfo[0],
                        Password = userInfo[1]
                    }))
                    {
                        await ftpClient.LoginAsync();
                        using (var stream = await ftpClient.OpenFileReadStreamAsync(uri.LocalPath))
                        {
                            await stream.CopyToAsync(file);
                        }
                        await ftpClient.LogOutAsync();
                    }
                }
                else if (uri.Scheme.StartsWith("http"))
                {
                    using (var httpClient = new HttpClient())
                    {
                        using (var stream = await httpClient.GetStreamAsync(uri))
                        {
                            await stream.CopyToAsync(file);
                        }
                    }
                }

                var archive = SevenZipArchive.Open(file);
                using (var reader = archive.ExtractAllEntries())
                {
                    while (reader.MoveToNextEntry())
                    {
                        foreach (var f in PboFile.EnumerateEntries(reader.OpenEntryStream()))
                        {
                            if (f.Path == "log.txt")
                            {
                                blob.Properties.CacheControl = "public, max-age=31536000";
                                blob.Properties.ContentType = "text/plain";
                                using(var s = new MemoryStream(f.FileContents.Length))
                                using (var input = new StreamReader(new MemoryStream(f.FileContents)))
                                using (var output = new StreamWriter(s))
                                {
                                    ReplayToJson.Normalize(input, output);
                                    s.Seek(0, SeekOrigin.Begin);
                                    await blob.UploadFromStreamAsync(s);
                                    await blob.SetPropertiesAsync();
                                    return RedirectPermanent(blob.Uri.ToString()); //File(f.FileContents, "text/plain");
                                }
                            }
                        }
                    }
                }
                return NotFound();
            }
            else
            {
                return RedirectPermanent(blob.Uri.ToString());
            }


        }
    }
}
