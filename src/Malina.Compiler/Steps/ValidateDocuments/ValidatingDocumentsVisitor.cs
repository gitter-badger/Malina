﻿using System;
using System.Collections.Generic;
using System.Linq;
using Malina.Compiler.Generator;
using Malina.DOM;
using Attribute = Malina.DOM.Attribute;

namespace Malina.Compiler.Steps
{
    public class ValidatingDocumentsVisitor: AliasResolvingVisitor
    {
        private DOM.Antlr.Module.TargetFormats _targetFormat = DOM.Antlr.Module.TargetFormats.Xml;

        private bool _blockStart;
        private Stack<JsonGenerator.BlockState> _blockState;
        private Module _currentModule;

        public ValidatingDocumentsVisitor(CompilerContext context):base(context)
        {
        }

        public DOM.Antlr.Module.TargetFormats TargetFormat
        {
            get { return _targetFormat; }
            set { _targetFormat = value; }
        }

        public override void OnModule(Module node)
        {
            if (node.FileName != null && node.FileName.EndsWith(".mlj"))
                _targetFormat = DOM.Antlr.Module.TargetFormats.Json;
            _currentModule = node;

            base.OnModule(node);

        }

        public override void OnDocument(Document node)
        {
            _blockStart = true;
            _blockState = new Stack<JsonGenerator.BlockState>();

            base.OnDocument(node);
        }

        public override void OnElement(Element node)
        {
            CheckBlockIntegrity(node);

            if (HasValue(node)) return;

            _blockStart = true;
            var prevBlockStateCount = _blockState.Count;

            base.OnElement(node);

            _blockStart = false;

            if (_blockState.Count > prevBlockStateCount)
            {
                _blockState.Pop();
            }
        }

        public override void OnAttribute(Attribute node)
        {
            CheckBlockIntegrity(node);
        }

        public override void OnAlias(Alias alias)
        {
            CheckAliasIntegrity(alias);
            base.OnAlias(alias);
        }

        public override void OnArgument(Argument argument)
        {
            CheckArgumentIntegrity(argument);
            base.OnArgument(argument);
        }

        private void CheckArgumentIntegrity(Argument argument)
        {
            if (! (argument.Parent is Alias))
                _context.AddError(CompilerErrorFactory.ArgumentMustBeDefinedInAlias(argument, _currentModule.FileName));
        }

        private void CheckAliasIntegrity(Alias @alias)
        {
            if (alias.Arguments.Count > 0)
            {
                foreach (var entity in alias.Entities)
                {
                    _context.AddError(CompilerErrorFactory.AliasCantHaveDefaultArgument(entity, _currentModule.FileName));
                }
            }
        }

        private void CheckBlockIntegrity(Node node)
        {
            if (!_blockStart)
            {
                var blockState = _blockState.Peek();
                if (blockState == JsonGenerator.BlockState.Array)
                {
                    if (!string.IsNullOrEmpty(node.Name))
                    {
                        ReportErrorForEachNodeInAliasContext(
                            n => CompilerErrorFactory.ArrayItemIsExpected(n, _currentModule.FileName));
                        _context.AddError(CompilerErrorFactory.ArrayItemIsExpected(node, _currentModule.FileName));
                    }
                }
                else
                {
                    if (string.IsNullOrEmpty(node.Name))
                    {
                        ReportErrorForEachNodeInAliasContext(
                            n => CompilerErrorFactory.PropertyIsExpected(n, _currentModule.FileName));
                        _context.AddError(CompilerErrorFactory.PropertyIsExpected(node, _currentModule.FileName));
                    }
                }

                return;
            }

            //This element is the first element of the block. It decides if the block is array or object
            _blockState.Push(string.IsNullOrEmpty(node.Name)
                ? JsonGenerator.BlockState.Array
                : JsonGenerator.BlockState.Object);

            _blockStart = false;
        }

        private void ReportErrorForEachNodeInAliasContext(Func<Node, CompilerError> func)
        {
            foreach (var item in AliasContext)
            {
                if (item != null)
                {
                    _context.AddError(func(item.Alias));
                }
            }
        }

        private static bool HasValue(IValueNode node)
        {
            object value = node.ObjectValue as Parameter;
            if (value != null)
            {
                return true;
            }

            value = node.ObjectValue as Alias;
            if (value != null)
            {
                return true;
            }

            return node.Value != null;
        }
    }
}
