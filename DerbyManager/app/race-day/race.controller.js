
(function () {
    'use strict';

    angular.module('pwderby')
      .controller('raceCtrl', raceCtrl);

    raceCtrl.$inject = ['EventService', 'race.service', 'track.monitor.service', '$rootScope', '$scope'];
    function raceCtrl(events, race, track, $rootScope, $scope) {
        var main = this;
        main.calculateScaleSpeed = calculateScaleSpeed;
        main.event = {};
        main.next = nextRace;
        main.previous = prevRace;
        main.raceIndex = 0;
        main.schedule = race.schedule;
        main.currentRace = {};
        main.nextRace = null;
        main.showTrackInfo = false;
        main.start = start;
        main.track = track;

        function init() {
            $rootScope.$on('race-results-event', onResults);
            track.start();

            events.get().then(function (response) {
                angular.copy(response.data, main.event);
            });

            race.load()
                .then(function () {
                    if (main.schedule.length > 0) {
                        main.currentRace = main.schedule[0];
                        main.nextRace = main.schedule[1];
                    }
                });
        };

        function calculateScaleSpeed(lane) {
            lane.ScaleSpeed = (((3600 / lane.Time) * main.event.TrackLength) / 5280) * 25;
        };

        function nextRace() {
            race.save(main.schedule);
            if (main.raceIndex === (main.schedule.length - 1)) { return; }
            main.raceIndex += 1;
            main.currentRace = main.schedule[main.raceIndex];
            setNextRace();
        };

        function onResults(event, data) {
            for (var i = 0; i < main.currentRace.laneAssignments.length; i++) {
                main.currentRace.laneAssignments[i].Place = data[i].Place;
                main.currentRace.laneAssignments[i].Time = data[i].Time
                calculateScaleSpeed(main.currentRace.laneAssignments[i]);
            }

            $scope.$apply();
            race.save(main.schedule);
        };

        function prevRace() {
            race.save(main.schedule);
            if (main.raceIndex === 0) { return; }
            main.raceIndex -= 1;
            main.currentRace = main.schedule[main.raceIndex];
            setNextRace();
        };

        function setNextRace() {
            if (main.raceIndex + 1 <= (main.schedule.length - 1)) {
                main.nextRace = main.schedule[main.raceIndex + 1];
            } else {
                main.nextRace = null;
            }
        }

        function start() {
            race.start(main.event);
            main.currentRace = main.schedule[main.raceIndex];
            setNextRace();
        };

        init();
    };
})();