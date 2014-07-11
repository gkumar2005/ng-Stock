angular.module('stockDetail', ['ngGrid'])
    .controller('stockDetailCtrl', ['$scope', '$http', '$q', function ($scope, $http, $q) {

        $scope.getList = function() {
            $http.get('/api/Values/Get')
                .success(function(data, status, headers, cofig) {
                    var arrSym = [];
                    angular.forEach(data, function(row) {
                        row.Type = row.Type == '0' ? 'Buy' : 'Sell';
                        arrSym.push(row.Sym);
                    });
                    //$scope.stocks = data;
                    $scope.mktPrice(arrSym, data);
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
                aggregateTemplate: "<div ng-click=\"row.toggleExpand()\" ng-style=\"rowStyle(row)\" class=\"ngAggregate\">" +
                                    "    <span class=\"ngAggregateText\">" +
                                    "       <span class='ngAggregateTextLeading'>{{row.totalChildren()}} {{row.label CUSTOM_FILTERS}} </span>" +
                                           "<span class='aggrVal'>Qty: {{ aggByLevel(row, 'Qty') }}  </span>" +
                                           "<span class='aggrVal'>Avg: {{ avgPrice(row, 'CostPrice')}}  </span>" +
                                           "<span class='aggrVal'>Cost: {{aggByLevel(row, 'CostPrice')}}  </span>" +
                                           "<span class='aggrVal'>Mkt: {{aggByLevel(row, 'MktValue')}}  </span>" +
                        "<span class='aggrVal'>{{profitByQty(row)}}  </span>" +
                                        "</span>" +
                                    "    <div class=\"{{row.aggClass()}}\"></div>" +
                                    "</div>" + "",
                columnDefs: [{field: 'Sym', displayName: 'Symbol'}, { field: 'Type', displayName: 'Type' },
                    { field: 'Qty', displayName: 'Qty' }, { field: 'Price', displayName: 'Unit Price' }, { field: 'MktPrice', displayName: 'Mkt Price' },
                    { field: 'CostPrice', displayName: 'Trans Cost' }, { field: 'MktValue', displayName: 'Mkt Value' },
                    { field: 'Cmsn', displayName: 'Commission' }, { field: 'Date', displayName: 'Date' }]
            };
        };
        $scope.avgPrice = function (row, col) {
            return ($scope.aggByLevel(row, col) / $scope.aggByLevel(row, 'Qty')).toFixed(2);
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
                            totSellPrice += ((item.Qty * item.Price) + item.Cmsn);
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
        $scope.aggByLevel = function(row, col) {
            var total;
            var children;
            if (row.children.length == 0) {
                children = row.aggChildren;
                total = (children.length == 2) ? agg(children[0].children, col) - agg(children[1].children, col) : agg(children[0].children, col); //ex: Type = buy and sell
            } else {
                children = row.children;
                total = agg(children, col);
            }
            return total.toFixed(2);
        };
        var agg = function (children, col) {
            var total = 0;
            angular.forEach(children, function(cropEntry) {
                total += parseFloat(cropEntry.entity[col]);
            });
            return total;
        };
       
        $scope.entryMaybePlural = function (row) {
            if (row.children.length > 1) {
                return "entries";
            }
            else
                return "entry";
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
                        row.MktValue = (row.MktPrice * row.Qty) - row.Cmsn;
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



