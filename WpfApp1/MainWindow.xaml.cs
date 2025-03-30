using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace WpfApp1
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }
        private void ShowError(string message)
        {
            errorWindow errorWindow = new errorWindow(message)
            {
                Owner = this,
                WindowStartupLocation = WindowStartupLocation.CenterOwner
            };
            errorWindow.ShowDialog();
        }

        private string HashPassword(string password)
        {
            using (SHA256 sha256 = SHA256.Create())
            {
                byte[] bytes = Encoding.UTF8.GetBytes(password);
                byte[] hashBytes = sha256.ComputeHash(bytes);
                return Convert.ToBase64String(hashBytes);
            }
        }

        private bool CheckUserCredentials(string login, string hashedPassword, string role)
        {
            var users = new List<(string Login, string PasswordHash, string Role)>
            {
                    ("admin", HashPassword("admin123"), "Администратор"),
                    ("dr_ayrapetyan", HashPassword("doctor77"), "Врач"),
                    ("manager_anna", HashPassword("annacool"), "Менеджер"),
                    ("patient_olga", HashPassword("helpme"), "Пациент")
            };

            return users.Any(u => u.Login == login && u.PasswordHash == hashedPassword && u.Role == role);
        }

        private void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            string login = LoginTextBox.Text;
            string password = PasswordBox.Password;
            string role = (RoleComboBox.SelectedItem as ComboBoxItem)?.Content.ToString();

            if (string.IsNullOrEmpty(login) || string.IsNullOrEmpty(password) || string.IsNullOrEmpty(role))
            {
                ShowError("Заполните все поля!");
                return;
            }

            string hashedPassword = HashPassword(password);
            bool isValidUser = CheckUserCredentials(login, hashedPassword, role);

            if (isValidUser)
            {
                Window1 appWindow = new Window1();
                appWindow.Show();
                Close();
            }
            else
            {
                ShowError("Неверный логин или пароль");
            }
        }

        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            DragMove();
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        private void RoleComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }
    }
}