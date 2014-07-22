using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.ModelBinding;
using ng.Net1.Models;

namespace ng.Net1.Controllers
{
    //[Authorize]
    public class ValuesController : ApiController
    {
        private DBContext db = new DBContext();
        // GET api/values
        public IEnumerable<Trade> Get()
        {
            return db.trades.Where(t=>t.Archive);
        }
        private static Decimal fn(IGrouping<int,Trade> grp)
        {
// ReSharper disable ReturnValueOfPureMethodIsNotUsed
            Decimal sum = 0;
            foreach (Trade td in grp)
            {
                if (td.Type == 1) sum += (td.Price*td.Qty - td.Cmsn);
            }
            return sum;
// ReSharper restore ReturnValueOfPureMethodIsNotUsed
        }

//        select Type, case when type=0 then SUM(Price*Qty+Cmsn) else SUM(Price*Qty-Cmsn) end as Cash into #Trade 
//          from trades group by type 
//        select type, SUM(amount) as Cash into #Account 
//          from account group by type
        // GET api/values/5
        [HttpGet]
        public Cash GetCash()
        {
            var cash = new Cash();
            var grTrade = db.trades.GroupBy(t => t.Type);
            cash.StockHand = grTrade.Where(g => g.Key == 0).Select(grp => grp.Where(t => !t.Archive).Sum(tr => tr.Price * tr.Qty + tr.Cmsn)).FirstOrDefault();
            cash.StockPur = grTrade.Where(g => g.Key == 0).Select(grp => grp.Sum(tr => tr.Price*tr.Qty + tr.Cmsn)).FirstOrDefault();
            Decimal x = 0.0m;
            foreach (var grp in grTrade.Where(g => g.Key == 1))
                cash.StockSold = fn(grp);
           
            var grAccount = db.account.GroupBy(a => a.Type);
            cash.Deposited = grAccount.Where(ac => ac.Key == 1).Select(grp => grp.Sum(grpDep => grpDep.Amount)).FirstOrDefault();
            cash.Withdrawn = grAccount.Where(ac => ac.Key == 0).Select(grp => grp.Select(grpWd => grpWd.Amount).Sum()).FirstOrDefault();
            cash.InHand = cash.Deposited + cash.StockSold - (cash.StockPur + cash.Withdrawn);
            return cash;
        }

        // POST api/values
        [HttpPost]
        public HttpResponseMessage Post(Trade td)
        {
            if (!ModelState.IsValid)
            {
                var err = ModelState.Values.SelectMany(m => m.Errors.Select(e => e.Exception.Message));
                return Request.CreateResponse(HttpStatusCode.BadRequest, err);
            }
            var tradeNew = new Trade() { Sym = td.Sym, Type = td.Type, Qty = td.Qty, Price = td.Price, DCash = td.DCash, Cmsn = td.Cmsn, Date = td.Date };
            db.trades.Add(tradeNew);
            db.SaveChanges();
            return Request.CreateResponse(HttpStatusCode.Accepted);
        }

        [HttpPost]
        public HttpResponseMessage PostAmt(Account c)
        {
            if (!ModelState.IsValid)
            {
                var err = ModelState.Values.SelectMany(m => m.Errors.Select(e => e.ErrorMessage));
                return Request.CreateResponse(HttpStatusCode.BadRequest, err);
            }
            //var tradeNew = new Account() { Type = cash.Type, Amount = cash.Amount, cash.TranDt = td.Date };
            db.account.Add(c);
            db.SaveChanges();
            return Request.CreateResponse(HttpStatusCode.Accepted);
        }
        // PUT api/values/5
        public void Put(int id, [FromBody]string value)
        {
        }

        // DELETE api/values/5
        public void Delete(int id)
        {
        }
    }
}
