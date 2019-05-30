namespace Kamban.Repository.Models
{
    public class Row
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public int Order { get; set; }
        public int BoardId { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
    }
}
