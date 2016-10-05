﻿using System;
using System.Text;

namespace Malina.DOM
{
    [Serializable]
    public class Document : ModuleMember
    {
        // Fields
        private NodeCollection<Entity> _entities;
        private NodeCollection<Namespace> _namespaces;
        public Entity DocumentElement;

        // Methods
        public Document()
        {
        }

        public Document(string name)
        {
            Name = name;
        }

        public Document(string name, string ext, NodeCollection<Namespace> namespaces, Entity documentElement)
        {
            Name = name;
            if (!string.IsNullOrEmpty(ext))
            {
                Name = new StringBuilder().Append(name).Append(".").Append(ext).ToString();
            }
            Namespaces = namespaces;
            DocumentElement = documentElement;
        }

        public override void Accept(IDomVisitor visitor)
        {
            visitor.OnDocument(this);
        }

        public override void AppendChild(Node child)
        {
            child.OwnerModule = OwnerModule;
            if (child is Entity)
            {
                DocumentElement = (Entity)child;
                child.InitializeParent(this);
                Entities.Add((Entity)child);
            }
            else if (child is Namespace)
            {
                Namespaces.Add((Namespace)child);
            }
            else
            {
                base.AppendChild(child);
            }
        }

        public override void Assign(Node node, bool shallow)
        {
            base.Assign(node, shallow);
            Document document = node as Document;
            Namespaces.AssignNodes(document.Namespaces);
            if (!shallow)
            {
                DocumentElement = (Entity)document.DocumentElement.Clone();
            }
        }

        public override Node Clone()
        {
            Document document = new Document();
            document.Assign(this, false);
            return document;
        }

        // Properties
        public NodeCollection<Entity> Entities
        {
            get
            {
                if (_entities == null)
                {
                    _entities = new NodeCollection<Entity>(this);
                }
                return _entities;
            }
            set
            {
                if (value != _entities)
                {
                    if (value != null)
                    {
                        value.InitializeParent(this);
                    }
                    _entities = value;
                }
            }
        }

        public Module Module
        {
            get
            {
                return (Parent as Module);
            }
        }

        public NodeCollection<Namespace> Namespaces
        {
            get
            {
                if (_namespaces == null)
                {
                    _namespaces = new NodeCollection<Namespace>(this);
                }
                return _namespaces;
            }
            set
            {
                if (value != _namespaces)
                {
                    if (value != null)
                    {
                        value.InitializeParent(this);
                    }
                    _namespaces = value;
                }
            }
        }
    }


}