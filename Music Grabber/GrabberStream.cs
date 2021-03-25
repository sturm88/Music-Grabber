///-----------------------------------------------------------------
///   Namespace: Music_Grabber
///   Class: GrabberStream
///   Description: class for play audio stream and save music from stream   
///   Author: sturmf_88                    
///   Date: 03.03.2021
///-----------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml.Linq;
using Un4seen.Bass;

namespace Music_Grabber
{
    public class GrabberStream
    {
        /// <summary>
        /// Create  event when stream cnahge music 
        /// </summary>
        /// <param name="message"></param>
        public delegate void OnMetaChanged(string message);
        /// <summary>
        /// define meta as  event 
        /// </summary>
        public event OnMetaChanged OnStreamChanged;

        /// <summary>
        /// Save music directory
        /// </summary>
        public string MusicFolderPath { get; private set; }
        /// <summary>
        /// Volume  5
        /// </summary>
        public float Volume { get; private set; }
        /// <summary>
        /// All url streams from file url.txt
        /// </summary>
        public List<string> UrlStreams { get; private set; }
        /// <summary>
        /// Current stream
        /// </summary>
        public int Stream { get; private set; }
        /// <summary>
        /// Stream data
        /// </summary>
        private byte[] _data;
        /// <summary>
        /// Old value meta tag 
        /// </summary>
        private string _previousMetaTagName;
        /// <summary>
        /// Current file stream 
        /// </summary>
        private FileStream _fileStream;
        /// <summary>
        /// Proc for audio stream
        /// </summary>
        private DOWNLOADPROC _downloadProcedure;
        /// <summary>
        /// Allow save music file in directory
        /// </summary>
        public bool AllowSaveAudio { get; set; } = true;
        /// <summary>
        /// Show cuurent track name 
        /// </summary>
        public bool AllowShowTrackName { get; set; } = true;


        /// <summary>
        /// main config file
        /// </summary>
        private string _cnfFile { get; set; } = "config.xml";
        /// <summary>
        ///main  url file
        /// </summary>
        private string _urlFile { get; set; } = "url.txt";


        /// <summary>
        /// Validate and init Bass.Net
        /// </summary>
        public GrabberStream()
        {
            if (_validateConfiguration())
            {
                BassNet.Registration("saxon_88@rambler.ru", "2X28330193738");

                Bass.BASS_Init(-1, 44100, BASSInit.BASS_DEVICE_DEFAULT, IntPtr.Zero);
                this.UrlStreams = new List<string>();

                Console.WriteLine("Configuration succsessfull");
            }
        }

