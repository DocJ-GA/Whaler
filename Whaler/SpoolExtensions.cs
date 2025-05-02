using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Whaler.Spoolman;

namespace Whaler
{
    public static class SpoolExtensions
    {
        public static WhaleTail ToWhaleTail(this Spool spool)
        {
            var name = string.Format("#{0:D4} - {1} - {2}", spool.Id, spool.Filament.Name, spool.Filament.Vendor?.Name ?? "Generic");
            var whale = new WhaleTail()
            {
                FilamentCost = [spool.Price.ToString() ?? "18"],
                FilamentSettingsId = [spool.Id.ToString()],
                FilamentVendor = [spool.Filament.Vendor?.Name ?? "Generic"],
                Name = name,
                FilamentType = [spool.Filament.FilamentType.GetTypeName()],
                FilamentDensity = [spool.Filament.Density.ToString()],
                FilamentDiameter = [spool.Filament.Diameter.ToString()],
                FilamentSpoolWeight = [spool.Filament.SpoolWeight?.ToString() ?? "1000"],
                TexturedPlateTemp = [spool.Filament.SettingsBedTemp?.ToString() ?? "55"],
                Inherits = spool.Filament.FilamentType.GetInheritName(),
                NozzleTemperature = [spool.Filament.SettingsExtruderTemp?.ToString() ?? "210"]
            };
            whale.FilamentStartGcode[0] += spool.Id.ToString() + Environment.NewLine + "FILAMENT_OFFSET FILAMENT_TYPE=\"" + spool.Filament.FilamentType.GetTypeName() + "\"";
            whale.FilamentFlowRatio = [spool.Filament.Extra.FlowRatio ?? "0.98"];


            if (spool.Filament.ColorHexes.Count > 0)
                whale.DefaultFilamentColour = spool.Filament.ColorHexes.Select(c => "#" + c).ToArray();
            else
                whale.DefaultFilamentColour = [(spool.Filament.ColorHex != null ? "#" + spool.Filament.ColorHex : "#000000")];

            return whale;
        }
    }
}
