using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using TestCore.Configuration;
using TestCore.Data;
using ToucanCore.Configuration;

namespace ToucanCore.Engine
{
    ///// <summary>
    ///// Script, File which store the sequence and more metadata
    ///// </summary>
    //public interface IScript : ICloneable
    //{
    //    /// <summary>
    //    /// for persistence
    //    /// </summary>
    //    int Id { get; }

    //    /// <summary>
    //    /// Script Name
    //    /// </summary>
    //    string Name { get; }

    //    /// <summary>
    //    /// Script Version
    //    /// </summary>
    //    string Version { get; }

    //    /// <summary>
    //    /// Last Saving time
    //    /// </summary>
    //    DateTime Time { get; }
    //    /// <summary>
    //    /// Script Author
    //    /// </summary>
    //    string Author { get; set; }

    //    /// <summary>
    //    /// Comments of Script
    //    /// </summary>
    //    string Note { get; set; }

    //    /// <summary>
    //    /// the part no the script could test
    //    /// </summary>
    //    string PartNo { get; }

    //    /// <summary>
    //    /// for validation
    //    /// </summary>
    //    string CheckValue { get; }

    //    /// <summary>
    //    /// If the script file modified
    //    /// </summary>
    //    bool IsModified { get; }

    //    /// <summary>
    //    /// dependence library
    //    /// </summary>
    //    IReadOnlyList<string> ReferencesLib { get; }

    //    /// <summary>
    //    /// Workbase, not contain the last '\'
    //    /// </summary>
    //    string BaseDirectory { get; }

    //    /// <summary>
    //    /// File Path
    //    /// </summary>
    //    string FilePath { get; set; }

    //    GlobalConfiguration SystemConfig { get; set; }
    //    HardwareConfig HardwareConfig { get; set; }

    //    /// <summary>
    //    /// Current Effective Spec
    //    /// </summary>
    //    TF_Spec Spec { get; set; }

    //    /// <summary>
    //    /// Spec for Golden Sample Pick, default should be null
    //    /// </summary>
    //    TF_Spec GoldenSampleSpec { get; set; }

    //    IList<Variable> Variables { get; }

    //    InjectedVariableTable InjectedVariableTable { get; set; }

    //    int Open(string path);
    //    /// <summary>
    //    /// Only Analysis the spec of script, the injection such as Update limit / defect should be handle by engine.
    //    /// This will update the spec of script
    //    /// </summary>
    //    /// <returns></returns>
    //    int Analyze();

    //    /// <summary>
    //    /// Analyze Current Entrypoint, DO Not update the property Spec.
    //    /// </summary>
    //    /// <returns></returns>
    //    TF_Spec AnalyzeSpec();

    //    int Save(string path = null);

    //    IReadOnlyCollection<ISequence> Sequences { get; }
    //    ISequence this[string name] { get; }
    //    ISequence this[int index] { get; }

    //    ISequence ActiveSequence { get; }

    //    /// <summary>
    //    /// Lock/Unlock the script, for prevent modification
    //    /// </summary>
    //    /// <param name="lockstatus"></param>
    //    bool LockStatus { get; set; }

    //    /// <summary>
    //    /// Calibration for script.
    //    /// </summary>
    //    /// <returns></returns>
    //    int StartCalibration();

    //    /// <summary>
    //    /// Calibration for script. For the Calibration is a precondition of Execution, make the calibration in Script
    //    /// </summary>
    //    int ApplyCalibration();

    //    /// <summary>
    //    /// Calibration Expired. 
    //    /// </summary>
    //    event EventHandler CalibrationExpired;

    //    /// <summary>
    //    /// Calibration Expiring Warning
    //    /// </summary>
    //    event EventHandler CalibrationExpiring;

    //    /// <summary>
    //    /// Reference. For the Reference is a precondition of Execution, make the Reference in Script
    //    /// </summary>
    //    int ApplyReference();

    //    /// <summary>
    //    /// Reference Expired. 
    //    /// </summary>
    //    event EventHandler ReferenceExpired;

    //    /// <summary>
    //    /// Reference Expiring Warning
    //    /// </summary>
    //    event EventHandler ReferenceExpiring;

    //    /// <summary>
    //    /// Reference. For the Reference is a precondition of Execution, make the Reference in Script
    //    /// </summary>
    //    int ApplyVerification();

    //    /// <summary>
    //    /// Reference Expired. 
    //    /// </summary>
    //    event EventHandler VerificationExpired;

    //    /// <summary>
    //    /// Reference Expiring Warning
    //    /// </summary>
    //    event EventHandler VerificationExpiring;

    //    /// <summary>
    //    /// Get the base dir for Reference Data. For it should be check if ref data valid before start execution
    //    /// </summary>
    //    /// <returns></returns>
    //    string GetReferenceBase();

    //    /// <summary>
    //    /// Get the base dir for Calibration Data. For it should be check if calib data valid before start execution
    //    /// </summary>
    //    /// <returns></returns>
    //    string GetCalibrationBase();

    //    /// <summary>
    //    /// Get the base dir for Verification Data. For it should be check if calib data valid before start execution
    //    /// </summary>
    //    /// <returns></returns>
    //    string GetVerificationBase();

    //    IReadOnlyCollection<string> GoldenSamples { get; }
    //    void UpdateGoldenSamples(IEnumerable<string> samples);
    //}

    ///// <summary>
    ///// Model of excution. 
    ///// </summary>
    //public interface IModel : IScript
    //{ }


    //public enum ModelType
    //{
    //    /// <summary>
    //    /// No Model
    //    /// </summary>
    //    None,

    //    /// <summary>
    //    /// Use Original Model. Engine Do Nothing But Data collection
    //    /// </summary>
    //    OriginalModel,

    //    /// <summary>
    //    /// Normal test model
    //    /// </summary>
    //    Sequential,

    //    /// <summary>
    //    /// mutiple dut test in different independency slot
    //    /// </summary>
    //    Parallel,

    //    /// <summary>
    //    /// multiple dut test togother, which means it can not complete until all dut is finished test
    //    /// </summary>
    //    Batch,

    //    /// <summary>
    //    /// Pick m in n to test. specially, Pingpong mode is take 1 in 2 to test
    //    /// </summary>
    //    Token,
    //}
}
