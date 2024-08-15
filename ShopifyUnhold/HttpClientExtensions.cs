namespace ShopifyUnhold;

public static class HttpClientExtensions
{
    public static void SetShopifyToken(this HttpClient httpClient, string token)
    {
        httpClient.DefaultRequestHeaders.Add("X-Shopify-Access-Token", token);
    }
}
