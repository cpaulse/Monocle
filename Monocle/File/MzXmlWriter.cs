
using Monocle.Data;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Xml;

namespace Monocle.File {
    /// <summary>
    /// Writes the scan data in mzXML format.
    /// </summary>
    public class MzXmlWriter : IScanWriter
    {
        /// <summary>
        /// The XML serializer.
        /// </summary>
        protected XmlWriter writer;

        /// <summary>
        /// The text output stream.
        /// The class holds on to this to remember the positions of the
        /// scans for creating the index at the end.
        /// </summary>
        protected StreamWriter output;

        /// <summary>
        /// Stores the position of each scan for creating the index.
        /// Keys are scan numbers and values are the text position in the file.
        /// </summary>
        protected Dictionary<long, long> scanIndex;

        /// <summary>
        /// Opens the file and initializes the XML stream.
        /// </summary>
        /// <param name="path"></param>
        public void Open(string path)
        {
            scanIndex = new Dictionary<long, long>();
            var settings = new XmlWriterSettings();
            settings.Indent = true;
            settings.NewLineChars = "\n";

            output = new StreamWriter(path);
            writer = XmlWriter.Create(output, settings);
            writer.WriteStartDocument();
        }

        /// <summary>
        /// Writes the index and closing tags, 
        /// and closes the XML and output streams.
        /// </summary>
        public void Close()
        {
            WriteIndex();
            writer.Close();
            output.Close();
        }

        /// <summary>
        /// Writes a scan tag.
        /// </summary>
        /// <param name="scan"></param>
        public virtual void WriteScan(Scan scan)
        {
            writer.Flush();
            long pos = output.BaseStream.Position;
            scanIndex.Add(scan.ScanNumber, pos);

            writer.WriteStartElement("scan");
            writer.WriteAttributeString("num", scan.ScanNumber.ToString());
            writer.WriteAttributeString("msLevel", scan.MsOrder.ToString());
            writer.WriteAttributeString("peaksCount", scan.PeakCount.ToString());
            writer.WriteAttributeString("polarity", scan.Polarity == Polarity.Positive ? "+" : "-");
            writer.WriteAttributeString("scanType", scan.ScanType.ToString());
            writer.WriteAttributeString("filterLine", scan.FilterLine);
            writer.WriteAttributeString("retentionTime", MakeRetentionTimeString(scan.RetentionTime));
            writer.WriteAttributeString("startMz", scan.StartMz.ToString());
            writer.WriteAttributeString("endMz", scan.EndMz.ToString("G17", CultureInfo.InvariantCulture));
            writer.WriteAttributeString("lowMz", scan.LowestMz.ToString("G17",CultureInfo.InvariantCulture));
            writer.WriteAttributeString("highMz", scan.HighestMz.ToString("G17", CultureInfo.InvariantCulture));
            writer.WriteAttributeString("basePeakMz", scan.BasePeakMz.ToString("G17", CultureInfo.InvariantCulture));
            writer.WriteAttributeString("basePeakIntensity", scan.BasePeakIntensity.ToString());
            writer.WriteAttributeString("totIonCurrent", scan.TotalIonCurrent.ToString());
            writer.WriteAttributeString("compensationVoltage", scan.FaimsCV.ToString());

            //tSIM/MSX methods could be MS1s with "SPS" ions so no ms order consideration here
            if (scan.MsOrder > 1)
            {
                writer.WriteAttributeString("collisionEnergy", scan.CollisionEnergy.ToString());
                foreach (Precursor precursor in scan.Precursors)
                {
                    writer.WriteStartElement("precursorMz");
                    writer.WriteAttributeString("precursorScanNum", scan.PrecursorMasterScanNumber.ToString());
                    writer.WriteAttributeString("precursorIntensity", precursor.Intensity.ToString());
                    writer.WriteAttributeString("precursorCharge", precursor.Charge.ToString());
                    writer.WriteAttributeString("activationMethod", scan.PrecursorActivationMethod.ToString());
                    writer.WriteString(precursor.Mz.ToString("G17", CultureInfo.InvariantCulture));
                    writer.WriteEndElement(); // precursorMz
                }
            }

            writer.WriteStartElement("peaks");
            writer.WriteAttributeString("precision", "32");
            writer.WriteAttributeString("byteOrder", "network");
            writer.WriteAttributeString("contentType", "m/z-int");
            writer.WriteAttributeString("compressionType", "none");
            writer.WriteAttributeString("compressedLen", "0");
            writer.WriteString(EncodePeaks(scan));
            writer.WriteEndElement(); // peaks

            writer.WriteEndElement(); // scan
        }

