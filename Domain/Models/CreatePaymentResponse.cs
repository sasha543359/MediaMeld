using Newtonsoft.Json;

namespace Domain.Models;

public class CreatePaymentResponse
{

    [JsonProperty("payment_id")]
    public string PaymentId { get; set; }

    [JsonProperty("payment_status")]
    public string PaymentStatus { get; set; }

    [JsonProperty("pay_address")]
    public string PayAddress { get; set; }

    [JsonProperty("price_amount")]
    public decimal PriceAmount { get; set; }

    [JsonProperty("price_currency")]
    public string PriceCurrency { get; set; }

    [JsonProperty("pay_amount")]
    public decimal PayAmount { get; set; }

    [JsonProperty("pay_currency")]
    public string PayCurrency { get; set; }

    [JsonProperty("order_id")]
    public string OrderId { get; set; }

    [JsonProperty("order_description")]
    public string OrderDescription { get; set; }

    [JsonProperty("ipn_callback_url")]
    public string IpnCallbackUrl { get; set; }

    [JsonProperty("created_at")]
    public DateTime CreatedAt { get; set; }

    [JsonProperty("updated_at")]
    public DateTime UpdatedAt { get; set; }

    [JsonProperty("purchase_id")]
    public string PurchaseId { get; set; }

    [JsonProperty("amount_received")]
    public decimal? AmountReceived { get; set; }

    [JsonProperty("payin_extra_id")]
    public string PayinExtraId { get; set; }

    [JsonProperty("smart_contract")]
    public string SmartContract { get; set; }

    [JsonProperty("network")]
    public string Network { get; set; }

    [JsonProperty("network_precision")]
    public int? NetworkPrecision { get; set; }  // Сделали тип int? чтобы допускать null

    [JsonProperty("time_limit")]
    public int? TimeLimit { get; set; }

    [JsonProperty("burning_percent")]
    public decimal? BurningPercent { get; set; }

    [JsonProperty("expiration_estimate_date")]
    public DateTime ExpirationEstimateDate { get; set; }
}