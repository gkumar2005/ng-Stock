angular.module('stock', ['ui.bootstrap'])
    .controller('stockCtrl', ['$scope', '$http', function ($scope, $http) {
        $scope.isCash = false;
        $scope.isDeposit = true;
        $scope.isWithdraw = true;
        $scope.deposit = function() {
            $scope.isWithdraw = false;
            $scope.isCash = true;
            $scope.cashType = 0;
        };
        $scope.withdraw = function () {
            $scope.isDeposit = false;
            $scope.isCash = true;
            $scope.cashType = 1;
        };
        $scope.cancel = function () {
            $scope.isDeposit = true;
            $scope.isWithdraw = true;
            $scope.isCash = false;
        };
        $scope.postAmt = function(parameters) {
            var params = { Type: $scope.cashType, Amount: $scope.cash, TranDt: $scope.tranDt };

            $http.post('/api/Values/PostAmt', params)
                .success(function (data, status, headers, cofig) {
                    $scope.message = data.Message;
                    $scope.showAlert = true;
                })
                .error(function (data, status, headers, config) {
                    $scope.message = data.Message;
                    $scope.showAlert = true;
                });
        }
        $scope.genData = function (key) {

            var YAHOO = window.YAHOO = { Finance: { SymbolSuggest: {} } };
            YAHOO.Finance.SymbolSuggest.ssCallback = function (data) {
                var sym = [];

                angular.forEach(data.ResultSet.Result, function (item) {
                    sym.push(item.symbol);//+ '(' + item.name + ')'
                });
                $scope.async = sym;
            }

            $http.jsonp('http://d.yimg.com/autoc.finance.yahoo.com/autoc?query=' + key + '&callback=YAHOO.Finance.SymbolSuggest.ssCallback&callback=JSON_CALLBACK')
            /*http://query.yahooapis.com/v1/public/yql?q=select%20*%20from%20yahoo.finance.quotes%20where%20symbol%3D%22goog%22&format=json&env=store%3A%2F%2Fdatatables.org%2Falltableswithkeys&callback=xyz')*/
             .success(function (data) {
             })
            .error(function (data, status, headers, config) {
                console.log(status);
            });
            if ($scope.async != undefined)
                return $scope.async;
        }
        $scope.post = function () {
            var params = { Sym: $scope.sym, Type: $scope.type, Qty: $scope.qty, Price: $scope.price, DCash: $scope.dCash, Cmsn: $scope.cmsn, Date: $scope.dt };

            $http.post('/api/Values/Post', params)
                .success(function (data, status, headers, cofig) {
                    $scope.message = data.Message;
                    $scope.showAlert = true;
                    location.href = '#/StockDetails';
                })
                .error(function (data, status, headers, config) {
                $scope.message = data.Message;
                $scope.showAlert = true;
            });
        }
    }]);

//$(function () {
//$("#symbol").autocomplete({
//    source: function (request, response) {

//        // faking the presence of the YAHOO library bc the callback will only work with
//        // "callback=YAHOO.Finance.SymbolSuggest.ssCallback"
//        var YAHOO = window.YAHOO = { Finance: { SymbolSuggest: {} } };

//        YAHOO.Finance.SymbolSuggest.ssCallback = function (data) {
//            var mapped = $.map(data.ResultSet.Result, function (e, i) {
//                return {
//                    label: e.symbol + ' (' + e.name + ')',
//                    value: e.symbol
//                };
//            });
//            response(mapped);
//        };

//        var url = [
//            "http://d.yimg.com/autoc.finance.yahoo.com/autoc?",
//            "query=" + request.term,
//            "&callback=YAHOO.Finance.SymbolSuggest.ssCallback"];

//        $.getScript(url.join(""));
//    },
//    minLength: 2
//});
//})