﻿using Antlr4.Runtime;
using Antlr4.Runtime.Misc;
using System.Text;
using System;
using System.Collections.Generic;

namespace Malina.DOM.Antlr
{
    public class Namespace : DOM.Namespace, IAntlrCharStreamConsumer, IValueNode
    {
        private ICharStream _charStream;
        private Interval _idInterval;
        private Interval _valueInterval = Interval.Invalid;
        private int _valueIndent;

        public ICharStream CharStream
        {
            set
            {
                _charStream = value;
            }
        }

        public Interval IDInterval
        {
            set
            {
                _idInterval = value;
            }
        }

        public Interval ValueInterval
        {
            get
            {
                return _valueInterval;
            }

            set
            {
                _valueInterval = value;
            }
        }

        public override string Name
        {
            get
            {
                if (base.Name != null) return base.Name;
                return _charStream.GetText(new Interval(_idInterval.a + 1, _idInterval.b));
            }

            set
            {
                base.Name = value;
            }
        }

        public override string Value
        {
            get
            {
                if (base.Value != null) return base.Value;
                return Element.GetValueFromValueInterval(_charStream, _valueInterval, _valueIndent, ValueType);
            }
        }

        public override void AppendChild(Node child)
        {
            ObjectValue = child;
        }
        public int ValueIndent
        {
            get
            {
                return _valueIndent;
            }

            set
            {
                _valueIndent = value;
            }
        }

        public List<Object> InterpolationItems => null;
    }
}
