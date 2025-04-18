using System.ComponentModel.DataAnnotations.Schema;

namespace Whaler.Spoolman
{
    public class Spool
    {
        float? _price, _spoolWeight;
        public int Id { get; set; }
        public DateTime Registered { get; set; }
        public DateTime? FirstUsed { get; set; }
        public DateTime? LastUsed { get; set; }
        public Filament Filament { get; set; } = null!;
        public float? Price
        {
            get
            {
                if (_price == null)
                    return Filament.Price;
                return _price;
            }
            set
            {
                _price = value;
            }
        }
        public float? RemainingWeight { get; set; }
        public float? InitialWeight { get; set; }
        public float? SpoolWeight { get; set; }
        public float UsedWeight { get; set; }
        public float? RemainingLength { get; set; }
        public float UsedLength { get; set; }
        public string? Location { get; set; }
        public string? LotNr { get; set; }
        public string? Comment { get; set; }
        public bool Archived { get; set; }
        public Extra Extra { get; set; } = new Extra();

        [NotMapped]
        public bool IsWhale { get; set; } = false;

    }
}
