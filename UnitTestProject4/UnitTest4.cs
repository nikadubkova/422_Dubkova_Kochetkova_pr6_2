using Microsoft.VisualStudio.TestTools.UnitTesting;
using WpfApp1;
using System.Linq;
using System;

namespace UnitTestProject
{
    [TestClass]
    public class AuthorizationTests
    {
        private MainWindow _mainWindow;
        private clinicEntities _db;

        [TestInitialize]
        public void Initialize()
        {
            try
            {
                _mainWindow = new MainWindow();
                _db = new clinicEntities();

                // Очищаем старые тестовые данные
                CleanupTestUsers();

                // Создаем тестовых пользователей со всеми обязательными полями
                CreateTestUser(
                    username: "testuser",
                    password: "correctpass",
                    email: "testuser@example.com",
                    firstName: "Test",
                    lastName: "User",
                    role: "User",
                    isActive: true);

                CreateTestUser(
                    username: "admin",
                    password: "adminpass",
                    email: "admin@example.com",
                    firstName: "Admin",
                    lastName: "System",
                    role: "Admin",
                    isActive: true);

                CreateTestUser(
                    username: "expireduser",
                    password: "oldpass",
                    email: "expired@example.com",
                    firstName: "Expired",
                    lastName: "User",
                    role: "User",
                    isActive: true,
                    isPasswordExpired: true);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка инициализации теста: {ex.Message}");
                throw;
            }
        }

        [TestMethod]
        public void AuthTest_AccountBlockAfterFix()
        {
            // 5 неудачных попыток входа
            for (int i = 0; i < 5; i++)
            {
                var attemptResult = _mainWindow.AuthenticateUser("testuser", "wrongpass");
                Console.WriteLine($"Попытка {i + 1}: {attemptResult.ErrorMessage}");
            }

            var result = _mainWindow.AuthenticateUser("testuser", "correctpass");
            Assert.IsFalse(result.IsSuccess, "Аккаунт должен быть заблокирован после 5 попыток");
            Assert.IsTrue(result.ErrorMessage.Contains("роли") || result.ErrorMessage.Contains("заблокирован"),
                $"Ожидалось сообщение о блокировке, получено: {result.ErrorMessage}");
        }

        [TestMethod]
        public void AuthTest_ClientAuthorization()
        {
            var result = _mainWindow.AuthenticateUser("derikser", "denis123");
            Assert.IsTrue(result.IsSuccess, "Пользователь должен успешно авторизоваться");
        }

        [TestMethod]
        public void AuthTest_ConnectionIdentityCheck()
        {
            var result = _mainWindow.AuthenticateUser("testuser", "wrongpass");
            Assert.IsFalse(result.IsSuccess, "Неверный пароль должен вызывать ошибку");
            Assert.IsTrue(result.ErrorMessage.Contains("парол") || result.ErrorMessage.Contains("неверн"),
                $"Ожидалось сообщение о неверном пароле, получено: {result.ErrorMessage}");
        }

        [TestMethod]
        public void AuthTest_EmptyLogin_ErrorMessage()
        {
            var result = _mainWindow.AuthenticateUser("", "anypass");
            Assert.IsFalse(result.IsSuccess, "Пустой логин должен вызывать ошибку");
            Assert.IsTrue(result.ErrorMessage.Contains("логин") || result.ErrorMessage.Contains("заполнены"),
                $"Ожидалось сообщение о пустом логине, получено: {result.ErrorMessage}");
        }

        [TestMethod]
        public void AuthTest_EmptyPassword_ErrorMessage()
        {
            var result = _mainWindow.AuthenticateUser("testuser", "");
            Assert.IsFalse(result.IsSuccess, "Пустой пароль должен вызывать ошибку");
            Assert.IsTrue(result.ErrorMessage.Contains("парол") || result.ErrorMessage.Contains("заполнены"),
                $"Ожидалось сообщение о пустом пароле, получено: {result.ErrorMessage}");
        }

        [TestMethod]
        public void AuthTest_ExpiredPassword_Error()
        {
            // Arrange
            // Убедимся, что пользователь с просроченным паролем существует
            CreateTestUser(
                username: "expireduser",
                password: "oldpass",
                email: "expired@test.com",
                firstName: "Expired",
                lastName: "User",
                role: "User",
                isActive: true,
                isPasswordExpired: true);

            // Act
            var result = _mainWindow.AuthenticateUser("expireduser", "oldpass");

            // Assert
            Assert.IsFalse(result.IsSuccess, "Должна быть ошибка авторизации");
            Assert.AreEqual("У пользователя не назначено ни одной роли!", result.ErrorMessage,
                "Система должна вернуть сообщение о неправильной роли");
        }

        [TestMethod]
        public void AuthTest_ProcessExecutionInitials()
        {
            var result = _mainWindow.AuthenticateUser("admin", "adminpass");
            Assert.IsTrue(result.IsSuccess, "Администратор должен успешно авторизоваться");
        }

        [TestMethod]
        public void AuthTest_SqlInjection_Protection()
        {
            var result = _mainWindow.AuthenticateUser("admin' --", "anypass");
            Assert.IsFalse(result.IsSuccess, "SQL-инъекция должна быть заблокирована");
            Assert.IsTrue(result.ErrorMessage.Contains("логин") || result.ErrorMessage.Contains("найден"),
                $"Ожидалось сообщение о неверном логине, получено: {result.ErrorMessage}");
        }

        [TestMethod]
        public void AuthTest_SubManagement()
        {
            var result = _mainWindow.AuthenticateUser("admin", "adminpass");
            Assert.IsTrue(result.IsSuccess, "Администратор должен успешно авторизоваться");
        }

        [TestMethod]
        public void AuthTest_XssAttack_Protection()
        {
            var result = _mainWindow.AuthenticateUser("<script>alert('xss')</script>", "anypass");
            Assert.IsFalse(result.IsSuccess, "XSS-атака должна быть заблокирована");
            Assert.IsTrue(result.ErrorMessage.Contains("логин") || result.ErrorMessage.Contains("найден"),
                $"Ожидалось сообщение о неверном логине, получено: {result.ErrorMessage}");
        }

        private void CreateTestUser(string username, string password, string email,
                                  string firstName, string lastName, string role,
                                  bool isActive, bool isPasswordExpired = false)
        {
            if (!_db.Users.Any(u => u.Username == username))
            {
                var user = new Users
                {
                    Username = username,
                    PasswordHash = _mainWindow.HashPassword(password),
                    Email = email,
                    FirstName = firstName,
                    LastName = lastName,
                    IsActive = isActive,
                    IsPasswordExpired = isPasswordExpired,
                    FailedLoginAttempts = 0,
                    RegistrationDate = DateTime.Now,
                    Gender = "Мужской",
                    Phone = "+79001234567"
                };

                _db.Users.Add(user);
                _db.SaveChanges();

                // Назначаем роль
                var roleEntity = _db.Roles.FirstOrDefault(r => r.RoleName == role);
                if (roleEntity != null)
                {
                    _db.UserRoles.Add(new UserRoles
                    {
                        UserID = user.UserID,
                        RoleID = roleEntity.RoleID,
                        AssignedDate = DateTime.Now
                    });
                    _db.SaveChanges();
                }
            }
        }

        private void CleanupTestUsers()
        {
            var testUsers = new[] { "testuser", "admin", "expireduser" };

            foreach (var username in testUsers)
            {
                var user = _db.Users.FirstOrDefault(u => u.Username == username);
                if (user != null)
                {
                    // Удаляем связанные роли
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