using Monocle.Data;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Xml;

namespace Monocle.File
{
    public class MzXmlReader : IScanReader
    {
        private XmlReader Reader;
        
        private ScanFileHeader Header;

        private string FilePath;

        #region Attributes for the MZXML file
        public Dictionary<string, string> mzxmlAttributes = new Dictionary<string, string>()
        {
            { "num" , "ScanNumber" },
            { "msLevel" , "MsOrder" },
            { "scanEvent" , "ScanEvent" },
            { "masterIndex" , "MasterIndex" },
            { "peaksCount" , "PeakCount" },
            { "ionInjectionTime" , "IonInjectionTime" },
            { "elapsedScanTime" , "ElapsedScanTime" },
            { "polarity" , "Polarity" },
            { "scanType" , "ScanType" },
            { "filterLine" , "FilterLine" },
            { "startMz","StartMz" },
            { "endMz","EndMz" },
            { "lowMz","LowestMz" },
            { "highMz","HighestMz" },
            { "basePeakMz","BasePeakMz" },
            { "basePeakIntensity","BasePeakIntensity" },
            { "faimsVoltageOn","FaimsVoltageOn" },
            { "faimsCv","FaimsCV" }
        };

        public Dictionary<string, string> mzxmlMsnAttributes = new Dictionary<string, string>()
        {
            { "totIonCurrent","TotalIonCurrent" },
            { "collisionEnergy","CollisionEnergy" }
        };

        public Dictionary<string, string> mzxmlPrecursorAttributes = new Dictionary<string, string>()
        {
            // Precusor information
            { "precursorMz","PrecursorMz" },
            { "precursorScanNum","PrecursorMasterScanNumber" },
            { "precursorIntensity","PrecursorIntensity" },
            { "precursorCharge","PrecursorCharge" },
            { "activationMethod","PrecursorActivationMethod" }
        };

        public Dictionary<string, string> mzxmlPeaksAttributes = new Dictionary<string, string>()
        {
            // Peaks information
            { "precision","PeaksPrecision" },
            { "byteOrder","PeaksByteOrder" },
            { "contentType","PeaksContentType" },
            { "compressionType", "PeaksCompressionType" },
            { "compressedLen", "PeaksCompressedLength" }
        };
        #endregion

        /// <summary>
        /// Open new fileStream to mzXML file.
        /// </summary>
        /// <param name="path"></param>
        public void Open(string path) {
            if (!System.IO.File.Exists(path)) {
                throw new IOException("File not found: " + path);
            }
            FilePath = path;
            Reader = XmlReader.Create(FilePath);
        }

        /// <summary>
        /// Returns header information from the mzXML file.
        /// </summary>
        /// <returns>An instance of the ScanFileHeader class</returns>
        public ScanFileHeader GetHeader() {
            return Header;
        }

        /// <summary>
        /// Dispose of the reader when reading multiple files.
        /// </summary>
        public void Close()
        {
            Reader.Dispose();
        }

        /// <summary>
        /// Open the given file and import scans into the reader.
        /// </summary>
        /// <returns></returns>
        public IEnumerator GetEnumerator() {
            Scan scan = null;
            while(Reader.Read()) {
                switch (Reader.NodeType) {
                    case XmlNodeType.Element:
                        if (Reader.Name == "scan") {
                            scan = new Scan();
                            while (Reader.MoveToNextAttribute()) {
                                SetAttribute(scan, Reader.Name, Reader.Value);
                            }
                        }
                        if (Reader.Name == "peaks" && scan != null) {
                            scan.Centroids = ReadPeaks(Reader.ReadElementContentAsString(), scan.PeakCount);
                        }
                        else if (Reader.Name == "precursorMz" && scan != null) {
                            while (Reader.MoveToNextAttribute()) {
                                SetAttribute(scan, Reader.Name, Reader.Value);
                            }
                            Reader.MoveToContent();
                            SetAttribute(scan, "precursorMz", Reader.ReadElementContentAsString());
                        }
                        break;
                    case XmlNodeType.EndElement:
                        if (Reader.Name == "scan") {
                            yield return scan;
                        }
                        break;
                    default:
                    break;
                }
            }
        }

