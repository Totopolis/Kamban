namespace Kamban.Contracts
{
    public class Column
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public int Order { get; set; }
        public int BoardId { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
        public bool LimitSet { get; set; }
        public int MaxNumberOfCards { get; set; }
    }
}
