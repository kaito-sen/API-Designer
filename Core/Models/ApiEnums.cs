namespace API_Integarated.Core.Models
{
    public enum ApiMethod
    {
        GET,
        POST,
        PUT,
        DELETE,
        PATCH,
        HEAD,
        OPTIONS
    }

    public enum ApiAuthType
    {
        None,
        BearerToken,
        ApiKey,
        BasicAuth,
        CustomHeader
    }

    public enum ApiKeyLocation
    {
        Header,
        QueryString
    }

    public enum ApiBodyType
    {
        None,
        Json,
        FormUrlEncoded,
        FormData,
        RawText
    }
}
