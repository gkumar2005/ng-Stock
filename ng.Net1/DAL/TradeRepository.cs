using StockMgr.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace StockMgr.DAL
{
    public class TradeRepository : GenericRepository<Trade>
    {
        public TradeRepository(StockCtxt ctxt) : base(ctxt)
        {
        }

        

 
    }
}