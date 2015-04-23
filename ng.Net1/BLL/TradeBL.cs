using StockMgr.DAL;
using StockMgr.Models;
using System;
using System.Collections.Generic;
using System.Linq;


namespace StockMgr.BLL
{
    public class TradeBL : ITradeBL
    {
        public TradeBL(IUnitOfWork u)
        {
            unitOfWork = u;
            tdRepo = unitOfWork.TradeRepository;
            accRepo = unitOfWork.AccountRepository;
        }
        private IUnitOfWork unitOfWork;
        IGenericRepository<Trade> tdRepo; IGenericRepository<Account> accRepo;

        Func<Trade, decimal> BoughtPrice = (tr) => (tr.Price * tr.Qty) + tr.Cmsn;
        Func<Trade, decimal> SoldPrice = tr => (tr.Price * tr.Qty) - tr.Cmsn;
        Func<IGrouping<AccountTrans, Account>, decimal> AccountValue = grps => grps.Key == AccountTrans.Deposit ? grps.Sum(grp => grp.Amount) : grps.Sum(grp => -grp.Amount);
        Func<Trade, IGrouping<string, Trade>, int, bool> BuyTran_Sym = (t, g, q) => t.Type == Transaction.Buy && t.Sym == g.Key && (t.Sold == null || t.Sold < t.Qty) && q > 0;
        private void ArchiveTrans()
        {

            tdRepo.Get(t => t.Sold == null).ToList().ForEach(a => a.Archive = false); //Recent

            foreach (var buyTran in tdRepo.Get(t => t.Sold < t.Qty))//split
            {
                buyTran.Qty = buyTran.Qty - buyTran.Sold.Value;
                buyTran.Archive = false;

                tdRepo.Add(new Trade() { Sym = buyTran.Sym, Type = 0, Qty = buyTran.Sold.Value, Price = buyTran.Price, Cmsn = buyTran.Cmsn, Date = buyTran.Date, Archive = true }); //create sold(arch)
            }

            tdRepo.Get(t => t.Sold == t.Qty).ToList().ForEach(a => a.Archive = true); // Archi
        }
        private void UpdateSold()
        {
            int recentTotSellQty = 0;
            foreach (var grpTran in tdRepo.Get().GroupBy(t => t.Sym))
            {
                var sellTran = grpTran.Where(t => t.Type == Transaction.Sell && t.Sold == null); //Recent sell trans only
                recentTotSellQty = sellTran.Sum(t => t.Qty);
                //foreach buy trans in buy sym,  if only there is a recent Sale trans q>0
                foreach (var buyTran in tdRepo.Get(t => BuyTran_Sym(t, grpTran, recentTotSellQty), t => t.OrderBy(tr => tr.Date)))
                {   //Adj Sold for each buy trans till sellQty = 0
                    if (recentTotSellQty > 0)
                    {
                        if (buyTran.Sold > 0) //ReUpdate Sold 
                            recentTotSellQty += buyTran.Sold.Value;

                        buyTran.Sold = recentTotSellQty >= buyTran.Qty ? buyTran.Qty : recentTotSellQty;
                        recentTotSellQty = recentTotSellQty >= buyTran.Qty ? (recentTotSellQty - buyTran.Qty) : 0;

                    }
                }
                sellTran.Where(t => t.Sold == null).ToList().ForEach(a => a.Sold = a.Qty);
            }
            unitOfWork.Save();
        }
        private List<TradeProfit> CalcProfit(IEnumerable<Trade> tdLocalRepo)
        {
            var grSymType = tdLocalRepo.GroupBy(t => new { t.Sym, t.Type });
            var sumBuy = grSymType.Where(g => g.Key.Type == Transaction.Buy).Select(grp => new
            {
                grp.Key.Sym,
                CP = (decimal?)grp.Where(t => t.Archive).Sum(BoughtPrice)
            }).ToList();
            var sumSell = grSymType.Where(g => g.Key.Type == Transaction.Sell).Select(grp => new
            {
                grp.Key.Sym,
                SP = (decimal?)grp.Where(t => t.Archive).Sum(SoldPrice)
            }).ToList();
            var profBySym = sumSell.Join(sumBuy, s => s.Sym, b => b.Sym, (s, b) => new TradeProfit { Sym = s.Sym, Profit = s.SP - b.CP }).ToList();
            return profBySym;
        }
        public IEnumerable<IGrouping<bool, Trade>> GetTransactions()
        {
            ArchiveTrans();
            return unitOfWork.TradeRepository.Get(orderBy: ts => ts.OrderBy(t => t.Archive).ThenBy(t => t.Sym), local: true).GroupBy(t => t.Archive);
        }

        public AccountStatus GetAccountStatus()
        {
            ArchiveTrans();
            var tdLocalRepo = tdRepo.Get(local: true);

            var cash = new AccountStatus();

            var profBySym = CalcProfit(tdLocalRepo);
            cash.Profit = profBySym.Sum(p => p.Profit);

            cash.StockHand = tdLocalRepo.Where(g => g.Type == Transaction.Buy && !g.Archive).Sum(BoughtPrice);

            var grAccount = accRepo.Get().GroupBy(a => a.Type);
            cash.Deposited = grAccount.Select(AccountValue).Sum();
            var x = tdRepo.Get(g => g.Type == Transaction.Buy);
            cash.StockPur = tdRepo.Get(g => g.Type == Transaction.Buy).Sum(BoughtPrice);
            cash.StockSold = tdRepo.Get(g => g.Type == Transaction.Sell).Sum(SoldPrice);
            cash.InHand = cash.Deposited + cash.StockSold - cash.StockPur;
            return cash;
        }

        public void AddTrade(Trade td)
        {
            tdRepo.Add(td);
            unitOfWork.Save();
        }

        public void AddTrades(IList<Trade> trades)
        {
            unitOfWork.TradeRepository.AddRange(trades);
            unitOfWork.Save();
            UpdateSold();
        }

        public void AddAccountTransaction(Account c)
        {
            accRepo.Add(c);
            unitOfWork.Save();
        }
    }
}