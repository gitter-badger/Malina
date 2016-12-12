﻿using Antlr4.Runtime;
using System;
using System.Collections.Generic;

namespace Malina.Parser
{

    public class MalinaToken: CommonToken
    {
        public int TokenIndent;
        public int StopLine;
        public int StopColumn;

        public MalinaToken(int type) : base(type)
        {

        }

        public MalinaToken(Tuple<ITokenSource, ICharStream> source, int type, int channel, int start, int stop):base(type)
        {
            if (source.Item1 != null)
            {
                line = source.Item1.Line;
                charPositionInLine = source.Item1.Column;
            }
            this.source = source;
            this.channel = channel;
            this.start = start;
            this.stop = stop;
        }
    }

    public class InterpolationStringToken : MalinaToken
    {
        private List<CommonToken> _interpolationTokens;
        public InterpolationStringToken(int type) : base(type)
        {
        }

        public InterpolationStringToken(Tuple<ITokenSource, ICharStream> source, int type, int channel, int start, int stop) : base(source, type, channel, start, stop)
        {
        }

        public List<CommonToken> InterpolationTokens => _interpolationTokens??(_interpolationTokens = new List<CommonToken>());
    }
    partial class MalinaLexer
    {
        public List<IToken> InvalidTokens = new List<IToken>();
        private Stack<int> _indents = new Stack<int>(new[] { 0 });
        private Queue<IToken> _tokens = new Queue<IToken>();
        private Stack<int> _wsaStack = new Stack<int>();
        private MalinaToken _currentToken; //This field is used for tokens created with several lexer rules.
        private MalinaToken _interpolationToken; //This field is used to build interpolation tokens
        private int _recordedIndex;


        private bool InWsaMode => _wsaStack.Count > 0;

        public override void Reset()
        {
            InvalidTokens = new List<IToken>();
            _indents = new Stack<int>(new[] { 0 });
            _tokens = new Queue<IToken>();
            _wsaStack = new Stack<int>();            
            _currentToken = null;
            base.Reset();
        }
        public override void Emit(IToken token)
        {
            if (_token == null)
            {
                base.Emit(token);
            }
            else
            {
                _tokens.Enqueue(token);
            }
        }

        public override IToken NextToken()
        {            
            //Return previously generated tokens first
            if (_tokens.Count > 0)
            {
                return _tokens.Dequeue();
            }
            
            Token = null;
            //Checking if EOF and still mode = IN_VALUE then we need to generate OPEN_VALUE
            //Scenario: Open value ends with EOF
            if (_input.La(1) == Eof && _mode == 1)
            {
                if (_currentToken == null)//Empty Open String
                {
                    //Creating Token for Empty OpenString
                    _currentToken =
                        new MalinaToken(new Tuple<ITokenSource, ICharStream>(this, (this as ITokenSource).InputStream),
                            OPEN_VALUE, Channel, -1, -1) {Text = ""};
                    Emit(_currentToken);
                }                
                PopMode();
            }
            
            if (_tokens.Count > 0) {
                //Return generated tokens
                return _tokens.Dequeue();
            }
            
            //Run regular path if there no extra tokens generated in queue
            return base.NextToken();
        }

        public override void Recover(LexerNoViableAltException e)
        {
            //Lexer recover strategy - ignore all till next space, tab or EOL
            while (_input.La(1) != -1 && _input.La(1) != ' ' && _input.La(1) != '\n' && _input.La(1) != '\r' && _input.La(1) != '\t')
            {
                Interpreter.Consume(_input);
            }
        }

        private void IndentDedent()
        {
            //Lexer reached EOF. 
            if (_input.La(1) == Eof)
            {
                Skip();
                return;
            }

            //Lexer hasn't reached EOF
            var indent = CalcIndent();
            var prevIndent = InWsaMode ? 0 : _indents.Peek();
            if (indent == prevIndent)
            {
                //Emitting NEWLINE
                //Scenario: Any node in the end of line and not EOF.
                if (!InWsaMode)
                {
                    EmitIndentationToken(NEWLINE, CharIndex - indent - 1, CharIndex - indent - 1);
                }
                else Skip();
            }
            else if (indent > prevIndent)
            {
                //Emitting INDENT
                _indents.Push(indent);
                EmitIndentationToken(INDENT, CharIndex - indent, CharIndex - 1);
            }
            else
            {
                //Adding 1 NEWLINE before DEDENTS
                //Scenario: Any node in the end of line and not EOF.
                if (_indents.Count > 1 && _indents.Peek() > indent)
                {
                    EmitIndentationToken(NEWLINE, CharIndex - indent - 1, CharIndex - indent - 1);
                }
                //Emitting 1 or more DEDENTS
                while (_indents.Count > 1 && _indents.Peek() > indent)
                {
                    EmitIndentationToken(DEDENT, CharIndex - indent, CharIndex - 1);
                    _indents.Pop();
                }
            }
        }

