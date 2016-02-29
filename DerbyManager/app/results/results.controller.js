
(function () {
    'use strict';

    angular.module('pwderby')
      .controller('resultsCtrl', resultsCtrl);

    resultsCtrl.$inject = ['event.service', 'results.service'];
    function resultsCtrl(events, results) {
        var main = this;
        main.results = [];
        main.selectedGroup = {};
        main.selectedGroupName = "";
        main.showResult = showResult;
        main.updateResults = updateResults;

        function init() {
            results.load()
                .then(function (response) {
                    angular.copy(response.data, main.results);
                });
        };

        function showResult(index) {
            main.selectedGroup.IndividualResults[index].Visible = true;
        };

        function updateResults(group) {
            for (var i = 0; i < main.results.length; i++) {
                if (main.results[i].GroupName === main.selectedGroupName) {
                    main.selectedGroup = main.results[i];
                    return;
                }
            }
        };

        init();
    };
})();