namespace PaymentService_NOWPaymentsService_.Models;

public class CreatePaymentResponse
{
    public string PaymentId { get; set; }
    public string PaymentStatus { get; set; }
    public string PayAddress { get; set; }
    public decimal PriceAmount { get; set; }
    public string PriceCurrency { get; set; }
    public decimal PayAmount { get; set; }
    public string PayCurrency { get; set; }
    public string OrderId { get; set; }
    public DateTime ExpirationEstimateDate { get; set; }
}