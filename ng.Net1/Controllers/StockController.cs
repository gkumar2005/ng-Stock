using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.ModelBinding;
using StockMgr.Models;
using System.Collections;
using System.Data.Entity;
using StockMgr.DAL;

namespace StockMgr.Controllers
{
    //[Authorize]
    public class StockController : ApiController
    {
        private UnitOfWork unitOfWork = new UnitOfWork();
        TradeRepository tdRepo; GenericRepository<Account> accRepo;
        public StockController()
        {
            tdRepo = unitOfWork.TradeRepository;
            accRepo = unitOfWork.AccountRepository;
        }
        
        [HttpPost]
        public HttpResponseMessage PostAllTrans(IList<Trade> td)
        {
            unitOfWork.TradeRepository.AddRange(td);
            unitOfWork.Save();
            UpdateSold();
            return Request.CreateResponse(HttpStatusCode.Accepted);
        }
        // GET api/values
        public IEnumerable<IGrouping<bool,Trade>> Get()
        {
            ArchiveTrans();
            return unitOfWork.TradeRepository.Get(orderBy: ts => ts.OrderBy(t => t.Archive).ThenBy(t => t.Sym), local: true).GroupBy(t => t.Archive);
        }

        private void ArchiveTrans()
        {
            
            tdRepo.Get(t => t.Sold == null).ToList().ForEach(a => a.Archive = false); //Recent

            foreach (var buyTran in tdRepo.Get(t => t.Sold < t.Qty))//split
            {
                buyTran.Qty = buyTran.Qty - buyTran.Sold.Value;
                buyTran.Archive = false;

                tdRepo.Add(new Trade() { Sym = buyTran.Sym, Type = 0, Qty = buyTran.Sold.Value, Price= buyTran.Price, Cmsn = buyTran.Cmsn, Date= buyTran.Date, Archive = true }); //create sold(arch)
            }

            tdRepo.Get(t => t.Sold == t.Qty).ToList().ForEach(a => a.Archive = true); // Archi
        }
        private void UpdateSold()
        {
            int totSellQty = 0;
            foreach (var grpTran in tdRepo.Get().GroupBy(t => t.Sym))
            {
                var sellTran = grpTran.Where(t => t.Type == 1 && t.Sold == null); //Recent sell trans only
                totSellQty = sellTran.Sum(t => t.Qty);
                //foreach buy trans in buy sym,  if only there is a recent Sale trans
                foreach (var buyTran in tdRepo.Get(t => t.Type == 0 && t.Sym == grpTran.Key && (t.Sold == null || t.Sold < t.Qty) && totSellQty > 0).OrderBy(t => t.Date))
                {   //Adj Sold for each buy trans till sellQty = 0
                    if (totSellQty > 0)
                    {
                        if (buyTran.Sold > 0) //ReUpdate Sold 
                            totSellQty += buyTran.Sold.Value;

                        buyTran.Sold = totSellQty >= buyTran.Qty ? buyTran.Qty : totSellQty;
                        totSellQty = totSellQty >= buyTran.Qty ? (totSellQty - buyTran.Qty) : 0;
                       
                    }
                }
                sellTran.Where(t =>t.Sold == null).ToList().ForEach(a => a.Sold = a.Qty);
            }
            unitOfWork.Save();
        }
        private void SegregateTrade()
        {
            var grpSumQty = tdRepo.Get().GroupBy(t => new { t.Sym, t.Type }, (a, ts) => new { a.Sym, a.Type, sumQty = ts.Sum(tr => tr.Qty) });
           
            foreach (var grpSym in grpSumQty.GroupBy(t => t.Sym).Where(grp => grp.Count() == 2)) //for every Sym if there is buy & sell
            {
                var sumBuyQty = grpSym.SingleOrDefault(grps => grps.Type == 0).sumQty;
                var sumSellQty = grpSym.SingleOrDefault(grps => grps.Type == 1).sumQty;
                var sellQty = sumSellQty;
                var soldQty = 0;
                if (sumBuyQty >= sellQty) // Always true as buy >= sell
                {
                    foreach (var trd in tdRepo.Get(t => t.Sym == grpSym.Key && t.Type == 0).OrderBy(t => t.Date)) //Loop buy transaction 
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
                    foreach (var trd in tdRepo.Get(t => t.Sym == grpSym.Key && t.Type == 1)) //Loop sell transaction 
                    {
                        if (sumBuyQty >= sumSellQty) // Buy:5 Sell:1
                        {
                            trd.Archive = true;
                        }
                    }
                }

                if (soldQty > 0) //sold out (or) archBuy
                    tdRepo.Add(new Trade() { Sym = grpSym.Key, Type = 0, Qty = soldQty, Archive = true });

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
            ArchiveTrans();
            var tdLocalRepo = tdRepo.Get(local: true);

            var cash = new Cash();
            
            var profBySym = CalcProfit(tdLocalRepo);
            cash.Profit = profBySym.Sum(p => p.Profit);

            cash.StockHand = tdLocalRepo.Where(g => g.Type == 0 && !g.Archive).Sum(tr => (tr.Price * tr.Qty) + tr.Cmsn);
           
            var grAccount = accRepo.Get().GroupBy(a => a.Type);
            cash.Deposited = grAccount.Select(grps => grps.Key == 1 ? grps.Sum(grp => grp.Amount) : grps.Sum(grp => -grp.Amount)).Sum();
            cash.InHand = cash.Deposited + cash.StockSold - cash.StockPur;
            return cash;
        }

        private static List<TradeProfit> CalcProfit(IEnumerable<Trade> tdLocalRepo)
        {
            var grSymType = tdLocalRepo.GroupBy(t => new { t.Sym, t.Type });
            var sumBuy = grSymType.Where(g => g.Key.Type == 0).Select(grp => new
            {
                grp.Key.Sym,
                CP = (decimal?)grp.Where(t => t.Archive).Sum(tr => tr.Price * tr.Qty + tr.Cmsn)
            }).ToList();
            var sumSell = grSymType.Where(g => g.Key.Type == 1).Select(grp => new
            {
                grp.Key.Sym,
                SP = (decimal?)grp.Where(t => t.Archive).Sum(tr => tr.Price * tr.Qty - tr.Cmsn)
            }).ToList();
            var profBySym = sumSell.Join(sumBuy, s => s.Sym, b => b.Sym, (s, b) => new TradeProfit { Sym = s.Sym, Profit = s.SP - b.CP }).ToList();
            return profBySym;
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
            tdRepo.Add(td);
            unitOfWork.Save();
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
            accRepo.Add(c);
            unitOfWork.Save();
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
