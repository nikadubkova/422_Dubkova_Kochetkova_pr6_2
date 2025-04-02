using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Windows;
using System.Windows.Input;

namespace WpfApp1
{
    public partial class EditUserWindow : Window
    {
        private clinicEntities db = new clinicEntities();
        private int userId;

        public EditUserWindow(int userId)
        {
            InitializeComponent();
            this.userId = userId;
            LoadUserData();
            LoadRoles();
        }

        private void LoadUserData()
        {
            try
            {
                var user = db.Users.Find(userId);
                if (user != null)
                {

                    LoginTextBox.Text = user.Username;
                    EmailTextBox.Text = user.Email;
                    PhoneTextBox.Text = user.Phone;

                    var role = db.UserRoles
                        .Where(ur => ur.UserID == user.UserID)
                        .Join(db.Roles, ur => ur.RoleID, r => r.RoleID, (ur, r) => r.RoleName)
                        .FirstOrDefault();

                    if (role != null)
                    {
                        RoleComboBox.SelectedItem = role;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки данных пользователя: {ex.Message}",
                              "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadRoles()
        {
            try
            {
                RoleComboBox.ItemsSource = db.Roles.Select(r => r.RoleName).ToList();
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
                string.IsNullOrWhiteSpace(EmailTextBox.Text) ||
                RoleComboBox.SelectedItem == null)
            {
                MessageBox.Show("Пожалуйста, заполните все обязательные поля!",
                              "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }

            if (!string.IsNullOrEmpty(PasswordBox.Password) && PasswordBox.Password.Length < 6)
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
                var user = db.Users.Find(userId);
                if (user != null)
                {
                    // Обновляем основные данные

                    user.Email = EmailTextBox.Text;
                    user.Phone = PhoneTextBox.Text;

                    // Обновляем пароль, если он был изменен
                    if (!string.IsNullOrEmpty(PasswordBox.Password))
                    {
                        user.PasswordHash = HashPassword(PasswordBox.Password);
                    }

                    // Обновляем роль пользователя
                    var currentRole = db.UserRoles.FirstOrDefault(ur => ur.UserID == user.UserID);
                    var selectedRole = db.Roles.FirstOrDefault(r => r.RoleName == RoleComboBox.SelectedItem.ToString());

                    if (currentRole != null && selectedRole != null && currentRole.RoleID != selectedRole.RoleID)
                    {
                        db.UserRoles.Remove(currentRole);
                        db.UserRoles.Add(new UserRoles
                        {
                            UserID = user.UserID,
                            RoleID = selectedRole.RoleID,
                            AssignedDate = DateTime.Now
                        });
                    }

                    db.SaveChanges();
                    DialogResult = true;
                    Close();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при сохранении изменений: {ex.Message}",
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

        private void RoleComboBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {

        }
    }
}