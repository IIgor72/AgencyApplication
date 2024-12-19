using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Reflection;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore.Metadata.Internal;

namespace AgencyApplication
{
    public partial class MainFunctionalityUserWindow : Window
    {
        private ApplicationDbContext _context;
        private Type _selectedTableType;

        public MainFunctionalityUserWindow()
        {
            InitializeComponent();
            _context = new ApplicationDbContext();
            TableSelectionUserComboBox.ItemsSource = GetTableUserTypes().Select(t => new { Name = t.Name }).ToList(); // Отображаем названия типов
            TableSelectionUserComboBox.SelectedIndex = 0; // Выбираем первую таблицу по умолчанию
        }


        private Type[] GetTableUserTypes()
        {
            return new[] { typeof(Flight), typeof(Passenger), typeof(Ticket), typeof(Airport), typeof(Airline), typeof(Aircraft), typeof(FlightList) };
        }

        private void TableSelectionUserChanged(object sender, SelectionChangedEventArgs e)
        {
            if (TableSelectionUserComboBox.SelectedItem != null)
            {
                _selectedTableType = GetTableUserTypes().FirstOrDefault(t => t.Name == ((dynamic)TableSelectionUserComboBox.SelectedItem).Name);

                if (_selectedTableType == typeof(Airport) || _selectedTableType == typeof(Airline)) 
                {
                    AddRecordButton.Visibility = Visibility.Visible;    
                }
                else
                {
                    AddRecordButton.Visibility = Visibility.Collapsed;
                }

                LoadData();
            }
        }

        private void LoadData()
        {
            try
            {
                var dbSet = GetDbSetForEntity(_selectedTableType);

                // Проверяем, что dbSet не null
                if (dbSet is IQueryable queryable)
                {
                    // Выполняем ToList через LINQ
                    var data = queryable.Cast<object>().ToList();
                    DataGridDisplay.ItemsSource = data;
                }
                else
                {
                    MessageBox.Show("Ошибка: не удалось загрузить данные для выбранной таблицы.");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке данных: {ex.Message}");
            }
        }

        private object GetDbSetForEntity(Type entityType)
        {
            // Получаем метод Set для типа сущности
            var setMethod = typeof(ApplicationDbContext).GetMethods()
                .FirstOrDefault(m => m.Name == "Set" && m.IsGenericMethod && m.GetParameters().Length == 0);

            if (setMethod != null)
            {
                var genericSetMethod = setMethod.MakeGenericMethod(entityType);
                return genericSetMethod.Invoke(_context, null);
            }

            return null;
        }

        private void Exit_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        private void AddRecord_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedTableType != null)
            {
                var addRecordWindow = new AddRecordWindow(_selectedTableType);
                if (addRecordWindow.ShowDialog() == true)
                {
                    var newEntity = addRecordWindow.NewEntity;

                    if (newEntity != null)
                    {
                        // Проверка уникальности первичных ключей
                        if (IsPrimaryKeyDuplicate(newEntity))
                        {
                            MessageBox.Show("Ошибка: запись с таким ключом уже существует.");
                            return;
                        }

                        var dbSet = GetDbSetForEntity(_selectedTableType);
                        var addMethod = dbSet.GetType().GetMethod("Add");
                        addMethod.Invoke(dbSet, new[] { newEntity });

                        _context.SaveChanges(); // Сохраняем изменения
                        LoadData(); // Обновляем данные в DataGrid
                    }
                }
            }
          
        }

        private bool IsPrimaryKeyDuplicate(object newEntity)
        {
            try
            {
                var dbSet = GetDbSetForEntity(_selectedTableType);
                var keyProperties = _selectedTableType.GetProperties()
                    .Where(p => p.GetCustomAttributes(typeof(KeyAttribute), true).Any()) // Поиск атрибутов [Key]
                    .ToList();

                if (!keyProperties.Any())
                {
                    MessageBox.Show("Ошибка: не найдены ключевые свойства для проверки уникальности.");
                    return false; // Предполагаем, что ключей нет
                }

                foreach (var entity in (IEnumerable<object>)dbSet)
                {
                    bool isDuplicate = keyProperties.All(keyProperty =>
                    {
                        var existingValue = keyProperty.GetValue(entity);
                        var newValue = keyProperty.GetValue(newEntity);
                        return existingValue != null && existingValue.Equals(newValue);
                    });

                    if (isDuplicate)
                        return true; // Найден дубликат
                }

                return false; // Нет дубликатов
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при проверке уникальности ключей: {ex.Message}");
                return true; // Возвращаем true, чтобы предотвратить добавление при ошибке
            }
        }

