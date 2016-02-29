/*******************************************************************************
Copyright 2010 Ben Mechling

This file is part of Fast Track Timer Library.

Fast Track Timer Library is free software: you can redistribute it and/or modify
it under the terms of the Lesser GNU General Public License as published by the 
Free Software Foundation, either version 3 of the License, or (at your option) 
any later version.

Fast Track Timer Library is distributed in the hope that it will be useful, but 
WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or 
FITNESS FOR A PARTICULAR PURPOSE.  See the GNU General Public License for more 
details.

You should have received a copy of the GNU General Public License along with 
Fast Track Timer Library.  If not, see <http://www.gnu.org/licenses/>.
*******************************************************************************/

// Default namespaces used 
using System;

namespace FastTrack
{
    /// <summary>
    /// The rece results class is designed to be a convenient container passing 
    /// results to the user. It also takes care of cleaning up the raw results
    /// from the device response based on the specified. 
    /// </summary>
    public class RaceResult
    {
        // Properties and Attributes
        //**********************************************************************

        /// <summary>
        /// The maximum number of lanes in the response.
        /// </summary>
        public static int MaxLogicalLaneCount
        {
            get { return RaceResultsResponse.MaxLogicalLaneCount; }
        }

        /// <summary>
        /// Results for each lane.
        /// </summary>
        private LaneResult[] laneResults;
        /// <summary>
        /// Results for each lane.
        /// </summary>
        public LaneResult[] LaneResults
        {
            get { return laneResults; }
            set { laneResults = value; }
        }

        /// <summary>
        /// Indicates whether the results have been ajusted with offsets for the
        /// racers following a tie.
        /// </summary>
        private bool offsetResultsForTies;
        /// <summary>
        /// Indicates whether the results have been ajusted with offsets for the
        /// racers following a tie.
        /// </summary>
        public bool OffsetResultsForTies
        {
            get { return offsetResultsForTies; }
            set { offsetResultsForTies = value; }
        }

        /// <summary>
        /// Indicates whether the results have been interpreted using the 
        /// eliminator mode.
        /// </summary>
        private bool useEliminatorMode;
        /// <summary>
        /// Indicates whether the results have been interpreted using the 
        /// eliminator mode.
        /// </summary>
        public bool UseEliminatorMode
        {
            get { return useEliminatorMode; }
            set { useEliminatorMode = value; }
        }

        // Constructors
        //**********************************************************************
        
        /// <summary>
        /// Initializes a new RaceResult object with the interpreted results of 
        /// a RaceResultsResponse.
        /// </summary>
        /// <param name="response">Response object with data set.</param>
        /// <param name="laneMask">
        /// Array of values indicating if the corresponding lane is masked.
        /// </param>
        public RaceResult(RaceResultsResponse response, bool[] laneMask) : 
            this(response, laneMask, true, false) { }

        /// <summary>
        /// Initializes a new RaceResult object with the interpreted results of 
        /// a RaceResultsResponse.
        /// </summary>
        /// <param name="response">Response object with data set.</param>
        /// <param name="laneMask">
        /// Array of values indicating if the corresponding lane is masked.
        /// </param>
        /// <param name="offsetResultsForTies">
        /// Interpret the resulst using the offset results for ties option.
        /// </param>
        /// <param name="useEliminatorMode">
        /// Interpret the results using eliminator mode.
        /// </param>
        public RaceResult(RaceResultsResponse response, bool[] laneMask, 
            bool offsetResultsForTies, bool useEliminatorMode)
        {
            this.laneResults = new LaneResult[MaxLogicalLaneCount];

            if (response == null)
            {
                response = new RaceResultsResponse();
            }

            bool mask = false;
            // Set initial mask values, times, and places.
            for (int i = 0; i < laneResults.Length; i++)
            {
                if (i < laneMask.Length &&
                    !(response.Response_GetPlace((Lanes)i) !=
                    FinishingPlaces.NoPlace
                    || response.Response_GetTime((Lanes)i) > 0)
                    && laneMask[i])
                {
                    mask = true;
                }
                else
                {
                    mask = false;
                }

                laneResults[i] = new LaneResult(
                    response.Response_GetPlace((Lanes)i),
                    response.Response_GetTime((Lanes)i),
                    mask);
            }
            // Set options
            this.offsetResultsForTies = offsetResultsForTies;
            this.useEliminatorMode = useEliminatorMode;
            // interpret the results based on the options.
            this.AdustPlaces();
        }

