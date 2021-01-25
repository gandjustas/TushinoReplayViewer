using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using System.Threading;
using Microsoft.EntityFrameworkCore;
using PboTools;
using SharpCompress.Archives.SevenZip;
using System.Threading.Channels;

namespace Tushino
{
    public record ReplayKey(string server, DateTime timestamp);
    public class Program
    {
        static int counterParsed = 0;
        static int counterProcessed = 0;
        static Channel<Tuple<string, Exception>> exceptions = Channel.CreateUnbounded<Tuple<string, Exception>>(new UnboundedChannelOptions { SingleReader = true });
        static Channel<Replay> queue = Channel.CreateUnbounded<Replay>(new UnboundedChannelOptions { SingleReader = true });
        static HashSet<ReplayKey> ExistingRecords = new HashSet<ReplayKey>();
        public static void Main(string[] args)
        {
            if (args.Length == 0)
            {
                Console.WriteLine("Usage: ParseTsgReplays.exe path-to-replays [-rebuild [-unfinished]]");
                return;
            }

            var rebuildBase = false;
            if (args.Contains("-rebuild", StringComparer.OrdinalIgnoreCase))
            {
                rebuildBase = true;
            }

            var unfinished = false;
            if (args.Contains("-unfinished", StringComparer.OrdinalIgnoreCase))
            {
                unfinished = true;
            }

            using (var db = new ReplaysContext())
            {
                db.Database.Migrate();
                if (!rebuildBase)
                {
                    var allReplays = db.Replays.AsQueryable();
                    if (unfinished)
                    {
                        allReplays = allReplays.Where(r => r.IsFinished == true);
                    }
                    ExistingRecords = allReplays.Select(r => new ReplayKey(r.Server, r.Timestamp)).ToHashSet();
                }
            };


            var task = Task.Run(async () =>
            {
                var set = new HashSet<ReplayKey>();

                var counter = 0;
                List<Replay> toAdd = new List<Replay>();
                await foreach (var r in queue.Reader.ReadAllAsync())
                {
                    if (ExistingRecords.Add(new(r.Server, r.Timestamp)))
                    {
                        toAdd.Add(r);
                        if (++counter % 1000 == 0)
                        {
                            using (var db = new ReplaysContext())
                            {
                                db.Replays.AddRange(toAdd);
                                await db.SaveChangesAsync();
                                counter = 0;
                                toAdd.Clear();
                            }
                        }
                    }
                }
                using (var db = new ReplaysContext())
                {
                    db.Replays.AddRange(toAdd);
                    await db.SaveChangesAsync();
                }
            });


            var provider = CultureInfo.InvariantCulture;

            Task.Run(async () =>
                {
                    await foreach (var t in exceptions.Reader.ReadAllAsync())
                    {
                        Console.WriteLine(t.Item1);
                        Console.WriteLine(t.Item2.ToString());
                        Console.WriteLine();
                    }
                });

            var dir = args[0];

            if (Path.HasExtension(dir))
            {
                if (Path.GetExtension(dir) == ".pbo") ParsePbo(dir);
                if (Path.GetExtension(dir) == ".7z") ParseArchive(dir);
            }
            else
            {
                Parallel.ForEach(Directory.EnumerateFiles(dir, "*.pbo", SearchOption.AllDirectories), ParsePbo);
                Parallel.ForEach(Directory.EnumerateFiles(dir, "*.7z", SearchOption.AllDirectories), ParseArchive);
            }

            Console.WriteLine("Processed {0} parsed {1}", counterProcessed, counterParsed);

            queue.Writer.TryComplete();
            exceptions.Writer.TryComplete();

            task.Wait();

            //ClearDuplicates();
        }

        static void ParsePbo(string pbo)
        {
            var replayName = Path.GetFileNameWithoutExtension(pbo);
            using (var file = File.OpenRead(pbo))
            {
                ParseReplayLog(replayName, file);
            }
            if (counterProcessed % 100 == 0) Console.WriteLine("Processed {0} parsed {1}", counterProcessed, counterParsed);
        }

        static void ParseArchive(string archive)
        {
            try
            {
                using (var arch = SevenZipArchive.Open(archive))
                {
                    foreach (var ent in arch.Entries)
                    {
                        var replayName = Path.GetFileNameWithoutExtension(ent.Key);
                        using (var file = ent.OpenEntryStream())
                        {
                            ParseReplayLog(replayName, file);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                exceptions.Writer.TryWrite(Tuple.Create(archive, e));
            }
            if (counterProcessed % 100 == 0) Console.WriteLine("Processed {0} parsed {1}", counterProcessed, counterParsed);
        }

        static void ParseReplayLog(string replayName, Stream file)
        {
            var key = new ReplayKey(
                  replayName.Substring(0, 2),
                  DateTime.ParseExact(replayName.Substring(3, 19), "yyyy-MM-dd-HH-mm-ss", CultureInfo.InvariantCulture)
                );
            if (ExistingRecords.Contains(key)) return;

            var stream = PboFile.FromStream(file).OpenFile("log.txt");

            if (stream != null)
            {
                using (var input = new StreamReader(stream))
                {
                    var p = new ReplayProcessor(input);
                    Replay replay;
                    try
                    {
                        replay = p.ProcessReplay();
                        if (replay != null)
                        {
                            Interlocked.Increment(ref counterParsed);
                        }
                    }
                    catch (ParseException e)
                    {
                        exceptions.Writer.TryWrite(Tuple.Create(replayName, (Exception)e));
                        replay = p.GetResult();
                    }
                    Interlocked.Increment(ref counterProcessed);
                    if (replay != null)
                    {
                        replay.Server = replayName.Substring(0, 2);
                        queue.Writer.TryWrite(replay);
                    }
                }
            }
        }

        private static void ClearDuplicates()
        {
            //Clear duplicates
            using (var db = new ReplaysContext())
            {
                db.Database.ExecuteSqlRaw(@"
                    delete   from EnterExitEvents
                    where    ReplayId not in
                             (
                             select  max(Id)
                             from    Replays
                             group by Server, Timestamp
                             );

                    delete   from Units
                    where    ReplayId not in
                             (
                             select  max(Id)
                             from    Replays
                             group by Server, Timestamp
                             );

                    delete   from Kills
                    where    ReplayId not in
                             (
                             select  max(Id)
                             from    Replays
                             group by Server, Timestamp
                             )	;	 
		 
                    delete   from Replays
                    where    Id not in
                             (
                             select  max(Id)
                             from    Replays
                             group by Server, Timestamp
                             )	;	 
                ");
            }
        }

    }
}
