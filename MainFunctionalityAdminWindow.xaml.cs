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
    /// <summary>
    /// Логика взаимодействия для MainFunctionalityAdminWindow.xaml
    /// </summary>
    public partial class MainFunctionalityAdminWindow : Window
    {
        private ApplicationDbContext _context;
        private Type _selectedTableType;

        public MainFunctionalityAdminWindow()
        {
            InitializeComponent();
            _context = new ApplicationDbContext();
            TableSelectionComboBox.ItemsSource = GetTableTypes().Select(t => new { Name = t.Name }).ToList(); // Отображаем названия типов
            TableSelectionComboBox.SelectedIndex = 0; // Выбираем первую таблицу по умолчанию
        }

        private Type[] GetTableTypes()
        {
            return new[] { typeof(Flight), typeof(Passenger), typeof(Ticket), typeof(Airport), typeof(Airline), typeof(Aircraft), typeof(User), typeof(FlightList) };
        }

        private void TableSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (TableSelectionComboBox.SelectedItem != null)
            {
                _selectedTableType = GetTableTypes().FirstOrDefault(t => t.Name == ((dynamic)TableSelectionComboBox.SelectedItem).Name);
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

                    // Применяем данные в DataGrid
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


        // Обработка кнопки "Выход"
        private void Exit_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        // Добавление новой записи в таблицу
        public void AddRecord_Click(object sender, RoutedEventArgs e)
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


        // Удаление записи из таблицы
        private void DeleteRecord_Click(object sender, RoutedEventArgs e)
        {
            if (DataGridDisplay.SelectedItem != null)
            {
                var selectedEntity = DataGridDisplay.SelectedItem;


                var dbSet = GetDbSetForEntity(selectedEntity.GetType()); // Получаем DbSet для типа записи
                var removeMethod = dbSet.GetType().GetMethod("Remove"); // Получаем метод Remove для удаления записи
                removeMethod.Invoke(dbSet, new[] { selectedEntity }); // Удаляем запись

                _context.SaveChanges(); // Сохраняем изменения
                LoadData(); // Обновляем данные в DataGrid
            }
            else
            {
                MessageBox.Show("Выберите запись для удаления");
            }
        }



        // Обработчик окончания редактирования ячейки
        private void DataGrid_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
        {
            var editedItem = e.Row.Item;
            if (editedItem != null)
            {
                var editedProperty = e.Column.Header.ToString(); // Название редактируемого столбца
                var propertyInfo = _selectedTableType.GetProperty(editedProperty);
                var editedValue = (e.EditingElement as TextBox)?.Text;

                if (propertyInfo != null && editedValue != null)
                {
                    try
                    {
                        // Проверяем, является ли редактируемое поле ключевым
                        var isPrimaryKey = propertyInfo.GetCustomAttributes(typeof(KeyAttribute), true).Any();

                        // Если редактируется ключевое поле, проверяем уникальность
                        if (isPrimaryKey)
                        {
                            var originalValue = propertyInfo.GetValue(editedItem);
                            if (!originalValue.Equals(Convert.ChangeType(editedValue, propertyInfo.PropertyType)))
                            {
                                // Создаем копию объекта с измененным значением ключевого поля
                                var tempEditedItem = Activator.CreateInstance(_selectedTableType);
                                foreach (var prop in _selectedTableType.GetProperties())
                                {
                                    if (prop.Name == propertyInfo.Name)
                                    {
                                        prop.SetValue(tempEditedItem, Convert.ChangeType(editedValue, prop.PropertyType));
                                    }
                                    else
                                    {
                                        prop.SetValue(tempEditedItem, prop.GetValue(editedItem));
                                    }
                                }

                                // Проверяем, не возникает ли дубликат
                                if (IsPrimaryKeyDuplicate(tempEditedItem))
                                {
                                    MessageBox.Show("Ошибка: изменение приводит к конфликту ключей. Операция отменена.");
                                    e.Cancel = true;
                                    return;
                                }
                            }
                        }

                        // Обработка для типов DateTime
                        if (propertyInfo.PropertyType == typeof(DateTime))
                        {
                            DateTime parsedDate;
                            if (DateTime.TryParseExact(editedValue, new string[] { "yyyy-MM-dd", "yyyy/MM/dd" }, null, System.Globalization.DateTimeStyles.None, out parsedDate))
                            {
                                propertyInfo.SetValue(editedItem, parsedDate);
                            }
                            else
                            {
                                MessageBox.Show($"Ошибка: неверный формат даты для свойства {editedProperty}. Ожидаемый формат: ГГГГ-ММ-ДД.");
                                e.Cancel = true;
                                return;
                            }
                        }
                        else if (propertyInfo.PropertyType == typeof(TimeSpan))
                        {
                            // Попытка преобразования строки во время
                            if (!TimeSpan.TryParseExact(editedValue, @"hh\:mm\:ss", null, System.Globalization.TimeSpanStyles.None, out TimeSpan parsedTime))
                            {
                                MessageBox.Show($"Ошибка: неверный формат времени для свойства {propertyInfo.Name}. Ожидаемый формат: ЧЧ:ММ.");
                                return;
                            }
                            propertyInfo.SetValue(editedItem, parsedTime);
                        }
                        // Обработка для типов Enum
                        else if (propertyInfo.PropertyType.IsEnum)
                        {
                            try
                            {
                                var enumValue = Enum.Parse(propertyInfo.PropertyType, editedValue);
                                propertyInfo.SetValue(editedItem, enumValue);
                            }
                            catch (ArgumentException)
                            {
                                MessageBox.Show($"Ошибка: неверное значение для перечисления {editedProperty}.");
                                e.Cancel = true;
                                return;
                            }
                        }
                        else
                        {
                            // Для других типов, выполняем стандартное преобразование
                            propertyInfo.SetValue(editedItem, Convert.ChangeType(editedValue, propertyInfo.PropertyType));
                        }

                        // Сохраняем изменения в базе данных
                        _context.SaveChanges();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Ошибка при сохранении изменений: {ex.Message}");
                    }
                }
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