        /// <summary>
        /// This method offsets the results for ties if necessary and fills in 
        /// any gaps in any places missing due to the response format.
        /// </summary>
#if TEST
        // In order to call this method directly from test code, it is public.
        public void AdustPlaces()
#else
        private void AdjustPlaces()
#endif
        {
            /// Eliminator Mode:
            /// Off: One loop over all six (or less) lanes.
            /// On: Run sort and assign process over two at a time for as many
            ///     times as necessary (1 - 3) to cover all of the lanes. 
            ///     An odd number will result in a lane evaluating by itself.

            /// Offset for Ties:
            /// On:
            ///     Place will increment for every lane processed
            ///     However, same times => same place
            /// Off: 
            ///     Place will increment for each different time.
            ///     same time => same place

            int start = 0;
            int length = laneResults.Length;

            if (this.useEliminatorMode)
            {
                length = 2;
            }

            int[] sortIndex;
            int rank = 0;

            do
            {
                // sort from 'start' for 'length'
                sortIndex = new int[length];
                for (int lane = start; lane < start + length && 
                    lane < laneResults.Length; lane++)
                {
                    rank = 0;

                    // find insert location
                    while (rank < lane - start && (
                        laneResults[lane].Time == 0 
                        || 
                        (laneResults[lane].Time > 
                        laneResults[sortIndex[rank]].Time)
                        && 
                        (laneResults[sortIndex[rank]].Time != 0)
                        ||
                        (laneResults[lane].Time == 
                        laneResults[sortIndex[rank]].Time)
                        && 
                        ((int)laneResults[lane].Place >= 
                        (int)laneResults[sortIndex[rank]].Place)
                        ))
                    {
                        rank++;
                    }

                    // shift lower values down
                    for (int bottom = lane - start; bottom > rank; bottom--)
                    {
                        sortIndex[bottom] = sortIndex[bottom - 1];
                    }

                    // insert value
                    sortIndex[rank] = lane;
                }
                
                // Iterate over the sorted list and assign place values
                if (laneResults[sortIndex[0]].Time <= 0)
                {
                    // If the fastest car did not place (time = 0) then 
                    // not of the 'slower' cars placed either.
                    for (int i = start; i < start + length && 
                        i < laneResults.Length; i++)
                    {
                        laneResults[sortIndex[i - start]].Place = FinishingPlaces.NoPlace;
                    }
                }
                else
                { // at least one racer exists
                    int[] newPlace = new int[length];
                    newPlace[0] = 1;

                    int nextPlace = 2;
                    int count = 1;

                    do
                    {
                        // time = 0, thus no place
                        if (laneResults[sortIndex[count]].Time <= 0)
                        { 
                            newPlace[count] = (int)FinishingPlaces.NoPlace;
                        }
                        // time is greater than previous
                        else if (laneResults[sortIndex[count]].Time > 
                            laneResults[sortIndex[count - 1]].Time)
                        {
                            newPlace[count] = nextPlace;
                            nextPlace++;
                        }
                        // times are equal
                        else /* if (laneResults[sortIndex[count]].Time ==
                            laneResults[sortIndex[count - 1]].Time)*/
                        {
                            /// The times may be equal and the cars not be tied.
                            /// If the times are maxed out (9.999) and the new 
                            /// format is used, then the result will include 
                            /// place indicators that may destinguish places. 
                            if ((int)laneResults[sortIndex[count]].Place >
                                (int)laneResults[sortIndex[count - 1]].Place)
                            {
                                /// The places are sorted, so the current place 
                                /// should only be greater than or equal to the 
                                /// previous.
                                newPlace[count] = nextPlace;
                                nextPlace++;
                            }
                            else // The places are also the same. It is a tie.
                            {
                                newPlace[count] = newPlace[count - 1];
                                if (offsetResultsForTies)
                                {
                                    nextPlace++;
                                }
                            }
                        }
                        count++;
                    } while (count < length);

                    // copy new places in to the list of places
                    for (int i = 0; i < length; i++)
                    {
                        laneResults[sortIndex[i]].Place = 
                            (FinishingPlaces)newPlace[i];
                    }
                }
                start += 2;
            } while (this.useEliminatorMode && start < laneResults.Length);
        }
    }

    /// <summary>
    /// Struct desiged as a convenient container for the time, place, and mask
    /// value for a single lane of a set of results.
    /// </summary>
    public struct LaneResult
    {
        // Attributes and Properties
        //**********************************************************************

        /// <summary>
        /// The finishing place received by the racer in this lane.
        /// </summary>
        private FinishingPlaces place;
        /// <summary>
        /// The finishing place received by the racer in this lane.
        /// </summary>
        public FinishingPlaces Place
        {
            get { return place; }
            set { place = value; }
        }

        /// <summary>
        /// The finishing timer recorded for the racer in this lane. 
        /// </summary>
        private double time;
        /// <summary>
        /// The finishing timer recorded for the racer in this lane. 
        /// </summary>
        public double Time
        {
            get { return time; }
            set { time = value; }
        }

        /// <summary>
        /// Boolean value indicating whether the lane was masked when the race 
        /// was run. 
        /// </summary>
        private bool wasMasked;
        /// <summary>
        /// Boolean value indicating whether the lane was masked when the race 
        /// was run. 
        /// </summary>
        public bool WasMasked
        {
            get { return wasMasked; }
            set { wasMasked = value; }
        }

        // Constructor
        //**********************************************************************

        /// <summary>
        /// Initializes a new stuct instance.
        /// </summary>
        /// <param name="place">Finishing place for this lane.</param>
        /// <param name="time">Finishing time for this lane.</param>
        /// <param name="wasMasked">Indicates that the lane was masked.</param>
        public LaneResult(FinishingPlaces place, double time, bool wasMasked)
        {
            this.place = place;
            this.time = time;
            this.wasMasked = wasMasked;
        }

        // Methods
        //**********************************************************************
        // This method is mostly provided for debugging.
        public override string ToString()
        {
            return string.Format("P:{0} T:{1} M:{3}",
                (int)this.place,
                this.time,
                (this.wasMasked ? "T" : "F"));
        }
    }
}
