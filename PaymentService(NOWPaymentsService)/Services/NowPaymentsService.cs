using System.Text;

namespace PaymentService_NOWPaymentsService_.Services;

public class NowPaymentsService : INowPaymentsService
{
    private readonly HttpClient _httpClient;

    public NowPaymentsService()
    {
        _httpClient = new HttpClient();
    }

    // Метод для получения статуса API
    public async Task CheckApiStatus(string apiKey)
    {
        _httpClient.DefaultRequestHeaders.Clear();
        _httpClient.DefaultRequestHeaders.Add("x-api-key", apiKey);
        var response = await _httpClient.GetAsync("https://api.nowpayments.io/v1/status");

        if (response.IsSuccessStatusCode)
        {
            Console.WriteLine("API доступен.");
        }
        else
        {
            Console.WriteLine("Ошибка при подключении к API: " + response.StatusCode);
        }
    }

    // Метод для получения доступных криптовалют
    public async Task GetAvailableCurrencies(string apiKey)
    {
        _httpClient.DefaultRequestHeaders.Clear();
        _httpClient.DefaultRequestHeaders.Add("x-api-key", apiKey);
        var response = await _httpClient.GetAsync("https://api.nowpayments.io/v1/currencies");

        var json = await response.Content.ReadAsStringAsync();
        Console.WriteLine("Доступные криптовалюты: " + json);
    }

    // Метод для создания платежа
    public async Task CreatePayment(string apiKey, string amount, string currency, string orderId)
    {
        _httpClient.DefaultRequestHeaders.Clear();
        _httpClient.DefaultRequestHeaders.Add("x-api-key", apiKey);

        var requestBody = new StringContent($"{{ \"price_amount\": {amount}, \"price_currency\": \"{currency}\", \"order_id\": \"{orderId}\", \"pay_currency\": \"usdtbsc\" }}", Encoding.UTF8, "application/json");

        var response = await _httpClient.PostAsync("https://api.nowpayments.io/v1/payment", requestBody);

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

    // Метод для получения статуса платежа
    public async Task GetPaymentStatus(string apiKey, string paymentId)
    {
        _httpClient.DefaultRequestHeaders.Clear();
        _httpClient.DefaultRequestHeaders.Add("x-api-key", apiKey);

        var response = await _httpClient.GetAsync($"https://api.nowpayments.io/v1/payment/{paymentId}");

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

    // Метод для получения минимальной суммы платежа
    public async Task GetMinimumPaymentAmount(string apiKey, string currencyFrom, string currencyTo)
    {
        _httpClient.DefaultRequestHeaders.Clear();
        _httpClient.DefaultRequestHeaders.Add("x-api-key", apiKey);

        string url = $"https://api.nowpayments.io/v1/min-amount?currency_from={currencyFrom}&currency_to={currencyTo}&fiat_equivalent=usd";

        var response = await _httpClient.GetAsync(url);

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
}