        /// <summary>
        /// Writes the opening tags and metadata information.
        /// </summary>
        /// <param name="header"></param>
        public void WriteHeader(ScanFileHeader header)
        {
            writer.WriteStartElement("mzXML", "http://sashimi.sourceforge.net/schema_revision/mzXML_3.1");
            writer.WriteAttributeString("xmlns", "xsi", null, "http://www.w3.org/2001/XMLSchema-instance");
            writer.WriteAttributeString("xsi", "schemaLocation", null, "http://sashimi.sourceforge.net/schema_revision/mzXML_3.1 http://sashimi.sourceforge.net/schema_revision/mzXML_3.1/mzXML_idx_3.1.xsd");

            writer.WriteStartElement("msRun");
            writer.WriteAttributeString("scanCount", header.ScanCount.ToString());
            writer.WriteAttributeString("startTime", MakeRetentionTimeString(header.StartTime));
            writer.WriteAttributeString("endTime", MakeRetentionTimeString(header.EndTime));

            writer.WriteStartElement("parentFile");
            writer.WriteAttributeString("fileName", header.FileName);
            writer.WriteAttributeString("fileType", "RAWData");
            writer.WriteEndElement(); // parentFile
            
            writer.WriteStartElement("msInstrument");
            writer.WriteStartElement("msManufacturer");
            writer.WriteAttributeString("category", "msManufacturer");
            writer.WriteAttributeString("value", header.InstrumentManufacturer);
            writer.WriteEndElement(); // msManufacturer
            writer.WriteStartElement("msModel");
            writer.WriteAttributeString("category", "msModel");
            writer.WriteAttributeString("value", header.InstrumentModel);
            writer.WriteEndElement(); // msModel
            writer.WriteEndElement(); // msInstrument
        }

        /// <summary>
        /// Encodes peak data in base64, 32bit, little-endian
        /// </summary>
        /// <param name="scan"></param>
        /// <returns></returns>
        protected string EncodePeaks(Scan scan) {
            if (scan.PeakCount == 0) {
                return "AAAAAAAAAAA=";
            }
            
            // Allocate space for m/z and int pairs, four bytes each.
            byte[] bytes = new byte[scan.PeakCount * 2 * 4];

            for (int i = 0; i < scan.PeakCount; ++i) {
                Centroid peak = scan.Centroids[i];
                byte[] mzBytes = BitConverter.GetBytes((float)peak.Mz);
                Array.Reverse(mzBytes);
                mzBytes.CopyTo(bytes, i * 8);

                byte[] intBytes = BitConverter.GetBytes((float)peak.Intensity);
                Array.Reverse(intBytes);
                intBytes.CopyTo(bytes, (i * 8) + 4);
            }
            return Convert.ToBase64String(bytes);
        }

        /// <summary>
        /// Prepares the retention time for the mzXML file,
        /// by converting it into seconds and formatting it.
        /// </summary>
        /// <param name="time">The retention time of the scan in minutes.</param>
        /// <returns>The string representation of the time in seconds.</returns>
        protected string MakeRetentionTimeString(double time) {
            return "PT" + System.Math.Round(time * 60, 4).ToString() + "S";
        }

        /// <summary>
        /// Writes the scan index and closes the tags for the
        /// end of the file.
        /// </summary>
        private void WriteIndex() 
        {
            writer.WriteEndElement(); // msRun

            writer.Flush();
            long indexOffset = output.BaseStream.Position;
            writer.WriteStartElement("index");
            foreach(var entry in scanIndex) {
                writer.WriteStartElement("offset");
                writer.WriteAttributeString("id", entry.Key.ToString());
                writer.WriteString(entry.Value.ToString());
                writer.WriteEndElement(); // offset
            }
            writer.WriteEndElement(); // index

            writer.WriteStartElement("indexOffset");
            writer.WriteString(indexOffset.ToString());
            writer.WriteEndElement();

            writer.WriteEndElement(); // mzXML
        }

        /// <summary>
        /// Prepraes the string for the parentFile fileType attribute.
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        private string FileNameToType(string name)
        {
            switch(Path.GetExtension(name).ToUpper()) {
                case ".RAW":
                    return "RawFile";
                case ".MZXML":
                    return "mzXML";
                default:
                    break;
            }
            return "unknown";
        }

    }
}
