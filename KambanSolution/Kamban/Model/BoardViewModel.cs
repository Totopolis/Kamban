using DynamicData;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;
using Ui.Wpf.Common;

namespace Kamban.Model
{
    public class BoardViewModel : ReactiveObject
    {
        [Reactive] public int Id { get; set; }
        [Reactive] public string Name { get; set; }
        [Reactive] public DateTime Created { get; set; }
        [Reactive] public DateTime Modified { get; set; }

        [Reactive] public CommandItem MenuCommand { get; set; }
        [Reactive] public bool IsChecked { get; set; }

        public BoardViewModel()
        {
            this.WhenAnyValue(x => x.Name)
                .Where(x => MenuCommand != null)
                .Subscribe(x => MenuCommand.Name = x);

            this.WhenAnyValue(x => x.IsChecked)
                .Where(x => MenuCommand != null)
                .Subscribe(x => MenuCommand.IsChecked = x);
        }
    }
}
