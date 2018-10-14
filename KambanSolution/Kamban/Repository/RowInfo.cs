namespace Kamban.Model
{
    public class RowInfo
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public int Order { get; set; }
        public BoardInfo Board { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
    }
}