        /// <summary>
        /// Check and set attribute based on attributes dictionary
        /// </summary>
        /// <param name="attribute"></param>
        /// <param name="value"></param>
        public void SetAttribute(Scan scan, string attribute, string value)
        {
            if (attribute == "retentionTime") {
                // Parse time and change to minutes.
                scan.RetentionTime = double.Parse(value.Trim("PTS ".ToCharArray())) / 60d;
            }

            string tempAttr = "";
            if (mzxmlAttributes.ContainsKey(attribute))
            {
                tempAttr = mzxmlAttributes[attribute];
            }
            else if (mzxmlPrecursorAttributes.ContainsKey(attribute))
            {
                tempAttr = mzxmlPrecursorAttributes[attribute];
            }
            else if (mzxmlPeaksAttributes.ContainsKey(attribute))
            {
                tempAttr = mzxmlPeaksAttributes[attribute];
            }
            else if (mzxmlMsnAttributes.ContainsKey(attribute))
            {
                tempAttr = mzxmlMsnAttributes[attribute];
            }

            if (tempAttr != "")
            {
                PropertyInfo piTmp;
                double dTmp; bool bTmp;
                if (typeof(Scan).GetProperty(tempAttr) != null) //check names even though readOnly DGV
                {
                    piTmp = typeof(Scan).GetProperty(tempAttr);

                    if (piTmp.PropertyType == typeof(int) && Int32.TryParse(value, out int iTmp))
                    {
                        piTmp.SetValue(scan, iTmp);
                    }
                    else if (piTmp.PropertyType == typeof(string))
                    {
                        piTmp.SetValue(scan, value);
                    }
                    else if (piTmp.PropertyType == typeof(double) && Double.TryParse(value, out dTmp))
                    {
                        piTmp.SetValue(scan, dTmp);
                    }
                    else if (piTmp.PropertyType == typeof(bool) && Boolean.TryParse(value, out bTmp))
                    {
                        piTmp.SetValue(scan, bTmp);
                    }
                }
            }
        }

        private void ReadHeader() {
            var header = new ScanFileHeader();
            header.FileName = Path.GetFileName(FilePath);
            while(Reader.Read()) {
                switch (Reader.NodeType) {
                    case XmlNodeType.Element:
                        if (Reader.Name == "msRun") {
                            while (Reader.MoveToNextAttribute()) {
                                switch(Reader.Name) {
                                    case "scanCount":
                                        header.ScanCount = int.Parse(Reader.Value);
                                        break;
                                    case "startTime":
                                        header.StartTime = float.Parse(Reader.Value);
                                        break;
                                    case "endTime":
                                        header.EndTime = float.Parse(Reader.Value);
                                        break;
                                    default:
                                        break;
                                }
                            }
                        }
                        else if (Reader.Name == "msManufacturer") {
                            while (Reader.MoveToNextAttribute()) {
                                if (Reader.Name == "value") {
                                    header.InstrumentManufacturer = Reader.Value;
                                }
                            }
                        }
                        else if (Reader.Name == "msModel") {
                            while (Reader.MoveToNextAttribute()) {
                                if (Reader.Name == "value") {
                                    header.InstrumentModel = Reader.Value;
                                }
                            }
                        }
                        else if (Reader.Name == "scan") {
                            // Gone too far.
                            Reader.Dispose();
                            Reader = XmlReader.Create(FilePath);
                            return;
                        }
                        break;
                    case XmlNodeType.EndElement:
                        if (Reader.Name == "msInstrument") {
                            return;
                        }
                        break;
                    default:
                    break;
                }
            }
        }

        /// <summary>
        /// Read mzXML peaks property
        /// </summary>
        /// <param name="str"></param>
        /// <param name="peakCount"></param>
        /// <returns></returns>
        private List<Centroid> ReadPeaks(string str,int peakCount) {
            List<Centroid> peaks = new List<Centroid>();
            int size = peakCount * 2;
            if (String.Compare(str, "AAAAAAAAAAA=") == 0)
            {
                return peaks;
            }
            byte[] byteEncoded = Convert.FromBase64String(str);
            Array.Reverse(byteEncoded);
            float[] values = new float[size];
            for(int i = 0; i < size; i++)
            {
                values[i] = BitConverter.ToSingle(byteEncoded, i * 4);
            }
            Array.Reverse(values);
            for (int i = 0; i < peakCount; ++i)
            {
                Centroid tempCent = new Centroid(values[2 * i], values[(2 * i) + 1]);
                peaks.Add(tempCent);
            }
            return peaks;
        }

        private void Cleanup() {
            if (Reader != null) {
                ((IDisposable)Reader).Dispose();
            }
        }
   }
}
