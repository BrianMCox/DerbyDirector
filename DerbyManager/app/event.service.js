
(function () {
    'use strict';

    angular.module('pwderby')
      .factory('event.service', eventService);

    eventService.$inject = [];
    function eventService() {
        var service = {};
        service.current = {
            "lanes": 3,
            "groups": [
              {
                  "name": "Pack 476",
                  "divisions": [
                    {
                        "name": "Wolves",
                        "awards": 3,
                        "entrants": [
                            {
                                "name": "Charlie Cox",
                                "carNumber": 2
                            },
                            {
                                "name": "David Grube",
                                "carNumber": 5
                            },
                            {
                                "name": "Jackson Davis",
                                "carNumber": 3
                            }
                            ,
                            {
                                "name": "Chaz Schnell",
                                "carNumber": 6
                            }
                        ]
                    },
                    {
                        "name": "Tigers",
                        "awards": 3,
                        "entrants": [
                            {
                                "name": "Caden Beaucham",
                                "carNumber": 8
                            },
                            {
                                "name": "Ethan Bomar",
                                "carNumber": 1
                            },
                            {
                                "name": "Christian Steinau",
                                "carNumber": 7
                            },
                            {
                                "name": "Jake Kamp",
                                "carNumber": 4
                            },
                            {
                                "name": "Julian Thomas",
                                "carNumber": 9
                            }
                        ]
                    }
                  ],
                  "awards": 0
              },
              {
                  "name": "Family",
                  "divisions": [],
                  "awards": "3",
                  "entrants": [
                    {
                        "name": "Aidan Cox",
                        "carNumber": 10
                    },
                    {
                        "name": "Jacob Grube",
                        "carNumber": 11
                    },
                    {
                        "name": "Angela Grube",
                        "carNumber": 12
                    },
                    {
                        "name": "Brian Cox",
                        "carNumber": 13
                    },
                    {
                        "name": "Chris Kamp",
                        "carNumber": 14
                    }
                  ]
              }
            ],
            "name": "2016 Pinewood Derby"
        };

        return service;
    };
})();