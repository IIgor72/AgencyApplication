﻿using System;
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

namespace AgencyApplication
{
    /// <summary>
    /// Логика взаимодействия для MainFunctionalityAdminWindow.xaml
    /// </summary>
    public partial class MainFunctionalityAdminWindow : Window
    {
        private ApplicationDbContext _context;

        public MainFunctionalityAdminWindow()
        {
            InitializeComponent();
            _context = new ApplicationDbContext();
            LoadData(); // Загружаем данные при инициализации
        }

        // Загрузка данных в DataGrid
        private void LoadData()
        {
            // Например, загружаем данные о рейсах
            var flights = _context.Flights.ToList();
            DataGridDisplay.ItemsSource = flights;
        }

        private void DataOfFlights_Click(object sender, RoutedEventArgs e)
        {
            DisplayData<Flight>("Flight");
        }

        private void DataOfCarriers_Click(object sender, RoutedEventArgs e)
        {
            DisplayData<Aircraft>("Aircraft");
        }

        private void DataOfAirports_Click(object sender, RoutedEventArgs e)
        {
            DisplayData<Airport>("Airport");
        }

        private void DataOfAirplanes_Click(object sender, RoutedEventArgs e)
        {
            DisplayData<Airline>("Airline");
        }

        private void DataOfPassengers_Click(object sender, RoutedEventArgs e)
        {
            DisplayData<Passenger>("Passenger");
        }

        private void DataOfFlightsList_Click(object sender, RoutedEventArgs e)
        {
            DisplayData<FlightList>("FlightList");
        }

        private void DataOfUsers_Click(object sender, RoutedEventArgs e)
        {
            DisplayData<User>("User");
        }

        private void DataOfTicket_Click(object sender, RoutedEventArgs e)
        {
            DisplayData<Ticket>("Ticket");
        }

        // Метод для отображения данных
        private void DisplayData<T>(string tableName) where T : class
        {
            try
            {
                using (var context = new ApplicationDbContext())
                {
                    // Очищаем старые данные
                    DataGridDisplay.ItemsSource = null;

                    // Получаем DbSet по имени таблицы
                    var dbSet = context.Set<T>();

                    if (dbSet == null)
                    {
                        MessageBox.Show($"Таблица '{tableName}' не найдена.");
                        return;
                    }

                    // Получаем данные из таблицы
                    var data = dbSet.ToList();

                    // Создаем новый список, чтобы не изменять исходные данные
                    var filteredData = data.Select(item =>
                    {
                        // Создаем словарь для хранения значений свойств
                        var filteredProperties = new Dictionary<string, object>();

                        // Получаем все свойства объекта
                        var properties = typeof(T).GetProperties()
                            .Where(p => p.CanRead && !IsNavigationProperty(p)) // Фильтруем навигационные свойства
                            .ToList();

                        // Для каждого свойства проверяем его значение и добавляем в словарь
                        foreach (var property in properties)
                        {
                            var value = property.GetValue(item);
                            if (value != null)
                            {
                                filteredProperties[property.Name] = value; // Добавляем в словарь
                            }
                        }

                        // Создаем новый экземпляр объекта T с фильтрацией свойств
                        var filteredObject = CreateFilteredObject<T>(filteredProperties);
                        return filteredObject;
                    }).ToList();

                    // Устанавливаем ItemsSource для DataGrid
                    DataGridDisplay.ItemsSource = filteredData;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке данных: {ex.Message}");
            }
        }

        // Метод для фильтрации навигационных свойств
        private bool IsNavigationProperty(System.Reflection.PropertyInfo property)
        {
            return property.PropertyType.IsClass && property.PropertyType != typeof(string) && !property.PropertyType.IsPrimitive;
        }

        // Метод для создания нового объекта типа T с фильтрованными свойствами
        private T CreateFilteredObject<T>(Dictionary<string, object> filteredProperties)
        {
            var constructor = typeof(T).GetConstructor(Type.EmptyTypes); // Получаем конструктор без параметров
            var newObject = (T)constructor.Invoke(null); // Создаем новый объект

            // Устанавливаем значения свойств в новый объект
            foreach (var property in filteredProperties)
            {
                var propInfo = typeof(T).GetProperty(property.Key);
                if (propInfo != null)
                {
                    propInfo.SetValue(newObject, property.Value); // Устанавливаем значение свойства
                }
            }

            return newObject;
        }

        // Обработка кнопки "Выход"
        private void Exit_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        // Обработчик кнопки "Добавить запись"
        private void AddRecord_Click(object sender, RoutedEventArgs e)
        {
            var newFlight = new Flight
            {
                FlightNumber = "FL123", // Здесь могут быть значения по умолчанию или данные из TextBox
                DepartureTime = TimeSpan.FromHours(10),
                DepartureDate = DateTime.Now.AddDays(1)
            };

            _context.Flights.Add(newFlight); // Добавляем в контекст
            _context.SaveChanges(); // Сохраняем изменения в БД
            LoadData(); // Перезагружаем данные в DataGrid
        }

        // Обработчик кнопки "Удалить запись"
        private void DeleteRecord_Click(object sender, RoutedEventArgs e)
        {
            if (DataGridDisplay.SelectedItem is Flight selectedFlight)
            {
                _context.Flights.Remove(selectedFlight); // Удаляем запись из контекста
                _context.SaveChanges(); // Сохраняем изменения в БД
                LoadData(); // Перезагружаем данные в DataGrid
            }
            else
            {
                MessageBox.Show("Выберите запись для удаления");
            }
        }

        // Обработчик события окончания редактирования ячейки
        private void DataGrid_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
        {
            // Получаем редактируемую строку
            var editedItem = e.Row.Item as Flight;

            // Получаем новое значение в редактируемой ячейке
            var cell = e.EditingElement as TextBox;
            var newValue = cell?.Text;

            if (editedItem != null && newValue != null)
            {
                // Обновляем значение в объекте
                if (e.Column.Header.ToString() == "FlightNumber")
                {
                    editedItem.FlightNumber = newValue;
                }
                else if (e.Column.Header.ToString() == "DepartureTime")
                {
                    editedItem.DepartureTime = TimeSpan.Parse(newValue);
                }
                else if (e.Column.Header.ToString() == "DepartureDate")
                {
                    editedItem.DepartureDate = DateTime.Parse(newValue);
                }

                // Сохраняем изменения в базе данных
                _context.SaveChanges();
            }
        }
    }
}
