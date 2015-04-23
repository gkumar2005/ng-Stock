using StockMgr.Models;
using System;
namespace StockMgr.DAL
{
    public interface IUnitOfWork
    {
        IGenericRepository<Account> AccountRepository { get; }
        int Save();
        IGenericRepository<Trade> TradeRepository { get; }
    }
}
