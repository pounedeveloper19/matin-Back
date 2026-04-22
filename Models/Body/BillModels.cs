namespace MatinPower.Server.Models.Body
{
    public class ManualBillAnalysisRequest
    {
        public int SubscriptionId { get; set; }
        public int Year { get; set; }    // Jalali year e.g. 1404
        public int Month { get; set; }   // Jalali month 1-12
        public decimal PeakKwh { get; set; }
        public decimal MidKwh { get; set; }
        public decimal LowKwh { get; set; }
        public decimal FridayPeakKwh { get; set; }
    }

    public class BillAnalysisBand
    {
        public string Name { get; set; }
        public decimal ActualKwh { get; set; }
        public decimal ContractedKwh { get; set; }
        public decimal ExcessKwh { get; set; }
        public decimal DeficitKwh { get; set; }
        public decimal MarketRateRial { get; set; }
        public decimal PenaltyRial { get; set; }
        public decimal CreditRial { get; set; }
    }

    public class BillAnalysisResult
    {
        public string MonthName { get; set; }
        public int Year { get; set; }
        public decimal TotalConsumption { get; set; }
        public decimal ContractCapacityKw { get; set; }
        public decimal ContractedEnergyKwh { get; set; }
        public decimal ContractRateRialPerKwh { get; set; }
        public int PeakHoursPerDay { get; set; }
        public int MidHoursPerDay { get; set; }
        public int LowHoursPerDay { get; set; }
        public List<BillAnalysisBand> Bands { get; set; }
        public decimal MarketPeakRate { get; set; }
        public decimal MarketMidRate { get; set; }
        public decimal MarketLowRate { get; set; }
        public decimal BackupRate { get; set; }
        public decimal TotalDifferentialRial { get; set; }
        public decimal TotalCreditRial { get; set; }
        public decimal Article16Rial { get; set; }
        public decimal FuelFeeRial { get; set; }
        public decimal MatinBillRial { get; set; }
        public decimal WithoutMatinBillRial { get; set; }
        public decimal WithMatinBillRial { get; set; }
        public decimal SavingRial { get; set; }
        public decimal SavingPercent { get; set; }
    }
}
