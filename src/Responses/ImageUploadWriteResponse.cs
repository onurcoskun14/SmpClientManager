using System.Text.Json.Serialization;

namespace SmpClient.Responses;

public class ImageUploadWriteResponse
{
    public int Off { get; set; }
    
    public bool Match { get; set; }
}