
(function () {
    'use strict';

    angular.module('pwderby')
        .config(LoadManagerRouting);

    LoadManagerRouting.$inject = ['$stateProvider', '$urlRouterProvider']

    function LoadManagerRouting($stateProvider, $urlRouterProvider) {
        $urlRouterProvider.otherwise("/");

        $stateProvider
        .state('home', {
            url: '/',
            templateUrl: '/app/home.tmpl.html',
            controller: 'homeCtrl',
            controllerAs: 'vm'
        })
        .state('event', {
            url: '/event',
            templateUrl: '/app/event/event.tmpl.html',
            controller: 'eventCtrl',
            controllerAs: 'vm'
        })
        .state('registration', {
            url: '/registration',
            templateUrl: '/app/registration/registration.tmpl.html',
            controller: 'regCtrl',
            controllerAs: 'vm'
        })
        .state('race', {
            url: '/race-day',
            templateUrl: '/app/race-day/race.tmpl.html',
            controller: 'raceCtrl',
            controllerAs: 'vm'
        })
        .state('results', {
            url: '/results',
            templateUrl: '/app/results/results.html',
            controller: 'resultsCtrl',
            controllerAs: 'vm'
        });
    };
})();