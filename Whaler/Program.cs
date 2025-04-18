using System.Net.Http.Json;
using System.Text.Json;
using Whaler.Spoolman;
using CommandLine;


namespace Whaler
{
    internal class Program
    {
        static void Main(string[] args)
        {
            var parsedArgs = Parser.Default.ParseArguments<Settings>(args);
            var settings = parsedArgs.Value;
            Console.WriteLine(settings.SnakeCaseLower + " " + settings.SnakeCaseUpper);
            settings.WriteVerbose("Verbose mode active.");
            settings.WriteLine("Grabbing spools from OrcaSlicer.", "Grabbing spools from OrcaSlicer." + Environment.NewLine +
                " Path: '" + settings.OrcaPath + "'.");
            var whaleTails = GetTails(settings);
            settings.WriteLine("Whale Tails retrieved.", "Whale Tails retrieved." + Environment.NewLine +
                " Number: " + whaleTails.Count + ".");

            settings.WriteLine("Grabbing spools from SpoolMan.", "Grabbing spools from SpoolMan." + Environment.NewLine +
                " Url: '" + settings.SpoolApi + "spools'.");
            var spools = GetSpools(settings);
            settings.WriteLine("Spools retrieved.", "Spools retrieved." + Environment.NewLine +
                "  Number: " + whaleTails.Count + ".");

            settings.WriteLine("Comparing spools and whales.");
            foreach (var spool in spools)
            {
                settings.WriteVerbose("Checking for spool's whaletail." + Environment.NewLine +
                    "  Id: " + spool.Id + "." + Environment.NewLine +
                    "  Name: " + spool.Filament.Name);
                var match = whaleTails.FirstOrDefault(whaleTail => whaleTail.FilamentSettingsId.Length >= 1 && whaleTail.FilamentSettingsId[0] == spool.Id.ToString());
                spool.IsWhale = match != null;
                if (match != null)
                    match.Active = true;
            }

            settings.WriteLine("Removing dead whale tails.",
                "Removing dead whale tails." + Environment.NewLine +
                "  Hit List Count: " + whaleTails.Count(w => !w.Active));
            var killed = 0;
            foreach (var whaleTail in whaleTails.Where(w => !w.Active))
            {
                settings.WriteVerbose("Removing whale tail." + Environment.NewLine +
                    "  Name: " + whaleTail.Name + "." + Environment.NewLine +
                    "  Id: " + whaleTail.FilamentSettingsId[0] + ".");
                try
                {
                    File.Delete(whaleTail.CurrentFilePath!);
                }
                catch (Exception ex)
                {
                    settings.WriteLine("There was an error deleting the whale tail file.",
                        "There was an error deleting the whale tail file." + Environment.NewLine +
                        "  File Path: '" + whaleTail.CurrentFilePath + "'." + Environment.NewLine +
                        "  Error Message: '" + ex.Message + "'.");
                }

                try
                {
                    File.Delete(whaleTail.GetInfoPath());
                }
                catch (Exception ex)
                {
                    settings.WriteLine("There was an error deleting the whale tail file.",
                        "There was an error deleting the whale tail file." + Environment.NewLine +
                        "  File Path: '" + whaleTail.GetInfoPath() + "'." + Environment.NewLine +
                        "  Error Message: '" + ex.Message + "'.");
                }
                killed++;
            }
            settings.WriteLine("Whale tails killed.",
                "Whale tails killed." + Environment.NewLine +
                "  Kill count: " + killed + ".");

            Console.WriteLine("Addig new whale tails.");
            foreach (var spool in spools.Where(s => !s.IsWhale))
            {
                settings.WriteVerbose("Adding spool's whaletail." + Environment.NewLine +
                    "  Id: " + spool.Id + "." + Environment.NewLine +
                    "  Name: " + spool.Filament.Name);
                settings.WriteVerbose("Creating whale tail.");
                var whaleTail = spool.ToWhaleTail();
                File.WriteAllText(Path.Combine(settings.OrcaPath, whaleTail.GetJsonFileName()), JsonSerializer.Serialize(whaleTail, settings.SerializerOptions));
                settings.WriteVerbose("  .json file created.");
                File.WriteAllText(Path.Combine(settings.OrcaPath, whaleTail.GetInfoFileName()), whaleTail.GetInfoContent());
                settings.WriteVerbose(" .info file created.");
            }
        }

        public static IList<Spool> GetSpools(Settings settings)
        {
            IList<Spool>? spools = Array.Empty<Spool>();
            var client = new HttpClient();
            try
            {
                spools = client.GetFromJsonAsync<IList<Spool>>(settings.SpoolApi + "spool", settings.SerializerOptions).Result;
            }
            catch (Exception ex)
            {
                settings.WriteLine("There was an error getting the spools from the SpoolMan API.",
                    "There was an error getting the spools from the SpoolMan API." + Environment.NewLine +
                    "  Error Message: '" + ex.Message + "'.");
                Environment.Exit(3);
            }
            return spools ?? Array.Empty<Spool>();
        }

        /// <summary>
        /// Gets the <see cref="WhaleTail"/>s fromt he specified directory.
        /// It will exit with code 2 if the directory does not exist.
        /// </summary>
        /// <param name="settings">The <see cref="Settings"/> to use.</param>
        /// <returns>An <see cref="IList{WhaleTail}"/> object.</returns>
        public static IList<WhaleTail> GetTails(Settings settings)
        {
            string[] whales = Array.Empty<string>();
            try
            {
                whales = Directory.GetFiles(settings.OrcaPath, "*.json");
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
            return whaleTails;
        }
    }
}