        private void RecordCharIndex()
        {
            _recordedIndex = CharIndex;
        }

        private int CalcIndent()
        {
            if (InWsaMode) return 0;

            return CharIndex - _recordedIndex;

        }

        //Calculates Indent for Multiline Open String
        private int CalcOsIndent()
        {
            if (InWsaMode) return 0;

            var i = -1;
            var osIndent = 0;
            int la = InputStream.La(i);
            while (la != '\n' && la != -1 && la != '\r')
            {
                if (la != '=')
                    osIndent++;
                la = InputStream.La(--i);
            }
            return osIndent;
        }


        private void StartNewMultiLineToken()
        {
            _currentToken = null;
        }

        //Open String Indents/Dedents processing
        private void OsIndentDedent()
        {
            var _currentIndent = InWsaMode ? 0 : _indents.Peek();
            var indent = CalcOsIndent();
            if (indent == _currentIndent)
            {
                //Emitting NEWLINE if OS is not ended by ==
                if (_input.La(-1) != '=')
                {
                    if (_currentToken == null)//Empty Open String
                    {
                        //Creating Token for Empty OpenString
                        _currentToken =
                            new MalinaToken(
                                new Tuple<ITokenSource, ICharStream>(this, (this as ITokenSource).InputStream),
                                OPEN_VALUE, Channel, -1, -1) {Text = ""};
                    }

                    Emit(_currentToken);
                    if (!InWsaMode)
                        EmitIndentationToken(NEWLINE, CharIndex - indent - 1, CharIndex - indent - 1);
                }
                else
                {
                    if (_currentToken == null)
                    {
                        //Creating Token 
                        _currentToken = new MalinaToken(new Tuple<ITokenSource, ICharStream>(this, (this as ITokenSource).InputStream), OPEN_VALUE, Channel, _tokenStartCharIndex, -1);
                    }
                    //if value was ended with ==  then we need to add \n                                     
                    _currentToken.StopIndex = CharIndex - Column - 1;
                    _currentToken.StopLine = this._tokenStartLine;
                    _currentToken.Type = OPEN_VALUE_ML;
                    _currentToken.StopColumn++;
                    Emit(_currentToken);
                    //EmitIndentationToken(NEWLINE, CharIndex - indent - 1, CharIndex - indent - 1);
                }
                PopMode(); 
            }
            else if (indent > _currentIndent)
            {
                if (_currentToken == null)
                {
                    //If Open String starts with empty string then ignore first line
                    var offset = 0;
                    if (InputStream.La(_tokenStartCharIndex - CharIndex) == '\r')
                    {
                        offset++;
                        if (InputStream.La(_tokenStartCharIndex - CharIndex + 1) == '\n') offset++;
                    }
                    else if (InputStream.La(_tokenStartCharIndex - CharIndex) == '\n') offset++;

                    if (offset > 0) offset += _indents.Peek() + 1;

                    _currentToken =
                        new MalinaToken(new Tuple<ITokenSource, ICharStream>(this, (this as ITokenSource).InputStream),
                            OPEN_VALUE, Channel, this._tokenStartCharIndex + offset, -1)
                        {
                            TokenIndent = _indents.Peek() + 1
                        };
                }


                if (_input.La(-1) == '=')//If indent ends with == then include it
                {
                    _currentToken.StopIndex = this.CharIndex - 1;
                    _currentToken.StopLine = Line;
                    _currentToken.StopColumn = Column - 1;
                }
                _currentToken.Type = OPEN_VALUE_ML;
                Skip();
            }
            else
            {
                if (_currentToken == null)//Empty Open String Scenario
                {
                    //Creating Token for Empty OpenString
                    _currentToken =
                        new MalinaToken(new Tuple<ITokenSource, ICharStream>(this, (this as ITokenSource).InputStream),
                            OPEN_VALUE, Channel, -1, -1) {Text = ""};
                }

                //Adding 1 NEWLINE before DEDENTS
                //_currentToken.StopIndex = CharIndex - Column;
                //_currentToken.StopLine = this._tokenStartLine;
                //_currentToken.StopColumn++;
                Emit(_currentToken);
                EmitIndentationToken(NEWLINE, CharIndex - indent - 1, CharIndex - indent - 1);

                //Emitting 1 or more DEDENTS
                while (_indents.Count > 1 && _indents.Peek() > indent)
                {
                    EmitIndentationToken(DEDENT, CharIndex - indent, CharIndex - 1);
                    _indents.Pop();
                }
                PopMode();
            }
        }

