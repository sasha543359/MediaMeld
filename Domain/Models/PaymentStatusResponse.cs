using Newtonsoft.Json;

namespace Domain.Models;

public class PaymentStatusResponse
{

    [JsonProperty("payment_id")]
    public string PaymentId { get; set; }

    [JsonProperty("payment_status")]
    public string PaymentStatus { get; set; }

    [JsonProperty("pay_address")]
    public string PayAddress { get; set; }

    [JsonProperty("price_amount")]
    public decimal? PriceAmount { get; set; }

    [JsonProperty("price_currency")]
    public string PriceCurrency { get; set; }

    [JsonProperty("pay_amount")]
    public decimal? PayAmount { get; set; }

    [JsonProperty("pay_currency")]
    public string PayCurrency { get; set; }

    [JsonProperty("actually_paid")]
    public decimal? ActuallyPaid { get; set; }

    [JsonProperty("created_at")]
    public DateTime CreatedAt { get; set; }

    [JsonProperty("updated_at")]
    public DateTime UpdatedAt { get; set; }

    [JsonProperty("payin_hash")]
    public string PayinHash { get; set; }

    [JsonProperty("outcome_currency")]
    public string OutcomeCurrency { get; set; }
}