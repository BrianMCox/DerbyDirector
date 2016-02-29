
(function () {
    'use strict';

    angular.module('pwderby', ['ui.router'])
        .config(LoadManagerConfig);

    LoadManagerConfig.$inject = ['$locationProvider'];

    function LoadManagerConfig($locationProvider) {
        $locationProvider.html5Mode(true);
    };
})();