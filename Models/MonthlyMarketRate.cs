namespace MatinPower.Server.Models;

public partial class MonthlyMarketRate
{
    public int Id { get; set; }
    public int Year { get; set; }
    public int Month { get; set; }

    // متوسط و حداکثر قیمت بازار برق (ریال/kWh)
    public decimal? MarketAvg  { get; set; }   // متوسط قیمت بازار برق
    public decimal? MarketPeak { get; set; }   // حداکثر - اوج بار
    public decimal? MarketMid  { get; set; }   // حداکثر - میان بار
    public decimal? MarketLow  { get; set; }   // حداکثر - کم بار

    // متوسط قیمت تابلوی اول بورس (ریال/kWh)
    public decimal? BoardPeak { get; set; }
    public decimal? BoardMid  { get; set; }
    public decimal? BoardLow  { get; set; }

    // تابلوهای سبز و آزاد — یک مقدار کافیه (مقدار یکسان برای همه بازه‌ها)
    public decimal? GreenBoardRate { get; set; }  // متوسط تابلوی سبز بورس
    public decimal? OpenBoardRate  { get; set; }  // متوسط تابلوی آزاد بورس

    // نرخ برق تجدیدپذیر (قانون جهش تولید) (ریال/kWh)
    public decimal? IndustrialTariffBase { get; set; }  // تعرفه مشترکین صنعتی
    public decimal? ExecutiveTariffBase  { get; set; }  // تعرفه دستگاه‌های اجرایی
}
