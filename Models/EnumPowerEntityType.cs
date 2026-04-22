using System;
using System.Collections.Generic;

namespace MatinPower.Server.Models;

public partial class EnumPowerEntityType
{
    public int Id { get; set; }

    public string? Title { get; set; }

    public virtual ICollection<PowerEntity> PowerEntities { get; set; } = new List<PowerEntity>();
}
