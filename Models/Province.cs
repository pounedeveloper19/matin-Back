using System;
using System.Collections.Generic;

namespace MatinPower.Server.Models;

public partial class Province
{
    public int Id { get; set; }

    public string Name { get; set; } = null!;

    public virtual ICollection<City> Cities { get; set; } = new List<City>();

    public virtual ICollection<PowerEntity> PowerEntities { get; set; } = new List<PowerEntity>();
}
