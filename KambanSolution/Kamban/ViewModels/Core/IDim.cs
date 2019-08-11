namespace Kamban.ViewModels.Core
{
    public interface IDim
    {
        int Id { get; set; }
        string Name { get; set; }
        int Size { get; set; }
        int Order { get; set; }
        int CurNumberOfCards { get; set; }
        bool LimitSet { get; set; }
        int MaxNumberOfCards { get; set; }
    }
}