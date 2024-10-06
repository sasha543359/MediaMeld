namespace PaymentService_NOWPaymentsService_;

public interface INowPaymentsService
{
    Task CheckApiStatus(string apiKey);
    Task GetAvailableCurrencies(string apiKey);
    Task CreatePayment(string apiKey, string amount, string currency, string orderId);
    Task GetPaymentStatus(string apiKey, string paymentId);
    Task GetMinimumPaymentAmount(string apiKey, string currencyFrom, string currencyTo);
}
