namespace Kamban.ViewModels.Core
{
    public interface IDim
    {
        int Id { get; set; }
        string Name { get; set; }
        string FullName { get; }
        int Size { get; set; }
        int Order { get; set; }
    }
}