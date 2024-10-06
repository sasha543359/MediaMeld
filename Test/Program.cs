using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

// await CheckApiStatus("HWHWJ12-XMBMH68-GSH7XSV-5YE60WJ");

// await GetAvailableCurrencies("HWHWJ12-XMBMH68-GSH7XSV-5YE60WJ");

// await CreatePayment("HWHWJ12-XMBMH68-GSH7XSV-5YE60WJ", "8", "usdtbsc", $"{Guid.NewGuid()}");

// await GetPaymentStatus("HWHWJ12-XMBMH68-GSH7XSV-5YE60WJ", "5829458757");

await GetMinimumPaymentAmount("HWHWJ12-XMBMH68-GSH7XSV-5YE60WJ", "usdtbsc", "usdtbsc");

async Task CheckApiStatus(string apiKey)
{
    using HttpClient client = new HttpClient();
    client.DefaultRequestHeaders.Add("x-api-key", apiKey);
    var response = await client.GetAsync("https://api.nowpayments.io/v1/status");

    if (response.IsSuccessStatusCode)
    {
        Console.WriteLine("API доступен.");
    }
    else
    {
        Console.WriteLine("Ошибка при подключении к API: " + response.StatusCode);
    }
}

async Task GetAvailableCurrencies(string apiKey)
{
    using HttpClient client = new HttpClient();
    client.DefaultRequestHeaders.Add("x-api-key", apiKey);
    var response = await client.GetAsync("https://api.nowpayments.io/v1/currencies");

    var json = await response.Content.ReadAsStringAsync();
    Console.WriteLine("Доступные криптовалюты: " + json);
}

async Task CreatePayment(string apiKey, string amount, string currency, string orderId)
{
    using HttpClient client = new HttpClient();
    client.DefaultRequestHeaders.Add("x-api-key", apiKey);

    // Указываем pay_currency и price_currency на usdtbsc для использования сети BSC
    var requestBody = new StringContent($"{{ \"price_amount\": {amount}, \"price_currency\": \"{currency}\", \"order_id\": \"{orderId}\", \"pay_currency\": \"usdtbsc\" }}", Encoding.UTF8, "application/json");

    var response = await client.PostAsync("https://api.nowpayments.io/v1/payment", requestBody);

    if (response.IsSuccessStatusCode)
    {
        var jsonResponse = await response.Content.ReadAsStringAsync();
        Console.WriteLine("Платеж успешно создан: " + jsonResponse);
    }
    else
    {
        Console.WriteLine("Ошибка при создании платежа: " + response.StatusCode);
    }
}

async Task GetPaymentStatus(string apiKey, string paymentId)
{
    using HttpClient client = new HttpClient();
    client.DefaultRequestHeaders.Add("x-api-key", apiKey);

    var response = await client.GetAsync($"https://api.nowpayments.io/v1/payment/{paymentId}");

    if (response.IsSuccessStatusCode)
    {
        var jsonResponse = await response.Content.ReadAsStringAsync();
        Console.WriteLine("Статус платежа: " + jsonResponse);
    }
    else
    {
        Console.WriteLine("Ошибка при получении статуса платежа: " + response.StatusCode);
    }
}

async Task GetMinimumPaymentAmount(string apiKey, string currencyFrom, string currencyTo)
{
    using HttpClient client = new HttpClient();
    client.DefaultRequestHeaders.Add("x-api-key", apiKey);

    string url = $"https://api.nowpayments.io/v1/min-amount?currency_from={currencyFrom}&currency_to={currencyTo}&fiat_equivalent=usd";

    var response = await client.GetAsync(url);

    if (response.IsSuccessStatusCode)
    {
        var jsonResponse = await response.Content.ReadAsStringAsync();
        Console.WriteLine("Минимальная сумма платежа: " + jsonResponse);
    }
    else
    {
        Console.WriteLine("Ошибка при получении минимальной суммы: " + response.StatusCode);
    }
}