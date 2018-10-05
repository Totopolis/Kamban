using System;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using System.Windows.Media;
using AutoMapper;
using Kamban.Model;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Ui.Wpf.Common;
using Ui.Wpf.Common.ViewModels;
using Brush = System.Windows.Media.Brush;
using ColorConverter = System.Windows.Media.ColorConverter;
using WpfColor = System.Windows.Media.Color;

namespace Kamban.ViewModels
{
    public class IssueViewRequest : ViewRequest
    {
        public int IssueId { get; set; }
        public IBoardService Scope { get; set; }
        public BoardInfo Board { get; set; }
    }

    public class ColorItem
    {
        public SolidColorBrush Brush { get; set; }
        public string Name { get; set; }

        public static ColorItem I(string colorName)
        {
            ColorConverter converter = new ColorConverter();
            Color color = (Color)converter.ConvertFromInvariantString(colorName);

            return new ColorItem
            {
                Brush = new SolidColorBrush(color),
                Name = colorName
            };
        }
    }
    
    public class IssueViewModel : ViewModelBase, IInitializableViewModel
    {
        private readonly IMapper mapper;
        private IBoardService scope;
        private BoardInfo board;

        public int Id { get; set; }
        public DateTime Created { get; set; }

        public ReactiveList<RowInfo> AvailableRows { get; set; }
        public ReactiveList<ColumnInfo> AvailableColumns { get; set; }

        [Reactive] public string Head { get; set; }
        [Reactive] public string Body { get; set; }
        [Reactive] public RowInfo Row { get; set; }
        [Reactive] public ColumnInfo Column { get; set; }
        [Reactive] public string Color { get; set; }

        public ReactiveCommand CancelCommand { get; set; }
        public ReactiveCommand SaveCommand { get; set; }
        [Reactive] public bool IsOpened { get; set; }
        [Reactive] public bool IssueChanged { get; set; }

        public ReactiveCommand DeleteCommand { get; set; }

        [Reactive] public Brush Background { get; set; }

        [Reactive] public ColorItem[] ColorItems { get; set; } =
        {
            ColorItem.I("LemonChiffon"),
            ColorItem.I("WhiteSmoke"),
            ColorItem.I("NavajoWhite"),
            ColorItem.I("HoneyDew")
        };

        [Reactive] public ColorItem SelectedColor { get; set; }

        public IssueViewModel()
        {
            mapper = CreateMapper();

            AvailableColumns = new ReactiveList<ColumnInfo>();
            AvailableRows = new ReactiveList<RowInfo>();

            var issueFilled = this.WhenAnyValue(
                t => t.Head, t => t.Row, t => t.Column, t => t.Color,
                (sh, sr, sc, cc) =>
                sr != null && sc != null && !string.IsNullOrEmpty(sh) && !string.IsNullOrEmpty(cc));

            SaveCommand = ReactiveCommand.Create(() =>
            {
                var editedIssue = new Issue() { BoardId = board.Id };

                mapper.Map(this, editedIssue);

                if (editedIssue.Id == 0)
                    editedIssue.Created = DateTime.Now;

                editedIssue.Modified = DateTime.Now;

                scope.CreateOrUpdateIssueAsync(editedIssue);

                IsOpened = false;
                IssueChanged = true;
            }, issueFilled);

            CancelCommand = ReactiveCommand.Create(() => IsOpened = false);

            DeleteCommand = ReactiveCommand.Create(Delete);

            this.WhenAnyValue(x => x.SelectedColor)
                        .Where(x => x != null)
                        .Subscribe(_ =>
                        {
                            Background = SelectedColor.Brush;
                            Color = SelectedColor.Brush.Color.ToString();
                        });
        }

        public void Delete()
        {
            scope.DeleteIssueAsync(Id);

            IssueChanged = true;
            IsOpened = false;
        }

        public async Task UpdateViewModel()
        {
            var columns = await scope.GetColumnsByBoardIdAsync(board.Id);
            var rows = await scope.GetRowsByBoardIdAsync(board.Id);
            
            AvailableColumns.PublishCollection(columns);
            Column = AvailableColumns.First();
            AvailableRows.PublishCollection(rows);
            Row = AvailableRows.First();

            if (Id == 0)
            {
                mapper.Map(new Issue(), this);
                SelectedColor = ColorItems.First();
            }
            else
            {
                var issue = await scope.LoadOrCreateIssueAsync(Id);
                mapper.Map(issue, this);

                Row = AvailableRows.First(r => r.Id == issue.RowId);
                Column = AvailableColumns.First(c => c.Id == issue.ColumnId);

                SelectedColor = 
                    ColorItems.FirstOrDefault(c => c.Brush.Color.ToString() == issue.Color)
                    ?? ColorItems.First();
            }
        }

        public void Initialize(ViewRequest viewRequest)
        {
            var request = viewRequest as IssueViewRequest;
            if (request == null)
                return;

            scope = request.Scope;
            board = request.Board;
            Id = request.IssueId;

            IssueChanged = false;

            Observable.FromAsync(() => UpdateViewModel())
                .ObserveOnDispatcher()
                .Subscribe();

            Title = $"Issue edit {Head}";
            IsOpened = true;
        }

        private IMapper CreateMapper()
        {
            var mapperConfig = new MapperConfiguration(cfg =>
                cfg.AddProfile(typeof(MapperProfileSqliteRepos)));

            return mapperConfig.CreateMapper();
        }

        private class MapperProfileSqliteRepos : Profile
        {
            public MapperProfileSqliteRepos()
            {
                CreateMap<Issue, IssueViewModel>();
                CreateMap<IssueViewModel, Issue>();
            }
        }
    }//end of class
}
