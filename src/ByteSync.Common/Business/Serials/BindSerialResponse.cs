namespace ByteSync.Common.Business.Serials;

public class BindSerialResponse
{
    public BindSerialResponse()
    {
        
    }
    
    public BindSerialResponse(BindSerialResponseStatus status, ProductSerialDescription? productSerialDescription)
    {
        Status = status;
        ProductSerialDescription = productSerialDescription;
    }

    public BindSerialResponseStatus Status { get; set; }
    
    public ProductSerialDescription? ProductSerialDescription { get; set; }
}