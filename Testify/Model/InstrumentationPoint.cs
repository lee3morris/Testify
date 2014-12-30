using System;
using System.Collections.Generic;
using System.Xml.Serialization;

namespace Leem.Testify.Model
{
    /// <summary>
    ///     An instrumentable point
    /// </summary>
    public class InstrumentationPoint
    {
        private static int _instrumentPoint;
        private static readonly List<InstrumentationPoint> InstrumentPoints;
        private List<TrackedMethodRef> _tracked;

        static InstrumentationPoint()
        {
            InstrumentPoints = new List<InstrumentationPoint> {null};
        }

        /// <summary>
        ///     Return the number of visit points
        /// </summary>
        public static int Count
        {
            get { return InstrumentPoints.Count; }
        }

        /// <summary>
        ///     Store the number of visits
        /// </summary>
        [XmlAttribute("vc")]
        public int VisitCount { get; set; }

        /// <summary>
        ///     A unique number
        /// </summary>
        [XmlAttribute("uspid")]
        public UInt32 UniqueSequencePoint { get; set; }

        /// <summary>
        ///     An order of the point within the method
        /// </summary>
        [XmlAttribute("ordinal")]
        public UInt32 Ordinal { get; set; }

        /// <summary>
        ///     The IL offset of the point
        /// </summary>
        [XmlAttribute("offset")]
        public int Offset { get; set; }

        /// <summary>
        ///     Used to hide an instrumentation point
        /// </summary>
        [XmlIgnore]
        public bool IsSkipped { get; set; }

        /// <summary>
        ///     The list of tracked methods
        /// </summary>
        public List<TrackedMethodRef> TrackedMethodRefs { get; set; }

        /// <summary>
        ///     Get the number of recorded visit points for this identifier
        /// </summary>
        /// <param name="spid">the sequence point identifier - NOTE 0 is not used</param>
        public static int GetVisitCount(uint spid)
        {
            return InstrumentPoints[(int) spid].VisitCount;
        }

        /// <summary>
        ///     Add a number of recorded visit pints against this identifier
        /// </summary>
        /// <param name="spid">the sequence point identifier - NOTE 0 is not used</param>
        /// <param name="trackedMethodId">the id of a tracked method - Note 0 means no method currently tracking</param>
        /// <param name="sum">the number of visit points to add</param>
        public static bool AddVisitCount(uint spid, uint trackedMethodId, int sum = 1)
        {
            if (spid != 0 && spid < InstrumentPoints.Count)
            {
                InstrumentationPoint point = InstrumentPoints[(int) spid];
                point.VisitCount += sum;
                if (trackedMethodId != 0)
                {
                    point._tracked = point._tracked ?? new List<TrackedMethodRef>();
                    TrackedMethodRef tracked = point._tracked.Find(x => x.UniqueId == trackedMethodId);
                    if (tracked == null)
                    {
                        tracked = new TrackedMethodRef {UniqueId = trackedMethodId, VisitCount = sum};
                        point._tracked.Add(tracked);
                    }
                    else
                    {
                        tracked.VisitCount += sum;
                    }
                }
                return true;
            }
            return false;
        }
    }
}