using AutoItX3Lib;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using System.Data;
using System.Globalization;

namespace SeleniumProject;

internal class Program
{
    public static void Main(string[] args)
    {
        MainAsync(args).GetAwaiter().GetResult();
    }

    private static async Task MainAsync(string[] args)
    {
        string videoPath = args[0];

        var options = new ChromeOptions();
        var driver = new ChromeDriver(options);

        driver.Navigate().GoToUrl("https://www.instagram.com/accounts/login/?source=auth_switcher");
        Thread.Sleep(2000);

        // Логика авторизации
        SignIn(driver);

        AddVideo(driver, videoPath);

        PostVideo(driver);
    }

    public static void PostVideo(ChromeDriver driver)
    {
        // Попытка найти элемент по XPath
        var element = driver.FindElement(By.XPath("/html/body/div[6]/div[2]/div/div/div[1]/div/div[2]/div/div/div/div/div[2]/div/div/div[3]/div/div[4]/button"));

        // Если элемент найден, выполняем нужный код
        if (element != null)
        {
            Thread.Sleep(1000);
            element.Click();
            Thread.Sleep(1000);
            // Ваш код здесь, если элемент найден
            Console.WriteLine("Элемент найден, выполняем действие.");
        }

        driver.FindElement(By.XPath("/html/body/div[6]/div[1]/div/div[3]/div/div/div/div/div/div/div/div[2]/div[1]/div/div/div/div[1]/div/div[2]/div/button")).Click();
        Thread.Sleep(1000);

        driver.FindElement(By.XPath("/html/body/div[6]/div[1]/div/div[3]/div/div/div/div/div/div/div/div[2]/div[1]/div/div/div/div[1]/div/div[1]/div/div[3]")).Click();
        Thread.Sleep(1000);

        driver.FindElement(By.XPath("/html/body/div[6]/div[1]/div/div[3]/div/div/div/div/div/div/div/div[1]/div/div/div/div[3]/div/div")).Click();
        Thread.Sleep(1000);

        driver.FindElement(By.XPath("/html/body/div[6]/div[1]/div/div[3]/div/div/div/div/div/div/div/div[1]/div/div/div/div[3]/div/div")).Click();
        Thread.Sleep(1000);

        driver.FindElement(By.XPath("/html/body/div[6]/div[1]/div/div[3]/div/div/div/div/div/div/div/div[1]/div/div/div/div[3]/div/div")).Click();
        Thread.Sleep(35000);

        driver.Navigate().GoToUrl("https://www.instagram.com/");
    }

    private static void SignIn(ChromeDriver driver)
    {
        var loginField = driver.FindElement(By.XPath("//*[@id=\"loginForm\"]/div/div[1]/div/label"));
        loginField.SendKeys("_bimbimbambam_");
        Thread.Sleep(2000);

        var passwordField = driver.FindElement(By.XPath("//*[@id=\"loginForm\"]/div/div[2]/div/label"));
        passwordField.SendKeys("Sasha19571958$");
        Thread.Sleep(2000);

        driver.FindElement(by: By.XPath("//*[@id=\"loginForm\"]/div/div[3]/button")).Click();
        Thread.Sleep(7000);
    }

    private static void AddVideo(ChromeDriver driver, string path)
    {
        driver.FindElement(by: By.XPath("/html/body/div[2]/div/div/div[2]/div/div/div[1]/div[1]/div[2]/div/div/div/div/div[2]/div[7]/div/span/div/a")).Click();
        Thread.Sleep(2000);

        driver.FindElement(by: By.XPath("/html/body/div[2]/div/div/div[2]/div/div/div[1]/div[1]/div[2]/div/div/div/div/div[2]/div[7]/div/span/div/div/div/div[1]/a[1]")).Click();
        Thread.Sleep(2000);

        driver.FindElement(by: By.XPath("/html/body/div[6]/div[1]/div/div[3]/div/div/div/div/div/div/div/div[2]/div[1]/div/div/div[2]/div")).Click();

        UploadFile(path, new AutoItX3());

        Thread.Sleep(2000);  // Ждём завершения загрузки видео
    }

    private static void UploadFile(string filePath, AutoItX3 autoIt)
    {
        autoIt.WinWait("Открытие", "", 10);  // Ждём появления окна

        // Активируем окно
        autoIt.WinActivate("Открытие");

        // Вводим путь к файлу
        autoIt.Send(filePath);

        // Нажимаем кнопку "Открыть"
        autoIt.Send("{Enter}");
    }
}
