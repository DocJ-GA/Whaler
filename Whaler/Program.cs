using System.Net.Http.Json;
using System.Text.Json;
using Whaler.Spoolman;
using CommandLine;


namespace Whaler
{
    internal class Program
    {
        protected static IList<WhaleTail> whaleTails = new List<WhaleTail>();
        protected static IList<Spool> spools = new List<Spool>();

        static void Main(string[] args)
        {
            var parsedArgs = Parser.Default.ParseArguments<Settings>(args);
            var settings = parsedArgs.Value;
            settings.WriteVerbose("Verbose mode active.");

            if (!String.IsNullOrWhiteSpace(settings.OrcaPath))
                whaleTails = GetTails(settings);

            if (!String.IsNullOrWhiteSpace(settings.SpoolApi))
                spools = GetSpools(settings);

            if (spools.Count == 0 && whaleTails.Count == 0)
            {
                settings.WriteLine("No tails or spools found.",
                    "Exiting.");
                settings.WaitForEnter();
                Environment.Exit(0);
            }

            if (settings.Archive)
                ArchiveEmptySpools(settings);

            if (!settings.Sync && !settings.Archive)
            {
                settings.WriteLine("No action specified.",
                    "Use --sync to sync spools and orca filaments, or --archive to archive empty spools.",
                    "Exiting.");
                settings.WaitForEnter();
                Environment.Exit(1);
            }

            if (settings.Sync)
                SyncSpools(settings);

        }

        /// <summary>
        ///  Syncs spools between spoolman and orca slicer.
        /// </summary>
        /// <param name="settings">The settings object.</param>
        public static void SyncSpools(Settings settings)
        {
            if (settings.OrcaPath == null)
            {
                settings.WriteLine("No OrcaSlicer path specified.",
                    "Use --orca-path to specify the path to the OrcaSlicer filament files.",
                    "Exiting.");
                settings.WaitForEnter();
                Environment.Exit(1);
            }

            if (settings.SpoolApi == null)
            {
                settings.WriteLine("No SpoolMan API path specified.",
                    "Use --spoolman-api to specify the path to the SpoolMan API.",
                    "Exiting.");
                settings.WaitForEnter();
                Environment.Exit(1);
            }

            if (spools.Count == 0)
            {
                settings.WriteLine("No spools found. Continuing will remove all filaments currently in orca slicer.");
                settings.CheckQuit();
            }

            settings.WriteLine("Comparing spools and whales.");
            foreach (var spool in spools)
            {
                settings.WriteVerbose("Checking for spool's whale tail.",
                    "Id: " + spool.Id + ".",
                    "Name: " + spool.Filament.Name);
                var match = whaleTails.FirstOrDefault(whaleTail => whaleTail.FilamentSettingsId.Length >= 1 && whaleTail.FilamentSettingsId[0] == spool.Id.ToString());
                spool.IsWhale = match != null;
                if (match != null)
                    match.Active = true;
            }

            settings.WriteLine("Removing dead whale tails.",
                "Removing dead whale tails.",
                "Hit List Count: " + whaleTails.Count(w => !w.Active));
            var killed = 0;
            foreach (var whaleTail in whaleTails.Where(w => !w.Active))
            {
                settings.WriteVerbose("Removing whale tail.",
                    "Name: " + whaleTail.Name + ".",
                    "Id: " + whaleTail.FilamentSettingsId[0] + ".");
                try
                {
                    File.Delete(whaleTail.CurrentFilePath!);
                }
                catch (Exception ex)
                {
                    settings.WriteLine("There was an error deleting the whale tail file.",
                        "File Path: '" + whaleTail.CurrentFilePath + "'.",
                        "Error Message: '" + ex.Message + "'.");
                }

                try
                {
                    File.Delete(whaleTail.GetInfoPath());
                }
                catch (Exception ex)
                {
                    settings.WriteLine("There was an error deleting the whale tail file.",
                        "File Path: '" + whaleTail.GetInfoPath() + "'.",
                        "Error Message: '" + ex.Message + "'.");
                }
                killed++;
            }
            settings.WriteLine("Whale tails killed.",
                "Kill count: " + killed + ".");

            Console.WriteLine("Addig new whale tails.");
            foreach (var spool in spools.Where(s => !s.IsWhale))
            {
                settings.WriteVerbose("Adding spool's whaletail.",
                    "Id: " + spool.Id + ".",
                    "Name: " + spool.Filament.Name);
                settings.WriteVerbose("Creating whale tail.");
                var whaleTail = spool.ToWhaleTail();
                File.WriteAllText(Path.Combine(settings.OrcaPath, whaleTail.GetJsonFileName()), JsonSerializer.Serialize(whaleTail, settings.SerializerOptions));
                settings.WriteVerbose("  .json file created.");
                File.WriteAllText(Path.Combine(settings.OrcaPath, whaleTail.GetInfoFileName()), whaleTail.GetInfoContent());
                settings.WriteVerbose(" .info file created.");
            }
        }

        /// <summary>
        /// Gets the <see cref="Spool"/>s fromt he specified SpoolMan API.
        /// </summary>
        /// <param name="settings">The settings object.</param>
        /// <returns>A list of <see cref="Spool"/>s.</returns>
        public static IList<Spool> GetSpools(Settings settings)
        {
            settings.WriteLine("Grabbing spools from SpoolMan.",
                "Url: '" + settings.SpoolApi + "spools'.");


            IList<Spool>? spools = Array.Empty<Spool>();
            var client = new HttpClient();
            try
            {
                spools = client.GetFromJsonAsync<IList<Spool>>(settings.SpoolApi + "spool", settings.SerializerOptions).Result;
            }
            catch (Exception ex)
            {
                settings.WriteLine("There was an error getting the spools from the SpoolMan API.",
                    "Error Message: '" + ex.Message + "'.");
                settings.WaitForEnter();
                Environment.Exit(3);
            }
            settings.WriteLine("Spools retrieved.",
                "Number: " + whaleTails.Count + ".");
            return spools ?? Array.Empty<Spool>();
        }

