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
        [Option('o', "orca-path", Required = true, HelpText = "The path to the OrcaSlicer filament files.")]
        public string OrcaPath { get; set; }
        
        /// <summary>
        /// The API url for the SpoolMan instance.
        /// </summary>
        [Option('s', "spoolman-api", Required = true, HelpText = "The api url for spoolman instance.")]
        public string SpoolApi { get; set; }
        
        /// <summary>
        /// True if verbosity should be used.
        /// </summary>
        [Option('v', "verbose", Default = false, HelpText = "Set if you want it to be verbose.")]
        public bool Verbose { get; set; }

        [Option('s', "silent", Default = false, HelpText = "Set if you want the CLI to be silent.")]
        public bool Silent { get; set; }

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
                var options = new JsonSerializerOptions();
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
        public void WriteLine(string message, string? verboseMessage = null)
        {
            if (Silent)
                return;

            if (Verbose)
                Console.WriteLine(verboseMessage ?? message);
            else
                Console.WriteLine(message);
        }

        /// <summary>
        /// Write a message to the console screen.
        /// </summary>
        /// <param name="message">The message to display.</param>
        /// <param name="verboseMessage">The message to display if in verbose mode.  Uses <paramref name="message"/> if it is not set.  Optional parameter./param>
        public void WriteVerbose(string message)
        {
            if (Silent)
                return;

            Console.WriteLine(message);
        }


        public void WaitForEnter()
        {
            if (Silent || NoPrompt)
                return;

            Console.WriteLine("Press ENTER to continue: ");
            Console.ReadLine();
        }

        #endregion
    }
}
