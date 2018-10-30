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
    public class DbViewModel : ReactiveObject
    {
        [Reactive] public string Uri { get; set; }
        [Reactive] public DateTime LastAccess { get; set; }
        [Reactive] public int TotalTickets { get; set; }
        [Reactive] public string BoardList { get; set; }

        [Reactive] public SourceList<BoardViewModel> Boards { get; set; }
    }
}
