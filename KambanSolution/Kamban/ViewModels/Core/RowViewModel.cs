using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace Kamban.ViewModels.Core
{
    public class RowViewModel : ReactiveObject, IDim
    {
        public RowViewModel() { }

        public int Id { get; set; }
        [Reactive] public int BoardId { get; set; }
        [Reactive] public string Name { get; set; }
        [Reactive] public int Size { get; set; }
        [Reactive] public int Order { get; set; }
    }
}
