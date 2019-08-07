using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace Kamban.ViewModels.ImportScheme
{
    public class BoardImportSchemeViewModel : ReactiveObject
    {
        [Reactive] public int Id { get; set; }
        [Reactive] public string Name { get; set; }
        [Reactive] public bool IsSelected { get; set; }
    }
}