using Microsoft.VisualStudio.TestTools.UnitTesting;
using WpfApp1;
using System;

namespace UnitTestProject
{
    [TestClass]
    public class UnitTest2
    {
        [TestMethod]
        public void AuthTest()
        {

            var mainWindow = new MainWindow();
            var result1 = mainWindow.AuthenticateUser("admin", "admin123");
            Assert.IsTrue(result1.IsSuccess, "Правильные учетные данные должны проходить аутентификацию");


            var result2 = mainWindow.AuthenticateUser("admin", "wrongpassword");
            Assert.IsFalse(result2.IsSuccess, "Неправильный пароль должен вызывать ошибку");
            Assert.AreEqual("Неверный пароль!", result2.ErrorMessage);


            var result3 = mainWindow.AuthenticateUser("", "");
            Assert.IsFalse(result3.IsSuccess, "Пустые учетные данные должны вызывать ошибку");


            var result4 = mainWindow.AuthenticateUser("nonexistent", "password");
            Assert.IsFalse(result4.IsSuccess, "Несуществующий пользователь должен вызывать ошибку");
        }
    }
}