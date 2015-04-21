using System;
using StockMgr.Models;

namespace StockMgr.DAL
{
    public class UnitOfWork : IDisposable
    {
        private StockCtxt context = new StockCtxt();
        private GenericRepository<Account> accountRepository;
        private TradeRepository tradeRepository;

        public GenericRepository<Account> AccountRepository
        {
            get
            {

                if (this.accountRepository == null)
                {
                    this.accountRepository = new GenericRepository<Account>(context);
                }
                return accountRepository;
            }
        }
        
        public TradeRepository TradeRepository
        {
            get
            {

                if (this.tradeRepository == null)
                {
                    this.tradeRepository = new TradeRepository(context);
                }
                return tradeRepository;
            }
        }


        public int Save()
        {
            return context.SaveChanges();
        }

        private bool disposed = false;

        protected virtual void Dispose(bool disposing)
        {
            if (!this.disposed)
            {
                if (disposing)
                {
                    context.Dispose();
                }
            }
            this.disposed = true;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}
