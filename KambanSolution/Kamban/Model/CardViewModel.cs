using Kamban.MatrixControl;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kamban.Model
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
        [Reactive] public string Header { get; set; }
        [Reactive] public string Color { get; set; }
        [Reactive] public int ColumnDeterminant { get; set; }
        [Reactive] public int RowDeterminant { get; set; }
        [Reactive] public int Order { get; set; }
        [Reactive] public string Body { get; set; }
        [Reactive] public DateTime Created { get; set; }
        [Reactive] public DateTime Modified { get; set; }
        [Reactive] public int BoardId { get; set; }
        [Reactive] public bool ShowDescription { get; set; }
    }
}
