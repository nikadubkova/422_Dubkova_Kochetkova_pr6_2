using Microsoft.VisualStudio.TestTools.UnitTesting;
using WpfApp1;
using System.Linq;

namespace UnitTestProject
{
    [TestClass]
    public class UnitTest3
    {
        [TestMethod]
        public void AuthTestSuccess()
        {
            var mainWindow = new MainWindow();
            var db = new clinicEntities();
            var users = db.Users.ToList();

            foreach (var user in users)
            {

                string testPassword = GetTestPasswordForUser(user.Username);

                var result = mainWindow.AuthenticateUser(user.Username, testPassword);

                Assert.IsTrue(result.IsSuccess,
                    $"Ошибка авторизации для пользователя {user.Username}");
                Assert.IsNotNull(result.User,
                    $"Не возвращен объект пользователя для {user.Username}");
            }
        }
        private string GetTestPasswordForUser(string username)
        {
            // В реальном проекте пароли должны храниться иначе!
            // Это только для тестового примера
            switch (username)
            {
                case "admin01":
                    return "admin10";
                case "admin":
                    return "admin123";
                case "derikser":
                    return "denis123";
                default:
                    return "defaultPassword";
            }
        }
    }
}