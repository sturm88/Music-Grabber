using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using Un4seen.Bass;

namespace Music_Grabber
{
    public class CMDArgsAggregate<T> where T : struct
    {
        public static (bool, Nullable<T>) _getValueCommand(string input, string command , string errorMessage = "") {

            char[] separators = new char[] { ' ', '.' };
            string[] words = input.Split(separators, StringSplitOptions.RemoveEmptyEntries);

            if (words[0] == command)
            {
                try
                {
                    Nullable<T> value = TryParse<T>(words[1]);
                    if (value.HasValue)
                    {
                        return (true, value.Value);
                    }
                    else
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine($"Error argument type for command  -> {command}");
                    }
                }
                catch (Exception ex)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine(errorMessage);
                }
            }
            return (false, null);
        }

         

        public static Nullable<T> TryParse<T>( String str) where T : struct
        {
            try
            {
                T parsedValue = (T)Convert.ChangeType(str, typeof(T));
                return parsedValue;
            }
            catch { return null; }
        }

    }


    class Program
    {

        private static string[] _commands_descriptions= { 

            "quit - exit program", 
            "stop - stop stream", 
            "play (url [index] - [1...N]. urls must be contains in file url.txt) - play audio stream ", 
            "save [(true or false)] - allow or disallow (create or save) audio files from thread stream", 
            "trackname [(true or false)] - (enable or disable) showing current track name from meta tags",
            "setvolume [0 to 1] - volume for audio floating point"
        };


        private static string[] _availableCommands = {

             "quit",
             "stop",
             "play",
             "save",
             "trackname",
             "setvolume",
             "help",
             "showurldata"
        };

        private static Thread _commandThread;

        static void Main(string[] args)
        {
            GrabberStream _grabberStream = new GrabberStream();
            _commandThread = new Thread(CommandHandler);
            _commandThread.Start(_grabberStream);

            if (_grabberStream._validateConfiguration())
            {

                Console.ForegroundColor = ConsoleColor.Green;

                _grabberStream.OnStreamChanged += _grabberStream_OnStreamChanged;







                string title = @"

                    Welcome to stream music grabber client v0.05 
                    Add url streams to url.txt 
                    Change configuration file (config.xml) to change volume or music save directory
                    Use 'help' for command information";

                Console.WriteLine(title);

                Console.ForegroundColor = ConsoleColor.Green;
                string logo = $@"
+---------------------------------------------------------------------------------------------------------------+
|    ________  ________  ________  ________  ________  _______   ________          ________  ________           |
|   |\   ____\|\   __  \|\   __  \|\   __  \|\   __  \|\  ___ \ |\   __  \        |\   __  \|\   __  \          |
|   \ \  \___|\ \  \|\  \ \  \|\  \ \  \|\ /\ \  \|\ /\ \   __/|\ \  \|\  \       \ \  \|\  \ \  \|\  \         |
|    \ \  \  __\ \   _  _\ \   __  \ \   __  \ \   __  \ \  \_|/_\ \   _  _\       \ \   __  \ \   __  \        |
|     \ \  \|\  \ \  \\  \\ \  \ \  \ \  \|\  \ \  \|\  \ \  \_|\ \ \  \\  \|       \ \  \|\  \ \  \|\  \       |
|      \ \_______\ \__\\ _\\ \__\ \__\ \_______\ \_______\ \_______\ \__\\ _\        \ \_______\ \_______\      |
|       \|_______|\|__|\|__|\|__|\|__|\|_______|\|_______|\|_______|\|__|\|__|        \|_______|\|_______|      |
|                                                                                                               |
+---------------------------------------------------------------------------------------------------------------+                                                                                                                                                                                                            
   ";

                Console.WriteLine(logo);
            }
        }

        private static void _grabberStream_OnStreamChanged(string message)
        {
            Console.WriteLine("Event is " + message);
        }

        private static void CommandHandler(object data)
        {
            bool _bQuit = false;
            char[] separators = new char[] { ' ', '.' };

            GrabberStream _gStream = (GrabberStream)data;

            while (!_bQuit)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                string cmd = Console.ReadLine().ToLower();
                string[] words = cmd.Split(separators, StringSplitOptions.RemoveEmptyEntries);

               
                if (words.Length > 0)
                {
                    if (_availableCommands.Where(x => x.Contains(words[0])).FirstOrDefault() != null)
                    {
                        if (words[0] == "quit")
                            _bQuit = true;


                        if (words[0] == "help")
                        {
                            Console.ForegroundColor = ConsoleColor.Yellow;
                            Console.WriteLine("+-----------------------------------GRABBER 88------------------------------------------------------+");
                            Console.WriteLine(String.Join("\n", _commands_descriptions));
                            Console.WriteLine("+---------------------------------------------------------------------------------------------------+");
                        }

                        if (words[0] == "stop")
                        {
                            _gStream.Stop();
                            Console.ForegroundColor = ConsoleColor.Green;
                            Console.WriteLine("Audio is stopped");
                        }

                        var play_cmd = CMDArgsAggregate<int>._getValueCommand(cmd, "play", "Error syntax: index parametr required");
                        var save_cmd = CMDArgsAggregate<bool>._getValueCommand(cmd, "save", "Error syntax: parametr (true or false) required");
                        var show_track_cmd = CMDArgsAggregate<bool>._getValueCommand(cmd, "trackname", "Error syntax: parametr (true or false) required");
                        var volume_cmd = CMDArgsAggregate<float>._getValueCommand(cmd, "setvolume", "Error syntax: parametr (true or false) required");

                        if (play_cmd.Item1)
                        {
                            Console.ForegroundColor = ConsoleColor.Green;


                            if (_gStream.PlayStream(play_cmd.Item2.Value))
                            {
                                Console.WriteLine("Loading... please stand by");
                            }
                               
                        }

                        if (save_cmd.Item1)
                        {
                            Console.ForegroundColor = ConsoleColor.Green;
                            _gStream.AllowSaveAudio = save_cmd.Item2.Value;
                            if (!_gStream.AllowSaveAudio)
                                Console.WriteLine("Save audio is disallowed");
                            else
                                Console.WriteLine("Save audio is allowed");

                        }

                        if (show_track_cmd.Item1)
                        {
                            Console.ForegroundColor = ConsoleColor.Green;
                            _gStream.AllowShowTrackName = show_track_cmd.Item2.Value;

                            if (!_gStream.AllowShowTrackName)
                                Console.WriteLine("Track name disabled");
                            else
                                Console.WriteLine("Track name enabled");

                        }
                        if (volume_cmd.Item1)
                        {
                            Console.ForegroundColor = ConsoleColor.Green;
                            _gStream.SetVolume(volume_cmd.Item2.Value);
                            Console.WriteLine($"Now volume is {_gStream.Volume}");
                        }
                    }
                    else
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine("Invalid command name");
                    }
                }
            }
        }




    }
}