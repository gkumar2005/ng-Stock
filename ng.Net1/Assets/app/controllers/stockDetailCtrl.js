angular.module('stockDetail', ['ngGrid'])
    .controller('stockDetailCtrl', ['$scope', '$http', '$filter','$cookieStore', function ($scope, $http, $filter,$cookieStore) {
        $scope.jsonPost = function () {
            $http.post('/api/Values/Postg', $scope.jsonRows)
            .success(function (data, status, headers, cofig) {
                if (status =202)
                $scope.msg = "Success";
            })
                .error(function (data, status, headers, config) {
                    $scope.msg = data.InnerException.InnerException.ExceptionMessage;
                });
        };
        $scope.amtHand = function() {
            $http.get('/api/Values/GetCash')
                .success(function(data, status, headers, cofig) {
                    $scope.StockPur = data.StockPur;
                    $scope.StockSold = data.StockSold;
                    $scope.Deposited = data.Deposited;
                    $scope.Withdrawn = data.Withdrawn;
                    $scope.StockHand = data.StockHand;
                    $scope.Profit = data.Profit;
                    $scope.InHand = data.InHand;
                });
        };
        
        $scope.getList = function() {
            $http.get('/api/Values/Get')
                .success(function(data, status, headers, cofig) {
                    var arrSym = [];
                    angular.forEach(data, function(grp) {
                        angular.forEach(grp, function(row) {
                            row.Type = row.Type == '0' ? 'Buy' : 'Sell';
                            row.Qty = parseInt(row.Qty);
                            row.Date = $filter('date')(row.Date, 'MM-dd-yyyy');
                            if ($.inArray(row.Sym, arrSym)==-1)
                                arrSym.push(row.Sym);
                        });
                    });
                    $scope.mktPrice(arrSym, data);
                    $scope.amtHand();
                    $scope.showAlert = true;
                })
                .error(function(data, status, headers, config) {
                    $scope.message = data.Message;
                    $scope.showAlert = true;
                });
            $scope.gridDefs = [{ field: 'Sym', displayName: 'Symbol', width: '10%' }, { field: 'Type', displayName: 'Type', width: '3%', cellClass: 'grid-align' },
                { field: 'Qty', displayName: 'Qty', width: '3%' }, { field: 'Price', displayName: 'Unit Price', width: '7%', cellClass: 'grid-align' }, 
                { field: 'CostPrice', displayName: 'Cost', width: '7%', cellClass: 'grid-align' }, { field: 'MktValue', displayName: 'Mkt Value', width: '7%', cellClass: 'grid-align' },
                { field: 'Cmsn', displayName: 'Commission', width: '5%' }, { field: 'Date', displayName: 'Date', width: '9%' }];
            $scope.gridOptions = {
                data: 'stocks',
                showGroupPanel: true,
                enableCellEdit: true,
                groups: ['Sym'],
                enableColumnResize: true,
                enableColumnReordering: true,
                showFilter: true,
                aggregateTemplate: "<div  ng-click=\"row.toggleExpand()\" ng-style=\"rowStyle(row)\" class=\"ngAggregate\">" +
                                        "<span class=\"ngAggregateText\">" +
                                            "<span class='ngAggregateTextLeading'>{{row.totalChildren()}} {{row.label}} </span>" +
                                            "<span class='aggrVal'> {{aggL1(row, 'Qty', 'Qty:', 1)}}  </span>" +
                                            "<span class='aggrVal'> {{ avgPriceL1(row, 'CostPrice')}}  </span>" +
                                            "<span> <input type='textbox' style='width: 30px;' ng-model='Qty2Buy' ng-change='ifAddQty(row, y, this)' ng-click='$event.stopPropagation();' />" +
                                                "<span ng-show='Qty2Buy'>{{expAvgPrice}}</span>" +
                                            "</span>" +
                                            "<span class='aggrVal'> MP: {{ getMktPriceBySym(row.label)}}  </span>" +
                                            "<span class='aggrVal'> {{aggL1(row, 'CostPrice', 'Cost:')}}  </span>" +
                                            "<span class='aggrVal'> {{aggL1(row, 'MktValue', 'Mrkt:')}}  </span>" +
                                            "<span class='aggrVal'> <label x={{y=gainOLoss(row)}} class={{color}}>{{y | currency}}</label> </span>" +
                                        "</span>" +
                                        "<div class=\"{{row.aggClass()}}\"></div>" +
                                   "</div>",
                columnDefs: 'gridDefs'
            }; 
            $scope.oldGridOptions = {
                data: 'oldStocks',
                showGroupPanel: true,
                enableCellEdit: true,
                groups: ['Sym', 'Type'],
                sortInfo: { fields: ['Sym'], directions: ['asc'] },
                enableColumnResize: true,
                enableColumnReordering: true,
                showColumnMenu: true,
                showFilter: true,
                enablePaging:true,
                aggregateTemplate: "<div ng-click=\"row.toggleExpand()\" ng-style=\"rowStyle(row)\" class=\"ngAggregate\">" +
                    "    <span class=\"ngAggregateText\">" +
                    "<span class='ngAggregateTextLeading'>{{row.totalChildren()}} {{row.label}} </span>" +
                    "<span class='aggrVal'> {{ aggLevel2(row, 'Qty')}} {{aggL1(row, 'Qty', 'Qty:', 1)}}  </span>" +
                    "<span class='aggrVal'> {{ avgPriceL1(row, 'CostPrice')}}  </span>" +
                    "<span class='aggrVal'> {{aggL1(row, 'CostPrice', 'Cost:')}}  </span>" +
                    "<span class='aggrVal'> {{aggL1(row, 'MktValue', 'Mrkt:')}}  </span>" +
                    "<span class='aggrVal'> <label x={{y=profitByQty(row)}} class={{color}}>{{y | currency}}</label> </span>" +
                    "</span>" +
                    "    <div class=\"{{row.aggClass()}}\"></div>" +
                    "</div>",
                columnDefs: 'gridDefs'
            };
        };
        $scope.getMktPriceBySym = function (sym) {
            var rowMP;
            $.each($scope.stocks, function (i, item) {
                if (this.Sym == sym) {
                    rowMP = item.MktPrice;
                    return false;
                }
            });
            return rowMP;
        };
        $scope.ifAddQty = function (row, diff, q2b) {
            if (diff < 0) {
                $scope.expAvgPrice = (($scope.aggLevel1(row, 'CostPrice') + $scope.getMktPriceBySym(row.label) * parseInt(q2b.Qty2Buy)) / ($scope.aggLevel1(row, 'Qty', 1) + parseInt(q2b.Qty2Buy))).toFixed(2);
                    //= Math.ceil(Math.abs(profit / rowMP));
            }
        }
        $scope.gainOLoss = function (row) {
            var profit = $scope.aggLevel1(row, 'MktValue') - $scope.aggLevel1(row, 'CostPrice');
            $scope.color = (profit < 0) ? "red" : "green";
            
            return profit.toFixed(2);
        };
        $scope.avgPriceL1 = function(row, col) {
            var avg;
            if (row.children.length > 0) {
                avg = "Avg:" + ($scope.aggLevel1(row, col) / $scope.aggLevel1(row, 'Qty')).toFixed(2);
                return avg;
            }
        };

        function group(term) {
            var grp = {};
            $.each($scope.oldStocks, function(i, item) {
                if (grp[item[term]])
                    grp[item[term]].push(item);
                else {
                    grp[item[term]] = [item];
                }
            });
            return grp;
        }

        $scope.profitByQty = function(row) {
            var profit ='';
            var children;
            if (row.children.length == 0) {
                children = row.aggChildren;
                if (children.length == 2) {
                    var grpSym = group('Sym');
                    var sQty = 0, bQty= 0;
                    var totSellPrice=0.0, totCostPrice = 0.0;
                    $.each(grpSym[row.label], function (j, item) {
                        if (item.Type == 'Sell') {
                            sQty += item.Qty;
                            totSellPrice += ((item.Qty * item.Price) - item.Cmsn);
                        } else {
                            bQty += item.Qty;
                            totCostPrice += ((item.Qty * item.Price) + item.Cmsn);
                        }
                    });
                    var avgSPrice = totSellPrice / sQty;
                    var avgCPrice = totCostPrice / bQty;
                    profit = (avgSPrice * sQty - avgCPrice * sQty).toFixed(2);
                    $scope.color = (profit < 0) ? "red" : "green"; 
                    return profit;
                }
            }
        };
        $scope.aggLevel2 = function (row, col) {
            var total;
            var children;
            if (row.children.length == 0) {
                children = row.aggChildren;
                if (children.length == 2 && col == 'Qty')
                    total = agg(children[0].children, col) - agg(children[1].children, col); //ex: qty = buyQty - sellQty
            } 
            if (total != undefined) {
                return col + ":" + total;
            }
        };
        $scope.aggL1 = function (row, col, label, integer) {
            var sum = $scope.aggLevel1(row, col, integer);
            if (sum != undefined)
                return label + sum;
        };
        $scope.aggLevel1 = function (row, col, integer) {
            var sum;
            if (row.children.length > 0) {
                sum = agg(row.children, col);
                if (!integer)
                    return parseFloat(sum.toFixed(2));
                else {
                    return parseInt(sum);
                }
            }
        };
        var agg = function (children, col) {
            var total = 0;
            angular.forEach(children, function(cropEntry) {
                total += parseFloat(cropEntry.entity[col]);
            });
            return total;
        };
       
        $scope.mktPrice = function(arrSym, grps) {
            getStock({
                    symbols: arrSym,
                    display: ['symbol', 'LastTradePriceOnly']
                }, function(err, stock) {
                    if (err) {
                        alert('Error:' + error);
                        return;
                    }
                    angular.forEach(grps, function(grp) {
                        angular.forEach(grp, function(row) {
                            if (stock.quote.length > 1)
                                row.MktPrice = $.grep(stock.quote, function(n, i) {
                                    return n.symbol == row.Sym;
                                })[0].LastTradePriceOnly;
                            else
                                row.MktPrice = stock.quote.LastTradePriceOnly;
                            //Price*Qty
                            row.MktValue = (row.MktPrice * row.Qty).toFixed(2);
                            row.CostPrice = (row.Type == 'Buy' ? (row.Price * row.Qty) + row.Cmsn : (row.Price * row.Qty) - row.Cmsn).toFixed(2);
                        });
                    });
                    $scope.stocks = grps[0];
                    $scope.oldStocks = grps[1];
                    $scope.$apply();
                });
        };

        function getStock(opts, complete) {
            var defs = {
                desc: false,
                baseURL: 'http://query.yahooapis.com/v1/public/yql?q=',
                query: 'select {display} from yahoo.finance.quotes where symbol in ({quotes}) | sort(field="{sortBy}", descending="{desc}")',
                suffixURL: '&env=store://datatables.org/alltableswithkeys&format=json&callback=?'
            };

            opts = $.extend({
                display: ['*'],
                symbols: []
            }, opts || {});

            if (!opts.symbols.length) {
                complete('No stock defined');
                return;
            }

            var query = {
                display: opts.display.join(', '),
                quotes: opts.symbols.map(function (stock) {
                    return '"' + stock + '"';
                }).join(', ')
            };

            defs.query = defs.query
                .replace('{display}', query.display)
                .replace('{quotes}', query.quotes)
                .replace('{sortBy}', defs.sortBy)
                .replace('{desc}', defs.desc);

            defs.url = defs.baseURL + defs.query + defs.suffixURL;
            
          $.getJSON(defs.url, function (data) {
                var err = null;
                if (!data || !data.query) {
                    err = true;
                }
                complete(err, !err && data.query.results);
            }); 
        }

        if ($cookieStore.get('_Token') == undefined)
            window.location = '#/signin';
        else
            $scope.getList();
    }]);



