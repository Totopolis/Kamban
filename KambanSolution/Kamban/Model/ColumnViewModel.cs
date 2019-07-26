using Kamban.MatrixControl;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace Kamban.Model
{
    public class ColumnViewModel : ReactiveObject, IDim
    {
        public ColumnViewModel() { }

        [Reactive] public int Id { get; set; }
        [Reactive] public int BoardId { get; set; }
        [Reactive] public string Name { get; set; }
        [Reactive] public int Size { get; set; }
        [Reactive] public int Order { get; set; }
        [Reactive] public int CurNumberOfCards { get; set; } = 0;

        [Reactive] public bool LimitSet { get; set; } = true;
        [Reactive] public int MaxNumberOfCards { get; set; } = 5;

    }
}