        /// <summary>
        /// Data attribute from XML element
        /// </summary>
        /// <param name="document">Created xml document</param>
        /// <param name="elementName">Element name</param>
        /// <param name="attributeName">Attribute name</param>
        /// <returns>String value</returns>
        private string _getDataAttributeFromXml(XDocument document, string elementName, string attributeName)
        {
            try
            {
                XElement xelement = document.Descendants().Where(x => x.Name == elementName).FirstOrDefault();
                if (xelement != null)
                {
                    XAttribute xattribute = xelement.Attribute(attributeName);
                    if (xattribute != null)
                    {
                        return xattribute.Value;
                    }
                    else
                    {
                        Console.WriteLine($"Error configuration file: element -> <{{0}}/> attribute {attributeName}  missing", elementName);
                        return null;
                    }
                }
                else
                {
                    Console.WriteLine($"Error configuration file: element <{{0}}/>  missing", attributeName);
                    return null;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return null;
            }
        }



        /// <summary>
        /// Stop current stream
        /// </summary>
        public void Stop()
        {
            if (Stream != 0)
            {
                Bass.BASS_ChannelStop(Stream);
            }
        }

       

        /// <summary>
        /// Load config data from config.xml
        /// </summary>
        /// <returns>True if data or file is valid else false</returns>
        private bool _loadConfigureData()
        {
            if (File.Exists(_urlFile))
            {
                try
                {
                    XDocument document = XDocument.Load(_cnfFile);
                    MusicFolderPath = _getDataAttributeFromXml(document, "MusicFolder", "path");

                    if (MusicFolderPath == null)
                    {
                        return false;
                    }
                    string Value = _getDataAttributeFromXml(document, "Volume", "value");

                    if (Value == null)
                    {
                        return false;
                    }
                    else
                    {
                        float volume;
                        if (float.TryParse(Value, out volume))
                        {
                            this.Volume = volume;
                        }
                        else
                        {
                            this.Volume = 1;
                        }
                    }

                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                    return false;
                }
            }
            else
            {
                Console.WriteLine("Error {0} file not found", _cnfFile);
                return false;
            }
            return true;
        }


    





        /// <summary>
        /// Check is URL valid 
        /// </summary>
        /// <param name="source">URL input</param>
        /// <returns>true or false if valid or invalid</returns>
        private bool _checkURLValid(string source)
        {
            Uri uriResult;
            return Uri.TryCreate(source, UriKind.Absolute, out uriResult) 
                && (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps);
        }





        /// <summary>
        ///  Load data with to  URL from file url.txt
        /// </summary>
        /// <returns>true is URL  valid, or file is valid or exist</returns>
        private bool _loadUrlStreamsFromFile()
        {
            if (File.Exists(_urlFile))
            {
                string[] lines = File.ReadAllLines("url.txt").Where(x => !String.IsNullOrWhiteSpace(x)).ToArray();
                if (lines.Length > 0)
                {
                    bool isInValidUrl = lines.Where(x => !_checkURLValid(x)).Count() > 0;
                    if (isInValidUrl)
                    {
                        Console.WriteLine("Error url file: url link is invalid");
                        return false;
                    }
                    else
                    {
                        bool isSameValues = lines.ToList().GroupBy(x => Regex.Replace(x, @"\s+", String.Empty)).Any(g => g.Count() > 1);
                        if (isSameValues)
                        {
                            Console.WriteLine("Error: url address have same values");
                            return false;
                        }

                        UrlStreams = lines.ToList();
                    }
                }
                else
                {
                    Console.WriteLine("Error url file: url links not found");
                    return false;
                }

            }
            else
            {
                Console.WriteLine("Error {0} file: file not found", _urlFile);
            }
            return true;
        }



        /// <summary>
        /// Get URL string by index
        /// </summary>
        /// <param name="index">index from file url.txt</param>
        /// <returns>URL string</returns>
        private string _getUrlStreamByIndex(int index)
        {
            
                if (UrlStreams != null)
                {
                    if (UrlStreams.Count > 0)
                    {
                        if (index >= 0 && index <= UrlStreams.Count - 1)
                        {
                            return UrlStreams[index];
                        }
                    }
                }
                return null;
            
        }



        /// <summary>
        /// Determines is configuration valid
        /// </summary>
        /// <returns>True is valid or not</returns>
        public bool _validateConfiguration()
        {
            bool _isConfigurationValid = this._loadConfigureData();
            bool _isUrlValid = this._loadUrlStreamsFromFile();

            return _isConfigurationValid && _isUrlValid;
        }


        /// <summary>
        /// Change bass volume
        /// </summary>
        /// <param name="volume">volume 0 .. 1</param>
        public void SetVolume(float volume)
        {
            this.Volume = volume;
            Bass.BASS_SetVolume(this.Volume);
        }



        /// <summary>
        /// Play stream
        /// </summary>
        /// <param name="urlIndex">URL index</param>
        /// <returns></returns>
        public bool PlayStream(int urlIndex = 0)
        {
                if (_validateConfiguration())
                {
                    string currentUrlStream = _getUrlStreamByIndex(urlIndex);

                    if (currentUrlStream != null)
                    {
                        _downloadProcedure = new DOWNLOADPROC(_download);

                        if (Stream != 0)
                            Bass.BASS_StreamFree(Stream);

                        Stream = Bass.BASS_StreamCreateURL(currentUrlStream, 0,
                        BASSFlag.BASS_STREAM_BLOCK | BASSFlag.BASS_SAMPLE_MONO | BASSFlag.BASS_STREAM_STATUS, _downloadProcedure, IntPtr.Zero);

                        Bass.BASS_ChannelPlay(Stream, false);

                        Bass.BASS_SetConfig(BASSConfig.BASS_CONFIG_NET_BUFFER, 10000);

                        Bass.BASS_SetVolume(Volume);

                        return true;
                    }
                    else
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine($"Error: url not found by index {urlIndex}");
                    }
                
         
            }
            return false;
        }





        /// <summary>
        /// Proc method call from bass when stream downloading
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="length"></param>
        /// <param name="user"></param>

        private void _download(IntPtr buffer, int length, IntPtr user)
        {
            if (Stream != 0)
            {
                ///get meta tags array from current stream
                string[] metaTags = Bass.BASS_ChannelGetTagsMETA(Stream);

                if (metaTags != null)
                {
                    ///cut unnecessary information from meta name
                    string currentMetaTag = metaTags[0].Replace("StreamTitle=", "")
                                                      .Replace("StreamUrl=", "")
                                                      .Replace(';', ' ')
                                                      .Replace("'", "")
                                                      .TrimEnd();


                    ///If old meta name is not equal current, create new file and print track name
                    if (currentMetaTag != _previousMetaTagName)
                    {
                        ///close stream if created already
                        _flushAndCloseFileStream();

                        ///Allow show track name
                        if (AllowShowTrackName)
                        {
                            Console.ForegroundColor = ConsoleColor.DarkCyan;
                            Console.WriteLine($"[{DateTime.Now}] Now play : [{currentMetaTag}]");
                            OnStreamChanged?.Invoke(currentMetaTag);
 



                        }
                        ///Allow save audio
                        if (AllowSaveAudio)
                        {
                            _fileStream = File.Create(MusicFolderPath + "\\" + currentMetaTag + ".mp3");
                        }
                        ///set old meta name to new name to avoid overwriting the file, and call event handler
                        _previousMetaTagName = currentMetaTag;
                    }

                    if (AllowSaveAudio)
                    {
                        if (_fileStream != null)
                        {
                            if (_data == null || _data.Length < length)
                                _data = new byte[length];

                            ///copy data  managment to unmanagment 
                            Marshal.Copy(buffer, _data, 0, length);

                            ///write data in file
                            _fileStream.Write(_data, 0, length);
                        }
                    }


                }
            }
        }



        /// <summary>
        /// Flush and close file stream
        /// </summary>
        private void _flushAndCloseFileStream()
        {
            if (_fileStream != null)
            {
                _fileStream.Flush();
                _fileStream.Close();
            }
        }
    }
}
