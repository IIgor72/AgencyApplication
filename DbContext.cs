using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;


namespace AgencyApplication
{
    public class ApplicationDbContext : DbContext
    {

        // Таблицы (DbSet)
        public DbSet<Airline> Airlines { get; set; }
        public DbSet<Aircraft> Aircrafts { get; set; }
        public DbSet<Airport> Airports { get; set; }
        public DbSet<Flight> Flights { get; set; }
        public DbSet<Passenger> Passengers { get; set; }
        public DbSet<Ticket> Tickets { get; set; }
        public DbSet<FlightList> FlightLists { get; set; }
        public DbSet<User> Users { get; set; }

        static readonly string connectionString = "server=localhost;port=3306;username=root;database=Agency";

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            // Настроим Serilog для записи логов в файл
            Log.Logger = new LoggerConfiguration()
                .WriteTo.File("logs/app_log.txt", rollingInterval: RollingInterval.Day) // Пишем логи в файл
                .CreateLogger();

            optionsBuilder.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString))
                          .UseLoggerFactory(LoggerFactory.Create(builder => builder.AddSerilog()));  // Добавляем Serilog как логер
        }


        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Настройка составного ключа для "Passenger"
            modelBuilder.Entity<Passenger>()
                .HasKey(p => new { p.PassportSeries, p.PassportNumber });

            // Уникальность FlightNumber в таблице Flight
            modelBuilder.Entity<Flight>()
                .HasIndex(f => f.FlightNumber)
                .IsUnique();

            // Уникальность FlightNumber в таблице FlightList
            modelBuilder.Entity<FlightList>()
                .HasIndex(f => f.FlightNumber)
                .IsUnique();

            // Настройка связей
            modelBuilder.Entity<Aircraft>()
                .HasOne(a => a.Airline)
                .WithMany()
                .HasForeignKey(a => a.Airline_ID)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Flight>()
                .HasOne(f => f.Aircraft)
                .WithMany()
                .HasForeignKey(f => f.Aircraft_ID)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Flight>()
                .HasOne(f => f.ArrivalAirport)
                .WithMany()
                .HasForeignKey(f => f.ArrivalAirport_ID)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Flight>()
                .HasOne(f => f.DepartureAirport)
                .WithMany()
                .HasForeignKey(f => f.DepartureAirport_ID)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Ticket>()
                .HasOne(f => f.Flight)
                .WithMany()
                .HasPrincipalKey(f => f.FlightNumber)
                .HasForeignKey(t => t.FlightNumber)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Ticket>()
                .HasOne(p => p.Passenger)
                .WithMany()
                .HasForeignKey(t => new { t.PassportSeries, t.PassportNumber })
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<FlightList>()
                .HasOne(f => f.Flight)
                .WithMany()
                .HasPrincipalKey(f => f.FlightNumber)
                .HasForeignKey(f => f.FlightNumber)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Flight>().ToTable("Flight");
            modelBuilder.Entity<Airport>().ToTable("Airport");
            modelBuilder.Entity<Aircraft>().ToTable("Aircraft");
            modelBuilder.Entity<Airline>().ToTable("Airline");
            modelBuilder.Entity<Ticket>().ToTable("Ticket");
            modelBuilder.Entity<Passenger>().ToTable("Passenger");
            modelBuilder.Entity<FlightList>().ToTable("FlightList");
            modelBuilder.Entity<User>().ToTable("Users");

        }
    }

}
