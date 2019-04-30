using System;
using Kamban.Repository.Attributes;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace Kamban.ViewModels.Core
{
    // def back color "#FFFFE0"
    public class CardViewModel : ReactiveObject, ICard
    {
        public CardViewModel()
        {
            this.WhenAnyValue(x => x.Body)
                .Subscribe(x => ShowDescription = !string.IsNullOrEmpty(x));
        }

        [Reactive] public int Id { get; set; }
        [AutoSave, Reactive] public string Header { get; set; }
        [AutoSave, Reactive] public string Color { get; set; }
        [AutoSave, Reactive] public int ColumnDeterminant { get; set; }
        [AutoSave, Reactive] public int RowDeterminant { get; set; }
        [AutoSave, Reactive] public int Order { get; set; }
        [AutoSave, Reactive] public string Body { get; set; }
        [AutoSave, Reactive] public DateTime Created { get; set; }
        [AutoSave, Reactive] public DateTime Modified { get; set; }
        [AutoSave, Reactive] public int BoardId { get; set; }
        [Reactive] public bool ShowDescription { get; set; }
    }
}
