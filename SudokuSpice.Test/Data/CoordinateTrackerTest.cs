﻿using System;
using System.Collections.Generic;
using Xunit;

namespace SudokuSpice
{
    public class CoordinateTrackerTest
    {
        [Fact]
        public void Add_UpToSize_Succeeds()
        {
            var sideLength = 3;
            var tracker = new CoordinateTracker(sideLength);
            var coords = _CreateCoordinateListForLength(sideLength);

            foreach (var c in coords)
            {
                tracker.Add(c);
            }

            var trackedCoords = tracker.GetTrackedCoords();
            Assert.Equal(coords.Count, trackedCoords.Length);
            for (int i = 0; i < coords.Count; i++)
            {
                Assert.Equal(coords[i], trackedCoords[i]);
            }
        }

        [Fact]
        public void Add_OverSize_Throws()
        {
            var sideLength = 3;
            var tracker = new CoordinateTracker(sideLength);
            var coords = _CreateCoordinateListForLength(sideLength + 1);

            Assert.Throws<IndexOutOfRangeException>(() =>
            {
                foreach (var c in coords)
                {
                    tracker.Add(c);
                }
            });
        }

        [Fact]
        public void Untrack_One_Succeeds()
        {
            var sideLength = 3;
            var tracker = new CoordinateTracker(sideLength);
            var coords = _CreateCoordinateListForLength(sideLength);
            foreach (var c in coords)
            {
                tracker.Add(c);
            }
            var untrackedCoord = new Coordinate(0, 1);

            tracker.Untrack(in untrackedCoord);

            var trackedCoords = tracker.GetTrackedCoords();
            Assert.Equal(coords.Count - 1, trackedCoords.Length);
            Assert.DoesNotContain(untrackedCoord, trackedCoords.ToArray());
        }

        [Fact]
        public void Untrack_All_Succeeds()
        {
            var sideLength = 3;
            var tracker = new CoordinateTracker(sideLength);
            var coords = _CreateCoordinateListForLength(sideLength);
            foreach (var c in coords)
            {
                tracker.Add(c);
            }

            foreach (var c in coords)
            {
                tracker.Untrack(in c);
            }

            var trackedCoords = tracker.GetTrackedCoords();
            Assert.Equal(0, trackedCoords.Length);
        }

        [Fact]
        public void Untrack_WhileEmpty_Succeeds()
        {
            var sideLength = 3;
            var tracker = new CoordinateTracker(sideLength);
            var coords = _CreateCoordinateListForLength(sideLength);
            foreach (var c in coords)
            {
                tracker.Add(c);
            }
            foreach (var c in coords)
            {
                tracker.Untrack(in c);
            }

            Assert.Throws<InvalidOperationException>(() => tracker.Untrack(coords[0]));

            var trackedCoords = tracker.GetTrackedCoords();
            Assert.Equal(0, trackedCoords.Length);
        }

        [Fact]
        public void Untrack_Untracked_Throws()
        {
            var sideLength = 3;
            var tracker = new CoordinateTracker(sideLength);
            var coords = _CreateCoordinateListForLength(sideLength);
            foreach (var c in coords)
            {
                tracker.Add(c);
            }
            var untrackedCoord = new Coordinate(0, 1);
            tracker.Untrack(in untrackedCoord);

            Assert.Throws<InvalidOperationException>(() => tracker.Untrack(in untrackedCoord));
        }

        [Fact]
        public void Track_One_Succeeds()
        {
            var sideLength = 3;
            var tracker = new CoordinateTracker(sideLength);
            var coords = _CreateCoordinateListForLength(sideLength);
            foreach (var c in coords)
            {
                tracker.Add(c);
            }
            var trackedCoord = new Coordinate(0, 1);

            tracker.Untrack(in trackedCoord);
            tracker.Track(in trackedCoord);

            var trackedCoords = tracker.GetTrackedCoords();
            Assert.Equal(coords.Count, trackedCoords.Length);
            Assert.Contains(trackedCoord, trackedCoords.ToArray());
        }

        [Fact]
        public void Track_WhileFull_Throws()
        {
            var sideLength = 3;
            var tracker = new CoordinateTracker(sideLength);
            var coords = _CreateCoordinateListForLength(sideLength);
            foreach (var c in coords)
            {
                tracker.Add(c);
            }
            var trackedCoord = new Coordinate(0, 1);

            Assert.Throws<InvalidOperationException>(() => tracker.Track(in trackedCoord));
        }

        [Fact]
        public void Track_Tracked_Throws()
        {
            var sideLength = 3;
            var tracker = new CoordinateTracker(sideLength);
            var coords = _CreateCoordinateListForLength(sideLength);
            foreach (var c in coords)
            {
                tracker.Add(c);
            }
            tracker.Untrack(coords[0]);
            Assert.Throws<InvalidOperationException>(() => tracker.Track(coords[1]));
        }

        [Fact]
        public void MixedTrackAndUntrack_Succeeds()
        {
            var sideLength = 3;
            var tracker = new CoordinateTracker(sideLength);
            var coords = _CreateCoordinateListForLength(sideLength);
            foreach (var c in coords)
            {
                tracker.Add(c);
            }
            int numFirstUntracked = 4;
            int numRetracked = 2;
            
            for (int i = 0; i < numFirstUntracked; i++)
            {
                tracker.Untrack(coords[i]);
            }
            for (int i = 0; i < numRetracked; i++)
            {
                tracker.Track(coords[i]);
            }

            var trackedCoords = tracker.GetTrackedCoords();
            Assert.Equal(7, trackedCoords.Length);
            Assert.Contains(coords[0], trackedCoords.ToArray());
            Assert.Contains(coords[1], trackedCoords.ToArray());
            Assert.DoesNotContain(coords[2], trackedCoords.ToArray());
            Assert.DoesNotContain(coords[3], trackedCoords.ToArray());
        }

        private IList<Coordinate> _CreateCoordinateListForLength(int sideLength)
        {
            var coordinates = new List<Coordinate>(sideLength * sideLength);
            for (int row = 0; row < sideLength; row++)
            {
                for (int col = 0; col < sideLength; col++)
                {
                    coordinates.Add(new Coordinate(row, col));
                }
            }
            return coordinates;
        }
    }
}