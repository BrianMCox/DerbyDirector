
(function () {
    'use strict';

    angular.module('pwderby')
      .factory('results.service', resultsService);

    resultsService.$inject = ['$http'];
    function resultsService($http) {
        var service = {};
        service.load = loadResults;

        return service;

        function loadResults() {
            return $http.get('http://localhost:51845/api/race/results');
        };
    };
})();