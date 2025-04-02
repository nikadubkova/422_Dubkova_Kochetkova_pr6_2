using Microsoft.VisualStudio.TestTools.UnitTesting;
using WpfApp1;
using System.Linq;
using System.Windows.Controls;
using System;

namespace UnitTestProject
{
    [TestClass]
    public class RegistrationTests
    {
        private clinicEntities _db;

        [TestInitialize]
        public void Initialize()
        {
            _db = new clinicEntities();
            CleanTestData();

            // Добавляем тестовые роли, если их нет
            if (!_db.Roles.Any())
            {
                _db.Roles.Add(new Roles { RoleName = "Посетитель" });
                _db.Roles.Add(new Roles { RoleName = "Волонтер" });
                _db.SaveChanges();
            }
        }

        [TestMethod]
        public void Register_ValidDataWithAllFields_Success()
        {
            // Arrange
            string testEmail = "full@example.com";
            string testLogin = "fulluser";
            int initialUserCount = _db.Users.Count();

            // Act
            var regPage = new RegPage();
            bool result = RegisterUserThroughUI(regPage,
                lastName: "Иванов",
                firstName: "Иван",
                middleName: "Иванович",
                login: testLogin,
                password: "Pass123!",
                confirmPassword: "Pass123!",
                email: testEmail,
                phone: "+7 (123) 456-78-90",
                gender: "Мужской",
                role: "Посетитель");

            // Assert
            Assert.IsTrue(result, "Регистрация должна быть успешной");
            Assert.AreEqual(initialUserCount + 1, _db.Users.Count(), "Должен добавиться 1 пользователь");
            Assert.IsTrue(_db.Users.Any(u => u.Email == testEmail), "Пользователь должен быть в БД");
        }

        [TestMethod]
        public void Register_DuplicateEmail_Failure()
        {
            // Arrange
            string existingEmail = "duplicate@example.com";
            CreateTestUser("user1", "pass1", existingEmail);
            int initialUserCount = _db.Users.Count();

            // Act
            var regPage = new RegPage();
            bool result = RegisterUserThroughUI(regPage,
                lastName: "Сидоров",
                firstName: "Сидор",
                login: "newuser",
                password: "Pass123!",
                confirmPassword: "Pass123!",
                email: existingEmail,
                role: "Посетитель");

            // Assert
            Assert.IsFalse(result, "Регистрация должна завершиться ошибкой");
            Assert.AreEqual(initialUserCount, _db.Users.Count(), "Не должно добавлять нового пользователя");
        }



        [TestMethod]
        public void Register_PasswordMismatch_Failure()
        {
            // Arrange
            string testEmail = "mismatch@example.com";
            int initialUserCount = _db.Users.Count();

            // Act
            var regPage = new RegPage();
            bool result = RegisterUserThroughUI(regPage,
                lastName: "Федоров",
                firstName: "Федор",
                login: "fedor",
                password: "Pass123!",
                confirmPassword: "Different456!",
                email: testEmail,
                role: "Посетитель");

            // Assert
            Assert.IsFalse(result, "Регистрация должна завершиться ошибкой");
            Assert.AreEqual(initialUserCount, _db.Users.Count(), "Не должно добавлять нового пользователя");
        }

        [TestMethod]
        public void Register_ShortPassword_Failure()
        {
            // Arrange
            string testEmail = "shortpass@example.com";
            int initialUserCount = _db.Users.Count();

            // Act
            var regPage = new RegPage();
            bool result = RegisterUserThroughUI(regPage,
                lastName: "Семенов",
                firstName: "Семен",
                login: "semen",
                password: "123",
                confirmPassword: "123",
                email: testEmail,
                role: "Посетитель");

            // Assert
            Assert.IsFalse(result, "Регистрация должна завершиться ошибкой");
            Assert.AreEqual(initialUserCount, _db.Users.Count(), "Не должно добавлять нового пользователя");
        }

