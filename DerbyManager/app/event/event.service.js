
(function () {
    'use strict';

    angular.module('pwderby')
    .factory('EventService', EventService);

    EventService.$inject = ['$http'];
    function EventService($http) {
        var service = {
            get : getEvent,
            save : saveEvent
        };

        return service;

        function getEvent() {
            return $http.get('http://localhost:51845/api/event');
        }

        function saveEvent(evt) {
            return $http.put('http://localhost:51845/api/event', evt);
        }
    };
})();