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
        string Header { get; set; }
        string Color { get; set; }

        object ColumnDeterminant { get; set; }
        object RowDeterminant { get; set; }

        bool ShowDescription { get; set; }
    }

    // def back color "#FFFFE0"
    public class CardViewModel : ReactiveObject, ICard
    {
        private readonly Issue issueInfo;

        public CardViewModel(Issue iss)
        {
            issueInfo = iss;
        }

        public int Id { get => issueInfo.Id; set => throw new NotImplementedException(); }
        public string Header { get => issueInfo.Head; set => throw new NotImplementedException(); }
        public string Color { get => issueInfo.Color; set => throw new NotImplementedException(); }
        public object ColumnDeterminant { get => issueInfo.ColumnId; set => throw new NotImplementedException(); }
        public object RowDeterminant { get => issueInfo.RowId; set => throw new NotImplementedException(); }

        public bool ShowDescription { get => !string.IsNullOrEmpty(issueInfo.Body); set => throw new NotImplementedException(); }
    }
}
