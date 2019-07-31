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
    public class HeaderPropertyViewModel : ViewModelBase, IInitializableViewModel
    {

        private DbViewModel db;

        public ReadOnlyObservableCollection<String> HeaderName { get; set; }
        
        public String xyz ="xyz";


        public ReactiveCommand<Unit, Unit> HeaderCancelCommand { get; set; }
        public ReactiveCommand<Unit, Unit> HeaderSaveCommand { get; set; }
        //public ReactiveCommand<Unit, Unit> EnterCommand { get; set; }

        [Reactive] public bool IsOpened { get; set; } = false;

        

        public HeaderPropertyViewModel()
        {
            HeaderSaveCommand = ReactiveCommand.Create(HeaderSaveCommandExecute );
            HeaderCancelCommand = ReactiveCommand.Create(HeaderCancelCommandExecute);
            //HeaderEnterCommand = ReactiveCommand.Create(HeaderEnterCommandExecute);
        }

        private void EnterCommandExecute()
        {
            throw new NotImplementedException();
        }

        private void HeaderCancelCommandExecute()
        {
            IsOpened = false;
            //throw new NotImplementedException();
        }

        private void HeaderSaveCommandExecute()
        {
            IsOpened = false;
            //throw new NotImplementedException();
        }

        public void Initialize(ViewRequest viewRequest)
        {
            var request = viewRequest as HeaderPropertyViewRequest;
            

            if (request == null)
                return;

            IDim Header = request.Header;
            db = request.Db;



            ColumnViewModel cvm = (ColumnViewModel)Header;

              db.Columns
                  .Connect()
                  .AutoRefresh()
                  .Filter(x => x.BoardId == 1 & x.Id == Header.Id)
                 
                  .Cast(x => x.Name)
                  .Bind(out ReadOnlyObservableCollection<String> temp)
                  .Subscribe();

            HeaderName=   temp;

            IsOpened = true;



        }
    }
}
