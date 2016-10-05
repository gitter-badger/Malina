﻿using System;
using System.Collections.Generic;

namespace Malina.DOM
{
    [Serializable]
    public class Alias : Element
    {
        // Fields
        private NodeCollection<Argument> _arguments;
        public AliasDefinition ResolvedAliasDefinition;

        // Methods
        public Alias()
        {
        }

        public Alias(string name)
        {
            Name = name;
        }

        public Alias(List<string> ns, string name)
        {
            string str = string.Empty;
            if (ns.Count > 0)
            {
                str = string.Join(".", ns) + ".";
            }
            Name = str + name;
        }

        public override void Accept(IDomVisitor visitor)
        {
            visitor.OnAlias(this);
        }

        public override void AppendChild(Node child)
        {
            child.OwnerModule = OwnerModule;
            if (child is Argument)
            {
                Arguments.Add((Argument)child);
            }
            else
            {
                base.AppendChild(child);
            }
        }

        public override void Assign(Node node, bool shallow)
        {
            base.Assign(node, shallow);
            Alias alias = node as Alias;
            ResolvedAliasDefinition = alias.ResolvedAliasDefinition;
            if (!shallow)
            {
                Arguments.AssignNodes(alias.Arguments);
            }
        }

        public override Node Clone()
        {
            Alias alias = new Alias();
            alias.Assign(this, false);
            return alias;
        }

        // Properties
        public NodeCollection<Argument> Arguments
        {
            get
            {
                if (_arguments == null)
                {
                    _arguments = new NodeCollection<Argument>(this);
                }
                return _arguments??new NodeCollection<Argument>(this);
            }
            set
            {
                if (value != _arguments)
                {
                    if (value != null)
                    {
                        value.InitializeParent(this);
                    }
                    _arguments = value;
                }
            }
        }
    }


}