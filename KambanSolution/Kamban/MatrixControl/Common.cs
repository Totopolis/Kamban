using Kamban.Model;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kamban.MatrixControl
{
    /// <summary>
    /// Row or column description
    /// </summary>
    public interface IDim
    {
        string Caption { get; set; }
        object Determinant { get; set; }
        int Size { get; set; }
    }

    public class ColumnViewModel : ReactiveObject, IDim
    {
        private readonly ColumnInfo columnInfo;

        public ColumnViewModel(ColumnInfo ci)
        {
            columnInfo = ci;

            Id = ci.Id;
            Caption = ci.Name;
            Determinant = ci.Id;
            Size = ci.Width;
        }

        [Reactive] public int Id { get; set; }
        [Reactive] public string Caption { get; set; }
        [Reactive] public object Determinant { get; set; }
        [Reactive] public int Size { get; set; }
    }

    public class RowViewModel : ReactiveObject, IDim
    {
        private readonly RowInfo rowInfo;

        public RowViewModel(RowInfo ri)
        {
            rowInfo = ri;

            Id = ri.Id;
            Caption = ri.Name;
            Determinant = ri.Id;
            Size = ri.Height;
        }

        [Reactive] public int Id { get; set; }
        [Reactive] public string Caption { get; set; }
        [Reactive] public object Determinant { get; set; }
        [Reactive] public int Size { get; set; }
    }

    public interface ICard
    {
        int Id { get; set; }
        string Header { get; set; }
        string Color { get; set; }

        object ColumnDeterminant { get; set; }
        object RowDeterminant { get; set; }

        string Body { get; set; }
        DateTime Created { get; set; }
        DateTime Modified { get; set; }

        int BoardId { get; set; }
        bool ShowDescription { get; set; }
    }

    // def back color "#FFFFE0"
    public class CardViewModel : ReactiveObject, ICard
    {
        private readonly Issue issueInfo;

        public CardViewModel(Issue iss)
        {
            issueInfo = iss;

            Id = iss.Id;
            Header = iss.Head;
            Color = iss.Color;
            ColumnDeterminant = iss.ColumnId;
            RowDeterminant = iss.RowId;
            Body = iss.Body;
            Created = iss.Created;
            Modified = iss.Modified;
            ShowDescription = !string.IsNullOrEmpty(issueInfo.Body);
            BoardId = iss.BoardId;
        }

        [Reactive] public int Id { get; set; }
        [Reactive] public string Header { get; set; }
        [Reactive] public string Color { get; set; }
        [Reactive] public object ColumnDeterminant { get; set; }
        [Reactive] public object RowDeterminant { get; set; }
        [Reactive] public string Body { get; set; }
        [Reactive] public DateTime Created { get; set; }
        [Reactive] public DateTime Modified { get; set; }
        [Reactive] public int BoardId { get; set; }
        [Reactive] public bool ShowDescription { get; set; }
    }
}
