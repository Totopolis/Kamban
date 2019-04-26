using System;
using System.ComponentModel;

namespace Kamban.ViewModels.Core
{
    public interface ICard : INotifyPropertyChanged
    {
        int Id { get; set; }
        string Header { get; set; }
        string Color { get; set; }

        int ColumnDeterminant { get; set; }
        int RowDeterminant { get; set; }
        int Order { get; set; }

        string Body { get; set; }
        DateTime Created { get; set; }
        DateTime Modified { get; set; }

        int BoardId { get; set; }
        bool ShowDescription { get; set; }
    }
}