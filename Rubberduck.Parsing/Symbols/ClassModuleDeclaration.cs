﻿using Rubberduck.Parsing.Annotations;
using Rubberduck.Parsing.VBA;
using Rubberduck.VBEditor;
using System.Collections.Generic;
using System.Linq;

namespace Rubberduck.Parsing.Symbols
{
    public sealed class ClassModuleDeclaration : Declaration
    {
        private readonly bool _isExposed;
        private readonly bool _isGlobalClassModule;
        private readonly List<string> _supertypeNames;
        private readonly List<Declaration> _supertypes;
        private readonly List<Declaration> _subtypes;

        public ClassModuleDeclaration(
                  QualifiedMemberName qualifiedName,
                  Declaration projectDeclaration,
                  string name,
                  bool isBuiltIn,
                  IEnumerable<IAnnotation> annotations,
                  Attributes attributes,
                  bool isExposed = false,
                  bool isGlobalClassModule = false)
            : base(
                  qualifiedName,
                  projectDeclaration,
                  projectDeclaration,
                  name,
                  false,
                  false,
                  Accessibility.Public,
                  DeclarationType.ClassModule,
                  null,
                  Selection.Home,
                  isBuiltIn,
                  annotations,
                  attributes)
        {
            _isExposed = isExposed;
            _isGlobalClassModule = isGlobalClassModule;
            _supertypeNames = new List<string>();
            _supertypes = new List<Declaration>();
            _subtypes = new List<Declaration>();
        }

        /// <summary>
        /// Gets an attribute value indicating whether a class is exposed to other projects.
        /// If this value is false, any public types and members cannot be accessed from outside the project they're declared in.
        /// </summary>
        public bool IsExposed
        {
            get
            {
                bool attributeIsExposed = false;
                IEnumerable<string> value;
                if (Attributes.TryGetValue("VB_Exposed", out value))
                {
                    attributeIsExposed = value.Single() == "True";
                }
                return _isExposed || attributeIsExposed;
            }
        }

        public bool IsGlobalClassModule
        {
            get
            {
                bool attributeIsGlobalClassModule = false;
                IEnumerable<string> value;
                if (Attributes.TryGetValue("VB_GlobalNamespace", out value))
                {
                    attributeIsGlobalClassModule = value.Single() == "True";
                }
                return _isGlobalClassModule || attributeIsGlobalClassModule;
            }
        }

        /// <summary>
        /// Gets an attribute value indicating whether a class has a predeclared ID.
        /// Such classes can be treated as "static classes", or as far as resolving is concerned, as standard modules.
        /// </summary>
        public bool HasPredeclaredId
        {
            get
            {
                IEnumerable<string> value;
                if (Attributes.TryGetValue("VB_PredeclaredId", out value))
                {
                    return value.Single() == "True";
                }
                return false;
            }
        }

        public bool HasDefaultInstanceVariable
        {
            get
            {
                return HasPredeclaredId || IsGlobalClassModule;
            }
        }

        public Declaration DefaultMember { get; internal set; }

        public List<string> SupertypeNames
        {
            get
            {
                return _supertypeNames;
            }
        }

        public List<Declaration> Supertypes
        {
            get
            {
                return _supertypes;
            }
        }

        public List<Declaration> Subtypes
        {
            get
            {
                return _subtypes;
            }
        }

        public void AddSupertype(string supertypeName)
        {
            _supertypeNames.Add(supertypeName);
        }

        public void AddSupertype(Declaration supertype)
        {
            _supertypes.Add(supertype);
        }

        public void AddSubtype(Declaration subtype)
        {
            _subtypes.Add(subtype);
        }
    }
}