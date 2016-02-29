
(function () {
    'use strict';

    angular.module('pwderby')
      .controller('homeCtrl', homeCtrl);

    homeCtrl.$inject = [];
    function homeCtrl() {
        var main = this;
        main.test = "TEST";
    };
})();