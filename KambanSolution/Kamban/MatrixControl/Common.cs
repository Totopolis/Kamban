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
        }

        public string Caption { get { return columnInfo.Name; } set { columnInfo.Name = value; } }
        public object Determinant { get => columnInfo.Id; set => throw new NotImplementedException(); }
        public int Size { get => columnInfo.Height; set => throw new NotImplementedException(); }
    }

    public class RowViewModel : ReactiveObject, IDim
    {
        private readonly RowInfo rowInfo;

        public RowViewModel(RowInfo ri)
        {
            rowInfo = ri;
        }

        public string Caption { get { return rowInfo.Name; } set { rowInfo.Name = value; } }
        public object Determinant { get => rowInfo.Id; set => throw new NotImplementedException(); }
        public int Size { get => rowInfo.Height; set => throw new NotImplementedException(); }
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

        public string Header { get => issueInfo.Head; set => throw new NotImplementedException(); }
        public string Color { get => issueInfo.Color; set => throw new NotImplementedException(); }
        public object ColumnDeterminant { get => issueInfo.ColumnId; set => throw new NotImplementedException(); }
        public object RowDeterminant { get => issueInfo.RowId; set => throw new NotImplementedException(); }

        public bool ShowDescription { get => !string.IsNullOrEmpty(issueInfo.Body); set => throw new NotImplementedException(); }
    }
}
