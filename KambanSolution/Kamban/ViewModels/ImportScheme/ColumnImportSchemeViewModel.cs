using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace Kamban.ViewModels.ImportScheme
{
    public class ColumnImportSchemeViewModel : ReactiveObject
    {
        public int Id { get; set; }
        public int BoardId { get; set; }
        public string Name { get; set; }
        [Reactive] public bool IsSelected { get; set; }
    }
}