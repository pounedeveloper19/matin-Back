using System.ComponentModel.DataAnnotations.Schema;

namespace MatinPower.Server.Models
{
    public partial class CustomersReal
    {
        [NotMapped]
        public bool? IsActive { get; set; }

        [NotMapped]
        public int? FamiliarityType { get; set; }

        [NotMapped]
        public int? CustomerTypeId { get; set; }

        [NotMapped]
        public string? Password { get; set; }
    }

    public partial class CustomersLegal
    {
        [NotMapped]
        public bool? IsActive { get; set; }

        [NotMapped]
        public int? FamiliarityType { get; set; }

        [NotMapped]
        public int? CustomerTypeId { get; set; }

        [NotMapped]
        public string? AgentFullName { get; set; }

        [NotMapped]
        public string? AgentMobile { get; set; }

        [NotMapped]
        public string? Password { get; set; }
    }
}
