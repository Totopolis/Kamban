﻿using Kamban.Repository;
using Ui.Wpf.Common;

namespace Kamban.ViewRequests
{
    public class ImportSchemeViewRequest : ViewRequest
    {
        public ILoadRepository Repository { get; set; }

        public ImportSchemeViewRequest()
        {
        }
    }
}