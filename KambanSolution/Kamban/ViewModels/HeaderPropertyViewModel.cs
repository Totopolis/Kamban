using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading.Tasks;
using System.Windows.Media;
using AutoMapper;
using DynamicData;
using Kamban.MatrixControl;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Ui.Wpf.Common;
using Ui.Wpf.Common.ViewModels;
using System.ComponentModel;
using Kamban.ViewModels.Core;

namespace Kamban.ViewModels
{
    public class HeaderPropertyViewModel : ViewModelBase, IInitializableViewModel ,INotifyPropertyChanged
    {
        private BoxViewModel Box;
        private BoardViewModel board;
        IDim Header { get; set; }
       
        public String TitleText { get; set;  }

        private String OldName;
        private int OldMaxNumberOfCards;
        private bool OldLimitSet;

        [Reactive] public CardViewModel Card { get; set; }
        
        public string HeaderName
        {
            get { return Header != null ? Header.Name : "nulll"; }
            set { if (Header != null)
                    {
                    Header.Name = value;
                    }
                }
        }

        public bool HeaderLimitSet
        {
            get { return Header != null ? Header.LimitSet : false; }
            set
            {
                if (Header != null)
                {
                    Header.LimitSet = value;
                }
            }
        }

        public int HeaderMaxNumber
        {
            get { return Header != null ? Header.MaxNumberOfCards : 8 ; }
            set
            {
                if (Header != null)
                {
                    Header.MaxNumberOfCards = value;
                }
            }
        }

        public ReactiveCommand<Unit, Unit> HeaderCancelCommand { get; set; }
        public ReactiveCommand<Unit, Unit> HeaderSaveCommand { get; set; }

        [Reactive] public bool IsOpened { get; set; } = false;

        public HeaderPropertyViewModel()
        {
            HeaderSaveCommand = ReactiveCommand.Create(HeaderSaveCommandExecute );
            HeaderCancelCommand = ReactiveCommand.Create(HeaderCancelCommandExecute);
        }

        private void EnterCommandExecute()
        {
            throw new NotImplementedException();
        }

        private void HeaderCancelCommandExecute()
        {
            // restore previous Values
            Header.LimitSet = OldLimitSet;
            HeaderMaxNumber = OldMaxNumberOfCards;
            Header.Name = OldName;

            IsOpened = false;
        }

        private void HeaderSaveCommandExecute()
        {
            IsOpened = false;
        }


        public void Initialize(ViewRequest viewRequest)
        {
            HeaderPropertyViewRequest request = viewRequest as HeaderPropertyViewRequest;

            if (request == null)
                return;

            Header = request.Header;
            Box = request.Box;
            board = request.Board;

            if (Header == null)
                return;

            OldLimitSet = Header.LimitSet;
            OldMaxNumberOfCards = HeaderMaxNumber;
            OldName = Header.Name;

            TitleText = Header is ColumnViewModel ? "Column Properties" : "Row Properties";

            this.RaisePropertyChanged("TitleText");
            this.RaisePropertyChanged("HeaderName");
            this.RaisePropertyChanged("HeaderLimitSet");
            this.RaisePropertyChanged("HeaderMaxNumber");

            IsOpened = true;
        }
    }
}