        private void EmitIndentationToken(int tokenType, int start, int stop)
        {
                Emit(new CommonToken(new Tuple<ITokenSource, ICharStream>(this, (this as ITokenSource).InputStream), tokenType, Channel, start, stop) { Text =  _SymbolicNames[tokenType]});
        }

        private void Emit(int tokenType)
        {
            Emit(new CommonToken(new Tuple<ITokenSource, ICharStream>(this, (this as ITokenSource).InputStream), tokenType, Channel, _tokenStartCharIndex, InputStream.Index - 1));
        }

        private void EnterWsa()
        {
            _wsaStack.Push(_tokenStartCharIndex);
        }

        private void ExitWsa()
        {
            if (_wsaStack.Count > 0)
                _wsaStack.Pop();
        }

        private void StartDqsMl()
        {
            _currentToken =
                new MalinaToken(new Tuple<ITokenSource, ICharStream>(this, (this as ITokenSource).InputStream), DQS,
                    Channel,
                    _tokenStartCharIndex, -1)
                {
                    TokenIndent = _indents.Peek() + 1,
                    Column = _tokenStartCharPositionInLine + 1
                };
            EndDqsIfEofOrWsa();
            _currentToken.Type = DQS_ML;
        }

        private void EndDqs()
        {
            _currentToken.StopIndex = this.CharIndex - 1;
            _currentToken.StopLine = Line;
            _currentToken.StopColumn = _tokenStartCharPositionInLine;
            Emit(_currentToken);
            PopMode();PopMode();
        }


        private void StartSqs()
        {
            var token =
                new MalinaToken(new Tuple<ITokenSource, ICharStream>(this, (this as ITokenSource).InputStream),
                    SQS, Channel, _tokenStartCharIndex, -1)
                {
                    TokenIndent = _indents.Peek() + 1,
                    Column = _tokenStartCharPositionInLine + 1,
                    StopIndex = CharIndex - 1
                    
                };
            Emit(token);
        }
       
        private void SqIndentDedent()
        {
            var _currentIndent = InWsaMode ? 0 : _indents.Peek();
            var indent = CalcIndent();

            if (indent <= _currentIndent || _input.La(1) == Eof)
            {
                //SQS is ended by indentation

                //Report Lexer Error - missing closing Double Quote.
                ReportMissingSq();

                //END Multiline SQS
                PopMode();
                PopMode();

                //Emitting NEWLINE
                EmitIndentationToken(NEWLINE, CharIndex - indent - 1, CharIndex - indent - 1);
                //Emitting 1 or more DEDENTS
                while (_indents.Count > 1 && _indents.Peek() > indent)
                {
                    EmitIndentationToken(DEDENT, CharIndex - indent, CharIndex - 1);
                    _indents.Pop();
                }

            }

            //Adding new line
            _interpolationToken = new MalinaToken(
                new Tuple<ITokenSource, ICharStream>(this, (this as ITokenSource).InputStream), SQS,
                Channel,
                _tokenStartCharIndex, -1)
            {
                Text = Environment.NewLine
            };

        }

        private void ReportMissingSq()
        {
            ErrorListenerDispatch.SyntaxError(this, 0, this._tokenStartLine, this._tokenStartCharPositionInLine,
                "Missing closing Single Quote",
                new MalinaException(this, InputStream as ICharStream)
                {
                    Code = MalinaErrorCode.ClosingSqMissing,
                    Start = new DOM.SourceLocation(_tokenStartLine, _tokenStartCharPositionInLine, _tokenStartCharIndex),
                    Stop = new DOM.SourceLocation(_tokenStartLine, _tokenStartCharPositionInLine, _tokenStartCharIndex)
                }
            );
        }

        private void EndSqsIfEofOrWsa()
        {
            if (this._input.La(1) == -1 || InWsaMode)
            {
                //Report Lexer Error - missing closing Single Quote.
                ReportMissingSq();

                PopMode(); PopMode();
            }
        }