        /// <summary>
        /// Archives the empty spools in the SpoolMan API.
        /// </summary>
        /// <param name="settings">The settings.</param>
        public static void ArchiveEmptySpools(Settings settings)
        {
            var client = new HttpClient();
            spools ??= Array.Empty<Spool>();
            foreach (var spool in spools.Where(s => s.RemainingWeight == 0))
            {
                spool.Archived = true;
                var response = client.PatchAsJsonAsync(settings.SpoolApi + "spool/" + spool.Id, new { archived = true }, settings.SerializerOptions).Result;
                if (response.IsSuccessStatusCode)
                    settings.WriteLine("Spool archived.",
                        "Id: " + spool.Id + ".",
                        "Name: " + spool.Filament.Name);
                else
                    settings.WriteLine("There was an error archiving the spool.",
                        "Id: " + spool.Id + ".",
                        "Name: " + spool.Filament.Name,
                        "Error Type: '" + response.ReasonPhrase + "'.",
                        "Error Message: '" + response.Content.ReadAsStringAsync().Result);
            }
        }

        /// <summary>
        /// Gets the <see cref="WhaleTail"/>s fromt he specified directory.
        /// It will exit with code 2 if the directory does not exist.
        /// </summary>
        /// <param name="settings">The <see cref="Settings"/> to use.</param>
        /// <returns>An <see cref="IList{WhaleTail}"/> object.</returns>
        public static IList<WhaleTail> GetTails(Settings settings)
        {
            settings.WriteLine("Grabbing spools from OrcaSlicer.",
                "Path: '" + settings.OrcaPath + "'.");

            string[] whales = Array.Empty<string>();
            try
            {
                whales = Directory.GetFiles(settings.OrcaPath, "*.json");
            }
            catch (DirectoryNotFoundException ex)
            {
                settings.WriteLine("Directory for OrcaSlicer filaments was not found.",
                    "Directory: '" + settings.OrcaPath + "'.",
                    "Error Message: '" + ex.Message + '.');
                settings.WaitForEnter();
                Environment.Exit(2);
            }
            catch (IOException ex)
            {
                settings.WriteLine("There was an error reading the OrcaSlicer filament directory files.",
                    "There was an error reading the OrcaSlicer filament directory files." + Environment.NewLine +
                    "  Directory: '" + settings.OrcaPath + "'." + Environment.NewLine +
                    "  Error Message: '" + ex.Message + '.');
                settings.WaitForEnter();
                Environment.Exit(2);
            }
            catch (UnauthorizedAccessException ex)
            {
                settings.WriteLine("Accessing the directory for OrcaSlicer filaments was denied.  Check permissions.",
                    "Accessing the directory for OrcaSlicer filaments was denied.  Check permsions." + Environment.NewLine +
                    "  Directory: '" + settings.OrcaPath + "'." + Environment.NewLine +
                    "  Error Message: '" + ex.Message + '.');
                settings.WaitForEnter();
                Environment.Exit(2);
            }

            var whaleTails = new List<WhaleTail>();
            foreach (var tailPath in whales)
            {
                string fileText = String.Empty;
                try
                {
                    fileText = File.ReadAllText(tailPath);
                }
                catch (DirectoryNotFoundException ex)
                {
                    settings.WriteLine("Directory for OrcaSlicer filaments was not found.",
                        "Directory for OrcaSlicer filaments was not found." + Environment.NewLine +
                        "  Directory: '" + settings.OrcaPath + "'." + Environment.NewLine +
                        "  Error Message: '" + ex.Message + '.');
                    settings.WaitForEnter();
                    Environment.Exit(2);
                }
                catch (FileNotFoundException ex)
                {
                    settings.WriteLine("File for OrcaSlicer filament was not found.",
                        "Directory for OrcaSlicer filaments was not found." + Environment.NewLine +
                        "  File Path: '" + tailPath + "'." + Environment.NewLine +
                        "  Error Message: '" + ex.Message + '.');
                    continue;
                }
                catch (IOException ex)
                {
                    settings.WriteLine("There was an error reading the OrcaSlicer filament file.",
                        "There was an error reading the OrcaSlicer filament file." + Environment.NewLine +
                        "  File Path: '" + tailPath + "'." + Environment.NewLine +
                        "  Error Message: '" + ex.Message + '.');
                    continue;
                }
                catch (UnauthorizedAccessException ex)
                {
                    settings.WriteLine("Accessing the directory for OrcaSlicer filaments was denied.  Check permissions.",
                        "Accessing the directory for OrcaSlicer filaments was denied.  Check permsions." + Environment.NewLine +
                        "  FilePath: '" + tailPath + "'." + Environment.NewLine +
                        "  Error Message: '" + ex.Message + '.');
                    continue;
                }
                var tail = JsonSerializer.Deserialize<WhaleTail>(fileText, settings.SerializerOptions);
                if (tail != null)
                    whaleTails.Add(tail.SetCurrentPath(tailPath));
            }
            settings.WriteLine("Whale Tails retrieved.",
                "Number: " + whaleTails.Count + ".");

            return whaleTails;
        }
    }
}