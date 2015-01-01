angular.module('stock', ['ui.bootstrap'])
    .controller('stockCtrl', ['$scope', '$http', '$filter', '$q', '$timeout','$cookieStore', function ($scope, $http, $filter, $q, $timeout, $cookieStore) {
        if ($cookieStore.get('_Token') == undefined)
            window.location = '#/signin';
        else {
            $scope.cmsn = 2.5;
            $scope.qty = 10;
            $scope.isCash = false;
            $scope.isDeposit = true;
            $scope.isWithdraw = true;

            $scope.dt = $filter('date')(new Date(), 'yyyy-MM-dd');
            $scope.tranDt = $filter('date')(new Date(), 'yyyy-MM-dd');
            $scope.types = { type: 0, values: [{ id: 0, val: "Buy" }, { id: 1, val: "Sell" }] };
        }
        $scope.deposit = function() {
            $scope.isWithdraw = false;
            $scope.isCash = true;
            $scope.cashType = 1;
        };
        $scope.withdraw = function() {
            $scope.isDeposit = false;
            $scope.isCash = true;
            $scope.cashType = 0;
        };
        $scope.cancel = function() {
            $scope.isDeposit = true;
            $scope.isWithdraw = true;
            $scope.isCash = false;
        };
        $scope.postAmt = function(parameters) {
            var params = { Type: $scope.cashType, Amount: $scope.cash, TranDt: $scope.tranDt };

            $http.post('/api/Values/PostAmt', params)
                .success(function(data, status, headers, cofig) {
                    $scope.message = data.Message;
                    $scope.showAlert = true;
                })
                .error(function(data, status, headers, config) {
                    $scope.message = data.Message;
                    $scope.showAlert = true;
                });
        };
        $scope.genData = function(key) {
            var deferred = $q.defer();
            var YAHOO = window.YAHOO = { Finance: { SymbolSuggest: {} } };
            YAHOO.Finance.SymbolSuggest.ssCallback = function(data) {
                var sym = [];

                angular.forEach(data.ResultSet.Result, function(item) {
                    sym.push({ sym: item.symbol, name: item.name });
                    deferred.resolve(sym);
                });
            };

            $http.jsonp('http://d.yimg.com/autoc.finance.yahoo.com/autoc?query=' + key + '&callback=YAHOO.Finance.SymbolSuggest.ssCallback');
            /*http://query.yahooapis.com/v1/public/yql?q=select%20*%20from%20yahoo.finance.quotes%20where%20symbol%3D%22goog%22&format=json&env=store%3A%2F%2Fdatatables.org%2Falltableswithkeys&callback=xyz')*/

            return deferred.promise;
        };
        $scope.post = function() {
            var params = { Sym: $scope.sym, Type: $scope.types.type, Qty: $scope.qty, Price: $scope.price, Cmsn: $scope.cmsn, Date: $scope.dt, Archive: false };
            $http.post('/api/Values/Post', params)
                .success(function (data, status, headers, cofig) {
                    if (status==202)
                        $scope.msg = "Success!!";
                    //location.href = '#/stockDetails';
                })
                .error(function (data, status, headers, config) {
                    //$timeout(function () {
                        $scope.messages = data;
                   // });
                    //return deferred.reject(data);
                });
            //return deferred.promise;
        };
    }])
//.directive(
//        'dateInput',
//        function (dateFilter) {
//            return {
//                require: 'ngModel',
//                template: '<input type="date"></input>',
//                replace: true,
//                link: function (scope, elm, attrs, c) {
//                    c.$formatters.unshift(fmt);
//                    c.$parsers.unshift(fmt);
//                    function fmt(viewValue) {
//                        return dateFilter(viewValue, 'yyyy-MM-dd');
//                    };
//                },
//            };
//        });
