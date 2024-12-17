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

            using (ApplicationDbContext context = new ApplicationDbContext())
            {
                try
                {
                    if (login == "")
                    {
                        MessageBox.Show("Заполните поле \"Логин\"");
                    }

                    if (password == "")
                    {
                        MessageBox.Show("Заполните поле \"Пароль\"");
                    }
                    if (!string.IsNullOrEmpty(password) && !string.IsNullOrEmpty(login))
                    {
                        try
                        {
                            var findUser = context.Users.FirstOrDefault(l => l.Username == login);
                            if (findUser == null)
                            {
                                throw new Exception("Учетная запись не обнаружена. Проверьте правильность введенных логина и пароля");
                            }
                            var findPassword = context.Users.FirstOrDefault(u => u.ID == findUser.ID).Password.ToString();
                            var findRole = context.Users.FirstOrDefault(u => u.ID == findUser.ID).AccessLevel.ToString();

                            if (findRole == "Administrator" && password == findPassword)
                            {
                                MessageBox.Show("Вы успешно авторизовались как \"Администратор\"");
                                // Здесь можно вызвать метод для отображения следующего окна или выполнения операций
                                MainFunctionalityAdminWindow functionalityWindow = new MainFunctionalityAdminWindow();
                                functionalityWindow.Show();
                                this.Close();
                            }
                            else if (findRole == "User" && password == findPassword)
                            {
                                MessageBox.Show("Вы успешно авторизовались как \"Пользователь\"");
                                MainFunctionalityUserWindow functionalityUserWindow = new MainFunctionalityUserWindow();
                                functionalityUserWindow.Show();
                                this.Close();
                            }
                            else 
                            {
                                MessageBox.Show("Логин или пароль введены неверно"); 
                            }
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show(ex.Message);
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка подключения к базе данных: {ex.Message}");
                }
            }
        }
    }
}