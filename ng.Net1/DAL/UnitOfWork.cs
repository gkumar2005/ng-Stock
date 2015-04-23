using System;
using StockMgr.Models;

namespace StockMgr.DAL
{
    public class UnitOfWork : IDisposable, IUnitOfWork
    {
        public UnitOfWork(IGenericRepository<Account> acc, IGenericRepository<Trade> tr)
        {
            accountRepository = acc;
            tradeRepository = tr;
        }
        private StockCtxt context = new StockCtxt();
        private IGenericRepository<Account> accountRepository;
        private IGenericRepository<Trade> tradeRepository;

        public IGenericRepository<Account> AccountRepository
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

        public IGenericRepository<Trade> TradeRepository
        {
            get
            {

                if (this.tradeRepository == null)
                {
                    this.tradeRepository = new GenericRepository<Trade>(context);
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
