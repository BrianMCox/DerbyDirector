
(function () {
    'use strict';

    angular.module('pwderby')
      .controller('regCtrl', regCtrl);

    regCtrl.$inject = ['EventService'];
    function regCtrl(EventService) {
        var main = this;
        main.addNewEntrant = addNewEntrant;
        main.allEntrants = [];
        main.cancel = cancel;
        main.divisions = [];
        main.event = {};
        main.getDivisions = getDivisions;
        main.newEntrant = null;
        main.groups = [];
        main.save = save;

        function init() {
            EventService.get()
                .then(function (response) {
                    angular.copy(response.data, main.event);
                    angular.copy(getGroups(), main.groups);
                    getAllEntrants();
                });
        };

        function addNewEntrant() {
            main.newEntrant = {};
        };

        function cancel() {
            main.newEntrant = null;
        };

        function getAllEntrants() {
            main.allEntrants.length = 0;

            for (var i = 0; i < main.event.Groups.length; i++) {
                if (main.event.Groups[i].Divisions.length > 0) {
                    for (var j = 0; j < main.event.Groups[i].Divisions.length; j++) {
                        getEntrants(main.event.Groups[i].Divisions[j]);
                    }
                }

                if (main.event.Groups[i].Entrants) { getEntrants(main.event.Groups[i]); }
            };
        };

        function getEntrants(unit) {
            for (var e = 0; e < unit.Entrants.length; e++) {
                main.allEntrants.push(unit.Entrants[e]);
            }
        };

        function getDivisions(groupName) {
            for (var i = 0; i < main.event.Groups.length; i++) {
                if (main.event.Groups[i].Name === groupName) {
                    main.divisions = getDivisionNames(i);
                }
            };
        };

        function getDivisionNames(i) {
            return main.event.Groups[i].Divisions.map(function (div) {
                return div.Name;
            });
        };

        function getGroups() {
            return main.event.Groups.map(function (group) {
                return group.Name;
            });
        };

        function save() {
            for (var i = 0; i < main.event.Groups.length; i++) {
                if (main.event.Groups[i].Name === main.newEntrant.Group) {
                    if (main.newEntrant.Division) {
                        for (var j = 0; j < main.event.Groups[i].Divisions.length; j++) {
                            if (main.event.Groups[i].Divisions[j].Name === main.newEntrant.Division) {
                                main.event.Groups[i].Divisions[j].Entrants = main.event.Groups[i].Divisions[j].Entrants || [];
                                main.event.Groups[i].Divisions[j].Entrants.push({Name: main.newEntrant.Name, CarNumber: main.newEntrant.CarNumber});
                                break;
                            }
                        }
                    } else {
                        main.event.Groups[i].Entrants = main.event.Groups[i].Entrants || [];
                        main.event.Groups[i].Entrants.push({ Name: main.newEntrant.Name, CarNumber: main.newEntrant.CarNumber });
                    }
                }
            }

            EventService.save(main.event);
            main.allEntrants.push({ Name: main.newEntrant.Name, CarNumber: main.newEntrant.CarNumber });
            main.newEntrant = null;
        };

        init();
    };
})();