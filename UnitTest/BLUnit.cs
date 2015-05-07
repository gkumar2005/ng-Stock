
using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using StockMgr.BLL;
using Ploeh.AutoFixture;
using StockMgr.Models;
using StockMgr.DAL;
using System.Collections.Generic;
using Moq;
using Ploeh.AutoFixture.AutoMoq;
using System.Linq.Expressions;
using System.Linq;

namespace UnitTest
{
    [TestClass]
    public class BLUnit
    {
        Fixture fix;
        Mock<IUnitOfWork> moqUOW;
        TradeBL sut;

        [TestMethod]
        public void TestMethod1()
        {
            //Arrange SUT
            CreateTradeBL(out fix, out moqUOW, out sut);

            //Act as real run
            sut.AddTrade(fix.Create<Trade>());

            //Assert
            moqUOW.Verify(x => x.TradeRepository.Add(It.IsAny<Trade>()), Times.Once);

            //var y = fix.AddManyTo<Trade>(new List<Trade>());

        }

        private static void CreateTradeBL(out Fixture fix, out Mock<IUnitOfWork> moqUOW, out TradeBL sut)
        {
            fix = new Fixture();
            fix.Customize(new AutoMoqCustomization());
            moqUOW = fix.Freeze<Mock<IUnitOfWork>>();

            sut = fix.Create<TradeBL>();
        }
        [TestMethod]
        public void AccountValue()
        {
            //Expression<Func<Account, bool>> filter = null;
            //Func<IQueryable<Account>, IOrderedQueryable<Account>> orderBy = null;
            //bool local = false; //Acc has no local
            
            //Expression<Func<Trade, bool>> receivedCriteria = null;
            CreateTradeBL(out fix, out moqUOW, out sut);

            var accounts = (new List<Account>
            { new Account
                {
                    Amount = 100,
                    TranDt = DateTime.Now,
                    Id = 5,
                    Type = AccountTrans.Deposit
                },
                new Account
                {
                    Amount = 30,
                    TranDt = DateTime.Now,
                    Id = 6,
                    Type = AccountTrans.Withdraw
                },
            }.AsEnumerable());

            var trades = (new List<Trade>
            { new Trade
                {
                    Qty = 10, Sold = 10, Price = 100, Sym = "TS", Cmsn = 2,
                    Type = Transaction.Buy
                },
                new Trade
                {
                    Qty = 10, Sold = 10, Price = 110, Sym = "TS", Cmsn = 3,
                    Type = Transaction.Sell
                },
            }.AsEnumerable());
            //sut.AddAccountTransaction(fix.Create<Account>());

            moqUOW.Setup(r => r.AccountRepository.Get(It.IsAny<Expression<Func<Account, bool>>>(), It.IsAny<Func<IQueryable<Account>, IOrderedQueryable<Account>>>(), It.IsAny<string>(), It.IsAny<bool>())).Returns(accounts);
            //moqUOW.Setup(r => r.TradeRepository.Get(It.Is<Expression<Func<Trade, bool>>>(t => t.Compile().Invoke(new Trade { Type=Transaction.Buy })), It.IsAny<Func<IQueryable<Trade>, IOrderedQueryable<Trade>>>(), It.IsAny<string>(), It.IsAny<bool>())).Returns(trades);

            moqUOW.Setup(r => r.TradeRepository.Get(It.IsAny<Expression<Func<Trade, bool>>>(), It.IsAny<Func<IQueryable<Trade>, IOrderedQueryable<Trade>>>(), It.IsAny<string>(), It.IsAny<bool>())).Returns(trades);
                //.Callback<Expression<Func<Trade, bool>>>(o=>receivedCriteria = o);
            var accStatus = sut.GetAccountStatus();

            Assert.IsTrue(accStatus.InHand == 165);

        }
    }
}
