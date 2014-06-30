﻿angular.module('stockDetail', ['ngGrid'])
    .controller('stockDetailCtrl', ['$scope', '$http', '$q', function ($scope, $http, $q) {
        
        $scope.getList = function () {

         $http.get('/api/Values/Get')
                .success(function (data, status, headers, cofig) {
                    var arrSym = [];
                    angular.forEach(data, function (row) {
                        row.Type = row.Type = '0' ? 'Buy' : 'Sell';
                        arrSym.push(row.Sym);
                    });
                    $scope.stocks = data;
                    $scope.mktPrice(arrSym, data);
                    $scope.showAlert = true;
                })
                .error(function (data, status, headers, config) {
                    $scope.message = data.Message;
                    $scope.showAlert = true;
                });
         $scope.gridOptions = {
             data: 'stocks',
             showGroupPanel: true,
             rowHeight:30
         };
         
        }

        $scope.mktPrice = function (arrSym, rows) {
            getStock({
                stocks: arrSym,
                display: ['symbol', 'LastTradePriceOnly']
            }, function (err, stock) {
                if (err) {
                    alert('Error:' + error);
                    return;
                }
                angular.forEach(rows, function (row) {
                    if (stock.quote.length > 1)
                        row.mktPrice = $.grep(stock.quote, function (n, i) {
                            return n.symbol == row.Sym;
                        }).LastTradePriceOnly;
                    else
                        row.mktPrice = stock.quote.LastTradePriceOnly;
                });
                $scope.$apply();
            })
        }

        function getStock(opts, complete) {
            var defs = {
                desc: false,
                baseURL: 'http://query.yahooapis.com/v1/public/yql?q=',
                query: 'select {display} from yahoo.finance.quotes where symbol in ({quotes}) | sort(field="{sortBy}", descending="{desc}")',
                suffixURL: '&env=store://datatables.org/alltableswithkeys&format=json&callback=?'
            };

            opts = $.extend({
                display: ['*'],
                stocks: []
            }, opts || {});

            if (!opts.stocks.length) {
                complete('No stock defined');
                return;
            }

            var query = {
                display: opts.display.join(', '),
                quotes: opts.stocks.map(function (stock) {
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

