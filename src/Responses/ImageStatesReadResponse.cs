using System.Collections.Generic;

namespace SmpClient.Responses;

public class ImageStatesReadResponse
{
    public List<ImageState>? Images { get; set; }

    public int SplitStatus { get; set; }
}

public class ImageState
{
    public int Slot { get; set; }

    public string? Version { get; set; }

    public int Image { get; set; }
    
    public byte[]? Hash { get; set; }

    public bool Bootable { get; set; }

    public bool Pending { get; set; }

    public bool Confirmed { get; set; }

    public bool Active { get; set; }

    public bool Permanent { get; set; }
}