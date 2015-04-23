using StockMgr.Models;
using System;
using System.Collections.Generic;
using System.Linq;
namespace StockMgr.BLL
{
    public interface ITradeBL
    {
        void AddAccountTransaction(Account c);
        void AddTrade(StockMgr.Models.Trade td);
        void AddTrades(IList<StockMgr.Models.Trade> trades);
        AccountStatus GetAccountStatus();
        IEnumerable<IGrouping<bool, Trade>> GetTransactions();
    }
}
