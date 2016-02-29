
(function () {
    'use strict';

    angular.module('pwderby')
      .controller('eventCtrl', eventCtrl);

    eventCtrl.$inject = ['$scope', 'commonServices', 'EventService'];
    function eventCtrl($scope, commonServices, EventService) {
        var main = this;
        main.addDivision = addDivision;
        main.addGroup = addGroup;
        main.isEventValid = isEventValid;
        main.save = save;
        main.selectedEvent = {};
        main.selectedGroup = {};
        main.setSelectedGroup = setSelectedGroup;
        main.showJson = false;
        //resetEvent();

        function init() {
            EventService.get()
                .then(function (response) {
                    angular.copy(response.data, main.selectedEvent);
                    if (!main.selectedEvent.Groups) { main.selectedEvent.Groups = []; }
                });
        };

        function addDivision(divisionName, groupName) {
            //var group = commonServices.find(main.selectedEvent.groups, groupName, 'name');
            var group = main.selectedGroup;
            if (!group) { return; }
            group.Divisions.push({
                Name: divisionName,
                Awards: 3,
                Entrants: []
            });
            main.divisionName = null;
        };

        function addGroup(groupName) {
            main.selectedEvent.Groups.push({
                Name: groupName,
                Divisions: [],
                Awards: 0,
                Entrants: []
            });
            $scope.groupName = null;
        };

        function isEventValid() {
            if (!main.selectedEvent) { return false; }
            if (!main.selectedEvent.Name || main.selectedEvent.Name.length === 0) { return false; }
            if (main.selectedEvent.Groups.length === 0) { return false; }

            return true;
        };

        function resetEvent() {
            main.selectedEvent = {};
            main.selectedEvent.Lanes = 3;
            main.selectedEvent.Groups = [];
        };

        function setSelectedGroup(group) {
            if (group === main.selectedGroup) {
                main.selectedGroup = {};
            } else {
                main.selectedGroup = group;
            }
        };

        function save() {
            EventService.save(main.selectedEvent);
        };

        init();
    };
})();