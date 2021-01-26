using System;
using System.Reactive.Linq;
using Kamban.Repository.Attributes;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using wpf.ui;

namespace Kamban.ViewModels.Core
{
    public class BoardViewModel : ReactiveObject
    {
        [Reactive] public int Id { get; set; }
        [AutoSave, Reactive] public string Name { get; set; }
        [AutoSave, Reactive] public DateTime Created { get; set; }
        [AutoSave, Reactive] public DateTime Modified { get; set; }

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
