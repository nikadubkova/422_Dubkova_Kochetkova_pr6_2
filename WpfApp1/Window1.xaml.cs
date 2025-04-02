using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;

namespace WpfApp1
{
    public partial class Window1 : Window
    {
        private clinicEntities db = new clinicEntities();
        private Users currentUser;
        private string userRole;

        public ObservableCollection<UserViewModel> UsersList { get; set; }

        public Window1(Users user, string role)
        {
            InitializeComponent();
            currentUser = user;
            userRole = role;

            UsersList = new ObservableCollection<UserViewModel>();
            DataGridUsers.ItemsSource = UsersList;

            ConfigureUIForRole();
            LoadUsers();

            WelcomeTextBlock.Text = $"Добро пожаловать, {userRole} {user.FirstName} {user.MiddleName}!";
        }

        private void ConfigureUIForRole()
        {
            switch (userRole)
            {
                case "Администратор":
                    break;
                case "Врач":
                    AddButton.Visibility = Visibility.Collapsed;
                    DeleteButton.Visibility = Visibility.Collapsed;
                    break;
                case "Менеджер":
                    DeleteButton.Visibility = Visibility.Collapsed;
                    break;
                case "Пациент":
                    AddButton.Visibility = Visibility.Collapsed;
                    EditButton.Visibility = Visibility.Collapsed;
                    DeleteButton.Visibility = Visibility.Collapsed;
                    break;
            }
        }

        private void LoadUsers()
        {
            try
            {
                UsersList.Clear();

                var users = db.Users.ToList();
                foreach (var user in users)
                {
                    var role = db.UserRoles
                        .Where(ur => ur.UserID == user.UserID)
                        .Join(db.Roles, ur => ur.RoleID, r => r.RoleID, (ur, r) => r.RoleName)
                        .FirstOrDefault();

                    UsersList.Add(new UserViewModel
                    {
                        UserID = user.UserID,
                        Username = user.Username,
                        FullName = user.LastName,
                        Email = user.Email,
                        Phone = user.Phone,
                        Role = role ?? "Не назначена"
                    });
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки пользователей: {ex.Message}", "Ошибка",
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }
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

        private void AddButton_Click(object sender, RoutedEventArgs e)
        {
            var addUserWindow = new AddUserWindow();
            if (addUserWindow.ShowDialog() == true)
            {
                LoadUsers();
            }
        }

        private void EditButton_Click(object sender, RoutedEventArgs e)
        {
            var selectedUser = DataGridUsers.SelectedItem as UserViewModel;
            if (selectedUser != null)
            {
                var editWindow = new EditUserWindow(selectedUser.UserID);
                if (editWindow.ShowDialog() == true)
                {
                    LoadUsers();
                }
            }
            else
            {
                MessageBox.Show("Выберите пользователя для редактирования", "Ошибка",
                              MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            var selectedUser = DataGridUsers.SelectedItem as UserViewModel;
            if (selectedUser != null)
            {
                if (MessageBox.Show($"Вы уверены, что хотите удалить пользователя {selectedUser.FullName}?",
                    "Подтверждение удаления", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                {
                    try
                    {
                        var userToDelete = db.Users.Find(selectedUser.UserID);
                        if (userToDelete != null)
                        {
                            db.Users.Remove(userToDelete);
                            db.SaveChanges();
                            LoadUsers();
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Ошибка при удалении пользователя: {ex.Message}", "Ошибка",
                                      MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
            else
            {
                MessageBox.Show("Выберите пользователя для удаления", "Ошибка",
                              MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void LogoutButton_Click(object sender, RoutedEventArgs e)
        {
            MainWindow authWindow = new MainWindow();
            authWindow.Show();
            this.Close();
        }
    }

    public class UserViewModel
    {
        public int UserID { get; set; }
        public string Username { get; set; }
        public string FullName { get; set; }
        public string Email { get; set; }
        public string Phone { get; set; }
        public string Role { get; set; }
    }
}