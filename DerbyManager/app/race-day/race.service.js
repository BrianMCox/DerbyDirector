
(function () {
    'use strict';

    angular.module('pwderby')
      .factory('race.service', raceService);

    raceService.$inject = ['$http'];
    function raceService($http) {
        var service = {};
        service.schedule = [];
        service.getSchedule = getSchedule;
        service.load = load;
        service.save = save;
        service.setResults = setResults;
        service.start = startRace;

        var numLanes = 0;
        var groupedHeats = [];

        return service;

        function checkGroup(element, index, array) {
            if (element.Divisions.length > 0) {
                element.Divisions.forEach(scheduleHeats);
            } else {
                scheduleHeats(element);
            }
        };

        function createHeat(lane, carIndex, entrants) {
            return {
                Lane: lane,
                CarNumber: entrants[carIndex % entrants.length].CarNumber,
                Driver: entrants[carIndex % entrants.length].Name,
                Time: 0.0,
                ScaleSpeed: 0.0
            }
        };

        function getSchedule() {
            var heatNum = 0;
            var keepChecking = true;

            while (keepChecking) {
                keepChecking = false;
                for (var i = 0; i < groupedHeats.length; i++) {
                    var race = groupedHeats[i].heats[heatNum];
                    if (race) {
                        race.number = service.schedule.length + 1;
                        service.schedule.push(race);
                        keepChecking = true;
                    }
                }
                heatNum += 1;
            }
        };

        function load() {
            return $http.get('http://localhost:51845/api/race')
                .then(function (result) {
                    if (result.data) {
                        angular.copy(result.data, service.schedule);
                    }
                });
        };

        function save(race) {
            return $http.put('http://localhost:51845/api/race', race);
        };

        function scheduleHeats(element) {
            //var heats = [];
            var groupHeats = {};
            groupHeats.name = element.Name;
            groupHeats.heats = [];
            var car = 0;
            for (var i = 0; i < element.Entrants.length; i++) {
                var heat = [];
                for (var ln = 0; ln < numLanes; ln++) {
                    if (element.Entrants.length % numLanes === 0) {
                        heat.push(createHeat(ln + 1, (car + i), element.Entrants));
                    } else {
                        heat.push(createHeat(ln + 1, car, element.Entrants));
                    }
                    car += 1;
                }
                groupHeats.heats.push({
                    number: 0,
                    groupName: element.Name,
                    laneAssignments: heat
                });
            }
            groupedHeats.push(groupHeats);
        };

        function setResults(currentRace, results) {
            for (var i = 0; i < currentRace.laneAssignments.length; i++) {
                currentRace.laneAssignments[i].Time = results[i].Time
            }
        };

        function startRace(event) {
            groupedHeats.length = 0;
            numLanes = event.Lanes;
            event.Groups.forEach(checkGroup);
            getSchedule();
        };
    };
})();