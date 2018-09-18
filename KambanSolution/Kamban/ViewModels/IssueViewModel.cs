using System;
using System.Linq;
using System.Reactive.Linq;
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
        public int? IssueId { get; set; }
        public IScopeModel Scope { get; set; }
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
        private IScopeModel scope;
        private BoardInfo board;

        public int Id { get; set; }
        public DateTime Created { get; set; }

        public ReactiveList<RowInfo> AwailableRows { get; set; } =
            new ReactiveList<RowInfo>();

        public ReactiveList<ColumnInfo> AwailableColumns { get; set; } =
            new ReactiveList<ColumnInfo>();

        [Reactive] public string Head { get; set; }
        [Reactive] public string Body { get; set; }
        [Reactive] public RowInfo Row { get; set; }
        [Reactive] public ColumnInfo Column { get; set; }
        public string Color { get; set; }

        public ReactiveCommand CancelCommand { get; set; }
        public ReactiveCommand SaveCommand { get; set; }
        [Reactive] public bool IsOpened { get; set; }
        [Reactive] public bool IssueChanged { get; set; }

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

            var issueFilled = this.WhenAnyValue(t => t.Head, t => t.Row, t => t.Column,
                (sh, sr, sc) => sr != null && sc != null && !string.IsNullOrEmpty(sh));

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
        }

        public void Initialize(ViewRequest viewRequest)
        {
            if (viewRequest is IssueViewRequest request)
            {
                scope = request.Scope;
                board = request.Board;

                mapper.Map(new Issue(), this);

                SelectedColor = ColorItems.First();

                IssueChanged = false;

                Observable.FromAsync(() => scope.GetRowsByBoardIdAsync(board.Id))
                    .ObserveOnDispatcher()
                    .Subscribe(rows =>
                    {
                        AwailableRows.PublishCollection(rows);
                        Row = AwailableRows.First();
                    });

                Observable.FromAsync(() => scope.GetColumnsByBoardIdAsync(board.Id))
                    .ObserveOnDispatcher()
                    .Subscribe(columns =>
                    {
                        AwailableColumns.PublishCollection(columns);
                        Column = AwailableColumns.First();
                    });

                var issueId = request.IssueId;

                if (issueId != null && issueId > 0)
                    Observable.FromAsync(() => scope.LoadOrCreateIssueAsync(issueId))
                        .ObserveOnDispatcher()
                        .Subscribe(issue =>
                        {
                            mapper.Map(issue, this);
                            Row = AwailableRows.First(r => r.Id == issue.RowId);
                            Column = AwailableColumns.First(c => c.Id == issue.ColumnId);
                            SelectedColor = ColorItems.FirstOrDefault(c => c.Brush.Color.ToString() == issue.Color);
                        });

                this.WhenAnyValue(x => x.SelectedColor)
                    .Where(x => x != null)
                    .Subscribe(_ =>
                    {
                        Background = SelectedColor.Brush;
                        Color = SelectedColor.Brush.Color.ToString();
                    });
            }

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
