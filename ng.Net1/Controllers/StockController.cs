using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.ModelBinding;
using StockMgr.Models;
using StockMgr.BLL;
using System.Linq;
using System.Collections.Generic;

namespace StockMgr.Controllers
{
    //[Authorize]
    public class StockController : ApiController
    {
        ITradeBL _bl;
        public StockController(ITradeBL bl)
        {
            _bl = bl;
        }
        
        [HttpPost]
        public HttpResponseMessage PostAllTrans(IList<Trade> trades)
        {
            _bl.AddTrades(trades);
            return Request.CreateResponse(HttpStatusCode.Accepted);
        }
        // GET api/values
        public IEnumerable<IGrouping<bool,Trade>> Get()
        {
            return _bl.GetTransactions();
        }
        
        /// <summary>
        /// 0 - Withdraw; 1 - Deposit; 0 - Buy; 1 - Sell
        ///  select Type, case when type=0 then SUM(Price*Qty+Cmsn) else SUM(Price*Qty-Cmsn) end as Cash into #Trade 
        ///         from trades group by type 
        ///  select type, SUM(amount) as Cash into #Account 
        ///          from account group by type
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        public AccountStatus GetCash()
        {
            return _bl.GetAccountStatus();
        }

        [HttpPost]
        public IHttpActionResult Post(Trade td)
        {
            if (!ModelState.IsValid)
            {
                var err = ModelState.Values.SelectMany(m => m.Errors.Select(e => e.Exception.Message));
                return BadRequest(ModelState);
            }
            _bl.AddTrade(td);            
            return Ok();
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
            _bl.AddAccountTransaction(c);
            return Request.CreateResponse(HttpStatusCode.Accepted);
        }
        public void Put(int id, [FromBody]string value)
        {
        }
        public void Delete(int id)
        {
        }
    }
}
