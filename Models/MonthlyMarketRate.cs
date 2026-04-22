using System;
using System.Collections.Generic;

namespace MatinPower.Server.Models;

public partial class MonthlyMarketRate
{
    public int Id { get; set; }

    public int Year { get; set; }

    public int Month { get; set; }

    public decimal? MarketPeak { get; set; }

    public decimal? MarketMid { get; set; }

    public decimal? MarketLow { get; set; }

    public decimal? BoardPeak { get; set; }

    public decimal? BoardMid { get; set; }

    public decimal? BoardLow { get; set; }

    public decimal? GreenBoardRate { get; set; }

    public decimal? BackupRate { get; set; }

    public decimal? Article16Rate { get; set; }

    public decimal? FuelFee { get; set; }

    public decimal? IndustrialTariffBase { get; set; }

    public decimal? ExecutiveTariffBase { get; set; }
}
