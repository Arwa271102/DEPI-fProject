namespace Sakanak.BLL.DTOs.Contract;

public class ContractDocumentDto
{
    public int MediaId { get; set; }
    public string Url { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public bool IsImage { get; set; }
}
