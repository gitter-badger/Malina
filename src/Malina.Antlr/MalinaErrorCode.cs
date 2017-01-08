﻿using System;

namespace Malina.Parser
{
    [Serializable]
    public enum MalinaErrorCode
    {
        [Message("")] NoError = 0,

        //Lexer Errors
        [Message("Missing closing Double Quote.")] ClosingDqMissing,
        [Message("Missing closing Single Quote.")] ClosingSqMissing,
        [Message("Incorrect usage of colon.")] IncorrectColon,
        [Message("Unexpected symbol.")] LexerNoViableAltException,

        //Parser Errors
        [Message("NoViableAltParserException")] NoViableAltParserException,
        
    }

    //[Serializable]
    //public class MalinaError
    //{
    //    private MalinaErrorCode _code;
    //    private SourceLocation _start;
    //    private SourceLocation _stop;

    //    public SourceLocation Start
    //    {
    //        get
    //        {
    //            return _start;
    //        }
    //        set { _start = value; }
    //    }

    //    public MalinaErrorCode Code
    //    {
    //        get
    //        {
    //            return _code;
    //        }
    //        set { _code = value; }
    //    }

    //    public SourceLocation Stop
    //    {
    //        get
    //        {
    //            return _stop;
    //        }
    //        set { _stop = value; }
    //    }

    //    //public RecognitionException Exception
    //    //{
    //    //    get
    //    //    {
    //    //        return _exception;
    //    //    }

    //    //    set
    //    //    {
    //    //        _exception = value;
    //    //    }
    //    //}

    //    public MalinaError() { }
    //    public MalinaError(MalinaErrorCode code, SourceLocation start, SourceLocation stop)
    //    {
    //        _code = code;
    //        _start = start;
    //        _stop = stop;
    //    }
    //}
}


