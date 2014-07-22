angular.module('stockDetail', ['ngGrid'])
    .controller('stockDetailCtrl', ['$scope', '$http', '$q', '$filter', function ($scope, $http, $q, $filter) {
        $scope.amtHand = function() {
            $http.get('/api/Values/GetCash')
                .success(function(data, status, headers, cofig) {
                    $scope.StockPur = data.StockPur;
                    $scope.StockSold = data.StockSold;
                    $scope.Deposited = data.Deposited;
                    $scope.Withdrawn = data.Withdrawn;
                    $scope.StockHand = data.StockHand;
                    $scope.InHand = data.InHand;
                    $scope.$apply();
                });
        };
        $scope.getList = function() {
            $http.get('/api/Values/Get')
                .success(function(data, status, headers, cofig) {
                    var arrSym = [];
                    angular.forEach(data, function(row) {
                        row.Type = row.Type == '0' ? 'Buy' : 'Sell';
                        row.Qty = parseInt(row.Qty);
                        row.Date = $filter('date')(row.Date, 'MM-dd-yyyy');
                        arrSym.push(row.Sym);
                    });
                    //$scope.stocks = data;
                    $scope.mktPrice(arrSym, data);
                    $scope.amtHand();
                    $scope.showAlert = true;
                })
                .error(function(data, status, headers, config) {
                    $scope.message = data.Message;
                    $scope.showAlert = true;
                });

            $scope.gridOptions = {
                data: 'stocks',
                showGroupPanel: true,
                rowHeight: 30,
                enableCellEdit: true,
                groups: ['Sym', 'Type'],
                sortInfo: {fields:['Type'], directions:['asc']},
                enableColumnResize: true,
                enableColumnReordering: true,
                showColumnMenu: true,
                showFilter: true,
                aggregateTemplate: "<div ng-click=\"row.toggleExpand()\" ng-style=\"rowStyle(row)\" class=\"ngAggregate\">" +
                                    "    <span class=\"ngAggregateText\">" +
                                           "<span class='ngAggregateTextLeading'>{{row.totalChildren()}} {{row.label CUSTOM_FILTERS}} </span>" +
                                           "<span class='aggrVal'> {{ aggLevel2(row, 'Qty')}} {{aggL1(row, 'Qty', 'Qty:', 1)}}  </span>" +
                                           "<span class='aggrVal'> {{ avgPriceL1(row, 'CostPrice')}}  </span>" +
                                           "<span class='aggrVal'> {{aggL1(row, 'CostPrice', 'Cost:')}}  </span>" +
                                           "<span class='aggrVal'> {{aggL1(row, 'MktValue', 'Mrkt:')}}  </span>" +
                                           "<span class='aggrVal'> {{profitByQty(row)}} </span>" +
                                        "</span>" +
                                    "    <div class=\"{{row.aggClass()}}\"></div>" +
                                    "</div>",
                columnDefs: [{ field: 'Sym', displayName: 'Symbol', width: '10%' }, { field: 'Type', displayName: 'Type', width: '3%', cellClass: 'grid-align' },
                    { field: 'Qty', displayName: 'Qty', width: '3%' }, { field: 'Price', displayName: 'Unit Price', width: '7%', cellClass: 'grid-align' }, { field: 'MktPrice', displayName: 'Mkt Price', width: '7%', cellClass: 'grid-align' },
                    { field: 'CostPrice', displayName: 'Cost', width: '7%', cellClass: 'grid-align' }, { field: 'MktValue', displayName: 'Mkt Value', width: '7%', cellClass: 'grid-align' },
                    { field: 'Cmsn', displayName: 'Commission', width: '5%' }, { field: 'Date', displayName: 'Date', width: '9%' }]
            };
        };
        $scope.avgPriceL1 = function (row, col) {
            var avg;
            if (row.children.length > 0) {
                avg = "Avg:" + ($scope.aggLevel1(row, col) / $scope.aggLevel1(row, 'Qty')).toFixed(2);
                return avg;
            }
        };
        $scope.profitByQty = function(row) {
            var profit ='';
            var children;
            if (row.children.length == 0) {
                children = row.aggChildren;
                if (children.length == 2) {
                    var grpSym = {};
                    $.each($scope.stocks, function(i, item) {
                        if (grpSym[item.Sym])
                            grpSym[item.Sym].push(item);
                        else {
                            grpSym[item.Sym] = [item];
                        }
                    });
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
                    return 'Profit: ' + profit;
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
                    return sum.toFixed(2);
                else {
                    return sum;
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
       
        $scope.mktPrice = function(arrSym, rows) {
            getStock({
                    symbols: arrSym,
                    display: ['symbol', 'LastTradePriceOnly']
                }, function(err, stock) {
                    if (err) {
                        alert('Error:' + error);
                        return;
                    }
                    angular.forEach(rows, function(row) {
                        if (stock.quote.length > 1)
                            row.MktPrice = $.grep(stock.quote, function(n, i) {
                                return n.symbol == row.Sym;
                            })[0].LastTradePriceOnly;
                        else
                            row.MktPrice = stock.quote.LastTradePriceOnly;
                        //Price*Qty
                        row.MktValue = ((row.MktPrice * row.Qty) - row.Cmsn).toFixed(2);
                        row.CostPrice = (row.Type == 'Buy' ? (row.Price * row.Qty) + row.Cmsn : (row.Price * row.Qty) - row.Cmsn).toFixed(2);
                    });
                    $scope.stocks = rows;
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

        $scope.getList();
    }]);



