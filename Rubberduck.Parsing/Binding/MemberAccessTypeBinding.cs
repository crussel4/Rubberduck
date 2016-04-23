﻿using Antlr4.Runtime;
using Rubberduck.Parsing.Symbols;

namespace Rubberduck.Parsing.Binding
{
    public sealed class MemberAccessTypeBinding : IExpressionBinding
    {
        private readonly DeclarationFinder _declarationFinder;
        private readonly Declaration _project;
        private readonly Declaration _module;
        private readonly Declaration _parent;
        private readonly VBAExpressionParser.MemberAccessExpressionContext _memberAccessExpression;
        private readonly VBAExpressionParser.MemberAccessExprContext _memberAccessExpr;
        private readonly IExpressionBinding _lExpressionBinding;

        public MemberAccessTypeBinding(
            DeclarationFinder declarationFinder,
            Declaration module,
            Declaration parent,
            VBAExpressionParser.MemberAccessExpressionContext expression,
            IExpressionBinding lExpressionBinding)
        {
            _declarationFinder = declarationFinder;
            _project = module.ParentDeclaration;
            _module = module;
            _parent = parent;
            _memberAccessExpression = expression;
            _lExpressionBinding = lExpressionBinding;
        }

        public MemberAccessTypeBinding(
            DeclarationFinder declarationFinder,
            Declaration module,
            Declaration parent,
            VBAExpressionParser.MemberAccessExprContext expression,
            IExpressionBinding lExpressionBinding)
        {
            _declarationFinder = declarationFinder;
            _project = module.ParentDeclaration;
            _module = module;
            _parent = parent;
            _memberAccessExpr = expression;
            _lExpressionBinding = lExpressionBinding;
        }

        private ParserRuleContext GetExpressionContext()
        {
            if (_memberAccessExpression != null)
            {
                return _memberAccessExpression;
            }
            return _memberAccessExpr;
        }

        private string GetUnrestrictedName()
        {
            if (_memberAccessExpression != null)
            {
                return ExpressionName.GetName(_memberAccessExpression.unrestrictedName());
            }
            return ExpressionName.GetName(_memberAccessExpr.unrestrictedName());
        }

        public IBoundExpression Resolve()
        {
            IBoundExpression boundExpression = null;
            var lExpression = _lExpressionBinding.Resolve();
            string unrestrictedName = GetUnrestrictedName();
            boundExpression = ResolveLExpressionIsProject(lExpression, unrestrictedName);
            if (boundExpression != null)
            {
                return boundExpression;
            }
            boundExpression = ResolveLExpressionIsModule(lExpression, unrestrictedName);
            return boundExpression;
        }

        private IBoundExpression ResolveLExpressionIsProject(IBoundExpression lExpression, string name)
        {
            if (lExpression.Classification != ExpressionClassification.Project)
            {
                return null;
            }
            IBoundExpression boundExpression = null;
            var referencedProject = lExpression.ReferencedDeclaration;
            bool lExpressionIsEnclosingProject = _project.Equals(referencedProject);
            boundExpression = ResolveProject(lExpression, name);
            if (boundExpression != null)
            {
                return boundExpression;
            }
            boundExpression = ResolveProceduralModule(lExpressionIsEnclosingProject, lExpression, name, referencedProject);
            if (boundExpression != null)
            {
                return boundExpression;
            }
            boundExpression = ResolveClassModule(lExpressionIsEnclosingProject, lExpression, name, referencedProject);
            if (boundExpression != null)
            {
                return boundExpression;
            }
            boundExpression = ResolveMemberInReferencedProject(lExpressionIsEnclosingProject, lExpression, name, referencedProject, DeclarationType.UserDefinedType);
            if (boundExpression != null)
            {
                return boundExpression;
            }
            boundExpression = ResolveMemberInReferencedProject(lExpressionIsEnclosingProject, lExpression, name, referencedProject, DeclarationType.Enumeration);
            if (boundExpression != null)
            {
                return boundExpression;
            }
            return boundExpression;
        }

        private IBoundExpression ResolveProject(IBoundExpression lExpression, string name)
        {
            /*
              <l-expression> refers to the enclosing project and <unrestricted-name> is either the name of 
                the enclosing project or a referenced project. In this case, the member access expression is 
                classified as a project and refers to the specified project. 
             */
            if (_project.Project.Name == name)
            {
                return new MemberAccessExpression(_project, ExpressionClassification.Project, GetExpressionContext(), lExpression);
            }
            var referencedProjectRightOfDot = _declarationFinder.FindReferencedProject(_project, name);
            if (referencedProjectRightOfDot != null)
            {
                return new MemberAccessExpression(referencedProjectRightOfDot, ExpressionClassification.Project, GetExpressionContext(), lExpression);
            }
            return null;
        }

