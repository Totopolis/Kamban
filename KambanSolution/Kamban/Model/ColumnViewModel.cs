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
    public class ColumnViewModel : ReactiveObject, IDim
    {
        public ColumnViewModel() { }

        [Reactive] public int Id { get; set; }
        [Reactive] public int BoardId { get; set; }
        [Reactive] public string Name { get; set; }
        [Reactive] public int Size { get; set; }
        [Reactive] public int Order { get; set; }

        [Reactive] public int CurNumberOfCards { get; set; }
    }
}
