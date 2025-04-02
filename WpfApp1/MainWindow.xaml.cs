using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace WpfApp1
{
    public partial class MainWindow : Window
    {
        private clinicEntities db = new clinicEntities();

        public MainWindow()
        {
            InitializeComponent();
        }
        public void ForceSessionTimeout() => _sessionLastActivity = DateTime.MinValue;
        public bool IsSessionActive() => (DateTime.Now - _sessionLastActivity).TotalMinutes < 15;
        private DateTime _sessionLastActivity;

        // Метод для тестирования, вынесена основная логика авторизации
        public AuthResult AuthenticateUser(string login, string password)
        {
            if (string.IsNullOrEmpty(login) || string.IsNullOrEmpty(password))
            {
                return new AuthResult { IsSuccess = false, ErrorMessage = "Логин и пароль должны быть заполнены!" };
            }

            try
            {
                string hashedPassword = HashPassword(password);
                var user = db.Users.FirstOrDefault(u => u.Username == login);

                if (user == null)
                {
                    return new AuthResult { IsSuccess = false, ErrorMessage = "Пользователь с таким логином не найден!" };
                }

                if (user.PasswordHash != hashedPassword)
                {
                    return new AuthResult { IsSuccess = false, ErrorMessage = "Неверный пароль!" };
                }

                var userRole = db.UserRoles
                    .Where(ur => ur.UserID == user.UserID)
                    .Join(db.Roles, ur => ur.RoleID, r => r.RoleID, (ur, r) => r.RoleName)
                    .FirstOrDefault();

                if (userRole == null)
                {
                    return new AuthResult { IsSuccess = false, ErrorMessage = "У пользователя не назначено ни одной роли!" };
                }

                user.LastLogin = DateTime.Now;
                db.SaveChanges();

                return new AuthResult
                {
                    IsSuccess = true,
                    User = user,
                    RoleName = userRole,
                    WelcomeMessage = $"Здравствуйте, {userRole} {user.FirstName}!"
                };
            }
            catch (Exception ex)
            {
                return new AuthResult { IsSuccess = false, ErrorMessage = $"Ошибка авторизации: {ex.Message}" };
            }
        }

        private void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            string login = LoginTextBox.Text.Trim();
            string password = PasswordBox.Password;

            var result = AuthenticateUser(login, password);

            if (result.IsSuccess)
            {
                MessageBox.Show(result.WelcomeMessage,
                    "Авторизация успешна",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);

                Window1 mainWindow = new Window1(result.User, result.RoleName);
                mainWindow.Show();
                this.Close();
            }
            else
            {
                ShowError(result.ErrorMessage);
            }
        }

        // Остальные методы остаются без изменений
        private void ShowError(string message)
        {
            MessageBox.Show(message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
        }

        public string HashPassword(string password)
        {
            using (SHA256 sha256 = SHA256.Create())
            {
                byte[] bytes = Encoding.UTF8.GetBytes(password);
                byte[] hashBytes = sha256.ComputeHash(bytes);
                return Convert.ToBase64String(hashBytes);
            }
        }

        private void RegisterButton_Click(object sender, RoutedEventArgs e)
        {
            RegPage regPage = new RegPage();
            regPage.Show();
            this.Close();
        }

        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                DragMove();
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }
    }

    // Класс для хранения результатов аутентификации
    public class AuthResult
    {
        public bool IsPasswordExpired { get; set; }
        public int FailedLoginAttempts { get; set; }
        public bool IsActive { get; set; } = true;

        public bool IsSuccess { get; set; }
        public string ErrorMessage { get; set; }
        public Users User { get; set; }
        public string RoleName { get; set; }
        public string WelcomeMessage { get; set; }
    }
}