        [TestMethod]
        public void Register_MissingRequiredFields_Failure()
        {
            // Arrange
            string testEmail = "missing@example.com";
            int initialUserCount = _db.Users.Count();

            // Act
            var regPage = new RegPage();
            bool result = RegisterUserThroughUI(regPage,
                lastName: "", // Пропущено обязательное поле
                firstName: "Анна",
                login: "anna",
                password: "Pass123!",
                confirmPassword: "Pass123!",
                email: "", // Пропущено обязательное поле
                role: "Посетитель");

            // Assert
            Assert.IsFalse(result, "Регистрация должна завершиться ошибкой");
            Assert.AreEqual(initialUserCount, _db.Users.Count(), "Не должно добавлять нового пользователя");
        }

        private bool RegisterUserThroughUI(RegPage regPage,
            string lastName, string firstName, string login,
            string password, string confirmPassword, string email,
            string middleName = "", string phone = "", string gender = "Мужской", string role = "Посетитель")
        {
            try
            {
                // Эмулируем ввод данных через UI
                var lastNameField = regPage.FindName("LastNameTextBox") as TextBox;
                var firstNameField = regPage.FindName("FirstNameTextBox") as TextBox;
                var middleNameField = regPage.FindName("MiddleNameTextBox") as TextBox;
                var loginField = regPage.FindName("LoginTextBox") as TextBox;
                var passwordField = regPage.FindName("PasswordBox") as PasswordBox;
                var confirmPasswordField = regPage.FindName("ConfirmPasswordBox") as PasswordBox;
                var emailField = regPage.FindName("EmailTextBox") as TextBox;
                var phoneField = regPage.FindName("PhoneTextBox") as TextBox;
                var roleCombo = regPage.FindName("RoleComboBox") as ComboBox;
                var maleRadio = regPage.FindName("MaleRadioButton") as RadioButton;
                var femaleRadio = regPage.FindName("FemaleRadioButton") as RadioButton;

                if (lastNameField != null) lastNameField.Text = lastName;
                if (firstNameField != null) firstNameField.Text = firstName;
                if (middleNameField != null) middleNameField.Text = middleName;
                if (loginField != null) loginField.Text = login;
                if (passwordField != null) passwordField.Password = password;
                if (confirmPasswordField != null) confirmPasswordField.Password = confirmPassword;
                if (emailField != null) emailField.Text = email;
                if (phoneField != null) phoneField.Text = phone;
                if (roleCombo != null) roleCombo.SelectedItem = role;
                if (maleRadio != null) maleRadio.IsChecked = gender == "Мужской";
                if (femaleRadio != null) femaleRadio.IsChecked = gender == "Женский";

                // Вызываем метод регистрации
                regPage.RegisterButton_Click(null, null);

                // Проверяем результат по изменению в БД
                return _db.Users.Any(u => u.Email == email && u.Username == login);
            }
            catch
            {
                return false;
            }
        }

        private void CreateTestUser(string login, string password, string email,
                                  string firstName = "Test", string lastName = "User")
        {
            if (!_db.Users.Any(u => u.Username == login))
            {
                var regPage = new RegPage();
                var user = new Users
                {
                    Username = login,
                    PasswordHash = regPage.HashPassword(password),
                    Email = email,
                    FirstName = firstName,
                    LastName = lastName,
                    IsActive = true,
                    RegistrationDate = DateTime.Now
                };

                _db.Users.Add(user);
                _db.SaveChanges();
            }
        }

        private void CleanTestData()
        {
            var testEmails = new[] { "full@example.com", "duplicate@example.com",
                                   "mismatch@example.com", "shortpass@example.com",
                                   "missing@example.com" };

            foreach (var email in testEmails)
            {
                var user = _db.Users.FirstOrDefault(u => u.Email == email);
                if (user != null)
                {
                    var roles = _db.UserRoles.Where(ur => ur.UserID == user.UserID).ToList();
                    _db.UserRoles.RemoveRange(roles);
                    _db.Users.Remove(user);
                }
            }
            _db.SaveChanges();
        }

        [TestCleanup]
        public void Cleanup()
        {
            _db?.Dispose();
        }
    }
}