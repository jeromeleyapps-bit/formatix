namespace FormationManager.Models
{
    public class DocumentsIndexViewModel
    {
        public List<Document> Documents { get; set; } = new();
        public List<DocumentExampleItem> Examples { get; set; } = new();
    }

    public class DocumentExampleItem
    {
        public string FileName { get; set; } = string.Empty;
        public string Url { get; set; } = string.Empty;
        public long SizeKb { get; set; }
        public DateTime LastModified { get; set; }
    }
}
