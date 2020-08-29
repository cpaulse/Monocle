using Monocle;
using Monocle.Data;
using Monocle.File;
using System;
using System.Collections.Generic;
using System.IO;

using Monocle.Data;
using Monocle.Math;
using Monocle.Peak;

namespace MakeMono
{
    internal class Program
    {
        private static NLog.Logger log = NLog.LogManager.GetCurrentClassLogger();

        static void Main(string[] args)
        {
            //List<double> expected = PeptideEnvelopeCalculator.GetTheoreticalEnvelope(400.0, 3, 10, true);

            var parser = new CliOptionsParser();
            MakeMonoOptions options = new MakeMonoOptions();
           // MakeMonoOptions options = parser.Parse(args);
            MonocleOptions monocleOptions = new MonocleOptions
            {
                AveragingVector = options.AveragingVector,
                Charge_Detection = options.ChargeDetection,
                Charge_Range = new ChargeRange(options.ChargeRange),
                MS_Level = options.MS_Level,
                Number_Of_Scans_To_Average = options.NumOfScans,
                WriteDebugString = options.WriteDebug,
                OutputFileType = options.OutputFileType,
                ConvertOnly = options.ConvertOnly,
                SkipMono = options.SkipMono,
                ChargeRangeUnknown = new ChargeRange(options.ChargeRangeUnknown),
                ForceCharges = options.ForceCharges,
                UseMostIntense = options.UseMostIntense
            };

            SetupLogger(options.RunQuiet, options.WriteDebug);

            try
            {
                log.Info("Starting Processing.");
                string file = options.InputFilePath;
                IScanReader reader = ScanReaderFactory.GetReader(file);
                reader.Open(file);
                var header = reader.GetHeader();
                header.FileName = Path.GetFileName(file);
                header.FilePath = file;
                /*    string file = options.InputFilePath;
                    IScanReader reader = ScanReaderFactory.GetReader(file);
                    reader.Open(file);
                    var header = reader.GetHeader();
                    header.FileName = Path.GetFileName(file);

                    log.Info("Reading scans: " + file);
                    List<Scan> Scans = new List<Scan>();
                    foreach (Scan scan in reader)
                    {
                        Scans.Add(scan);
                    }*/

                if (!monocleOptions.ConvertOnly)
                {
                    log.Info("Starting monoisotopic assignment.");
                    List<Scan> sc = new List<Scan>();
                    /*     (double, double)[] spec = {
                             (2402.1108971135104, 0.22060805866762456),
                         (2403.11425194871, 0.2598358412016946),
                         (2404.11760678391, 0.15160264089116693),
                         (2405.1209616191104, 0.058417814509564306),
                         (2406.12185177741, 0.009664396403474669),
                         (2407.11675747501, 0.007845324196425973)};*/

                    (double, double)[] spec = { (2444.0613018494096, 0.002076000386498873),
                        (2445.0642016483553, 0.002634857788601345),
                        (2446.0587079385014, 0.023733548272158123),
                        (2447.060284438129, 0.047413904440783354),
                        (2448.0589364318535, 0.10081323671737553),
                        (2449.0604956707034, 0.10264173057676143),
                        (2450.0578713518753, 0.18621715205910416),
                        (2451.059271771276, 0.18905476867329743),
                        (2452.06004372538, 0.15267802392623717),
                        (2453.0611217888004, 0.09771267076617624),
                        (2454.0622984847323, 0.05151117322730185),
                        (2455.0635612303695, 0.022586797855032866),
                        (2456.0647877529905, 0.008126259987255476),
                        (2457.0664136937303, 0.002423895298733596),
                        (2458.068275087755, 0.00038042740980991655)};
                    Scan s = new Scan();
                    s.MsOrder = 2;
                    foreach (var pair in spec)
                    {
                        s.Centroids.Add(new Centroid(pair.Item1 / 3.0 + 1.007276466879000, pair.Item2 * 100));
                    }

                    sc.Add(s);
                    monocleOptions.SearchForSelenium = true;

                   // Monocle.Monocle.Run(ref sc, monocleOptions);
                    var pc = new Scan();
                    var p = new Precursor();
                    double mass = 2447.060284438129;
                    //double mass = 2402.1108971135104;
                    p.IsolationMz = mass / 3.0 + 1.0073;
                    p.Mz = mass / 3.0 + 1.0073;
                    //pc.
                    Monocle.Monocle.Run(sc, s, p, monocleOptions);

                }

                string outputFilePath = options.OutputFilePath.Trim();
                if(outputFilePath.Length == 0) {
               //     outputFilePath = ScanWriterFactory.MakeTargetFileName(file, monocleOptions.OutputFileType);
                }
                log.Info("Writing output: " + outputFilePath);
                IScanWriter writer = ScanWriterFactory.GetWriter(monocleOptions.OutputFileType);
                writer.Open(outputFilePath);
             //   writer.WriteHeader(header);
             /*   foreach (Scan scan in Scans)
                {
                    writer.WriteScan(scan);
                }*/
                writer.Close();
                log.Info("Done.");
            }
            catch (Exception e)
            {
                log.Error("An error occurred.");
                log.Debug(e.GetType().ToString());
                log.Debug(e.Message);
                log.Debug(e.ToString());
            }
        }

        static void SetupLogger(bool quiet, bool debug)
        {
            var config = new NLog.Config.LoggingConfiguration();
            var logconsole = new NLog.Targets.ConsoleTarget("logconsole")
            {
                Layout = "${message}"
            };
            var minLogLevel = NLog.LogLevel.Info;
            if (quiet) {
                minLogLevel = NLog.LogLevel.Error;
            }
            else if (debug) {
                minLogLevel = NLog.LogLevel.Debug;
            }
            config.AddRule(minLogLevel, NLog.LogLevel.Fatal, logconsole);
            NLog.LogManager.Configuration = config;
        }
    }
}
