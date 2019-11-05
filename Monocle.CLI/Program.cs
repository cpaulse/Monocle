﻿using Monocle;
using Monocle.Data;
using Monocle.File;
using System;
using System.Collections.Generic;

namespace MakeMono
{
    internal class Program
    {
        static void Main(string[] args)
        {
            try {
                Console.WriteLine("MakeMono, a console application wrapper for Monocle.");

                var parser = new CliOptionsParser();
                MakeMonoOptions options = parser.Parse(args);
                var file = options.InputFilePath;

                var monocleOptions = new MonocleOptions
                {
                    AveragingVector = AveragingVector.Both,
                    Charge_Detection = options.ChargeDetection,
                    Charge_Range = new ChargeRange(options.ChargeRange),
                    Number_Of_Scans_To_Average = options.NumOfScans
                };

                IScanReader reader = ScanReaderFactory.GetReader(file);
                reader.Open(file);

                var Scans = new List<Scan>();
                foreach (Scan scan in reader)
                {
                    Scans.Add(scan);
                }

                Monocle.Monocle.Run(ref Scans, monocleOptions);

                IScanWriter writer = ScanWriterFactory.GetWriter(file, options.OutputFileType);
                writer.Open(file);
                writer.WriteHeader(new ScanFileHeader());
                foreach (Scan scan in Scans) {
                    writer.WriteScan(scan);
                }
                writer.Close();
            }
            catch(Exception e) {
                Console.WriteLine("An error occurred:\n");
                Console.WriteLine(e.Message);
            }
        }
    }
}
