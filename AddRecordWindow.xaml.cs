using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;

namespace AgencyApplication
{
    public partial class AddRecordWindow : Window
    {
        private readonly Type _entityType;
        public object NewEntity { get; private set; }

        public AddRecordWindow(Type entityType)
        {
            InitializeComponent();
            _entityType = entityType;
            GenerateFields();
        }

        private void GenerateFields()
        {
            // Получаем свойства выбранного типа
            var properties = _entityType.GetProperties(BindingFlags.Public | BindingFlags.Instance);
            foreach (var property in properties)
            {
                if (!property.CanWrite) continue; // Пропускаем свойства только для чтения

                // Создаем метку
                var label = new TextBlock
                {
                    Text = property.Name,
                    FontWeight = FontWeights.Bold,
                    Margin = new Thickness(0, 5, 0, 0)
                };
                FieldsPanel.Children.Add(label);

                // Если свойство типа DateTime, создаем DatePicker
                if (property.PropertyType == typeof(DateTime))
                {
                    var datePicker = new DatePicker
                    {
                        Name = $"Field_{property.Name}",
                        Tag = property, // Сохраняем свойство для связи
                        Margin = new Thickness(0, 5, 0, 10),
                        SelectedDate = DateTime.Now // Устанавливаем текущую дату по умолчанию
                    };
                    FieldsPanel.Children.Add(datePicker);
                }
                else if (property.PropertyType.IsEnum) // Если это Enum, создаем ComboBox
                {
                    var comboBox = new ComboBox
                    {
                        Name = $"Field_{property.Name}",
                        Tag = property, // Сохраняем свойство для связи
                        Margin = new Thickness(0, 5, 0, 10)
                    };

                    // Получаем все возможные значения enum
                    var enumValues = Enum.GetValues(property.PropertyType);
                    foreach (var value in enumValues)
                    {
                        comboBox.Items.Add(value);
                    }

                    FieldsPanel.Children.Add(comboBox);
                }
                else
                {
                    // Для всех других типов создаем текстовое поле
                    var textBox = new TextBox
                    {
                        Name = $"Field_{property.Name}",
                        Tag = property, // Сохраняем свойство для связи
                        Margin = new Thickness(0, 5, 0, 10)
                    };
                    FieldsPanel.Children.Add(textBox);
                }
            }
        }

        private void AddButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Создаем новый экземпляр выбранного типа
                NewEntity = Activator.CreateInstance(_entityType);

                // Устанавливаем значения свойств
                foreach (var child in FieldsPanel.Children)
                {
                    if (child is TextBox textBox && textBox.Tag is PropertyInfo property)
                    {
                        var value = textBox.Text;
                        if (!string.IsNullOrWhiteSpace(value))
                        {
                            object convertedValue;

                            if (property.PropertyType == typeof(DateTime))
                            {
                                // Попытка преобразования строки в дату с конкретным форматом
                                if (!DateTime.TryParseExact(value, "yyyy-MM-dd", null, System.Globalization.DateTimeStyles.None, out DateTime parsedDate))
                                {
                                    MessageBox.Show($"Ошибка: неверный формат даты для свойства {property.Name}. Ожидаемый формат: ГГГГ-ММ-ДД.");
                                    return;
                                }
                                convertedValue = parsedDate;
                            }
                            else if (property.PropertyType == typeof(TimeSpan))
                            {
                                // Попытка преобразования строки во время
                                if (!TimeSpan.TryParse(value, out TimeSpan parsedTime))
                                {
                                    MessageBox.Show($"Ошибка: неверный формат времени для свойства {property.Name}. Ожидаемый формат: ЧЧ:ММ.");
                                    return;
                                }
                                convertedValue = parsedTime;
                            }
                            else if (property.PropertyType.IsEnum) // Обработка Enum
                            {
                                var comboBox = FieldsPanel.Children.OfType<ComboBox>()
                                    .FirstOrDefault(c => c.Name == $"Field_{property.Name}");

                                if (comboBox != null && comboBox.SelectedItem != null)
                                {
                                    convertedValue = Enum.Parse(property.PropertyType, comboBox.SelectedItem.ToString());
                                }
                                else
                                {
                                    MessageBox.Show($"Ошибка: не выбрано значение для свойства {property.Name}.");
                                    return;
                                }
                            }
                            else
                            {
                                // Общее преобразование для других типов
                                convertedValue = Convert.ChangeType(value, property.PropertyType);
                            }

                            property.SetValue(NewEntity, convertedValue);
                        }
                    }
                }

                DialogResult = true; // Успешное завершение
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}");
            }
        }



        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false; // Отмена
            Close();
        }
    }
}
