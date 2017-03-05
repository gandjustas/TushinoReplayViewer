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

namespace ReplayViewerTest.Controllers
{
    [Route("api/[controller]")]
    public class ReplayController : Controller
    {
        [HttpGet]
        public async Task<ActionResult> Get(string replay)
        {
            var uri = new Uri(replay);
            var file = new MemoryStream();
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
                            return File(f.FileContents, "text/plain");
                        }
                    }
                }
            }
            return NotFound();


        }
    }
}
