--Profit
Select sum(Profit) as TotProfit from 
  (Select Sym,sum(TotPrice) as Profit from (select Sym, type, case when type=0 then -sum(price*qty+cmsn) else sum(price*qty-cmsn) end as TotPrice
from trades where archive=1 group by sym, type) as T1 group by Sym) as Profit1

--Stock pur/sold
select Type, sum(TotPrice) as TranCost from (select Sym, type, case when type=0 then -sum(price*qty+cmsn) else sum(price*qty-cmsn) end as TotPrice
from trades where archive=1 group by sym, type) as T1 group by Type

--Stock pur
select Type, sum(TotPrice) as TranCost from (select Sym, type, case when type=0 then -sum(price*qty+cmsn) else sum(price*qty-cmsn) end as TotPrice
from trades where archive=0 group by sym, type) as T1 group by Type

select -33057.95-157528.48

select sym,type,sum(qty), sum(price*qty+cmsn) from Trades where Sym in('nflx', 'tsla','GOOGL','GOOG','AAPL','ACE') and cmsn in (9.99,5) group by sym, type

select * from trades where sym='ACE'
--insert trades values('ACE',0,10,100.5,1,9.99,'01/07/2014', 1)
--insert trades values('ACE',1,10,101,1,9.99,'04/16/2014', 1)
select SUM(amount) from Account where Type=1
select sum(amount) from Account where [desc] is not null
select * from account
select 71978.64+595.2*7 -- Funds Available for Trading

select 111761.75-6405.66+161925.33-158543.47-33057.95 -- stockInHand
select 111761.75-61.75-6405.66-8500.00-6000.00 -- Dep in E*trade


select 32538.42+489.50 -- stockinhand without cmsn
select sym, type,qty, price, case when type=0 then price*qty+cmsn else price*qty-cmsn end,date,cmsn from Trades where Cmsn like '2.5%'
 and Archive=0
