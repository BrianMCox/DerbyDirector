
(function () {
    'use strict';

    angular.module('pwderby')
      .factory('commonServices', commonServices);

    commonServices.$inject = [];
    function commonServices() {
        // publicly exposed service 
        var service = {};
        service.find = find;

        return service;

        // "private" methods

        function find(arr, toFind, propName) {
            for(var i=0; i<arr.length; i++) {
                if (arr[i][propName] === toFind) { return arr[i]; }
            }
        };

        // end "private" section
    };
})();
