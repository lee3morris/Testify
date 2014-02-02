﻿//
// OpenCover - S Wilde
//
// This source code is released under the MIT License; see the accompanying license file.
//
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;

namespace Leem.Testify.Domain.Model
{
    /// <summary>
    /// An entity that contains methods
    /// </summary>
    public class Class : SkippedEntity
    {
        public Class()
        {
            Methods = new List<Method>();
            Summary = new Summary();
        }

        /// <summary>
        /// A Summary of results for a class
        /// </summary>
        public Summary Summary { get; set; }

        /// <summary>
        /// Control serialization of the Summary  object
        /// </summary>
        /// <returns></returns>
        public bool ShouldSerializeSummary() { return !ShouldSerializeSkippedDueTo(); }

        /// <summary>
        /// The full name of the class
        /// </summary>
        public string FullName { get; set; }
        
        [XmlIgnore]
        internal List<File> Files { get; set; }

        /// <summary>
        /// A list of methods that make up the class
        /// </summary>
        public List<Method> Methods { get; set; }

        public override void MarkAsSkipped(SkippedMethod reason)
        {
            SkippedDueTo = reason;
        }
    }
}
