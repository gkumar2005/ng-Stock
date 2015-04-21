using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace StockMgr.Controllers
{
    public class AppController : Controller
    {
        public ActionResult Register()
        {
            return PartialView();
        }
        public ActionResult SignIn()
        {
            return PartialView();
        }
        public ActionResult Home()
        {
            return PartialView();
        }
        public ActionResult Stock()
        {
            return PartialView();
        }
        public ActionResult StockDetails()
        {
            return PartialView();
        }
        [Authorize]
        public ActionResult TodoManager()
        {
            return PartialView();
        }
    }
}