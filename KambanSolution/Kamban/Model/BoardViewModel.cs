using DynamicData;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kamban.Model
{
    public class BoardViewModel : ReactiveObject
    {
        [Reactive] public int Id { get; set; }
        [Reactive] public string Name { get; set; }
        [Reactive] public DateTime Created { get; set; }
        [Reactive] public DateTime Modified { get; set; }
        
        [Reactive] public SourceList<ColumnViewModel> Columns { get; set; }
        [Reactive] public SourceList<RowViewModel> Rows { get; set; }
        [Reactive] public SourceList<CardViewModel> Cards { get; set; }
    }
}
