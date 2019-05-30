namespace Kamban.Repository.Models
{
    public class CardFilter
    {
        public static CardFilter None => new CardFilter();
            
        public int[] BoardIds { get; set; }
        public int[] RowIds { get; set; }
        public int[] ColumnIds { get; set; }

        public bool IsEmpty =>
            BoardIds == null && RowIds == null && ColumnIds == null;
    }
}