        private void EndDqsIfEofOrWsa()
        {
            if (this._input.La(1) == -1 || InWsaMode)
            {
                //Report Lexer Error - missing closing Double Quote.
                ErrorListenerDispatch.SyntaxError(this, 0, this._tokenStartLine, this._tokenStartCharPositionInLine, "Missing closing Double Quote",
                    new MalinaException(this, InputStream as ICharStream)
                    {
                        Code = MalinaErrorCode.ClosingDqMissing,
                        Start = new DOM.SourceLocation(_currentToken.Line, _currentToken.Column, _currentToken.StartIndex),
                        Stop = new DOM.SourceLocation(Line, Column, CharIndex)
                    }
                );

                _currentToken.StopIndex = this.CharIndex;
                _currentToken.StopLine = Line;
                _currentToken.StopColumn = Column;
                Emit(_currentToken);
                PopMode(); PopMode();
                //Emitting NEWLINE
                if(!InWsaMode)
                    EmitIndentationToken(NEWLINE, CharIndex, CharIndex);
            }
            else Skip();
        }

        private void EndOpenValueIfEofOrWsa()
        {
            if(_currentToken == null)
            {
                _currentToken =
                    new MalinaToken(new Tuple<ITokenSource, ICharStream>(this, (this as ITokenSource).InputStream),
                        OPEN_VALUE, Channel, this._tokenStartCharIndex, -1)
                    {
                        Column = _tokenStartCharPositionInLine,
                        TokenIndent = _indents.Peek() + 1
                    };
            }

            _currentToken.StopIndex = this.CharIndex - 1;
            _currentToken.StopLine = Line;
            _currentToken.StopColumn = Column - 1;

            if (this._input.La(1) == -1 || InWsaMode)
            {
                Emit(_currentToken);
                PopMode();
                //Emitting NEWLINE
                if(!InWsaMode)
                    EmitIndentationToken(NEWLINE, CharIndex, CharIndex);
            }
            else Skip();

        }

        private void DqIndentDedent()
        {
            var _currentIndent = InWsaMode ? 0 : _indents.Peek();
            var indent = CalcIndent();

            if (indent <= _currentIndent || _input.La(1) == Eof)
            {
                //DQS is ended by indentation

                //Report Lexer Error - missing closing Double Quote.
                ErrorListenerDispatch.SyntaxError(this, 0, this._tokenStartLine, this._tokenStartCharPositionInLine, "Missing closing Double Quote",
                    new MalinaException(this, InputStream as ICharStream)
                    {
                        Code = MalinaErrorCode.ClosingDqMissing,
                        Start = new DOM.SourceLocation(_currentToken.Line, _currentToken.Column, _currentToken.StartIndex),
                        Stop = new DOM.SourceLocation(_tokenStartLine, _tokenStartCharPositionInLine, _tokenStartCharIndex)
                    }
                    );

                //END Multiline DQS
                _currentToken.StopIndex = this._tokenStartCharIndex;
                _currentToken.StopLine = this._tokenStartLine;
                _currentToken.StopColumn = this._tokenStartCharPositionInLine;
                Emit(_currentToken);
                PopMode(); PopMode();

                //Emitting NEWLINE
                EmitIndentationToken(NEWLINE, CharIndex - indent - 1, CharIndex - indent - 1);
                //Emitting 1 or more DEDENTS
                while (_indents.Count > 1 && _indents.Peek() > indent)
                {
                    EmitIndentationToken(DEDENT, CharIndex - indent, CharIndex - 1);
                    _indents.Pop();
                }

            }
            else //Continue Multine DQS
                Skip();
        }

        private void EmitIdWithColon(int tokenType)
        {
            if(InputStream.La(-1) == ':')
            {
                var token =
                    new MalinaToken(new Tuple<ITokenSource, ICharStream>(this, (this as ITokenSource).InputStream),
                        tokenType, Channel, _tokenStartCharIndex, CharIndex - 2)
                    {
                        Line = _tokenStartLine,
                        StopLine = _tokenStartLine,
                        Column = _tokenStartCharPositionInLine,
                        StopColumn = Column - 2
                    };

                Emit(token);

                token = new MalinaToken(new Tuple<ITokenSource, ICharStream>(this, (this as ITokenSource).InputStream),
                    COLON, Channel, CharIndex - 1, CharIndex - 1)
                {
                    Line = _tokenStartLine,
                    StopLine = _tokenStartLine,
                    Column = Column - 1,
                    StopColumn = Column - 1
                };
                Emit(token);
            }
            else
            {
                var token =
                    new MalinaToken(new Tuple<ITokenSource, ICharStream>(this, (this as ITokenSource).InputStream),
                        tokenType, Channel, _tokenStartCharIndex, CharIndex - 1)
                    {
                        Line = _tokenStartLine,
                        StopLine = _tokenStartLine,
                        Column = _tokenStartCharPositionInLine,
                        StopColumn = Column - 1
                    };
                Emit(token);
            }
        }
    }
}
