using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using Microsoft.Win32;

namespace WpfApp1
{
    public partial class RegPage : Window
    {
        private clinicEntities db = new clinicEntities();

        public RegPage()
        {
            InitializeComponent();
            LoadRoles();
        }

        public void LoadRoles()
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

        public bool ValidateInput()
        {
            // Проверка обязательных полей
            if (string.IsNullOrWhiteSpace(LastNameTextBox.Text) ||
                string.IsNullOrWhiteSpace(FirstNameTextBox.Text) ||
                string.IsNullOrWhiteSpace(LoginTextBox.Text) ||
                string.IsNullOrWhiteSpace(PasswordBox.Password) ||
                string.IsNullOrWhiteSpace(ConfirmPasswordBox.Password) ||
                string.IsNullOrWhiteSpace(EmailTextBox.Text) ||
                RoleComboBox.SelectedItem == null)
            {
                MessageBox.Show("Пожалуйста, заполните все обязательные поля!",
                              "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }



            // Проверка паролей
            if (PasswordBox.Password != ConfirmPasswordBox.Password)
            {
                MessageBox.Show("Пароли не совпадают!", "Ошибка",
                              MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }

            if (PasswordBox.Password.Length < 6)
            {
                MessageBox.Show("Пароль должен содержать минимум 6 символов!",
                              "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }

            return true;
        }

        private bool IsValidEmail(string email)
        {
            try
            {
                var addr = new System.Net.Mail.MailAddress(email);
                return addr.Address == email;
            }
            catch
            {
                return false;
            }
        }

        public void RegisterButton_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidateInput())
                return;

            try
            {
                // Проверка уникальности логина
                if (db.Users.Any(u => u.Username == LoginTextBox.Text))
                {
                    MessageBox.Show("Пользователь с таким логином уже существует!",
                                  "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                // Проверка уникальности email
                if (db.Users.Any(u => u.Email == EmailTextBox.Text))
                {
                    MessageBox.Show("Пользователь с таким email уже существует!",
                                  "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                // Определение пола
                string gender = MaleRadioButton.IsChecked == true ? "Мужской" : "Женский";

                // Создание нового пользователя
                var newUser = new Users
                {
                    LastName = LastNameTextBox.Text,
                    FirstName = FirstNameTextBox.Text,
                    MiddleName = MiddleNameTextBox.Text,
                    Username = LoginTextBox.Text,
                    PasswordHash = HashPassword(PasswordBox.Password),
                    Email = EmailTextBox.Text,
                    Gender = gender,
                    Phone = PhoneTextBox.Text,
                    PhotoPath = PhotoPathTextBox.Text,
                    RegistrationDate = DateTime.Now,
                    IsActive = true
                };

                // Добавление пользователя в БД
                db.Users.Add(newUser);
                db.SaveChanges();

                // Получение выбранной роли
                var selectedRole = db.Roles.FirstOrDefault(r => r.RoleName == RoleComboBox.SelectedItem.ToString());
                if (selectedRole != null)
                {
                    // Добавление связи пользователь-роль
                    db.UserRoles.Add(new UserRoles
                    {
                        UserID = newUser.UserID,
                        RoleID = selectedRole.RoleID,
                        AssignedDate = DateTime.Now
                    });
                    db.SaveChanges();
                }

                MessageBox.Show("Регистрация прошла успешно!", "Успех",
                              MessageBoxButton.OK, MessageBoxImage.Information);

                // Возврат на страницу авторизации
                BackToAuth();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при регистрации: {ex.Message}", "Ошибка",
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public string HashPassword(string password)
        {
            using (var sha256 = System.Security.Cryptography.SHA256.Create())
            {
                var bytes = System.Text.Encoding.UTF8.GetBytes(password);
                var hashBytes = sha256.ComputeHash(bytes);
                return Convert.ToBase64String(hashBytes);
            }
        }

        private void BrowsePhotoButton_Click(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new OpenFileDialog
            {
                Filter = "Image Files (*.jpg; *.jpeg; *.png)|*.jpg; *.jpeg; *.png",
                Title = "Выберите фото пользователя"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                PhotoPathTextBox.Text = openFileDialog.FileName;

                // Показываем превью фото
                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.UriSource = new Uri(openFileDialog.FileName);
                bitmap.EndInit();
                UserPhoto.Source = bitmap;
            }
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            BackToAuth();
        }

        private void BackToAuth()
        {
            MainWindow authWindow = new MainWindow();
            authWindow.Show();
            this.Close();
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                DragMove();
            }
        }

        private void PhoneTextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            var textBox = sender as TextBox;
            string newText = textBox.Text.Insert(textBox.CaretIndex, e.Text);

            // Разрешаем только цифры
            if (!char.IsDigit(e.Text, 0))
            {
                e.Handled = true;
                return;
            }

            // Проверяем соответствие маске +7 (XXX) XXX-XX-XX
            e.Handled = !IsValidPhoneInput(newText);
        }

        private void PhoneTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            var textBox = sender as TextBox;
            if (textBox.IsFocused) // Чтобы не срабатывало при программном изменении
            {
                // Автоматически добавляем элементы маски
                if (textBox.Text.Length == 1 && !textBox.Text.StartsWith("+"))
                {
                    textBox.Text = "+7" + textBox.Text;
                    textBox.CaretIndex = textBox.Text.Length;
                }
                else if (textBox.Text.Length == 2 && !textBox.Text.StartsWith("+7"))
                {
                    textBox.Text = "+7" + textBox.Text.Substring(1);
                    textBox.CaretIndex = textBox.Text.Length;
                }
                else if (textBox.Text.Length == 3 && textBox.Text != "+7 ")
                {
                    textBox.Text = "+7 " + textBox.Text.Substring(2);
                    textBox.CaretIndex = textBox.Text.Length;
                }
                else if (textBox.Text.Length == 6 && !textBox.Text.Contains("("))
                {
                    textBox.Text = textBox.Text.Insert(3, "(");
                    textBox.CaretIndex = textBox.Text.Length;
                }
                else if (textBox.Text.Length == 7 && textBox.Text[6] != ')')
                {
                    textBox.Text = textBox.Text.Insert(6, ") ");
                    textBox.CaretIndex = textBox.Text.Length;
                }
                else if (textBox.Text.Length == 12 && textBox.Text[11] != '-')
                {
                    textBox.Text = textBox.Text.Insert(11, "-");
                    textBox.CaretIndex = textBox.Text.Length;
                }
                else if (textBox.Text.Length == 15 && textBox.Text[14] != '-')
                {
                    textBox.Text = textBox.Text.Insert(14, "-");
                    textBox.CaretIndex = textBox.Text.Length;
                }
            }
        }

        private void PhoneTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            var textBox = sender as TextBox;
            if (!IsValidPhoneNumber(textBox.Text))
            {
                MessageBox.Show("Введите телефон в формате: +7 (XXX) XXX-XX-XX",
                              "Неверный формат", MessageBoxButton.OK, MessageBoxImage.Warning);
                textBox.Focus();
            }
        }

        private void PhoneTextBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            var textBox = sender as TextBox;

            // Запрещаем удаление символов маски
            if (e.Key == Key.Back && IsPhoneMaskCharacter(textBox.CaretIndex - 1))
            {
                e.Handled = true;
            }
            else if (e.Key == Key.Delete && IsPhoneMaskCharacter(textBox.CaretIndex))
            {
                e.Handled = true;
            }
        }

        private bool IsPhoneMaskCharacter(int position)
        {
            // Позиции символов маски: +, 7, пробел, (, ), пробел, -, -
            int[] maskPositions = { 0, 1, 2, 3, 6, 7, 11, 14 };
            return maskPositions.Contains(position);
        }

        private bool IsValidPhoneInput(string text)
        {
            return Regex.IsMatch(text, @"^\+7 \(\d{0,3}\) \d{0,3}-\d{0,2}-\d{0,2}$");
        }

        private bool IsValidPhoneNumber(string phone)
        {
            return Regex.IsMatch(phone, @"^\+7 \(\d{3}\) \d{3}-\d{2}-\d{2}$");
        }

        #region Кастомная маска для email
        private void EmailTextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            var textBox = sender as TextBox;
            string newText = textBox.Text.Insert(textBox.CaretIndex, e.Text);

            // Запрещаем пробелы
            if (e.Text.Contains(" "))
            {
                e.Handled = true;
                return;
            }

            // Проверяем допустимые символы
            if (!Regex.IsMatch(e.Text, @"^[a-zA-Z0-9@._-]*$"))
            {
                e.Handled = true;
                return;
            }

            // Проверяем, что @ не вводится повторно
            if (e.Text == "@" && textBox.Text.Contains("@"))
            {
                e.Handled = true;
                return;
            }
        }

        private void EmailTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            var textBox = sender as TextBox;
            if (textBox.IsFocused && !textBox.Text.Contains("@") && textBox.Text.Length > 0)
            {
                // Автоматически добавляем @ после первого символа
                textBox.Text += "@";
                textBox.CaretIndex = textBox.Text.Length;
            }
        }

    }
    #endregion
}