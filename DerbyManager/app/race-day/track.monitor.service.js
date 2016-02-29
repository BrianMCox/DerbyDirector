
(function () {
    'use strict';

    angular.module('pwderby')
      .factory('track.monitor.service', trackMonitorService);

    trackMonitorService.$inject = ['$http', '$rootScope'];
    function trackMonitorService($http, $rootScope) {
        var resultsHub = $.connection.resultsHub;
        resultsHub.client.broadcastResults = function (results) {
            console.log(results);
            //angular.copy(results, results.LaneResults);
            $rootScope.$broadcast('race-results-event', results.LaneResults);
        };
        $.connection.hub.start().done(function () {

        });

        var service = {};
        service.clearRace = clearRace;;
        service.endRace = endRace;
        service.lastTest = '';
        service.newConnection = newConn;
        service.port = {};
        //service.results = [];
        service.start = start;
        service.testConnection = testConn;

        init();

        return service;

        function init() {
            
        }

        function clearRace() {
            return $http.get('http://localhost:51845/api/race/timer/clearRace');
        }

        function endRace() {
            return $http.get('http://localhost:51845/api/race/timer/endRace');
        }

        function newConn() {
            return $http.get('http://localhost:51845/api/race/timer/newConnection')
                .then(setPort);
        }

        function setPort(response) {
            service.port = response.data;
        }

        function start() {
            return $http.get('http://localhost:51845/api/race/timer/init')
                .then(setPort);
        }

        function testConn() {
            return $http.get('http://localhost:51845/api/race/timer/test')
                .then(function (response) {
                    if (response.data === true) {
                        service.lastTest = 'Successful';
                    } else {
                        service.lastTest = 'Failure';
                    }
                });
        }
    };
})();