﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Kamban.Contracts
{
    public interface ILoadRepository : IDisposable
    {
        Task<Box> Load();

        Task<BoxScheme> LoadScheme();
        Task<List<Board>> LoadSchemeBoards();
        Task<List<Column>> LoadSchemeColumns(int[] boardIds = null);
        Task<List<Row>> LoadSchemeRows(int[] boardIds = null);

        Task<List<Card>> LoadCards(CardFilter filter);
    }
}