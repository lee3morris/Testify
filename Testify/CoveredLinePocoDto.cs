﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Leem.Testify
{
    class CoveredLinePocoDto
    {
        public int Module_CodeModuleId { get; set; }
        public int Class_CodeClassId { get; set; }
        public int Method_CodeMethodId { get; set; }

        public int LineNumber { get; set; }
        public bool IsCode { get; set; }
        public bool IsCovered { get; set; }
        public int CoveredLineId { get; set; }
        public bool IsSuccessful { get; set; }
        public int UnitTestId { get; set; }
        public string FileName { get; set; }
    }
}