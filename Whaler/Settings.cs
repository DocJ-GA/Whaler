using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using CommandLine;

namespace Whaler
{
    /// <summary>
    /// Holds settings for the running program
    /// </summary>
    public class Settings
    {
        protected bool _snakeCaseLower, _snakeCaseUpper;

        /// <summary>
        /// True if the prompt should be displayed
        /// </summary>
        [Option('N', "no-prompt", Default = false, HelpText = "Set if the console app should look for user input.")]
        public bool NoPrompt { get; set; }
        
        /// <summary>
        /// The path to the orca filament files.
        /// </summary>
        [Option('o', "orca-path", Default = null, HelpText = "The path to the OrcaSlicer filament files.")]
        public string? OrcaPath { get; set; }
        
        /// <summary>
        /// The API url for the SpoolMan instance.
        /// </summary>
        [Option('s', "spoolman-api", Default = null, HelpText = "The api url for spoolman instance.")]
        public string? SpoolApi { get; set; }

        /// <summary>
        /// True if spoolman should sync to orca.
        /// </summary>
        [Option('S', "sync", Default = false, HelpText = "Sync spools to orca.")]
        public bool Sync { get; set; }

        /// <summary>
        /// True if spoolman used spools should be archived
        /// </summary>
        [Option('a', "archive", Default = false, HelpText = "Archive completely used spools in spoolman.")]
        public bool Archive { get; set; }

        /// <summary>
        /// True if verbosity should be used.
        /// </summary>
        [Option('v', "verbose", Default = false, HelpText = "Set if you want it to be verbose.")]
        public bool Verbose { get; set; }

        /// <summary>
        /// True if you want the CLI to be silent.
        /// </summary>
        [Option('q', "silent", Default = false, HelpText = "Set if you want the CLI to be silent.")]
        public bool Silent { get; set; }

        /// <summary>
        /// True for snake case lower.
        /// </summary>
        [Option('c', "snake-case-lower", Default = false, HelpText = "Set if the Json filament files use Snake Case Lower.")]
        public bool SnakeCaseLower
        {
            get => _snakeCaseLower;
            set
            {
                if (value)
                {
                    ClearJsonNaming();
                    _snakeCaseLower = true;
                }
            }
        }

        /// <summary>
        /// True for snake case upper.
        /// </summary>
        [Option('C', "snake-case-upper", Default = false, HelpText = "Set if the Json filament files use Snake Case Upper.")]
        public bool SnakeCaseUpper
        {
            get => _snakeCaseUpper;
            set
            {
                if (value)
                {
                    ClearJsonNaming();
                    _snakeCaseUpper = true;
                }
            }
        }


        public JsonSerializerOptions SerializerOptions
        {
            get
            {
                var options = new JsonSerializerOptions()
                {
                    WriteIndented = true,
                };
                if (SnakeCaseLower)
                    options.PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower;
                else if (SnakeCaseUpper)
                    options.PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseUpper;
                else
                    options.PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower;
                return options;
            }
        }

        /// <summary>
        /// Default contstructor.
        /// </summary>
        public Settings()
        {
            OrcaPath = String.Empty;
            SpoolApi = String.Empty;
            SnakeCaseLower = true;
        }

        protected void ClearJsonNaming()
        {
            _snakeCaseLower = false;
            _snakeCaseUpper = false;
        }

        #region Methods

        /// <summary>
        /// Write a message to the console screen.
        /// </summary>
        /// <param name="message">The message to display.</param>
        /// <param name="verboseMessage">The message to display if in verbose mode.  Uses <paramref name="message"/> if it is not set.  Optional parameter./param>
        public void WriteLine(string message, params string[] args)
        {
            if (Silent)
                return;

            var verbose = message + String.Join(Environment.NewLine + "  ", args);

            if (Verbose)
                Console.WriteLine(verbose ?? message);
            else
                Console.WriteLine(message);
        }

        /// <summary>
        /// Write a message to the console screen.
        /// </summary>
        /// <param name="message">The message to display.</param>
        /// <param name="verboseMessage">The message to display if in verbose mode.  Uses <paramref name="message"/> if it is not set.  Optional parameter./param>
        public void WriteVerbose(string message, params string[] args)
        {
            if (Silent)
                return;

            Console.WriteLine(message + String.Join(Environment.NewLine + "  ", args));
        }

        /// <summary>
        /// Waits until user hits enter unless we are running in silent or no prompt mode.
        /// </summary>
        public string? WaitForEnter()
        {
            if (Silent || NoPrompt)
                return null;

            Console.WriteLine("Press ENTER to continue: ");
            return Console.ReadLine()?.Trim();
        }

        /// <summary>
        /// Checks if the user wants to quit the program.
        /// </summary>
        public void CheckQuit()
        {
            if (Silent || NoPrompt)
                Environment.Exit(1);
            var response = WaitForEnter();
            if (response == null)
                return;
            if (response.ToLower() == "q")
                Environment.Exit(0);
        }

        #endregion
    }
}
