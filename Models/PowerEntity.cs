using System;
using System.Collections.Generic;

namespace MatinPower.Server.Models;

public partial class PowerEntity
{
    public int Id { get; set; }

    public int ProvinceId { get; set; }

    public string Name { get; set; } = null!;

    public int EntityTypeId { get; set; }

    public bool? IsActive { get; set; }

    public virtual ICollection<Address> Addresses { get; set; } = new List<Address>();

    public virtual EnumPowerEntityType EntityType { get; set; } = null!;

    public virtual Province Province { get; set; } = null!;

    public virtual ICollection<Tariff> Tariffs { get; set; } = new List<Tariff>();

    public virtual ICollection<Touschedule> Touschedules { get; set; } = new List<Touschedule>();
}
