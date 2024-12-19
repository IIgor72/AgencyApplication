using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using static System.Runtime.InteropServices.JavaScript.JSType;
using System.Windows.Documents;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AgencyApplication
{

    // ENUMs
    public enum FlightStatusType
    {
        Scheduled,
        In_Progress,
        Completed,
        Cancelled
    }

    public enum AccessLevelType
    {
        Administrator,
        User
    }

    // Airline
    public class Airline
    {
        [Key]
        public int ID { get; set; }
        [Required]
        public string Name { get; set; }
        public string Address { get; set; }
        public string Phone { get; set; }
    }

    // Aircraft
    public class Aircraft
    {
        [Key]
        public int ID { get; set; }
        [Required]
        public string Model { get; set; }
        public string Type { get; set; }
        [Required]
        public int Capacity { get; set; }
        [Column("Airline_ID")]
        public int? Airline_ID { get; set; }
        public Airline Airline { get; set; }
    }

    // Airport
    public class Airport
    {
        [Key]
        public int ID { get; set; }
        [Required]
        public string Name { get; set; }
        public string Location { get; set; }
    }

    // Flight
    public class Flight
    {
        [Key]
        public int ID { get; set; }
        [Required]
        public string FlightNumber { get; set; }
        [Required]
        public TimeSpan DepartureTime { get; set; }
        [Required]
        public DateTime DepartureDate { get; set; }
        [Column("Aircraft_ID")]
        public int? Aircraft_ID { get; set; }
        [Column("DepartureAirport_ID")]
        public int? DepartureAirport_ID { get; set; }
        [Column("ArrivalAirport_ID")]
        public int? ArrivalAirport_ID { get; set; }

        public Aircraft Aircraft { get; set; }
        public Airport DepartureAirport { get; set; }
        public Airport ArrivalAirport { get; set; }
    }

    // Passenger
    public class Passenger
    {
        [Key]
        public string PassportSeries { get; set; }
        [Key]
        public string PassportNumber { get; set; }
        [Required]
        public string FullName { get; set; }
        [Required]
        [StringLength(1)]
        public string Gender { get; set; }
        public DateTime BirthDate { get; set; }
        public string Phone { get; set; }
    }

    // Ticket
    public class Ticket
    {
        [Key]
        public int ID { get; set; }
        [Required]
        public decimal Price { get; set; }
        public string FlightNumber { get; set; }
        public string Seat { get; set; }
        public bool Baggage { get; set; } = false;
        public string AdditionalServices { get; set; }
        public string PassportSeries { get; set; }
        public string PassportNumber { get; set; }

        public Flight Flight { get; set; }
        public Passenger Passenger { get; set; }
    }

    // Flight List
    public class FlightList
    {
        [Key]
        public int ID { get; set; }
        public string FlightNumber { get; set; }
        [Required]
        public FlightStatusType FlightStatus { get; set; }

        public Flight Flight { get; set; }
    }

    // User
    public class User
    {
        [Key]
        public int ID { get; set; }
        [Required]
        public string Username { get; set; }
        [Required]
        public string Password { get; set; }
        [Required]
        public AccessLevelType AccessLevel { get; set; }
    }

}
