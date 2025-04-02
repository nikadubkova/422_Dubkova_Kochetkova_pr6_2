using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Windows;
using System.Windows.Input;

namespace WpfApp1
{
    public partial class AddUserWindow : Window
    {
        private clinicEntities db = new clinicEntities();

        public AddUserWindow()
        {
            InitializeComponent();
            LoadRoles();
        }

        private void LoadRoles()
        {
            try
            {
                RoleComboBox.ItemsSource = db.Roles.Select(r => r.RoleName).ToList();
                RoleComboBox.SelectedIndex = 0;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки ролей: {ex.Message}", "Ошибка",
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }
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

        private bool ValidateInput()
        {
            if (string.IsNullOrWhiteSpace(FullNameTextBox.Text) ||
                string.IsNullOrWhiteSpace(LoginTextBox.Text) ||
                string.IsNullOrWhiteSpace(PasswordBox.Password) ||
                string.IsNullOrWhiteSpace(EmailTextBox.Text) ||
                RoleComboBox.SelectedItem == null)
            {
                MessageBox.Show("Пожалуйста, заполните все обязательные поля!",
                              "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }

            if (PasswordBox.Password.Length < 6)
            {
                MessageBox.Show("Пароль должен содержать минимум 6 символов!",
                              "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }

            if (!EmailTextBox.Text.Contains("@") || !EmailTextBox.Text.Contains("."))
            {
                MessageBox.Show("Введите корректный email адрес!",
                              "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }

            return true;
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidateInput())
                return;

            try
            {
                if (db.Users.Any(u => u.Username == LoginTextBox.Text))
                {
                    MessageBox.Show("Пользователь с таким логином уже существует!",
                                  "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                if (db.Users.Any(u => u.Email == EmailTextBox.Text))
                {
                    MessageBox.Show("Пользователь с таким email уже существует!",
                                  "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                var newUser = new Users
                {
                    Username = LoginTextBox.Text,
                    PasswordHash = HashPassword(PasswordBox.Password),

                    Email = EmailTextBox.Text,
                    Phone = PhoneTextBox.Text,
                    RegistrationDate = DateTime.Now,
                    IsActive = true
                };

                db.Users.Add(newUser);
                db.SaveChanges();

                var selectedRole = db.Roles.FirstOrDefault(r => r.RoleName == RoleComboBox.SelectedItem.ToString());
                if (selectedRole != null)
                {
                    db.UserRoles.Add(new UserRoles
                    {
                        UserID = newUser.UserID,
                        RoleID = selectedRole.RoleID,
                        AssignedDate = DateTime.Now
                    });
                    db.SaveChanges();
                }

                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при добавлении пользователя: {ex.Message}",
                              "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                DragMove();
            }
        }
    }
}