using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace MatinPower.Server.Models;

public partial class MatinPowerDbContext : DbContext
{
    public MatinPowerDbContext()
    {
    }

    public MatinPowerDbContext(DbContextOptions<MatinPowerDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Address> Addresses { get; set; }

    public virtual DbSet<Announcement> Announcements { get; set; }

    public virtual DbSet<Bill> Bills { get; set; }

    public virtual DbSet<BillAnalysisReport> BillAnalysisReports { get; set; }

    public virtual DbSet<BillUsingByTou> BillUsingByTous { get; set; }

    public virtual DbSet<City> Cities { get; set; }

    public virtual DbSet<Contract> Contracts { get; set; }

    public virtual DbSet<CustomerProfile> CustomerProfiles { get; set; }

    public virtual DbSet<CustomersLegal> CustomersLegals { get; set; }

    public virtual DbSet<CustomersReal> CustomersReals { get; set; }

    public virtual DbSet<ElectricityOrder> ElectricityOrders { get; set; }

    public virtual DbSet<EnumContractStatus> EnumContractStatuses { get; set; }

    public virtual DbSet<EnumCustomerType> EnumCustomerTypes { get; set; }

    public virtual DbSet<EnumEnergyType> EnumEnergyTypes { get; set; }

    public virtual DbSet<EnumFamiliarityType> EnumFamiliarityTypes { get; set; }

    public virtual DbSet<EnumGuaranteeType> EnumGuaranteeTypes { get; set; }

    public virtual DbSet<EnumOrderStatus> EnumOrderStatuses { get; set; }

    public virtual DbSet<EnumPaymentMethod> EnumPaymentMethods { get; set; }

    public virtual DbSet<EnumPaymentStatus> EnumPaymentStatuses { get; set; }

    public virtual DbSet<EnumPowerEntityType> EnumPowerEntityTypes { get; set; }

    public virtual DbSet<EnumTariffType> EnumTariffTypes { get; set; }

    public virtual DbSet<EnumTicketStatus> EnumTicketStatuses { get; set; }

    public virtual DbSet<EnumToutype> EnumToutypes { get; set; }

    public virtual DbSet<EnumUserRole> EnumUserRoles { get; set; }

    public virtual DbSet<MonthlyMarketRate> MonthlyMarketRates { get; set; }

    public virtual DbSet<OptimumPurchaseSuggestion> OptimumPurchaseSuggestions { get; set; }

    public virtual DbSet<Payment> Payments { get; set; }

    public virtual DbSet<PowerEntity> PowerEntities { get; set; }

    public virtual DbSet<Province> Provinces { get; set; }

    public virtual DbSet<Role> Roles { get; set; }

    public virtual DbSet<SiteMap> SiteMaps { get; set; }

    public virtual DbSet<SiteMapRole> SiteMapRoles { get; set; }

    public virtual DbSet<Subscription> Subscriptions { get; set; }

    public virtual DbSet<Tariff> Tariffs { get; set; }

    public virtual DbSet<TariffSlab> TariffSlabs { get; set; }

    public virtual DbSet<Ticket> Tickets { get; set; }

    public virtual DbSet<TicketMessage> TicketMessages { get; set; }

    public virtual DbSet<Touschedule> Touschedules { get; set; }

    public virtual DbSet<User> Users { get; set; }

    public virtual DbSet<UserRole> UserRoles { get; set; }

    public virtual DbSet<Warranty> Warranties { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (!optionsBuilder.IsConfigured)
        {
            // Fallback: read from appsettings.json when DI is not available (e.g. EF migrations)
            var configuration = new ConfigurationBuilder()
                .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
                .AddJsonFile("appsettings.json", optional: false)
                .Build();

            optionsBuilder.UseSqlServer(
                configuration.GetConnectionString("SqlServerConnection"),
                sql => sql.CommandTimeout(60));
        }
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Address>(entity =>
        {
            entity.ToTable("Address");

            entity.Property(e => e.MainAddress)
                .HasMaxLength(250)
                .HasColumnName("MainAddress");
            entity.Property(e => e.PostalCode).HasMaxLength(10);

            entity.HasOne(d => d.City).WithMany(p => p.Addresses)
                .HasForeignKey(d => d.CityId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Address_City");

            entity.HasOne(d => d.CustomerProfile).WithMany(p => p.Addresses)
                .HasForeignKey(d => d.CustomerProfileId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Address_CustomerProfiles");

            entity.HasOne(d => d.PowerEntity).WithMany(p => p.Addresses)
                .HasForeignKey(d => d.PowerEntityId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Address_PowerEntities");
        });

        modelBuilder.Entity<Announcement>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Announce__3214EC07987F91D6");

            entity.Property(e => e.PublishDate).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.Title).HasMaxLength(500);
        });

        modelBuilder.Entity<Bill>(entity =>
        {
            entity.HasKey(e => e.BillId).HasName("PK__Bills__11F2FC6A6E876790");

            entity.Property(e => e.BillNumber).HasMaxLength(50);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.TotalAmount).HasColumnType("decimal(18, 2)");

            entity.HasOne(d => d.Subscription).WithMany(p => p.Bills)
                .HasForeignKey(d => d.SubscriptionId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Bills__Subscript__2BFE89A6");
        });

        modelBuilder.Entity<BillAnalysisReport>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__BillAnal__3214EC07A20D5C04");

            entity.Property(e => e.CostWithMatin).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.CostWithoutMatin).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.LowCons).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.MidCons).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.NetSaving).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.PeakCons).HasColumnType("decimal(18, 2)");

            entity.HasOne(d => d.Subscription).WithMany(p => p.BillAnalysisReports)
                .HasForeignKey(d => d.SubscriptionId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__BillAnaly__Subsc__0A9D95DB");
        });

        modelBuilder.Entity<BillUsingByTou>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__BillCons__3214EC0749BE676F");

            entity.ToTable("BillUsingByTOU");

            entity.Property(e => e.ToutypeId).HasColumnName("TOUTypeId");

            entity.HasOne(d => d.Bill).WithMany(p => p.BillUsingByTous)
                .HasForeignKey(d => d.BillId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__BillConsu__BillI__44CA3770");

            entity.HasOne(d => d.Toutype).WithMany(p => p.BillUsingByTous)
                .HasForeignKey(d => d.ToutypeId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__BillConsu__TOUTy__45BE5BA9");
        });

        modelBuilder.Entity<City>(entity =>
        {
            entity.ToTable("City");

            entity.Property(e => e.Title).HasMaxLength(150);

            entity.HasOne(d => d.Province).WithMany(p => p.Cities)
                .HasForeignKey(d => d.ProvinceId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_City_Provinces");
        });

        modelBuilder.Entity<Contract>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Contract__3214EC077254856C");

            entity.HasIndex(e => e.ContractNumber, "UQ__Contract__C51D43DA113E7F5A").IsUnique();

            entity.Property(e => e.ContractNumber).HasMaxLength(100);
            entity.Property(e => e.ContractRate).HasColumnType("decimal(18, 4)");

            entity.HasOne(d => d.Status).WithMany(p => p.Contracts)
                .HasForeignKey(d => d.StatusId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Contracts__Statu__03F0984C");

            entity.HasOne(d => d.Subscription).WithMany(p => p.Contracts)
                .HasForeignKey(d => d.SubscriptionId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Contracts__Subsc__02FC7413");
        });

        modelBuilder.Entity<CustomerProfile>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Customer__3214EC078F551B0E");

            entity.Property(e => e.IsActive).HasDefaultValue(true);

            entity.HasOne(d => d.CustomerType).WithMany(p => p.CustomerProfiles)
                .HasForeignKey(d => d.CustomerTypeId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__CustomerP__Custo__4CA06362");

            entity.HasOne(d => d.FamiliarityTypeNavigation).WithMany(p => p.CustomerProfiles)
                .HasForeignKey(d => d.FamiliarityType)
                .HasConstraintName("FK_CustomerProfiles_EnumFamiliarityType");
        });

        modelBuilder.Entity<CustomersLegal>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Customer__3214EC075EFECAA8");

            entity.ToTable("Customers_Legal");

            entity.HasIndex(e => e.NationalId, "UQ__Customer__E9AA32FADC18EAC3").IsUnique();

            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.CeoFullName)
                .HasMaxLength(100)
                .HasColumnName("CEO_FullName");
            entity.Property(e => e.CeoMobile)
                .HasMaxLength(11)
                .HasColumnName("CEO_Mobile");
            entity.Property(e => e.CompanyName).HasMaxLength(200);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.EconomicCode).HasMaxLength(20);
            entity.Property(e => e.NationalId).HasMaxLength(12);

            entity.HasOne(d => d.IdNavigation).WithOne(p => p.CustomersLegal)
                .HasForeignKey<CustomersLegal>(d => d.Id)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Customers_Le__Id__5629CD9C");
        });

        modelBuilder.Entity<CustomersReal>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Customer__3214EC072DEF967E");

            entity.ToTable("Customers_Real");

            entity.HasIndex(e => e.NationalCode, "UQ__Customer__3DFA4106DDBFA884").IsUnique();

            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.FirstName).HasMaxLength(150);
            entity.Property(e => e.LastName).HasMaxLength(150);
            entity.Property(e => e.Mobile).HasMaxLength(11);
            entity.Property(e => e.NationalCode).HasMaxLength(10);

            entity.HasOne(d => d.IdNavigation).WithOne(p => p.CustomersReal)
                .HasForeignKey<CustomersReal>(d => d.Id)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Customers_Re__Id__5165187F");
        });

        modelBuilder.Entity<ElectricityOrder>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Electric__3214EC0781034BEE");

            entity.Property(e => e.IsPriceRequest).HasDefaultValue(true);
            entity.Property(e => e.OrderDate).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.PriceAtMoment).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.RequestedKwh).HasColumnType("decimal(18, 2)");

            entity.HasOne(d => d.Bill).WithMany(p => p.ElectricityOrders)
                .HasForeignKey(d => d.BillId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ElectricityOrders_Bills");

            entity.HasOne(d => d.EnergyType).WithMany(p => p.ElectricityOrders)
                .HasForeignKey(d => d.EnergyTypeId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Electrici__Energ__778AC167");

            entity.HasOne(d => d.Status).WithMany(p => p.ElectricityOrders)
                .HasForeignKey(d => d.StatusId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Electrici__Statu__787EE5A0");

            entity.HasOne(d => d.User).WithMany(p => p.ElectricityOrders)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Electrici__UserI__76969D2E");
        });

        modelBuilder.Entity<EnumContractStatus>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__EnumCont__3214EC07807E662F");

            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.Title).HasMaxLength(100);
        });

        modelBuilder.Entity<EnumCustomerType>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__EnumCust__3214EC07D2D6662F");

            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.Title).HasMaxLength(100);
        });

        modelBuilder.Entity<EnumEnergyType>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__EnumEner__3214EC0705AEEC8D");

            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.Title).HasMaxLength(100);
        });

        modelBuilder.Entity<EnumFamiliarityType>(entity =>
        {
            entity.ToTable("EnumFamiliarityType");

            entity.Property(e => e.Title).HasMaxLength(50);
        });

        modelBuilder.Entity<EnumGuaranteeType>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__EnumGuar__3214EC07EBBF2693");

            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.Title).HasMaxLength(100);
        });

        modelBuilder.Entity<EnumOrderStatus>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__EnumOrde__3214EC07C5552918");

            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.Title).HasMaxLength(100);
        });

        modelBuilder.Entity<EnumPaymentMethod>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__EnumPaym__3214EC0753E30FFD");

            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.Title).HasMaxLength(100);
        });

        modelBuilder.Entity<EnumPaymentStatus>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__EnumPaym__3214EC071ACFB9A7");

            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.Title).HasMaxLength(100);
        });

        modelBuilder.Entity<EnumPowerEntityType>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__EnumPowe__3214EC0726221793");

            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.Title).HasMaxLength(100);
        });

        modelBuilder.Entity<EnumTariffType>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__EnumTari__3214EC07D2225658");

            entity.ToTable("EnumTariffType");

            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.Title).HasMaxLength(100);
        });

        modelBuilder.Entity<EnumTicketStatus>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__EnumTick__3214EC071802DE28");

            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.Title).HasMaxLength(100);
        });

        modelBuilder.Entity<EnumToutype>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__EnumTOUT__3214EC07C9C6DF4A");

            entity.ToTable("EnumTOUTypes");

            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.Title).HasMaxLength(100);
        });

        modelBuilder.Entity<EnumUserRole>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__EnumUser__3214EC072768E364");

            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.Title).HasMaxLength(100);
        });

        modelBuilder.Entity<MonthlyMarketRate>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__MonthlyM__3214EC07E68F40E5");

            entity.HasIndex(e => new { e.Year, e.Month }, "UC_MonthlyRates_YM").IsUnique();

            entity.Property(e => e.Article16Rate).HasColumnType("decimal(18, 4)");
            entity.Property(e => e.BackupRate).HasColumnType("decimal(18, 4)");
            entity.Property(e => e.BoardLow).HasColumnType("decimal(18, 4)");
            entity.Property(e => e.BoardMid).HasColumnType("decimal(18, 4)");
            entity.Property(e => e.BoardPeak).HasColumnType("decimal(18, 4)");
            entity.Property(e => e.ExecutiveTariffBase).HasColumnType("decimal(18, 4)");
            entity.Property(e => e.FuelFee).HasColumnType("decimal(18, 4)");
            entity.Property(e => e.GreenBoardRate).HasColumnType("decimal(18, 4)");
            entity.Property(e => e.IndustrialTariffBase).HasColumnType("decimal(18, 4)");
            entity.Property(e => e.MarketLow).HasColumnType("decimal(18, 4)");
            entity.Property(e => e.MarketMid).HasColumnType("decimal(18, 4)");
            entity.Property(e => e.MarketPeak).HasColumnType("decimal(18, 4)");
        });

        modelBuilder.Entity<OptimumPurchaseSuggestion>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__OptimumP__3214EC07AB19F30D");

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.EstimatedSaving).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.SuggestedKwh).HasColumnType("decimal(18, 2)");

            entity.HasOne(d => d.Subscription).WithMany(p => p.OptimumPurchaseSuggestions)
                .HasForeignKey(d => d.SubscriptionId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__OptimumPu__Subsc__0E6E26BF");
        });

        modelBuilder.Entity<Payment>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Payments__3214EC07C71C6F3A");

            entity.Property(e => e.Amount).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.ReferenceNumber).HasMaxLength(100);

            entity.HasOne(d => d.Method).WithMany(p => p.Payments)
                .HasForeignKey(d => d.MethodId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Payments__Method__7D439ABD");

            entity.HasOne(d => d.Order).WithMany(p => p.Payments)
                .HasForeignKey(d => d.OrderId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Payments__OrderI__7C4F7684");

            entity.HasOne(d => d.Status).WithMany(p => p.Payments)
                .HasForeignKey(d => d.StatusId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Payments__Status__7E37BEF6");
        });

        modelBuilder.Entity<PowerEntity>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__PowerEnt__3214EC0707ED2338");

            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.Name).HasMaxLength(200);

            entity.HasOne(d => d.EntityType).WithMany(p => p.PowerEntities)
                .HasForeignKey(d => d.EntityTypeId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__PowerEnti__Entit__628FA481");

            entity.HasOne(d => d.Province).WithMany(p => p.PowerEntities)
                .HasForeignKey(d => d.ProvinceId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__PowerEnti__Provi__619B8048");
        });

        modelBuilder.Entity<Province>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Province__3214EC07B2EC7E17");

            entity.Property(e => e.Name).HasMaxLength(100);
        });

        modelBuilder.Entity<Role>(entity =>
        {
            entity.ToTable("Role");

            entity.Property(e => e.Title).HasMaxLength(255);
        });

        modelBuilder.Entity<SiteMap>(entity =>
        {
            entity.ToTable("SiteMap");

            entity.Property(e => e.Arguments).HasMaxLength(255);
            entity.Property(e => e.ControlKey).HasMaxLength(256);
            entity.Property(e => e.Icon).HasMaxLength(255);
            entity.Property(e => e.IsInMenu).HasDefaultValue(true);
            entity.Property(e => e.Title).HasMaxLength(256);
        });

        modelBuilder.Entity<SiteMapRole>(entity =>
        {
            entity.ToTable("SiteMapRole");

            entity.HasOne(d => d.Role).WithMany(p => p.SiteMapRoles)
                .HasForeignKey(d => d.RoleId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_SiteMapRole_Role");

            entity.HasOne(d => d.SiteMap).WithMany(p => p.SiteMapRoles)
                .HasForeignKey(d => d.SiteMapId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_SiteMapRole_SiteMap");
        });

        modelBuilder.Entity<Subscription>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Subscrip__3214EC07482B418E");

            entity.HasIndex(e => e.BillIdentifier, "UQ__Subscrip__C594896FF8B897AE").IsUnique();

            entity.Property(e => e.BillIdentifier).HasMaxLength(50);
            entity.Property(e => e.ContractCapacityKw)
                .HasColumnType("decimal(18, 2)")
                .HasColumnName("ContractCapacityKW");

            entity.HasOne(d => d.Address).WithMany(p => p.Subscriptions)
                .HasForeignKey(d => d.AddressId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Address_Subscriptions");
        });

        modelBuilder.Entity<Tariff>(entity =>
        {
            entity.HasKey(e => e.TariffId).HasName("PK__Tariffs__EBAF9DB3FE5AFAF0");

            entity.HasOne(d => d.CustomerType).WithMany(p => p.Tariffs)
                .HasForeignKey(d => d.CustomerTypeId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Tariffs__Custome__3B40CD36");

            entity.HasOne(d => d.PowerEntities).WithMany(p => p.Tariffs)
                .HasForeignKey(d => d.PowerEntitiesId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Tariffs_PowerEntities");

            entity.HasOne(d => d.TariffType).WithMany(p => p.Tariffs)
                .HasForeignKey(d => d.TariffTypeId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Tariffs__TariffT__3D2915A8");
        });

        modelBuilder.Entity<TariffSlab>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__TariffSl__3214EC07D4D7EDEB");

            entity.Property(e => e.FromKwh).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.Multiplier).HasColumnType("decimal(18, 4)");
            entity.Property(e => e.ToKwh).HasColumnType("decimal(18, 2)");

            entity.HasOne(d => d.Tariff).WithMany(p => p.TariffSlabs)
                .HasForeignKey(d => d.TariffId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_TariffSlabs_Tariffs");
        });

        modelBuilder.Entity<Ticket>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Tickets__3214EC07543A285B");

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.Subject).HasMaxLength(250);

            entity.HasOne(d => d.CustomerProfile).WithMany(p => p.Tickets)
                .HasForeignKey(d => d.CustomerProfileId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Tickets__Custome__123EB7A3");

            entity.HasOne(d => d.Status).WithMany(p => p.Tickets)
                .HasForeignKey(d => d.StatusId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Tickets__StatusI__1332DBDC");
        });

        modelBuilder.Entity<TicketMessage>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__TicketMe__3214EC07F149F9E8");

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getdate())");

            entity.HasOne(d => d.SenderUser).WithMany(p => p.TicketMessages)
                .HasForeignKey(d => d.SenderUserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__TicketMes__Sende__17F790F9");

            entity.HasOne(d => d.Ticket).WithMany(p => p.TicketMessages)
                .HasForeignKey(d => d.TicketId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__TicketMes__Ticke__17036CC0");
        });

        modelBuilder.Entity<Touschedule>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__TOUSched__3214EC074A444796");

            entity.ToTable("TOUSchedules");

            entity.HasIndex(e => new { e.PowerEntityId, e.MonthNumber, e.HourNumber }, "UC_TOU_Entity").IsUnique();

            entity.Property(e => e.ToutypeId).HasColumnName("TOUTypeId");

            entity.HasOne(d => d.PowerEntity).WithMany(p => p.Touschedules)
                .HasForeignKey(d => d.PowerEntityId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__TOUSchedu__Power__6C190EBB");

            entity.HasOne(d => d.Toutype).WithMany(p => p.Touschedules)
                .HasForeignKey(d => d.ToutypeId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__TOUSchedu__TOUTy__6D0D32F4");
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Users__3214EC07ABC057CA");

            entity.HasIndex(e => e.Mobile, "UQ__Users__6FAE0782AD5AB5C4").IsUnique();

            entity.Property(e => e.FullName).HasMaxLength(200);
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.Mobile).HasMaxLength(20);
            entity.Property(e => e.Password).HasMaxLength(10);

            entity.HasOne(d => d.CustomerProfile).WithMany(p => p.Users)
                .HasForeignKey(d => d.CustomerProfileId)
                .HasConstraintName("FK__Users__CustomerP__5AEE82B9");
        });

        modelBuilder.Entity<UserRole>(entity =>
        {
            entity.ToTable("UserRole");

            entity.HasOne(d => d.Role).WithMany(p => p.UserRoles)
                .HasForeignKey(d => d.RoleId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_UserRole_Role");
        });

        modelBuilder.Entity<Warranty>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Guarante__3214EC0709D41382");

            entity.ToTable("Warranty");

            entity.Property(e => e.Amount).HasColumnType("decimal(18, 2)");

            entity.HasOne(d => d.Contract).WithMany(p => p.Warranties)
                .HasForeignKey(d => d.ContractId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Warranty__Contr__06CD04F7");

            entity.HasOne(d => d.Type).WithMany(p => p.Warranties)
                .HasForeignKey(d => d.TypeId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Guarantee__TypeI__07C12930");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
