using Kamban.Repository.Attributes;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace Kamban.ViewModels.Core
{
    public class RowViewModel : ReactiveObject, IDim
    {
        public RowViewModel() { }

        [Reactive] public int Id { get; set; }
        [AutoSave, Reactive] public int BoardId { get; set; }
        [AutoSave, Reactive] public string Name { get; set; }
        [AutoSave, Reactive] public int Size { get; set; }
        [AutoSave, Reactive] public int Order { get; set; }
    }
}
