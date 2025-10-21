public class AddNoteDto
{
    public Guid RunId { get; set; }
    public string? Note { get; set; }
    public string Status { get; set; }

    public string? CreatedBy { get; set; }
}
