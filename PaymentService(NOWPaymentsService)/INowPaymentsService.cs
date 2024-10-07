using PaymentService_NOWPaymentsService_.Models;

namespace PaymentService_NOWPaymentsService_;

public interface INowPaymentsService
{
    Task CheckApiStatus(string apiKey);
    Task GetAvailableCurrencies(string apiKey);
    Task<CreatePaymentResponse> CreatePayment(string apiKey, string amount, string currency, string orderId);
    Task<PaymentStatusResponse> GetPaymentStatus(string apiKey, string paymentId);
    Task GetMinimumPaymentAmount(string apiKey, string currencyFrom, string currencyTo);
}