        private IBoundExpression ResolveProceduralModule(bool lExpressionIsEnclosingProject, IBoundExpression lExpression, string name, Declaration referencedProject)
        {
            /*
                The project has an accessible procedural module named <unrestricted-name>. In this case, the 
                member access expression is classified as a procedural module and refers to the specified 
                procedural module.  
             */
            if (lExpressionIsEnclosingProject)
            {
                if (_module.DeclarationType == DeclarationType.ProceduralModule && _module.IdentifierName == name)
                {
                    return new MemberAccessExpression(_module, ExpressionClassification.ProceduralModule, GetExpressionContext(), lExpression);
                }
                var proceduralModuleEnclosingProject = _declarationFinder.FindModuleEnclosingProjectWithoutEnclosingModule(_project, _module, name, DeclarationType.ProceduralModule);
                if (proceduralModuleEnclosingProject != null)
                {
                    return new MemberAccessExpression(proceduralModuleEnclosingProject, ExpressionClassification.ProceduralModule, GetExpressionContext(), lExpression);
                }
            }
            else
            {
                var proceduralModuleInReferencedProject = _declarationFinder.FindModuleReferencedProject(_project, _module, referencedProject, name, DeclarationType.ProceduralModule);
                if (proceduralModuleInReferencedProject != null)
                {
                    return new MemberAccessExpression(proceduralModuleInReferencedProject, ExpressionClassification.ProceduralModule, GetExpressionContext(), lExpression);
                }
            }
            return null;
        }

        private IBoundExpression ResolveClassModule(bool lExpressionIsEnclosingProject, IBoundExpression lExpression, string name, Declaration referencedProject)
        {
            /*
                The project has an accessible class module named <unrestricted-name>. In this case, the 
                member access expression is classified as a type and refers to the specified class.  
             */
            if (lExpressionIsEnclosingProject)
            {
                if (_module.DeclarationType == DeclarationType.ClassModule && _module.IdentifierName == name)
                {
                    return new MemberAccessExpression(_module, ExpressionClassification.Type, GetExpressionContext(), lExpression);
                }
                var classModuleEnclosingProject = _declarationFinder.FindModuleEnclosingProjectWithoutEnclosingModule(_project, _module, name, DeclarationType.ClassModule);
                if (classModuleEnclosingProject != null)
                {
                    return new MemberAccessExpression(classModuleEnclosingProject, ExpressionClassification.Type, GetExpressionContext(), lExpression);
                }
            }
            else
            {
                var classModuleInReferencedProject = _declarationFinder.FindModuleReferencedProject(_project, _module, referencedProject, name, DeclarationType.ClassModule);
                if (classModuleInReferencedProject != null)
                {
                    return new MemberAccessExpression(classModuleInReferencedProject, ExpressionClassification.Type, GetExpressionContext(), lExpression);
                }
            }
            return null;
        }

        private IBoundExpression ResolveMemberInReferencedProject(bool lExpressionIsEnclosingProject, IBoundExpression lExpression, string name, Declaration referencedProject, DeclarationType memberType)
        {
            /*
                The project does not have an accessible module named <unrestricted-name> and exactly one of 
                the procedural modules within the project contains a UDT or Enum definition named 
                <unrestricted-name>. In this case, the member access expression is classified as a type and 
                refers to the specified UDT or enum. 
             */
            if (lExpressionIsEnclosingProject)
            {
                var foundType = _declarationFinder.FindMemberEnclosingModule(_project, _module, _parent, name, memberType);
                if (foundType != null)
                {
                    return new MemberAccessExpression(foundType, ExpressionClassification.Type, GetExpressionContext(), lExpression);
                }
                var accessibleType = _declarationFinder.FindMemberEnclosedProjectWithoutEnclosingModule(_project, _module, _parent, name, memberType);
                if (accessibleType != null)
                {
                    return new MemberAccessExpression(accessibleType, ExpressionClassification.Type, GetExpressionContext(), lExpression);
                }
            }
            else
            {
                var referencedProjectType = _declarationFinder.FindMemberReferencedProject(_project, _module, _parent, referencedProject, name, memberType);
                if (referencedProjectType != null)
                {
                    return new MemberAccessExpression(referencedProjectType, ExpressionClassification.Type, GetExpressionContext(), lExpression);
                }
            }
            return null;
        }

        private IBoundExpression ResolveLExpressionIsModule(IBoundExpression lExpression, string name)
        {
            if (lExpression.Classification != ExpressionClassification.ProceduralModule && lExpression.Classification != ExpressionClassification.Type)
            {
                return null;
            }
            IBoundExpression boundExpression = null;
            boundExpression = ResolveMemberInModule(lExpression, name, lExpression.ReferencedDeclaration, DeclarationType.UserDefinedType);
            if (boundExpression != null)
            {
                return boundExpression;
            }
            boundExpression = ResolveMemberInModule(lExpression, name, lExpression.ReferencedDeclaration, DeclarationType.Enumeration);
            if (boundExpression != null)
            {
                return boundExpression;
            }
            return boundExpression;
        }

        private IBoundExpression ResolveMemberInModule(IBoundExpression lExpression, string name, Declaration module, DeclarationType memberType)
        {
            /*
                <l-expression> is classified as a procedural module or a type referencing a class defined in a 
                class module, and one of the following is true:  

                This module has an accessible UDT or Enum definition named <unrestricted-name>. In this 
                case, the member access expression is classified as a type and refers to the specified UDT or 
                Enum type.  
             */
            var enclosingProjectType = _declarationFinder.FindMemberEnclosedProjectInModule(_project, _module, _parent, module, name, memberType);
            if (enclosingProjectType != null)
            {
                return new MemberAccessExpression(enclosingProjectType, ExpressionClassification.Type, GetExpressionContext(), lExpression);
            }
       
            var referencedProjectType = _declarationFinder.FindMemberReferencedProjectInModule(_project, _module, _parent, module, name, memberType);
            if (referencedProjectType != null)
            {
                return new MemberAccessExpression(referencedProjectType, ExpressionClassification.Type, GetExpressionContext(), lExpression);
            }
            return null;
        }
    }
}
