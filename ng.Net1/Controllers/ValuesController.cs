using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.ModelBinding;
using ng.Net1.Models;
using System.Collections;
using System.Data.Entity;
namespace ng.Net1.Controllers
{
    //[Authorize]
    public class ValuesController : ApiController
    {
        private DBContext db = new DBContext();
         [HttpPost]
        public void Postg(IList<Trade> td)
        {
            db.trades.AddRange(td);
            db.SaveChanges();
        }
        // GET api/values
        public IEnumerable<IGrouping<bool,Trade>> Get()
        {
            SegregateTrade();
            return db.trades.Local.OrderBy(t => t.Archive).ThenBy(t=>t.Sym).GroupBy(t => t.Archive);
        }
        private void SegregateTrade()
        {
            var grSymType = db.trades.GroupBy(t => new { t.Sym, t.Type });
            var grpSumQty = grSymType.Select(grps => new { grps.Key.Sym, grps.Key.Type, sumQty = grps.Sum(t => t.Qty) });

            foreach (var grpSym in grpSumQty.GroupBy(t => t.Sym).Where(grp => grp.Count() == 2)) //for every Sym if there is buy & sell
            {
                var sumBuyQty = grpSym.SingleOrDefault(grps => grps.Type == 0).sumQty;
                var sumSellQty = grpSym.SingleOrDefault(grps => grps.Type == 1).sumQty;
                var sellQty = sumSellQty;
                var soldQty = 0;
                if (sumBuyQty >= sellQty)
                {
                    foreach (var trd in db.trades.Where(t => t.Sym == grpSym.Key && t.Type == 0).OrderBy(t => t.Date)) //Loop buy transaction 
                    {
                        if (trd.Qty > sellQty && sellQty > 0) // Buy:5 Sell:1 then currBuy:4  arcBuy:1 | Buy:2 Sell:2 then currBuy:0  arcBuy:2
                        {
                            soldQty = sellQty;
                            trd.Qty = trd.Qty - sellQty; //update current qty and set as not sold | currBuy:4
                            trd.Archive = false;
                        }
                        else //Sell:5 Buy:1 | Sell:5 Buy:5   //totSale >= current buy transaction, then it is marked as sold out (or) archBuy
                        {
                            sellQty -= trd.Qty;
                            if (sellQty >= 0) //totSale >= current buy transaction, then it is marked as sold out
                                trd.Archive = true;
                            else
                                trd.Archive = false;
                        }
                    }
                    foreach (var trd in db.trades.Where(t => t.Sym == grpSym.Key && t.Type == 1)) //Loop sell transaction 
                    {
                        if (sumBuyQty >= sumSellQty) // Buy:5 Sell:1
                        {
                            trd.Archive = true;
                        }
                    }
                }

                if (soldQty > 0) //sold out (or) archBuy
                    db.trades.Add(new Trade() { Sym = grpSym.Key, Type = 0, Qty = soldQty, Archive = true });

            }
        }

//        select Type, case when type=0 then SUM(Price*Qty+Cmsn) else SUM(Price*Qty-Cmsn) end as Cash into #Trade 
//          from trades group by type 
//        select type, SUM(amount) as Cash into #Account 
//          from account group by type
        // GET api/values/GetCash
        [HttpGet]
        public Cash GetCash()
        {
            SegregateTrade();
            var cash = new Cash(); 
            var grSymType = db.trades.Local.GroupBy(t => new { t.Sym, t.Type });
            var sumBuy = grSymType.Where(g => g.Key.Type == 0).Select(grp => new
            {
                grp.Key.Sym,
                CP = (decimal?)grp.Where(t => t.Archive).Sum(tr => tr.Price * tr.Qty + tr.Cmsn)
            }).ToList();
            var sumSell = grSymType.Where(g => g.Key.Type == 1).Select(grp => new {grp.Key.Sym,
                                                                                   SP = (decimal?)grp.Where(t => t.Archive).Sum(tr => tr.Price * tr.Qty - tr.Cmsn)
            }).ToList();
            var profBySym =
                sumSell.Join(sumBuy, s => s.Sym, b => b.Sym, (s, b) => new { s.Sym, Prof = s.SP - b.CP }).ToList();
            cash.Profit = profBySym.Sum(p => p.Prof);

            //cash.Profit = (from s in sumSell
            //              join b in sumBuy
            //                  on s.Sym equals b.Sym
            //               select new { s.Sym, Prof = s.SP - b.CP }).Sum(p => p.Prof);
            var grTrade = db.trades.Local;
            cash.StockHand = grTrade.Where(g => g.Type == 0 && !g.Archive).Sum(tr => (tr.Price * tr.Qty) + tr.Cmsn);
            cash.StockPur = grTrade.Where(g => g.Type == 0).Sum(tr => (tr.Price * tr.Qty) + tr.Cmsn);
            cash.StockSold = grTrade.Where(g => g.Type == 1).Sum(tr => (tr.Price * tr.Qty) - tr.Cmsn);
           
            var grAccount = db.account.GroupBy(a => a.Type);
            cash.Deposited = grAccount.Select(grps => grps.Key == 1 ? grps.Sum(grp => grp.Amount) : grps.Sum(grp => -grp.Amount)).Sum();
            cash.InHand = cash.Deposited + cash.StockSold - cash.StockPur;
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
            //var tradeNew = new Trade(){ Sym = td.Sym, Type = td.Type, Qty = td.Qty, Price = td.Price, Cmsn = td.Cmsn, Date = td.Date, Archive=td.Archive };
            db.trades.Add(td);
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
