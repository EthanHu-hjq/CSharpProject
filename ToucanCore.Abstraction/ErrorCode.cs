using System;
using System.Collections.Generic;
using System.Text;

namespace ToucanCore.Abstraction
{
    public enum ErrorCode
    {
        InvalidOperation = -1,
        AccessDenied = -2,

        MesError = -23,

        ToolboxServiceError = -100,
        AuthServiceError = -200,
        ReportServiceError = -300,


        StaticAnalysisError = -1000,
        DynamicAnalysisError = -2000,

        ExecutionOperationError = -3000,

        StartTriggerError = -5000,
        FixtureTriggerError = -6000,
    }

    public class CalibrationDataExpiringWarning : Exception
    {
        public CalibrationDataExpiringWarning() : base()
        { }

        public CalibrationDataExpiringWarning(string message) : base(message) { }
        public string CalibrationDataPath { get; set; }
        public double RemainedHours { get; set; }
    }

    public class CalibrationDataExpiredException : Exception
    {
        public CalibrationDataExpiredException() : base()
        { }

        public CalibrationDataExpiredException(string message) : base(message) { }
        public string CalibrationDataPath { get; set; }
    }

    public class ReferenceDataExpiringWarning : Exception
    {
        public ReferenceDataExpiringWarning() : base()
        { }

        public ReferenceDataExpiringWarning(string message) : base(message) { }
        public string ReferenceDataPath { get; set; }
        public double RemainedHours { get; set; }
    }

    public class ReferenceDataExpiredException : Exception
    {
        public ReferenceDataExpiredException() : base()
        { }

        public ReferenceDataExpiredException(string message) : base(message) { }

        public string ReferenceDataPath { get; set; }
    }

    public class VerificationDataExpiredException : Exception
    {
        public VerificationDataExpiredException() : base()
        { }

        public VerificationDataExpiredException(string message) : base(message) { }

        public string VerificationDataPath { get; set; }
    }

    public class VerificationDataExpiringWarning : Exception
    {
        public VerificationDataExpiringWarning() : base()
        { }

        public VerificationDataExpiringWarning(string message) : base(message) { }
        public string VerificationDataPath { get; set; }
        public double RemainedHours { get; set; }
    }
}
