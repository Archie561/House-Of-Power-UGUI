using System.Collections.Generic;

public class TradeOfferData
{
    public CountryId CountryId;
    public List<ResourceData> Import;
    public List<ResourceData> Export;

    public TradeOfferData(CountryId countryId, List<ResourceData> import, List<ResourceData> export)
    {
        CountryId = countryId;
        Import = import;
        Export = export;
    }
}
