using System.Text.Json.Serialization;

namespace JohnIsDev.Core.LLM.Models;

/// <summary>
/// Represents the response from Azure OpenAI API.
/// </summary>
public class ResponseAzureOpenAi
{
    /// <summary>
    /// Gets or sets the timestamps associated with the creation of the response.
    /// This property maps to the "created_at" field in the JSON response.
    /// </summary>
    [JsonPropertyName("created_at")]
    private string RegDateTimStamps { get; set; } = "";
    
    /// <summary>
    /// Gets the date and time when the response was created.
    /// </summary>
    public DateTime RegDate => DateTime.Parse(RegDateTimStamps);

    /// <summary>
    /// Gets or sets the timestamps associated with the completion of the response.
    /// </summary>
    [JsonPropertyName("completed_at")]
    private string FinishDateTimStamps { get; set; } = "";
    
    /// <summary>
    /// Gets the date and time when the response was completed.
    /// </summary>
    public DateTime FinishDate => DateTime.Parse(FinishDateTimStamps);

    /// <summary>
    /// Gets or sets the outputs contained in the response.
    /// </summary>
    [JsonPropertyName("output")]
    public List<ResponseOutput> Outputs { get; set; } = [];
}

/// <summary>
/// Represents the output contained in the response from the Azure OpenAI API.
/// </summary>
public class ResponseOutput
{
    /// <summary>
    /// Gets or sets the content of the response.
    /// </summary>
    [JsonPropertyName("content")]
    public List<ResponseContent> Answers { get; set; } = [];
}

public class ResponseContent
{
    [JsonPropertyName("status")] 
    public string Status { get; set; } = "";
    
    [JsonPropertyName( "type")] 
    public string ContentType { get; set; } = "";

    [JsonPropertyName("text")] 
    public string Answer { get; set; } = "";
}


