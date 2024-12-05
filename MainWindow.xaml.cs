using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace AgencyApplication
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }
        private void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            string login = LoginField.Text;
            string password = PasswordField.Password;

            // Пример проверки логина и пароля
            //if (AuthenticateUser(login, password))
            {
                MessageBox.Show("Успешный вход!");
/*                //login = "postgres";
                //password = "123";

                // Устанавливаем подключение в зависимости от пользователя
                string connectionString = GetConnectionString(login, password);
                MessageBox.Show(connectionString);*/
                try
                {
                    {
                        // Здесь можно вызвать метод для отображения следующего окна или выполнения операций
                        MainFunctionalityAdminWindow functionalityWindow = new MainFunctionalityAdminWindow();
                        functionalityWindow.Show();
                        this.Close();
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка подключения к базе данных: {ex.Message}");
                }
            }
/*            else
            {
                MessageBox.Show("Неверный логин или пароль.");
            }*/
        }

/*        private bool AuthenticateUser(string login, string password)
        {
            if ((login == "Admin" && password == "admin") || (login == "User" && password == "user"))
                return true;

            return false;
        }

        private string GetConnectionString(string login, string password)
        {
            // Настройка строки подключения в зависимости от роли
            string host = "localhost";
            string port = "5432";
            string database = "Agency";
            return $"Host={host};Username={login};Password={password};Database={database};Port={port};";
        }*/

    }
}