        private void ReportSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ReportSelectionComboBox.SelectedItem is ComboBoxItem selectedItem)
            {
                var reportName = selectedItem.Content.ToString();
                switch (reportName)
                {
                    case "Отчет 1: Рейсы за 3 месяца":
                        GenerateFlightsReport();
                        break;

                    case "Отчет 2: Выручка авиакомпаний":
                        GenerateAirlineRevenueReport();
                        break;

                    case "Отчет 3: Пассажиры по рейсу":
                        GeneratePassengerReport();
                        break;

                    case "Отчет 4: Заполненность самолетов":
                        GenerateAircraftLoadReport();
                        break;

                    case "Отчет 5: Частота рейсов":
                        GenerateFlightFrequencyReport();
                        break;

                    default:
                        MessageBox.Show("Выберите доступный отчет.");
                        break;
                }
            }
        }

        private void GenerateFlightsReport()
        {
            try
            {
                DateTime threeMonthsAgo = DateTime.Now.AddMonths(-3);

                var reportData = _context.Flights
                    .Include(f => f.Aircraft)
                    .Where(f => f.DepartureDate >= threeMonthsAgo)
                    .Select(f => new
                    {
                        FlightNumber = f.FlightNumber,
                        TicketsSold = _context.Tickets.Count(t => t.FlightNumber == f.FlightNumber),
                        TotalRevenue = _context.Tickets
                            .Where(t => t.FlightNumber == f.FlightNumber)
                            .Sum(t => (decimal?)t.Price) ?? 0,
                        DepartureDate = f.DepartureDate,
                        DepartureLocation = f.DepartureAirport.Location
                    })
                    .ToList();

                DataGridDisplay.ItemsSource = reportData;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при генерации отчета: {ex.Message}");
            }
        }


        private void GenerateAirlineRevenueReport()
        {
            try
            {
                var reportData = _context.Tickets
                    .Include(t => t.Flight)
                    .ThenInclude(f => f.Aircraft)
                    .ThenInclude(a => a.Airline)
                    .GroupBy(t => t.Flight.Aircraft.Airline.Name)
                    .Select(group => new
                    {
                        AirlineName = group.Key,
                        TotalRevenue = group.Sum(t => t.Price),
                        TicketsSold = group.Count()
                    })
                    .ToList();

                DataGridDisplay.ItemsSource = reportData;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при генерации отчета: {ex.Message}");
            }
        }

        private void GeneratePassengerReport()
        {
            try
            {
                var reportData = _context.Tickets
                    .Include(t => t.Passenger)
                    .Include(t => t.Flight)
                    .Select(t => new
                    {
                        FlightNumber = t.Flight.FlightNumber,
                        PassengerName = t.Passenger.FullName,
                        Passport = $"{t.Passenger.PassportSeries} {t.Passenger.PassportNumber}"
                    })
                    .ToList();

                DataGridDisplay.ItemsSource = reportData;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при генерации отчета: {ex.Message}");
            }
        }

        private void GenerateAircraftLoadReport()
        {
            try
            {
                var reportData = _context.Flights
                    .Include(f => f.Aircraft)
                    .Where(f => f.Aircraft_ID != null) // Исключаем рейсы без привязанного самолета
                    .GroupJoin(
                        _context.Tickets,
                        flight => flight.FlightNumber,
                        ticket => ticket.FlightNumber,
                        (flight, tickets) => new { Flight = flight, Tickets = tickets }
                    )
                    .GroupBy(
                        grouped => new { grouped.Flight.Aircraft.Model, grouped.Flight.Aircraft.Capacity }
                    )
                    .Select(group => new
                    {
                        AircraftModel = group.Key.Model, // Модель самолета
                        TotalCapacity = group.Key.Capacity * group.Count(), // Общая вместимость всех рейсов
                        TicketsSold = group.Sum(g => g.Tickets.Count()), // Проданные билеты
                        LoadPercentage = Math.Round(
                            group.Sum(g => g.Tickets.Count()) / (double)(group.Key.Capacity * group.Count()) * 100, 2) // Загрузка
                    })
                    .ToList();

                DataGridDisplay.ItemsSource = reportData;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при генерации отчета: {ex.Message}");
            }
        }

        private void GenerateFlightFrequencyReport()
        {
            try
            {
                var reportData = _context.Flights
                    .Include(f => f.DepartureAirport)
                    .GroupBy(f => new { f.DepartureAirport.Name, f.DepartureAirport.Location })
                    .Select(group => new
                    {
                        AirportName = group.Key.Name,
                        Location = group.Key.Location,
                        TotalFlights = group.Count()
                    })
                    .OrderByDescending(r => r.TotalFlights)
                    .ToList();

                DataGridDisplay.ItemsSource = reportData;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при генерации отчета: {ex.Message}");
            }
        }
    }

}
