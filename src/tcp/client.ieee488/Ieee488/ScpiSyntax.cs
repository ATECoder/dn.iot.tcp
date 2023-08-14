namespace cc.isr.VI.Pith.Scpi
{
    /// <summary> includes the SCPI Commands. </summary>
    /// <remarks> (c) 2005 Integrated Scientific Resources, Inc. All rights reserved. <para>
    /// Licensed under The MIT License.</para><para>
    /// David, 2005-01-15, 1.0.1841.x. </para></remarks>
    public static class Syntax
    {

        #region " format constants "

        /// <summary> Gets the SCPI value for infinity. </summary>
        public const double Infinity = 9.9E+37d;

        /// <summary> Gets the SCPI caption for infinity. </summary>
        public const string InfinityCaption = "9.90000E+37";

        /// <summary> Gets the SCPI value for negative infinity. </summary>
        public const double NegativeInfinity = -9.91E+37d;

        /// <summary> Gets the SCPI caption for negative infinity. </summary>
        public const string NegativeInfinityCaption = "-9.91000E+37";

        /// <summary> Gets the SCPI value for 'non-a-number' (NAN). </summary>
        public const double NotANumber = 9.91E+37d;

        /// <summary> Gets the SCPI caption for 'not-a-number' (NAN). </summary>
        public const string NotANumberCaption = "9.91000E+37";

        #endregion

        #region " default error messages "

        /// <summary> Gets the error message representing no error. </summary>
        public const string NoErrorMessage = "No Error";

        /// <summary> Gets the compound error message representing no error. </summary>
        public const string NoErrorCompoundMessage = "0,No Error";

        #endregion

        #region " status "

        /// <summary> Gets the 'Next Error' query command. </summary>
        public const string NextErrorQueryCommand = ":STAT:QUE?";

        /// <summary> Gets the error queue clear command. </summary>
        public const string ClearErrorQueueCommand = ":STAT:QUE:CLEAR";

        /// <summary> Gets the preset status command. </summary>
        public const string StatusPresetCommand = ":STAT:PRES";

        /// <summary> Gets the measurement event condition command. </summary>
        public const string MeasurementEventConditionQueryCommand = ":STAT:MEAS:COND?";

        /// <summary> Gets the measurement event status query command. </summary>
        public const string MeasurementEventQueryCommand = ":STAT:MEAS:EVEN?";

        /// <summary> Gets the Measurement event enable Query command. </summary>
        public const string MeasurementEventEnableQueryCommand = ":STAT:MEAS:ENAB?";

        /// <summary> Gets the Measurement event enable command format. </summary>
        public const string MeasurementEventEnableCommandFormat = ":STAT:MEAS:ENAB {0:D}";

        /// <summary> Gets the Measurement event Positive Transition Query command. </summary>
        public const string MeasurementEventPositiveTransitionQueryCommand = ":STAT:MEAS:PTR?";

        /// <summary> Gets the Measurement event Positive Transition command format. </summary>
        public const string MeasurementEventPositiveTransitionCommandFormat = ":STAT:MEAS:PTR {0:D}";

        /// <summary> Gets the Measurement event Negative Transition Query command. </summary>
        public const string MeasurementEventNegativeTransitionQueryCommand = ":STAT:MEAS:NTR?";

        /// <summary> Gets the Measurement event Negative Transition command format. </summary>
        public const string MeasurementEventNegativeTransitionCommandFormat = ":STAT:MEAS:NTR {0:D}";

        /// <summary> Gets the measurement event condition command. </summary>
        public const string OperationEventConditionQueryCommand = ":STAT:OPER:COND?";

        /// <summary> Gets the operation event enable command format. </summary>
        public const string OperationEventEnableCommandFormat = ":STAT:OPER:ENAB {0:D}";

        /// <summary> Gets the operation event enable Query command. </summary>
        public const string OperationEventEnableQueryCommand = ":STAT:OPER:ENAB?";

        /// <summary> Gets the operation register event status query command. </summary>
        public const string OperationEventQueryCommand = ":STAT:OPER:EVEN?";

        /// <summary> Gets the operation event map command format. </summary>
        public const string OperationEventMapCommandFormat = ":STAT:OPER:MAP {0:D},{1:D},{2:D}";

        /// <summary> Gets the operation map query command format. </summary>
        public const string OperationEventMapQueryCommandFormat = ":STAT:OPER:MAP? {0:D}";

        /// <summary> Gets the measurement event condition command. </summary>
        public const string QuestionableEventConditionQueryCommand = ":STAT:QUES:COND?";

        /// <summary> Gets the Questionable event enable command format. </summary>
        public const string QuestionableEventEnableCommandFormat = ":STAT:QUES:ENAB {0:D}";

        /// <summary> Gets the Questionable event enable Query command. </summary>
        public const string QuestionableEventEnableQueryCommand = ":STAT:QUES:ENAB?";

        /// <summary> Gets the Questionable register event status query command. </summary>
        public const string QuestionableEventQueryCommand = ":STAT:QUES:EVEN?";

        /// <summary> Gets the Questionable event map command format. </summary>
        public const string QuestionableEventMapCommandFormat = ":STAT:QUES:MAP {0:D},{1:D},{2:D}";

        /// <summary> Gets the Questionable map query command format. </summary>
        public const string QuestionableEventMapQueryCommandFormat = ":STAT:QUES:MAP? {0:D}";


        #endregion

        #region " system "

        /// <summary> Gets the last system error queue query command. </summary>
        public const string LastSystemErrorQueryCommand = ":SYST:ERR?";

        /// <summary> Gets clear system error queue command. </summary>
        public const string ClearSystemErrorQueueCommand = ":SYST:CLE";

        /// <summary> The read line frequency command. </summary>
        public const string ReadLineFrequencyCommand = ":SYST:LFR?";

        /// <summary> The initialize memory command. </summary>
        public const string InitializeMemoryCommand = ":SYST:MEM:INIT";

        /// <summary> The preset command. </summary>
        public const string SystemPresetCommand = ":SYST:PRES";

        /// <summary> The language (SCPI) revision query command. </summary>
        public const string LanguageRevisionQueryCommand = ":SYST:VERS?";

        #endregion

    }
}
