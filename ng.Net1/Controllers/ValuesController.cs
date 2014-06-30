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
            return db.trades;
        }

        // GET api/values/5
        public string Get(int id)
        {
            return "value";
        }

        // POST api/values
        public HttpResponseMessage Post(Trade td)
        {
            if (!ModelState.IsValid)
            {
                var err = ModelState.Values.SelectMany(m => m.Errors.Select(e => e.ErrorMessage));
                return Request.CreateResponse(HttpStatusCode.BadRequest, err);
            }
            var tradeNew = new Trade() { Sym = td.Sym, Type = td.Type, Qty = td.Qty, Price = td.Price, DCash = td.DCash, Cmsn = td.Cmsn, Date = td.Date };
            db.trades.Add(tradeNew);
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
