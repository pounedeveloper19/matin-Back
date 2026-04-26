using System;
using System.Collections.Generic;

namespace MatinPower.Server.Models;

public partial class Tariff
{
    public int TariffId { get; set; }

    public int TariffTypeId { get; set; }

    public int CustomerTypeId { get; set; }

    public int PowerEntitiesId { get; set; }

    public DateTime EffectiveFrom { get; set; }

    public virtual EnumCustomerType CustomerType { get; set; } = null!;

    public virtual PowerEntity PowerEntities { get; set; } = null!;

    public virtual ICollection<TariffSlab> TariffSlabs { get; set; } = new List<TariffSlab>();

    public virtual EnumTariffType TariffType { get; set; } = null!;
}
