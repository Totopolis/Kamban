using DynamicData;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kamban.Model
{
    public class BoxViewModel : ReactiveObject
    {
        [Reactive] public string Uri { get; set; }
        [Reactive] public bool Loaded { get; set; }

        [Reactive] public string Title { get; set; }
        [Reactive] public string Path { get; set; }
        [Reactive] public string SizeOf { get; set; }
        [Reactive] public DateTime LastEdit { get; set; }
        [Reactive] public int TotalTickets { get; set; }
        [Reactive] public string BoardList { get; set; }

        [Reactive] public SourceList<ColumnViewModel> Columns { get; set; }
        [Reactive] public SourceList<RowViewModel> Rows { get; set; }

        // TODO: hide SL<> Boards and make Add/Remove/AddRange methods ?
        // TODO: produce bvm.ROOC with filter ?
        // Cant do it becaus bvm not have id at add
        // what if methods add to AppModel where will be repo exec and ROOC provide?

        [Reactive] public SourceList<BoardViewModel> Boards { get; set; }

        public IObservable<bool> BoardsCountMoreOne { get; set; }

        [Reactive] public SourceList<CardViewModel> Cards { get; set; }

        public BoxViewModel()
        {
            Columns = new SourceList<ColumnViewModel>();
            Rows = new SourceList<RowViewModel>();

            Boards = new SourceList<BoardViewModel>();
            Cards = new SourceList<CardViewModel>();

            Cards
                .Connect()
                .AutoRefresh()
                .Subscribe(x => TotalTickets = Cards.Count);

            BoardsCountMoreOne = Boards
                .Connect()
                .AutoRefresh()
                .Select(x => Boards.Count > 1);

            Boards
                .Connect()
                .AutoRefresh()
                .Subscribe(bvm =>
                {
                    var lst = Boards.Items.Select(x => x.Name).ToList();
                    var str = string.Join(",", lst);
                    BoardList = str;
                });
        }
    }
}
